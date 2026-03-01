using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Điều phối toàn bộ giao diện người dùng trong game.
/// Subscribe các sự kiện từ GameManager, QuizManager, ScoreManager, TimerController
/// và cập nhật UI tương ứng.
/// Attach vào GameObject "UIController" trong Scene.
/// </summary>
public class UIController : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static UIController Instance { get; private set; }

    // ==================== INSPECTOR — PANELS ====================
    [Header("Các màn hình (Panels)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject gameOverPanel;

    // ==================== INSPECTOR — COUNTDOWN ====================
    [Header("Màn hình đếm ngược")]
    [SerializeField] private Text countdownText;

    // ==================== INSPECTOR — GAMEPLAY ====================
    [Header("Màn hình thi đấu")]
    [SerializeField] private Text questionText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text player1ScoreText;
    [SerializeField] private Text player2ScoreText;
    [SerializeField] private Text questionCounterText; // Ví dụ: "Câu 3/10"

    // ==================== INSPECTOR — GAME OVER ====================
    [Header("Màn hình kết quả")]
    [SerializeField] private Text resultText;           // "THẮNG!" / "THUA!" / "HÒA!"
    [SerializeField] private Text finalScoreText;       // "Bạn: 70 | Đối thủ: 50"
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe tất cả sự kiện
        GameManager.OnGameStateChanged  += HandleGameStateChanged;
        GameManager.OnCountdownTick     += HandleCountdownTick;
        QuizManager.OnQuestionChanged   += HandleQuestionChanged;
        ScoreManager.OnScoreChanged     += HandleScoreChanged;
        TimerController.OnTimerTick     += HandleTimerTick;
        GameManager.OnGameOver          += HandleGameOver;

        // Gán nút kết quả
        playAgainButton?.onClick.AddListener(() => GameManager.Instance.RestartGame());
        mainMenuButton?.onClick.AddListener(() => GameManager.Instance.ChangeState(GameState.Idle));

        // Bắt đầu ở Main Menu
        ShowPanel(mainMenuPanel);
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged  -= HandleGameStateChanged;
        GameManager.OnCountdownTick     -= HandleCountdownTick;
        QuizManager.OnQuestionChanged   -= HandleQuestionChanged;
        ScoreManager.OnScoreChanged     -= HandleScoreChanged;
        TimerController.OnTimerTick     -= HandleTimerTick;
        GameManager.OnGameOver          -= HandleGameOver;
    }

    // ==================== ĐIỀU HƯỚNG PANEL ====================
    private void ShowPanel(GameObject target)
    {
        mainMenuPanel?.SetActive(mainMenuPanel   == target);
        countdownPanel?.SetActive(countdownPanel == target);
        gameplayPanel?.SetActive(gameplayPanel   == target);
        gameOverPanel?.SetActive(gameOverPanel   == target);
    }

    // ==================== EVENT HANDLERS ====================
    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Idle:
                ShowPanel(mainMenuPanel);
                break;
            case GameState.Countdown:
                ShowPanel(countdownPanel);
                break;
            case GameState.Playing:
                ShowPanel(gameplayPanel);
                // Reset hiển thị điểm
                UpdateScoreUI(0, 0);
                break;
            case GameState.GameOver:
                // GameOver panel được hiển thị trong HandleGameOver
                break;
        }
    }

    private void HandleCountdownTick(int value)
    {
        if (countdownText != null)
            countdownText.text = value.ToString();
    }

    private void HandleQuestionChanged(QuestionData question)
    {
        if (question == null) return;

        if (questionText != null)
            questionText.text = question.questionText;

        // Cập nhật bộ đếm câu hỏi
        if (questionCounterText != null && QuizManager.Instance != null)
        {
            int answered = QuizManager.Instance.AnsweredCount + 1;
            int total    = QuizManager.Instance.TotalCount;
            questionCounterText.text = $"Câu {answered}/{total}";
        }
    }

    private void HandleScoreChanged(int p1Score, int p2Score)
    {
        UpdateScoreUI(p1Score, p2Score);
    }

    private void HandleTimerTick(float remaining)
    {
        if (timerText == null) return;

        // Đổi màu đỏ khi dưới 30 giây
        timerText.color = remaining <= 30f ? Color.red : Color.white;
        timerText.text  = TimerController.Instance != null
            ? TimerController.Instance.GetFormattedTime()
            : $"{Mathf.CeilToInt(remaining)}";
    }

    private void HandleGameOver()
    {
        ShowPanel(gameOverPanel);

        // Hiện kết quả thắng/thua/hòa
        if (ScoreManager.Instance == null) return;

        WinResult result = ScoreManager.Instance.GetWinner();
        int p1 = ScoreManager.Instance.Player1Score;
        int p2 = ScoreManager.Instance.Player2Score;

        if (resultText != null)
        {
            resultText.text = result switch
            {
                WinResult.Player1Wins => "🏆 THẮNG!",
                WinResult.Player2Wins => "😔 THUA!",
                WinResult.Draw        => "🤝 HÒA!",
                _                     => ""
            };
        }

        if (finalScoreText != null)
            finalScoreText.text = $"Bạn: {p1} điểm  |  Đối thủ: {p2} điểm";
    }

    // ==================== HELPERS ====================
    private void UpdateScoreUI(int p1Score, int p2Score)
    {
        if (player1ScoreText != null) player1ScoreText.text = p1Score.ToString();
        if (player2ScoreText != null) player2ScoreText.text = p2Score.ToString();
    }
}
