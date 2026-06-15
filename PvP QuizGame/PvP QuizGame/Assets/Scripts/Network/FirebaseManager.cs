using UnityEngine;
using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.RemoteConfig;

/// <summary>
/// [REFACTOR-P2] Façade quản lý kết nối Firebase — ĐÃ PHÂN RÃ thành partial class:
/// - FirebaseManager.cs              (file này): singleton, SDK refs, events, state, Init + RemoteConfig
/// - Firebase/FirebaseManager.Auth.cs        : Auth + Profile sync + Tier config
/// - Firebase/FirebaseManager.Matchmaking.cs : queue / search / claim / timeout / cancel
/// - Firebase/FirebaseManager.Room.cs        : join room / presence / in-match sync / leave
/// Public API GIỮ NGUYÊN — FirebaseMatchProvider và UI không cần sửa.
///
/// SCHEMA:
///   /users/{uid}                : displayName, level, currentExp, money, lastSeen
///   /matchmakingQueue/{uid}     : { name, joinedAt }
///   /rooms/{roomId}             : createdAt, seed, state, players, currentQ, answers, scores, winner
/// </summary>
public partial class FirebaseManager : MonoBehaviour
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
    public static event Action OnMatchmakingTimeout; // UX-06: Hết thời gian tìm trận

    // ==================== STATE ====================
    public bool IsConnected { get; private set; } = false;
    public bool IsAuthenticated => _auth?.CurrentUser != null;
    public bool IsAnonymous => _auth?.CurrentUser != null && _auth.CurrentUser.IsAnonymous;
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

                // [FIX] Đảm bảo FirebaseMatchProvider tồn tại (singleton MonoBehaviour).
                // Trước đây nó không được place trong scene → Instance luôn null
                // → InputController fallback sang LocalMatchProvider → online mode kẹt.
                EnsureFirebaseMatchProvider();

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

    // ==================== AUTO-CREATE FirebaseMatchProvider ====================
    private void EnsureFirebaseMatchProvider()
    {
        if (FirebaseMatchProvider.Instance != null)
        {
            Debug.Log("[FirebaseManager] FirebaseMatchProvider đã có sẵn.");
            return;
        }
        var go = new GameObject("FirebaseMatchProvider (auto)");
        go.AddComponent<FirebaseMatchProvider>();
        // FirebaseMatchProvider.Awake sẽ tự DontDestroyOnLoad
        Debug.Log("[FirebaseManager] Đã auto-tạo FirebaseMatchProvider GameObject.");
    }

    // ==================== REMOTE CONFIG ====================
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
}
