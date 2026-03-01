using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Quản lý câu hỏi: load từ QuizDatabase, xáo trộn (Shuffle)
/// và cung cấp câu hỏi theo từng lượt trong trận đấu.
/// Attach vào cùng GameObject với GameManager.
/// </summary>
public class QuizManager : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static QuizManager Instance { get; private set; }

    // ==================== EVENTS ====================
    /// <summary>Phát khi có câu hỏi mới được load lên</summary>
    public static event Action<QuestionData> OnQuestionChanged;

    /// <summary>Phát khi đã hết toàn bộ câu hỏi trong trận</summary>
    public static event Action OnQuestionsExhausted;

    // ==================== INSPECTOR ====================
    [Header("Ngân hàng câu hỏi")]
    [SerializeField] private QuizDatabase quizDatabase;

    // ==================== INTERNAL STATE ====================
    private List<QuestionData> _shuffledQuestions;
    private int _currentIndex = -1;

    /// <summary>Câu hỏi hiện tại đang hiển thị</summary>
    public QuestionData CurrentQuestion =>
        (_currentIndex >= 0 && _shuffledQuestions != null && _currentIndex < _shuffledQuestions.Count)
            ? _shuffledQuestions[_currentIndex]
            : null;

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ==================== API CÔNG KHAI ====================
    /// <summary>Khởi động quiz: shuffle và load câu đầu tiên</summary>
    public void StartQuiz()
    {
        if (quizDatabase == null || quizDatabase.Count == 0)
        {
            Debug.LogError("[QuizManager] Chưa gán QuizDatabase hoặc database rỗng!");
            return;
        }

        _shuffledQuestions = new List<QuestionData>(quizDatabase.questions);
        Shuffle(_shuffledQuestions);
        _currentIndex = -1;

        Debug.Log($"[QuizManager] Đã shuffle {_shuffledQuestions.Count} câu hỏi.");
        NextQuestion();
    }

    /// <summary>Chuyển sang câu hỏi tiếp theo</summary>
    public void NextQuestion()
    {
        _currentIndex++;

        if (_currentIndex >= _shuffledQuestions.Count)
        {
            Debug.Log("[QuizManager] Đã hết câu hỏi.");
            OnQuestionsExhausted?.Invoke();
            return;
        }

        Debug.Log($"[QuizManager] Câu hỏi [{_currentIndex + 1}/{_shuffledQuestions.Count}]");
        OnQuestionChanged?.Invoke(_shuffledQuestions[_currentIndex]);
    }

    /// <summary>Kiểm tra còn câu hỏi tiếp theo không</summary>
    public bool HasMoreQuestions() => _currentIndex + 1 < (_shuffledQuestions?.Count ?? 0);

    /// <summary>Số câu hỏi đã trả lời</summary>
    public int AnsweredCount => Mathf.Max(0, _currentIndex);

    /// <summary>Tổng số câu hỏi trong trận</summary>
    public int TotalCount => _shuffledQuestions?.Count ?? 0;

    // ==================== FISHER-YATES SHUFFLE ====================
    /// <summary>
    /// Thuật toán Fisher-Yates: xáo trộn list ngẫu nhiên không trùng lặp
    /// </summary>
    private void Shuffle(List<QuestionData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
