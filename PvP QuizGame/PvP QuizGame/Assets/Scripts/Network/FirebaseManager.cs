using UnityEngine;
using System;

/// <summary>
/// ==================== TASK 2.5 — LÀM SAU ====================
/// Stub placeholder cho FirebaseManager.
/// Sẽ implement đầy đủ sau khi cài Firebase SDK vào Unity:
///   Window > Package Manager > Add package by name:
///   - com.google.firebase.auth
///   - com.google.firebase.database
/// =============================================================
///
/// Vai trò:
/// - Firebase Authentication: Đăng ký / Đăng nhập người chơi
/// - Matchmaking: Tìm và ghép phòng đấu qua Realtime Database
/// - Real-time Sync: Đồng bộ điểm số giữa 2 thiết bị
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static FirebaseManager Instance { get; private set; }

    // ==================== EVENTS ====================
    /// <summary>Phát khi Firebase kết nối thành công</summary>
    public static event Action OnConnected;

    /// <summary>Phát khi có lỗi kết nối</summary>
    public static event Action<string> OnConnectionError;

    /// <summary>Phát khi Matchmaking thành công — 2 người chơi đã vào phòng</summary>
    public static event Action OnMatchFound;

    /// <summary>Phát khi nhận điểm cập nhật của đối thủ từ Firebase</summary>
    public static event Action<int> OnOpponentScoreUpdated;

    // ==================== THÔNG TIN PHÒNG ====================
    /// <summary>ID phòng đấu hiện tại</summary>
    public string CurrentRoomId { get; private set; }

    /// <summary>UID người chơi từ Firebase Auth</summary>
    public string LocalUserId { get; private set; }

    /// <summary>Trạng thái kết nối</summary>
    public bool IsConnected { get; private set; }

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ==================== TODO: TASK 2.5 ====================

    /// <summary>
    /// [TODO 2.5] Khởi tạo Firebase App và kiểm tra kết nối
    /// </summary>
    public void InitializeFirebase()
    {
        Debug.Log("[FirebaseManager] TODO (Task 2.5): Initialize Firebase App");
        // Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(...)
    }

    /// <summary>
    /// [TODO 2.5] Đăng nhập bằng email + password
    /// </summary>
    public void SignIn(string email, string password)
    {
        Debug.Log($"[FirebaseManager] TODO (Task 2.5): Sign In với {email}");
        // FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password)...
    }

    /// <summary>
    /// [TODO 2.5] Đăng ký tài khoản mới
    /// </summary>
    public void SignUp(string email, string password)
    {
        Debug.Log($"[FirebaseManager] TODO (Task 2.5): Sign Up với {email}");
        // FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(email, password)...
    }

    /// <summary>
    /// [TODO 2.5] Tìm phòng có sẵn hoặc tạo phòng mới (Matchmaking)
    /// Cấu trúc Firebase DB:
    /// rooms/{roomId}/player1, player2, state, currentQuestion, scores/
    /// </summary>
    public void JoinOrCreateRoom()
    {
        Debug.Log("[FirebaseManager] TODO (Task 2.5): JoinOrCreateRoom");
        // FirebaseDatabase.DefaultInstance.GetReference("rooms")
        //   .OrderByChild("state").EqualTo("waiting").GetValueAsync()...
    }

    /// <summary>
    /// [TODO 2.5] Gửi đáp án của người chơi lên Firebase
    /// </summary>
    public void SendAnswer(int answerIndex)
    {
        Debug.Log($"[FirebaseManager] TODO (Task 2.5): SendAnswer({answerIndex})");
        // ref.Child("rooms").Child(CurrentRoomId).Child("answers").Child(LocalUserId).SetValueAsync(answerIndex)
    }

    /// <summary>
    /// [TODO 2.5] Cập nhật điểm số của bản thân lên Firebase
    /// </summary>
    public void UpdateMyScore(int score)
    {
        Debug.Log($"[FirebaseManager] TODO (Task 2.5): UpdateScore({score})");
        // ref.Child("rooms").Child(CurrentRoomId).Child("scores").Child(LocalUserId).SetValueAsync(score)
    }
}
