using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Điều phối giao diện người dùng TRONG trận đấu (GameplayScene) sử dụng UI Toolkit.
/// </summary>
public class GameplayUIController_UXML : MonoBehaviour
{
    public static GameplayUIController_UXML Instance { get; private set; }

    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;
    
    // Bạn có thể kéo file ResultPopup.uxml vào đây để load động khi kết thúc
    [SerializeField] private VisualTreeAsset resultPopupTemplate;

    // HUD Elements
    private Label _p1ScoreLabel;
    private Label _p2ScoreLabel;
    private Label _questionText;
    private Label _questionCounter;
    private Label _timerText;
    private VisualElement _timerFill;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        // Query HUD
        _p1ScoreLabel = root.Q<Label>("p1-score");
        _p2ScoreLabel = root.Q<Label>("p2-score");
        _questionText = root.Q<Label>("question-text");
        _questionCounter = root.Q<Label>("question-counter");
        _timerText = root.Q<Label>("timer-text");
        _timerFill = root.Q<VisualElement>("timer-fill");

        // Đăng ký các sự kiện gameplay
        GameController.OnGameStateChanged  += HandleGameStateChanged;
        QuizManager.OnQuestionChanged      += HandleQuestionChanged;
        ScoreManager.OnScoreChanged        += HandleScoreChanged;
        TimerController.OnTimerTick        += HandleTimerTick;
        GameController.OnGameOver          += HandleGameOver;
    }

    private void OnDisable()
    {
        GameController.OnGameStateChanged  -= HandleGameStateChanged;
        QuizManager.OnQuestionChanged      -= HandleQuestionChanged;
        ScoreManager.OnScoreChanged        -= HandleScoreChanged;
        TimerController.OnTimerTick        -= HandleTimerTick;
        GameController.OnGameOver          -= HandleGameOver;
    }

    // ==================== EVENT HANDLERS ====================
    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                UpdateScoreUI(0, 0);
                break;
        }
    }

    private void HandleQuestionChanged(QuestionData question)
    {
        if (question == null) return;

        if (_questionText != null)
            _questionText.text = question.questionText;

        if (_questionCounter != null && QuizManager.Instance != null)
        {
            int answered = QuizManager.Instance.AnsweredCount + 1;
            int total    = QuizManager.Instance.TotalCount;
            _questionCounter.text = $"CÂU {answered} / {total}";
        }
    }

    private void HandleScoreChanged(int p1Score, int p2Score)
    {
        UpdateScoreUI(p1Score, p2Score);
    }

    private void HandleTimerTick(float remaining)
    {
        if (_timerText != null)
        {
            _timerText.text = TimerController.Instance != null
                ? TimerController.Instance.GetFormattedTime()
                : $"{Mathf.CeilToInt(remaining)}s";
            
            _timerText.style.color = remaining <= 5f ? Color.red : Color.white;
        }

        if (_timerFill != null && TimerController.Instance != null)
        {
            float percent = (remaining / TimerController.Instance.TotalTime) * 100f;
            _timerFill.style.width = Length.Percent(percent);
        }
    }

    private void HandleGameOver()
    {
        if (resultPopupTemplate == null) return;

        // Load Result Popup động từ Template và add vào root
        var popupInstance = resultPopupTemplate.Instantiate();
        uiDocument.rootVisualElement.Add(popupInstance);

        // Setup dữ liệu cho Popup
        if (ScoreManager.Instance == null) return;
        
        WinResult result = ScoreManager.Instance.GetWinner();
        var title = popupInstance.Q<Label>("result-title");
        if (title != null)
        {
            title.text = result switch {
                WinResult.Player1Wins => "THẮNG!",
                WinResult.Player2Wins => "THUA!",
                WinResult.Draw        => "HÒA!",
                _ => ""
            };
            title.style.color = result == WinResult.Player1Wins ? Color.green : Color.red;
        }

        popupInstance.Q<Label>("p1-score-final").text = ScoreManager.Instance.Player1Score.ToString();
        popupInstance.Q<Label>("p2-score-final").text = ScoreManager.Instance.Player2Score.ToString();

        // Nút bấm trong Popup
        popupInstance.Q<Button>("play-again-btn").clicked += () => GameController.Instance.RestartGame();
        popupInstance.Q<Button>("back-home-btn").clicked += () => {
            if (GameManager.Instance != null) GameManager.Instance.LoadHomeScene();
            else UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
        };
    }

    private void UpdateScoreUI(int p1Score, int p2Score)
    {
        if (_p1ScoreLabel != null) _p1ScoreLabel.text = p1Score.ToString();
        if (_p2ScoreLabel != null) _p2ScoreLabel.text = p2Score.ToString();
    }
}
