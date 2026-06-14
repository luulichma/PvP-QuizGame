using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;

/// <summary>
/// [REFACTOR-P2] FirebaseManager — phần MATCHMAKING.
/// Public API: StartMatchmaking() / CancelMatchmaking().
/// Internal flow: AddSelfToQueueAndSearch() → SearchAndMatch() → TryClaimOpponent()
///                → CreateRoomWithFoundOpponent() hoặc SetupWaitingForOpponent().
/// Giữ nguyên FIX-CANCEL (race condition) + UX-06 (timeout 45s).
/// </summary>
public partial class FirebaseManager
{
    // ==================== MATCHMAKING STATE ====================
    private EventHandler<ValueChangedEventArgs> _matchmakingHandler;
    private DatabaseReference _matchmakingRef;
    // FIX-CANCEL: Flag tránh race condition khi OnMatchFound fire sau khi đã cancel
    private bool _isMatchmakingCancelled = false;

    private string _pendingOpponentUid;
    private string _pendingOpponentName;
    private int _pendingOpponentAvatar;

    // UX-06: Matchmaking timeout
    private Coroutine _matchmakingTimeoutCoroutine;
    private const float MATCHMAKING_TIMEOUT = 45f; // 45 giây timeout

    private string GetCurrentQueuePath()
    {
        // [PHASE-2] Tier dựa trên Rank Points (không phải Level)
        int myRP = PlayerDataManager.Instance?.Data?.rankPoints ?? 0;
        int myTier = GetPlayerTier(myRP);
        return $"matchmakingQueue/tier_{myTier}";
    }

    /// <summary>Public entry — UI gọi hàm này khi user bấm "Tìm trận".</summary>
    public void StartMatchmaking()
    {
        if (!IsConnected || !IsAuthenticated)
        {
            OnMatchmakingError?.Invoke("Chưa đăng nhập Firebase.");
            return;
        }
        // FIX-CANCEL: Reset flag khi bắt đầu tìm trận mới
        _isMatchmakingCancelled = false;
        // UX-06: Bắt đầu timeout
        if (_matchmakingTimeoutCoroutine != null) StopCoroutine(_matchmakingTimeoutCoroutine);
        _matchmakingTimeoutCoroutine = StartCoroutine(MatchmakingTimeoutRoutine());
        AddSelfToQueueAndSearch();
    }

    /// <summary>User bấm Cancel — xoá khỏi queue.</summary>
    public void CancelMatchmaking()
    {
        if (!IsConnected || !IsAuthenticated) return;

        // FIX-CANCEL: Đánh dấu đã cancel để block OnMatchFound nếu fire muộn
        _isMatchmakingCancelled = true;

        // UX-06: Hủy timeout
        if (_matchmakingTimeoutCoroutine != null)
        {
            StopCoroutine(_matchmakingTimeoutCoroutine);
            _matchmakingTimeoutCoroutine = null;
        }

        if (_matchmakingHandler != null)
        {
            _root.Child("users").Child(LocalUserId).Child("currentRoom").ValueChanged -= _matchmakingHandler;
            _matchmakingHandler = null;
        }

        // FIX-CANCEL: Xoá pending opponent để tránh CreateRoom sau khi cancel
        _pendingOpponentUid = null;
        _pendingOpponentName = null;

        string queuePath = GetCurrentQueuePath();
        _root.Child(queuePath).Child(LocalUserId).RemoveValueAsync();
        Debug.Log("[FirebaseManager] Đã huỷ matchmaking.");
    }

    // UX-06: Timeout coroutine
    private IEnumerator MatchmakingTimeoutRoutine()
    {
        yield return new WaitForSeconds(MATCHMAKING_TIMEOUT);
        Debug.LogWarning("[FirebaseManager] Hết thời gian tìm trận.");
        CancelMatchmaking();
        OnMatchmakingTimeout?.Invoke();
    }

    /// <summary>Thêm mình vào queue trước rồi mới tìm trận.</summary>
    private void AddSelfToQueueAndSearch()
    {
        string queuePath = GetCurrentQueuePath();
        Debug.Log($"[FirebaseManager] Đang thêm mình vào queue: {queuePath}");

        var myEntry = new Dictionary<string, object> {
            { "name", LocalDisplayName },
            { "avatar", PlayerDataManager.Instance?.Data?.avatarIndex ?? 0 },
            { "joinedAt", ServerValue.Timestamp }
        };

        _root.Child(queuePath).Child(LocalUserId).SetValueAsync(myEntry).ContinueWithOnMainThread(t => {
            if (t.IsFaulted)
            {
                OnMatchmakingError?.Invoke("Không thể vào hàng chờ.");
                return;
            }

            // OnDisconnect: nếu rớt mạng, tự xoá khỏi queue
            _matchmakingRef = _root.Child(queuePath).Child(LocalUserId);
            _matchmakingRef.OnDisconnect().RemoveValue();

            // Sau khi vào queue thành công, bắt đầu tìm kiếm
            SearchAndMatch();
        });
    }

    /// <summary>Đọc hàng chờ và tìm đối thủ. Áp dụng quy tắc người vào trước là chủ.</summary>
    private void SearchAndMatch()
    {
        string queuePath = GetCurrentQueuePath();
        var queueRef = _root.Child(queuePath).OrderByChild("joinedAt");

        queueRef.GetValueAsync().ContinueWithOnMainThread(t => {
            if (t.IsFaulted)
            {
                OnMatchmakingError?.Invoke("Lỗi đọc hàng chờ.");
                return;
            }

            var snap = t.Result;
            var enumerator = snap.Children.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                // Queue rỗng (vô lý vì mình vừa add xong, nhưng có thể do trễ)
                Debug.LogWarning("[FirebaseManager] Queue rỗng sau khi add!");
                SetupWaitingForOpponent();
                return;
            }

            var firstChild = enumerator.Current;

            if (firstChild.Key == LocalUserId)
            {
                // Mình là người cũ nhất! Đứng yên và chờ.
                Debug.Log("[FirebaseManager] Tôi là người cũ nhất. Chờ đối thủ...");
                SetupWaitingForOpponent();
            }
            else
            {
                // Có người cũ hơn. Thử claim họ.
                string foundUid = firstChild.Key;
                string foundName = firstChild.Child("name").Value?.ToString() ?? "Opponent";
                int avatar = 0;
                if (firstChild.Child("avatar").Value != null)
                    int.TryParse(firstChild.Child("avatar").Value.ToString(), out avatar);

                TryClaimOpponent(foundUid, foundName, avatar);
            }
        });
    }

    /// <summary>Thử chiếm opponent bằng transaction.</summary>
    private void TryClaimOpponent(string oppUid, string oppName, int oppAvatar)
    {
        string queuePath = GetCurrentQueuePath();
        _pendingOpponentUid = oppUid;
        _pendingOpponentName = oppName;
        _pendingOpponentAvatar = oppAvatar;

        bool didAbort = false;

        _root.Child(queuePath).Child(oppUid).RunTransaction(md => {
            if (md.Value == null)
            {
                didAbort = true;
                return TransactionResult.Abort(); // Ai đó đã nhận rồi
            }
            didAbort = false;
            md.Value = null;
            return TransactionResult.Success(md);
        }).ContinueWithOnMainThread(rt => {
            if (rt.IsFaulted || rt.IsCanceled || didAbort)
            {
                _pendingOpponentUid = null;
                _pendingOpponentName = null;
                SearchAndMatch(); // Thử lại
            }
            else
            {
                // Thành công! Xoá MÌNH khỏi queue và tạo phòng
                _root.Child(queuePath).Child(LocalUserId).RemoveValueAsync();
                CreateRoomWithFoundOpponent();
            }
        });
    }

    /// <summary>
    /// Khi mình là người đến sau và đã xoá đối thủ khỏi queue → tạo room mới với cả 2.
    /// </summary>
    private void CreateRoomWithFoundOpponent()
    {
        if (string.IsNullOrEmpty(_pendingOpponentUid))
        {
            // Không có info → retry
            SearchAndMatch();
            return;
        }

        string roomId = "room_" + Guid.NewGuid().ToString("N").Substring(0, 6);
        int seed = (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF);

        // [PHASE-2] Tier theo Rank Points
        int myRP = PlayerDataManager.Instance?.Data?.rankPoints ?? 0;
        int myTier = GetPlayerTier(myRP);
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

            // FIX-CANCEL: Nếu đã cancel thì bỏ qua, xóa room vừa tạo luôn
            if (_isMatchmakingCancelled)
            {
                Debug.Log("[FirebaseManager] Matchmaking đã bị cancel sau khi tạo room — xóa room và bỏ qua.");
                _root.Child("rooms").Child(roomId).RemoveValueAsync();
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
            // UX-06: Hủy timeout khi match thành công
            if (_matchmakingTimeoutCoroutine != null)
            {
                StopCoroutine(_matchmakingTimeoutCoroutine);
                _matchmakingTimeoutCoroutine = null;
            }
            OnMatchFound?.Invoke();
        });
    }

    /// <summary>Mình là người cũ nhất trong queue — listen users/{myUid}/currentRoom chờ được ghép.</summary>
    private void SetupWaitingForOpponent()
    {
        Debug.Log("[FirebaseManager] Đang đợi đối thủ...");

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
}
