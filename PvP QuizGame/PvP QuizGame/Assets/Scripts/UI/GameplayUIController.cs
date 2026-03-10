using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều phối giao diện người dùng TRONG trận đấu (GameplayScene).
/// Chỉ phân phát UI liên quan đến trạng thái chơi: Đếm ngược, Hiển thị Câu hỏi, Game Over.
/// Không còn chứa MainMenu như cách làm Single-Scene cũ.
/// </summary>
public class GameplayUIController : MonoBehaviour
{
    public static GameplayUIController Instance { get; private set; }

    [Header("Các màn hình trận đấu (Panels)")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Màn hình đếm ngược")]
    [SerializeField] private Text countdownText;

    [Header("Màn hình thi đấu")]
    [SerializeField] private Text questionText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text player1ScoreText;
    [SerializeField] private Text player2ScoreText;
    [SerializeField] private Text questionCounterText;

    [Header("Màn hình kết quả")]
    [SerializeField] private Text resultText;
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button backToMainMenuButton;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Lắng nghe tất cả sự kiện gameplay
        GameController.OnGameStateChanged  += HandleGameStateChanged;
        GameController.OnCountdownTick     += HandleCountdownTick;
        QuizManager.OnQuestionChanged      += HandleQuestionChanged;
        ScoreManager.OnScoreChanged        += HandleScoreChanged;
        TimerController.OnTimerTick        += HandleTimerTick;
        GameController.OnGameOver          += HandleGameOver;

        // Cấu hình Nút
        playAgainButton?.onClick.AddListener(() => GameController.Instance.RestartGame());
        
        // Khi quay ra, ta gọi App Manager (GameManager) để tiêu hủy GameplayScene
        backToMainMenuButton?.onClick.AddListener(() => {
            if (GameManager.Instance != null) {
                GameManager.Instance.LoadMainMenuScene();
            } else {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
            }
        });

        // Ẩn tất cả khi khởi tạo, chờ GameController tự động báo event Countdown tới
        ShowPanel(null);
    }

    private void OnDestroy()
    {
        GameController.OnGameStateChanged  -= HandleGameStateChanged;
        GameController.OnCountdownTick     -= HandleCountdownTick;
        QuizManager.OnQuestionChanged      -= HandleQuestionChanged;
        ScoreManager.OnScoreChanged        -= HandleScoreChanged;
        TimerController.OnTimerTick        -= HandleTimerTick;
        GameController.OnGameOver          -= HandleGameOver;
    }

    // ==================== ĐIỀU HƯỚNG PANEL ====================
    private void ShowPanel(GameObject target)
    {
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
                ShowPanel(null); // Không có MainMenu ở scene này
                break;
            case GameState.Countdown:
                ShowPanel(countdownPanel);
                break;
            case GameState.Playing:
                ShowPanel(gameplayPanel);
                UpdateScoreUI(0, 0); // Đặt lại giao diện điểm đầu trận
                break;
            case GameState.GameOver:
                // GameOver panel được hiển thị trong HandleGameOver để kết xuất data đầy đủ
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

        // Báo đỏ khi hết giờ gấp
        timerText.color = remaining <= 30f ? Color.red : Color.white;
        timerText.text  = TimerController.Instance != null
            ? TimerController.Instance.GetFormattedTime()
            : $"{Mathf.CeilToInt(remaining)}";
    }

    private void HandleGameOver()
    {
        ShowPanel(gameOverPanel);

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
