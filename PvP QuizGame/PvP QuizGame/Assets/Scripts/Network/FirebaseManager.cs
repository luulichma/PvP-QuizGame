using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.RemoteConfig;

/// <summary>
/// Quản lý toàn bộ kết nối Firebase: Auth, Matchmaking, Room, Cloud Save.
///
/// SCHEMA:
///   /users/{uid}                : displayName, level, currentExp, money, lastSeen
///   /matchmakingQueue/{uid}     : { name, joinedAt }
///   /rooms/{roomId}             : createdAt, seed, state, players, currentQ, answers, scores, winner
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static FirebaseManager Instance { get; private set; }

    // ==================== SDK REFERENCES ====================
    private FirebaseApp _app;
    private FirebaseAuth _auth;
    private FirebaseDatabase _database;
    private DatabaseReference _root;

    // ==================== EVENTS ====================
    public static event Action OnFirebaseReady;
    public static event Action<string> OnAuthError;
    public static event Action OnAuthSuccess;
    public static event Action OnMatchFound;       // Khi đã ghép cặp xong, có roomId
    public static event Action<string> OnMatchmakingError;
    public static event Action OnOpponentDisconnected;

    // ==================== STATE ====================
    public bool IsConnected { get; private set; } = false;
    public bool IsAuthenticated => _auth?.CurrentUser != null;
    public string LocalUserId => _auth?.CurrentUser?.UserId;
    public string LocalDisplayName { get; private set; } = "Player";
    public string CurrentRoomId { get; private set; }
    public string OpponentId { get; private set; }
    public string OpponentName { get; private set; }
    public int OpponentAvatarIndex { get; private set; } = 0;
    public bool IsHost { get; private set; }       // Host = uid nhỏ hơn (so sánh string)

    [Header("Debug Settings")]
    [Tooltip("Chế độ chơi hiện tại (Online/Offline). Biến này sẽ được tự động thay đổi bởi các nút bấm trên UI.")]
    public bool isOfflineMode = false;

    [Header("Remote Config Values")]
    public float QuestionDuration { get; private set; } = 15f;

    // ==================== INTERNAL HANDLERS (để unsubscribe đúng) ====================
    private EventHandler<ValueChangedEventArgs> _matchmakingHandler;
    private DatabaseReference _matchmakingRef;

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Luôn khởi tạo Firebase để sẵn sàng cho chế độ Online bất cứ lúc nào
        InitializeFirebase();
    }

    public void InitializeFirebase()
    {
        Debug.Log("[FirebaseManager] Đang kiểm tra phụ thuộc Firebase...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                _app = FirebaseApp.DefaultInstance;
                _auth = FirebaseAuth.GetAuth(_app);
                _database = FirebaseDatabase.GetInstance(_app);
                _root = _database.RootReference;

                IsConnected = true;
                Debug.Log("[FirebaseManager] Firebase đã sẵn sàng!");
                
                // Khởi tạo Remote Config sau khi Firebase sẵn sàng
                InitializeRemoteConfig();

                OnFirebaseReady?.Invoke();
            }
            else
            {
                string error = $"Không thể cài đặt phụ thuộc Firebase: {dependencyStatus}";
                Debug.LogError($"[FirebaseManager] {error}");
                OnAuthError?.Invoke(error);
            }
        });
    }

    private void InitializeRemoteConfig()
    {
        // Set giá trị mặc định
        Dictionary<string, object> defaults = new Dictionary<string, object>();
        defaults.Add("question_duration", 15.0);
        
        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults).ContinueWithOnMainThread(task => {
            FetchRemoteConfig();
        });
    }

    public void FetchRemoteConfig()
    {
        Debug.Log("[FirebaseManager] Đang lấy dữ liệu Remote Config...");
        
        // Fetch dữ liệu mới (cache trong 1 giờ)
        TimeSpan cacheExpiration = TimeSpan.FromHours(1);
        
        FirebaseRemoteConfig.DefaultInstance.FetchAsync(cacheExpiration).ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                Debug.Log("[FirebaseManager] Remote Config Fetch thành công!");
                FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(activateTask => {
                    // Cập nhật giá trị
                    QuestionDuration = (float)FirebaseRemoteConfig.DefaultInstance.GetValue("question_duration").DoubleValue;
                    Debug.Log($"[FirebaseManager] QuestionDuration từ Remote Config: {QuestionDuration}s");
                });
            }
            else
            {
                Debug.LogWarning("[FirebaseManager] Remote Config Fetch thất bại, dùng giá trị mặc định.");
            }
        });
    }

    // ==================== AUTHENTICATION ====================
    /// <summary>
    /// Đăng nhập ẩn danh + load profile từ cloud (hoặc tạo mới).
    /// </summary>
    public async Task<bool> SignInAnonymousAndLoadProfile(string desiredDisplayName = null)
    {
        if (!IsConnected || _auth == null)
        {
            OnAuthError?.Invoke("Firebase chưa sẵn sàng.");
            return false;
        }

        try
        {
            var authResult = await _auth.SignInAnonymouslyAsync();
            return await HandleAuthResult(authResult.User, desiredDisplayName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] SignIn Failed: {ex.Message}");
            OnAuthError?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Đồng bộ lại profile từ Cloud nếu đã authenticated.
    /// </summary>
    public async Task<bool> SyncProfile()
    {
        if (!IsConnected || _auth == null || _auth.CurrentUser == null) return false;
        return await HandleAuthResult(_auth.CurrentUser);
    }

    /// <summary>
    /// Đăng ký tài khoản mới bằng Email/Password.
    /// </summary>
    public async Task<bool> SignUpWithEmail(string email, string password, string displayName)
    {
        if (!IsConnected || _auth == null) { OnAuthError?.Invoke("Firebase chưa sẵn sàng."); return false; }

        try
        {
            var authResult = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
            return await HandleAuthResult(authResult.User, displayName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] SignUp Failed: {ex.Message}");
            OnAuthError?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Đăng nhập tài khoản bằng Email/Password.
    /// </summary>
    public async Task<bool> SignInWithEmail(string email, string password)
    {
        if (!IsConnected || _auth == null) { OnAuthError?.Invoke("Firebase chưa sẵn sàng."); return false; }

        try
        {
            var authResult = await _auth.SignInWithEmailAndPasswordAsync(email, password);
            return await HandleAuthResult(authResult.User);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] SignIn Failed: {ex.Message}");
            OnAuthError?.Invoke(ex.Message);
            return false;
        }
    }

    private async Task<bool> HandleAuthResult(FirebaseUser user, string desiredDisplayName = null)
    {
        string uid = user.UserId;
        Debug.Log($"[FirebaseManager] Auth OK. UID = {uid}");

        // Load profile từ cloud
        var snapshot = await _root.Child("users").Child(uid).GetValueAsync();
        if (snapshot.Exists)
        {
            if (snapshot.Child("displayName").Value != null)
                LocalDisplayName = snapshot.Child("displayName").Value.ToString();

            // Sync về PlayerData local
            var pd = PlayerDataManager.Instance?.Data;
            if (pd != null)
            {
                pd.playerName = LocalDisplayName;
                if (snapshot.Child("level").Value != null) pd.level = int.Parse(snapshot.Child("level").Value.ToString());
                if (snapshot.Child("currentExp").Value != null) pd.currentExp = int.Parse(snapshot.Child("currentExp").Value.ToString());
                if (snapshot.Child("money").Value != null) pd.money = int.Parse(snapshot.Child("money").Value.ToString());
                if (snapshot.Child("avatarIndex").Value != null) pd.avatarIndex = int.Parse(snapshot.Child("avatarIndex").Value.ToString());
                PlayerDataManager.Instance.SaveData();
            }

            Debug.Log($"[FirebaseManager] Loaded cloud profile: {LocalDisplayName}");
        }
        else
        {
            // Profile mới
            LocalDisplayName = !string.IsNullOrEmpty(desiredDisplayName)
                ? desiredDisplayName
                : (PlayerDataManager.Instance?.Data?.playerName ?? "Player");
            await SaveProfileToCloud();
            Debug.Log($"[FirebaseManager] Created new cloud profile: {LocalDisplayName}");
        }

        // Cập nhật lastSeen
        await _root.Child("users").Child(uid).Child("lastSeen").SetValueAsync(ServerValue.Timestamp);

        OnAuthSuccess?.Invoke();
        return true;
    }

    /// <summary>
    /// Đăng xuất khỏi Firebase và reset trạng thái local.
    /// </summary>
    public void SignOut()
    {
        if (_auth != null)
        {
            _auth.SignOut();
            Debug.Log("[FirebaseManager] Đã đăng xuất khỏi Firebase.");
        }
        
        IsConnected = false; // Force re-init if needed
        LocalDisplayName = "Player";
        CurrentRoomId = null;
        OpponentId = null;
        OpponentName = null;
    }

    /// <summary>
    /// Đẩy PlayerData hiện tại lên cloud.
    /// </summary>
    public async Task SaveProfileToCloud()
    {
        if (!IsConnected || !IsAuthenticated) return;
        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null) return;

        var data = new Dictionary<string, object> {
            { "displayName", LocalDisplayName },
            { "level",       pd.level },
            { "currentExp",  pd.currentExp },
            { "money",       pd.money },
            { "avatarIndex", pd.avatarIndex },
            { "lastSeen",    ServerValue.Timestamp }
        };

        await _root.Child("users").Child(LocalUserId).UpdateChildrenAsync(data);
        Debug.Log($"[FirebaseManager] Đã sync profile lên cloud.");
    }

    public void UpdateDisplayName(string newName)
    {
        LocalDisplayName = newName;
        if (PlayerDataManager.Instance?.Data != null)
        {
            PlayerDataManager.Instance.Data.playerName = newName;
            PlayerDataManager.Instance.SaveData();
        }
        if (IsConnected && IsAuthenticated)
            _root.Child("users").Child(LocalUserId).Child("displayName").SetValueAsync(newName);
    }

    // ==================== TIER & MATCH CONFIG ====================
    public int GetPlayerTier(int level)
    {
        if (level <= 10) return 1;
        if (level <= 30) return 2;
        return 3;
    }

    public int GetQuestionCountForTier(int tier)
    {
        return tier switch {
            1 => 10,
            2 => 20,
            3 => 30,
            _ => 10
        };
    }

    private string GetCurrentQueuePath()
    {
        int myLevel = PlayerDataManager.Instance?.Data?.level ?? 1;
        int myTier = GetPlayerTier(myLevel);
        return $"matchmakingQueue/tier_{myTier}";
    }

    // ==================== MATCHMAKING ====================
    // Public API: StartMatchmaking() / CancelMatchmaking() — xem phía dưới.
    // Internal flow: RetryFindOpponent() → CreateRoomWithFoundOpponent() / SetupWaitingForOpponent()

    private void SetupWaitingForOpponent()
    {
        Debug.Log("[FirebaseManager] Đang đợi đối thủ...");

        string queuePath = GetCurrentQueuePath();

        // OnDisconnect: nếu rớt mạng, tự xoá khỏi queue
        _matchmakingRef = _root.Child(queuePath).Child(LocalUserId);
        _matchmakingRef.OnDisconnect().RemoveValue();

        // Listen `users/{myUid}/currentRoom` — người ghép xong sẽ ghi vào đây
        _matchmakingHandler = (sender, args) => {
            if (args.DatabaseError != null) return;
            if (args.Snapshot.Exists && args.Snapshot.Value != null)
            {
                string roomId = args.Snapshot.Value.ToString();
                Debug.Log($"[FirebaseManager] Được ghép vào room: {roomId}");

                // Cleanup listener
                _root.Child("users").Child(LocalUserId).Child("currentRoom").ValueChanged -= _matchmakingHandler;
                _matchmakingHandler = null;

                // Xoá ghi chú currentRoom (đã đọc)
                _root.Child("users").Child(LocalUserId).Child("currentRoom").RemoveValueAsync();

                JoinExistingRoom(roomId);
            }
        };
        _root.Child("users").Child(LocalUserId).Child("currentRoom").ValueChanged += _matchmakingHandler;
    }

    /// <summary>
    /// Khi mình là người đến sau và đã xoá đối thủ khỏi queue → tạo room mới với cả 2.
    /// Vấn đề: ta cần biết uid đối thủ. Ta lấy lại từ snapshot trước hoặc dùng cách khác:
    /// → Đơn giản hoá: lúc transaction, ghi tạm ra `_pendingOpponentUid`.
    /// </summary>
    private string _pendingOpponentUid;
    private string _pendingOpponentName;
    private int _pendingOpponentAvatar;

    private void CreateRoomWithFoundOpponent()
    {
        // Vì transaction đã xoá đối thủ khỏi queue mà không lưu lại uid,
        // ta dùng cách thay thế: thay vì chạy transaction trên cả queue, ta:
        //  1. Đọc queue 1 lần
        //  2. Nếu có người → chạy transaction CHỈ XOÁ person đó (atomic check-and-remove)
        //  3. Nếu thành công → biết uid, tạo room
        // Hàm này chỉ gọi khi đã có _pendingOpponentUid (set trong RetryFindOpponent).

        if (string.IsNullOrEmpty(_pendingOpponentUid))
        {
            // Không có info → retry
            RetryFindOpponent();
            return;
        }

        string roomId = "room_" + Guid.NewGuid().ToString("N").Substring(0, 6);
        int seed = (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF);

        int myLevel = PlayerDataManager.Instance?.Data?.level ?? 1;
        int myTier = GetPlayerTier(myLevel);
        int questionCount = GetQuestionCountForTier(myTier);

        var roomData = new Dictionary<string, object> {
            { "createdAt", ServerValue.Timestamp },
            { "seed", seed },
            { "state", "waiting" },
            { "currentQ", 0 },
            { "questionCount", questionCount },
            { "players", new Dictionary<string, object> {
                { LocalUserId,         new Dictionary<string, object>{{"name", LocalDisplayName}, {"avatar", PlayerDataManager.Instance?.Data?.avatarIndex ?? 0}, {"online", true}} },
                { _pendingOpponentUid, new Dictionary<string, object>{{"name", _pendingOpponentName}, {"avatar", _pendingOpponentAvatar}, {"online", true}} }
            }}
        };

        _root.Child("rooms").Child(roomId).SetValueAsync(roomData).ContinueWithOnMainThread(t => {
            if (t.IsFaulted)
            {
                OnMatchmakingError?.Invoke("Không tạo được room.");
                return;
            }

            // Báo cho đối thủ biết roomId qua users/{oppUid}/currentRoom
            _root.Child("users").Child(_pendingOpponentUid).Child("currentRoom").SetValueAsync(roomId);

            // Mình tự join room này
            CurrentRoomId = roomId;
            OpponentId = _pendingOpponentUid;
            OpponentName = _pendingOpponentName;
            IsHost = string.Compare(LocalUserId, OpponentId, StringComparison.Ordinal) < 0;

            _pendingOpponentUid = null;
            _pendingOpponentName = null;

            SetupRoomPresence();
            Debug.Log($"[FirebaseManager] Tạo room {roomId} (Host={IsHost}). Vào trận.");
            OnMatchFound?.Invoke();
        });
    }

    /// <summary>
    /// Chạy transaction "atomic": tìm 1 đối thủ + xoá khỏi queue + ghi nhớ uid.
    /// Đây là cách thực hiện đúng đắn cho FindMatch (override StartFindMatchTransaction).
    /// </summary>
    private void RetryFindOpponent()
    {
        string queuePath = GetCurrentQueuePath();
        var queueRef = _root.Child(queuePath);
        queueRef.GetValueAsync().ContinueWithOnMainThread(t => {
            if (t.IsFaulted) { OnMatchmakingError?.Invoke("Lỗi đọc queue."); return; }

            var snap = t.Result;
            string foundUid = null;
            string foundName = "Opponent";

            foreach (var child in snap.Children)
            {
                if (child.Key != LocalUserId)
                {
                    foundUid = child.Key;
                    if (child.Child("name").Value != null) foundName = child.Child("name").Value.ToString();
                    if (child.Child("avatar").Value != null) int.TryParse(child.Child("avatar").Value.ToString(), out _pendingOpponentAvatar);
                    else _pendingOpponentAvatar = 0;
                    break;
                }
            }

            if (foundUid == null)
            {
                // Không có ai → ghi mình vào queue và đợi
                var myEntry = new Dictionary<string, object> {
                    { "name", LocalDisplayName },
                    { "avatar", PlayerDataManager.Instance?.Data?.avatarIndex ?? 0 },
                    { "joinedAt", ServerValue.Timestamp }
                };
                _root.Child(queuePath).Child(LocalUserId).SetValueAsync(myEntry).ContinueWithOnMainThread(_ => {
                    SetupWaitingForOpponent();
                });
            }
            else
            {
                // Có người → atomic remove (transaction trên node của họ)
                _pendingOpponentUid = foundUid;
                _pendingOpponentName = foundName;

                // FIX: RunTransaction() trong Firebase Unity SDK trả về `Task` non-generic
                // (không có .Result). Dùng local flag để track xem có aborted hay không.
                bool didAbort = false;

                _root.Child(queuePath).Child(foundUid).RunTransaction(md => {
                    if (md.Value == null)
                    {
                        // Đã có ai khác xoá rồi → mình thua cuộc đua, retry
                        didAbort = true;
                        return TransactionResult.Abort();
                    }
                    didAbort = false;
                    md.Value = null;
                    return TransactionResult.Success(md);
                }).ContinueWithOnMainThread(rt => {
                    if (rt.IsFaulted || rt.IsCanceled || didAbort)
                    {
                        // Cuộc đua thua — retry
                        _pendingOpponentUid = null;
                        _pendingOpponentName = null;
                        RetryFindOpponent();
                    }
                    else
                    {
                        CreateRoomWithFoundOpponent();
                    }
                });
            }
        });
    }

    /// <summary>
    /// Public entry — thay thế FindMatch cũ. UI gọi hàm này khi user bấm "Tìm trận".
    /// </summary>
    public void StartMatchmaking()
    {
        if (!IsConnected || !IsAuthenticated)
        {
            OnMatchmakingError?.Invoke("Chưa đăng nhập Firebase.");
            return;
        }
        RetryFindOpponent();
    }

    /// <summary>
    /// User bấm Cancel — xoá khỏi queue.
    /// </summary>
    public void CancelMatchmaking()
    {
        if (!IsConnected || !IsAuthenticated) return;

        if (_matchmakingHandler != null)
        {
            _root.Child("users").Child(LocalUserId).Child("currentRoom").ValueChanged -= _matchmakingHandler;
            _matchmakingHandler = null;
        }
        
        string queuePath = GetCurrentQueuePath();
        _root.Child(queuePath).Child(LocalUserId).RemoveValueAsync();
        Debug.Log("[FirebaseManager] Đã huỷ matchmaking.");
    }

    private void JoinExistingRoom(string roomId)
    {
        _root.Child("rooms").Child(roomId).GetValueAsync().ContinueWithOnMainThread(t => {
            if (t.IsFaulted) { OnMatchmakingError?.Invoke("Không đọc được room."); return; }
            var snap = t.Result;

            CurrentRoomId = roomId;

            // Tìm uid đối thủ
            foreach (var child in snap.Child("players").Children)
            {
                if (child.Key != LocalUserId)
                {
                    OpponentId = child.Key;
                    var nameNode = child.Child("name");
                    if (nameNode.Value != null) OpponentName = nameNode.Value.ToString();
                    
                    var avatarNode = child.Child("avatar");
                    if (avatarNode.Value != null) OpponentAvatarIndex = int.Parse(avatarNode.Value.ToString());
                    else OpponentAvatarIndex = 0;
                    
                    break;
                }
            }

            IsHost = string.Compare(LocalUserId, OpponentId, StringComparison.Ordinal) < 0;
            SetupRoomPresence();
            Debug.Log($"[FirebaseManager] Đã join room {roomId} (Host={IsHost}, Opp={OpponentName}).");
            OnMatchFound?.Invoke();
        });
    }

    private void SetupRoomPresence()
    {
        if (string.IsNullOrEmpty(CurrentRoomId) || string.IsNullOrEmpty(LocalUserId)) return;

        // Đặt mình online + onDisconnect set offline
        var presenceRef = _root.Child("rooms").Child(CurrentRoomId).Child("players").Child(LocalUserId).Child("online");
        presenceRef.SetValueAsync(true);
        presenceRef.OnDisconnect().SetValue(false);

        // Listen trạng thái online của đối thủ
        var oppPresence = _root.Child("rooms").Child(CurrentRoomId).Child("players").Child(OpponentId).Child("online");
        oppPresence.ValueChanged += OnOpponentPresenceChanged;
    }

    private void OnOpponentPresenceChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;
        if (args.Snapshot.Value == null) return;
        if (args.Snapshot.Value.ToString().ToLower() == "false")
        {
            Debug.LogWarning("[FirebaseManager] Đối thủ ngắt kết nối!");
            OnOpponentDisconnected?.Invoke();
        }
    }

    // ==================== ROOM API (cho FirebaseMatchProvider gọi) ====================
    public DatabaseReference GetRoomRef() =>
        string.IsNullOrEmpty(CurrentRoomId) ? null : _root.Child("rooms").Child(CurrentRoomId);

    public DatabaseReference GetRootRef() => _root;

    public async Task<int> ReadSeedFromRoom()
    {
        if (string.IsNullOrEmpty(CurrentRoomId)) return -1;
        var snap = await _root.Child("rooms").Child(CurrentRoomId).Child("seed").GetValueAsync();
        if (snap.Exists && int.TryParse(snap.Value.ToString(), out int seed)) return seed;
        return -1;
    }

    public async Task<int> ReadQuestionCountFromRoom()
    {
        if (string.IsNullOrEmpty(CurrentRoomId)) return 10;
        var snap = await _root.Child("rooms").Child(CurrentRoomId).Child("questionCount").GetValueAsync();
        if (snap.Exists && int.TryParse(snap.Value.ToString(), out int count)) return count;
        return 10;
    }

    public void UpdateMyScore(int score)
    {
        if (!IsConnected || !IsAuthenticated || string.IsNullOrEmpty(CurrentRoomId)) return;
        _root.Child("rooms").Child(CurrentRoomId).Child("scores").Child(LocalUserId).SetValueAsync(score);
    }

    public void SubmitMyAnswer(int answerIndex)
    {
        if (!IsConnected || !IsAuthenticated || string.IsNullOrEmpty(CurrentRoomId)) return;
        _root.Child("rooms").Child(CurrentRoomId).Child("answers").Child(LocalUserId).SetValueAsync(answerIndex);
    }

    /// <summary>
    /// Host gọi sau mỗi câu để clear answers và tăng currentQ.
    /// </summary>
    public void HostAdvanceQuestion(int newQuestionIndex)
    {
        if (!IsHost || string.IsNullOrEmpty(CurrentRoomId)) return;
        var roomRef = _root.Child("rooms").Child(CurrentRoomId);
        roomRef.Child("answers").RemoveValueAsync();
        roomRef.Child("currentQ").SetValueAsync(newQuestionIndex);
    }

    /// <summary>
    /// Host gọi khi trận kết thúc.
    /// </summary>
    public async Task HostEndMatch(string winnerUidOrDraw)
    {
        if (!IsHost || string.IsNullOrEmpty(CurrentRoomId)) return;
        var roomRef = _root.Child("rooms").Child(CurrentRoomId);
        await roomRef.Child("state").SetValueAsync("ended");
        await roomRef.Child("winner").SetValueAsync(winnerUidOrDraw);
    }

    /// <summary>
    /// Cleanup khi rời room (về Home).
    /// </summary>
    public void LeaveRoom()
    {
        if (!IsConnected || string.IsNullOrEmpty(CurrentRoomId)) return;

        // Bỏ presence listener
        if (!string.IsNullOrEmpty(OpponentId))
        {
            _root.Child("rooms").Child(CurrentRoomId).Child("players").Child(OpponentId)
                 .Child("online").ValueChanged -= OnOpponentPresenceChanged;
        }

        // Đặt mình offline
        _root.Child("rooms").Child(CurrentRoomId).Child("players").Child(LocalUserId)
             .Child("online").SetValueAsync(false);

        // Host xoá toàn bộ room sau 5s (cho cả 2 client kịp đọc trạng thái ended)
        if (IsHost)
        {
            var roomToDelete = CurrentRoomId;
            Invoke(nameof(DeleteRoomDelayed), 5f);
            _roomToDelete = roomToDelete;
        }

        CurrentRoomId = null;
        OpponentId = null;
        OpponentName = null;
        IsHost = false;
    }

    private string _roomToDelete;
    private void DeleteRoomDelayed()
    {
        if (!string.IsNullOrEmpty(_roomToDelete) && _root != null)
        {
            _root.Child("rooms").Child(_roomToDelete).RemoveValueAsync();
            _roomToDelete = null;
        }
    }
}
