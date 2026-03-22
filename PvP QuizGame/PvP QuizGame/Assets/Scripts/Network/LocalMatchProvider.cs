using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// [OFFLINE LAYER] Đóng vai trò như một Server cục bộ thu nhỏ.
/// Chờ cả 2 người chơi nộp đáp án rồi mới ra lệnh cho GameController công bố kết quả.
/// 
/// *** SCALE LÊN ONLINE ***
/// Khi tích hợp Firebase: Xoá/Bỏ qua class này.
/// Tạo FirebaseMatchProvider.cs implement cùng logic nhưng
/// thay vì set biến local, nó sẽ ghi lên Firebase DB và lắng nghe
/// snapshot để biết cả 2 đã trả lời chưa.
/// GameController và InputController KHÔNG CẦN SỬA DÙ 1 DÒNG.
/// </summary>
public class LocalMatchProvider : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static LocalMatchProvider Instance { get; private set; }

    // ==================== EVENTS ====================
    /// <summary>Phát khi cả 2 người chơi đã nộp đáp án. Tham số: (p1AnswerIdx, p2AnswerIdx)</summary>
    public static event Action<int, int> OnBothPlayersAnswered;

    /// <summary>Phát khi bắt đầu câu hỏi mới (để MockOpponent biết mà bắt đầu đếm giờ)</summary>
    public static event Action OnNewQuestionStarted;

    // ==================== INTERNAL STATE ====================
    private int _p1Answer = -1; // -1 = chưa trả lời
    private int _p2Answer = -1;
    private bool _isWaiting = false;

    [Header("Cài đặt Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Lắng nghe sự kiện câu hỏi mới để reset state
        QuizManager.OnQuestionChanged += HandleNewQuestion;
    }

    private void OnDestroy()
    {
        QuizManager.OnQuestionChanged -= HandleNewQuestion;
    }

    // ==================== API CÔNG KHAI ====================

    /// <summary>
    /// Gọi khi Player 1 (người thật) chọn đáp án.
    /// InputController sẽ gọi hàm này thay vì gọi ScoreManager trực tiếp.
    /// </summary>
    public void SubmitAnswerP1(int answerIndex)
    {
        if (!_isWaiting) return;
        if (_p1Answer != -1)
        {
            Log("P1 đã nộp bài rồi, bỏ qua.");
            return;
        }

        _p1Answer = answerIndex;
        Log($"P1 nộp đáp án: {answerIndex}");
        CheckIfBothAnswered();
    }

    /// <summary>
    /// Gọi khi Player 2 (Bot / Firebase) chọn đáp án.
    /// MockOpponent sẽ gọi hàm này.
    /// </summary>
    public void SubmitAnswerP2(int answerIndex)
    {
        if (!_isWaiting) return;
        if (_p2Answer != -1)
        {
            Log("P2 đã nộp bài rồi, bỏ qua.");
            return;
        }

        _p2Answer = answerIndex;
        Log($"P2 (Bot) nộp đáp án: {answerIndex}");
        CheckIfBothAnswered();
    }

    // ==================== INTERNAL ====================
    private void HandleNewQuestion(QuestionData question)
    {
        // Reset state cho câu hỏi mới
        _p1Answer = -1;
        _p2Answer = -1;
        _isWaiting = true;

        Log("Câu mới bắt đầu — Đang chờ 2 bên trả lời...");
        OnNewQuestionStarted?.Invoke();
    }

    private void CheckIfBothAnswered()
    {
        if (_p1Answer == -1 && _p2Answer != -1)
            Log("Bot (P2) đã chọn xong. Đang đợi Người chơi (P1) bấm nút...");
        else if (_p1Answer != -1 && _p2Answer == -1)
            Log("Người chơi (P1) đã chọn xong. Đang đợi Bot (P2) suy nghĩ...");

        if (_p1Answer == -1 || _p2Answer == -1) return;

        // Cả 2 đã trả lời -> Tắt cờ chờ và phát sự kiện
        _isWaiting = false;
        Log($"<color=green>✅ CẢ HAI ĐÃ TRẢ LỜI!</color> P1 chọn [{_p1Answer}], P2 chọn [{_p2Answer}]");
        OnBothPlayersAnswered?.Invoke(_p1Answer, _p2Answer);
    }

    private void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[LocalMatchProvider] {msg}");
    }
}
