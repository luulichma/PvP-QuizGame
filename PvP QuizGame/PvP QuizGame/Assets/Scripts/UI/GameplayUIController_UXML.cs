using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Router UI của GameplayScene — UI Toolkit.
/// KHÔNG còn build UI trực tiếp; chỉ subscribe event từ Game layer và route xuống sub-controller:
/// - GameplayHUDController      : score, timer, tên/avatar, streak, turn summary, trạng thái đối thủ
/// - QuestionViewController     : câu hỏi + counter + progress bar
/// - CountdownOverlayController : đếm ngược 3-2-1-GO
/// - ResultPopupController      : popup kết quả trận
/// - GameplaySettingsPopupController / ExitConfirmPopupController (popup trong trận)
/// </summary>
public class GameplayUIController_UXML : MonoBehaviour
{
    public static GameplayUIController_UXML Instance { get; private set; }

    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;

    [SerializeField] private VisualTreeAsset resultPopupTemplate;
    [SerializeField] private VisualTreeAsset exitPopupTemplate;
    [SerializeField] private VisualTreeAsset settingsPopupTemplate;

    // ==================== SUB-CONTROLLERS ====================
    private GameplayHUDController _hud;
    private QuestionViewController _questionView;
    private CountdownOverlayController _countdown;
    private ResultPopupController _resultPopup;
    private GameplaySettingsPopupController _settingsPopup;
    private ExitConfirmPopupController _exitPopup;
    // [PHASE-2] Power-Up HUD
    private PowerUpHUDController _powerUpHUD;

    private Button _settingsBtn;

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

        // ---- Khởi tạo sub-controllers ----
        _hud = new GameplayHUDController(root, this);
        _questionView = new QuestionViewController(root);
        _countdown = new CountdownOverlayController(root);
        _hud.Attach();

        // [PHASE-2] Power-Up bar
        _powerUpHUD = new PowerUpHUDController(root);
        _powerUpHUD.Attach();

        _settingsBtn = root.Q<Button>("settings-btn");
        if (_settingsBtn != null) _settingsBtn.clicked += ShowSettingsPopup;

        // ---- Subscribe Game layer events ----
        GameController.OnGameStateChanged     += HandleGameStateChanged;
        QuizManager.OnQuestionChanged         += HandleQuestionChanged;
        ScoreManager.OnScoreChanged           += HandleScoreChanged;
        TimerController.OnTimerTick           += HandleTimerTick;
        GameController.OnGameOver             += HandleGameOver;
        GameController.OnOpponentLeft         += HandleOpponentLeft;
        GameController.OnOpponentAnswerResult += HandleOpponentAnswerResult;
        GameController.OnCountdownTick        += HandleCountdownTick;
        GameController.OnTurnSummary          += HandleTurnSummary;
        ScoreManager.OnStreakChanged          += HandleStreakChanged;

        LocalizationManager.OnLanguageChanged += LocalizeHUD;
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
            LocalizeHUD();

        _hud.AnimateEntry();
    }

    private void OnDisable()
    {
        GameController.OnGameStateChanged     -= HandleGameStateChanged;
        QuizManager.OnQuestionChanged         -= HandleQuestionChanged;
        ScoreManager.OnScoreChanged           -= HandleScoreChanged;
        TimerController.OnTimerTick           -= HandleTimerTick;
        GameController.OnGameOver             -= HandleGameOver;
        GameController.OnOpponentLeft         -= HandleOpponentLeft;
        GameController.OnOpponentAnswerResult -= HandleOpponentAnswerResult;
        GameController.OnCountdownTick        -= HandleCountdownTick;
        GameController.OnTurnSummary          -= HandleTurnSummary;
        ScoreManager.OnStreakChanged          -= HandleStreakChanged;

        LocalizationManager.OnLanguageChanged -= LocalizeHUD;

        _hud?.Detach();
        _powerUpHUD?.Detach(); // [PHASE-2]

        if (_settingsBtn != null) _settingsBtn.clicked -= ShowSettingsPopup;
    }

    // ==================== EVENT ROUTING ====================

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Countdown:
                _countdown.Create();
                break;
            case GameState.Playing:
                _hud.ResetForNewGame();
                _countdown.Remove();
                if (_resultPopup != null && _resultPopup.IsOpen)
                    _resultPopup.CloseImmediate();
                _resultPopup = null;
                break;
        }
    }

    private void HandleCountdownTick(int tick) => _countdown.HandleTick(tick);

    private void HandleQuestionChanged(QuestionData question)
    {
        if (question == null) return;
        _questionView.ShowQuestion(question);
        _hud.OnNewQuestion();
        _powerUpHUD?.RefreshOnNewQuestion(); // [PHASE-2]
    }

    private void HandleScoreChanged(int p1Score, int p2Score) => _hud.HandleScoreChanged(p1Score, p2Score);
    private void HandleTimerTick(float remaining) => _hud.HandleTimerTick(remaining);
    private void HandleOpponentLeft() => _hud.HandleOpponentLeft();
    private void HandleOpponentAnswerResult(bool isCorrect) => _hud.HandleOpponentAnswerResult(isCorrect);
    private void HandleStreakChanged(int streak) => _hud.HandleStreakChanged(streak);

    private void HandleTurnSummary(bool p1Correct, bool p2Correct, int p1Score, int p2Score, bool isLast)
        => _hud.HandleTurnSummary(p1Correct, p2Correct, p1Score, p2Score, isLast);

    /// <summary>Public API giữ tương thích — InputController/Network có thể gọi.</summary>
    public void SetOpponentStatus(string statusKey, string fallback)
        => _hud?.SetOpponentStatus(statusKey, fallback);

    private void LocalizeHUD()
    {
        _hud?.LocalizeNames();
        _questionView?.ShowLoadingText();
    }

    // ==================== GAME OVER ====================

    private void HandleGameOver()
    {
        // Giữ behavior cũ: fallback load template từ Resources nếu chưa gán Inspector
        if (resultPopupTemplate == null)
            resultPopupTemplate = Resources.Load<VisualTreeAsset>("UI/ResultPopup");

        if (resultPopupTemplate == null)
        {
            Debug.LogError("[GameplayUI] Không tìm thấy resultPopupTemplate!");
            return;
        }

        if (_resultPopup != null && _resultPopup.IsOpen)
            _resultPopup.CloseImmediate();

        _resultPopup = new ResultPopupController(resultPopupTemplate, uiDocument.rootVisualElement);
        _resultPopup.Show();
    }

    // ==================== POPUPS ====================

    private void ShowSettingsPopup()
    {
        if (settingsPopupTemplate == null) return;
        if (_settingsPopup != null && _settingsPopup.IsOpen) return;

        _settingsPopup = new GameplaySettingsPopupController(
            settingsPopupTemplate, uiDocument.rootVisualElement, ShowExitConfirmation);
        _settingsPopup.Show();
    }

    private void ShowExitConfirmation()
    {
        if (exitPopupTemplate == null) return;
        if (_exitPopup != null && _exitPopup.IsOpen) return;

        _exitPopup = new ExitConfirmPopupController(exitPopupTemplate, uiDocument.rootVisualElement);
        _exitPopup.Show();
    }

    // UX-08: Android back button
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_exitPopup != null && _exitPopup.IsOpen) return;
            if (_resultPopup != null && _resultPopup.IsOpen) return;
            ShowExitConfirmation();
        }
    }
}
