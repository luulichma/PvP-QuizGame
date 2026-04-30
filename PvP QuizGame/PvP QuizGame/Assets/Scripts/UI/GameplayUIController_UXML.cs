using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Linq;
using DG.Tweening;

/// <summary>
/// Điều phối giao diện người dùng TRONG trận đấu (GameplayScene) sử dụng UI Toolkit.
/// Online mode: hiển thị displayName của 2 người chơi.
/// </summary>
public class GameplayUIController_UXML : MonoBehaviour
{
    public static GameplayUIController_UXML Instance { get; private set; }

    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;

    [SerializeField] private VisualTreeAsset resultPopupTemplate;
    [SerializeField] private VisualTreeAsset exitPopupTemplate;
 
    [Header("Avatar Settings")]
    [SerializeField] private Sprite[] avatarSprites;

    private Label _p1ScoreLabel;
    private Label _p2ScoreLabel;
    private Label _p1Label;
    private Label _p2Label;
    private Label _questionText;
    private Label _questionCounter;
    private Label _timerText;
    private VisualElement _timerFill;
    private VisualElement _p1Avatar;
    private VisualElement _p2Avatar;

    private VisualElement _resultPopupInstance;
    private VisualElement _exitPopupInstance;

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

        _p1ScoreLabel = root.Q<Label>("p1-score");
        _p2ScoreLabel = root.Q<Label>("p2-score");
        _p1Label      = root.Q<Label>("p1-label");
        _p2Label      = root.Q<Label>("p2-label");
        _questionText = root.Q<Label>("question-text");
        _questionCounter = root.Q<Label>("question-counter");
        _timerText = root.Q<Label>("timer-text");
        _timerFill = root.Q<VisualElement>("timer-fill");
        _p1Avatar = root.Q<VisualElement>("p1-avatar");
        _p2Avatar = root.Q<VisualElement>("p2-avatar");
 
        var exitBtn = root.Q<Button>("exit-btn");
        if (exitBtn != null) exitBtn.clicked += ShowExitConfirmation;

        GameController.OnGameStateChanged  += HandleGameStateChanged;
        QuizManager.OnQuestionChanged      += HandleQuestionChanged;
        ScoreManager.OnScoreChanged        += HandleScoreChanged;
        TimerController.OnTimerTick        += HandleTimerTick;
        GameController.OnGameOver          += HandleGameOver;
        GameController.OnOpponentLeft      += HandleOpponentLeft;

        LocalizationManager.OnLanguageChanged += LocalizeHUD;
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
            LocalizeHUD();
    }

    private void OnDisable()
    {
        GameController.OnGameStateChanged  -= HandleGameStateChanged;
        QuizManager.OnQuestionChanged      -= HandleQuestionChanged;
        ScoreManager.OnScoreChanged        -= HandleScoreChanged;
        TimerController.OnTimerTick        -= HandleTimerTick;
        GameController.OnGameOver          -= HandleGameOver;
        GameController.OnOpponentLeft      -= HandleOpponentLeft;

        LocalizationManager.OnLanguageChanged -= LocalizeHUD;
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                UpdateScoreUI(0, 0);
                if (_resultPopupInstance != null)
                {
                    _resultPopupInstance.RemoveFromHierarchy();
                    _resultPopupInstance = null;
                }
                break;
        }
    }

    private void HandleQuestionChanged(QuestionData question)
    {
        if (question == null) return;

        if (_questionText != null)
        {
            string qKey = question.questionText;
            _questionText.text = LocalizationManager.Instance != null
                                 ? LocalizationManager.Instance.GetText(qKey)
                                 : qKey;
            
            // Animation cho câu hỏi (Trượt từ trên xuống)
            var questionCard = _questionText.parent;
            if (questionCard != null)
            {
                questionCard.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(-50)));
                questionCard.style.opacity = 0f;
                UIAnimator.DOFade(questionCard, 1f, 0.3f);
                UIAnimator.DOTranslate(questionCard, Vector2.zero, 0.4f).SetEase(DG.Tweening.Ease.OutBack);
            }
        }

        if (_questionCounter != null && QuizManager.Instance != null)
        {
            int answered = QuizManager.Instance.AnsweredCount + 1;
            int total    = QuizManager.Instance.TotalCount;

            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
            {
                string fmt = LocalizationManager.Instance.GetText("game_question_counter");
                if (string.IsNullOrEmpty(fmt) || fmt.StartsWith("["))
                    fmt = "{0} / {1}";
                _questionCounter.text = string.Format(fmt, answered, total);
            }
            else
            {
                _questionCounter.text = $"{answered} / {total}";
            }
        }
    }

    private void HandleScoreChanged(int p1Score, int p2Score) => UpdateScoreUI(p1Score, p2Score);

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

    private void HandleOpponentLeft()
    {
        // Hiện toast ngắn gọn — popup sẽ hiển thị thắng cuộc
        Debug.LogWarning("[GameplayUI] Đối thủ đã rời trận!");
    }

    private void HandleGameOver()
    {
        if (resultPopupTemplate == null)
        {
            Debug.LogWarning("[GameplayUI] Chưa gán resultPopupTemplate!");
            return;
        }

        if (_resultPopupInstance != null)
        {
            _resultPopupInstance.RemoveFromHierarchy();
            _resultPopupInstance = null;
        }

        _resultPopupInstance = resultPopupTemplate.Instantiate();
        
        // FIX: Đảm bảo TemplateContainer chiếm toàn bộ màn hình
        _resultPopupInstance.style.position = Position.Absolute;
        _resultPopupInstance.style.top = 0;
        _resultPopupInstance.style.bottom = 0;
        _resultPopupInstance.style.left = 0;
        _resultPopupInstance.style.right = 0;

        // ANIMATION: Result Popup
        var overlay = _resultPopupInstance.Q<VisualElement>("overlay") ?? _resultPopupInstance.Children().First();
        var popupCard = _resultPopupInstance.Q<VisualElement>("popup") ?? overlay.Children().First();
        UIAnimator.ShowPopupAnim(overlay, popupCard);

        if (ScoreManager.Instance == null) return;

        WinResult result = ScoreManager.Instance.GetWinner();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayResultSound(result == WinResult.Player1Wins);
        }
        
        var L = LocalizationManager.Instance;

        var title = _resultPopupInstance.Q<Label>("result-title");
        if (title != null)
        {
            string titleKey = result switch
            {
                WinResult.Player1Wins => "game_win",
                WinResult.Player2Wins => "game_lose",
                WinResult.Draw        => "game_draw",
                _ => "game_draw"
            };
            title.text = L != null ? L.GetText(titleKey) : titleKey;
            title.style.color = result switch
            {
                WinResult.Player1Wins => new Color(0f, 0.9f, 0.46f),
                WinResult.Player2Wins => new Color(1f, 0.32f, 0.32f),
                WinResult.Draw        => new Color(1f, 0.92f, 0f),
                _ => Color.white
            };
        }

        var p1Final = _resultPopupInstance.Q<Label>("p1-score-final");
        if (p1Final != null) p1Final.text = ScoreManager.Instance.Player1Score.ToString();

        var p2Final = _resultPopupInstance.Q<Label>("p2-score-final");
        if (p2Final != null) p2Final.text = ScoreManager.Instance.Player2Score.ToString();

        // Reward thực tế từ ScoreManager
        var rewardLabel = _resultPopupInstance.Q<Label>("reward-amount");
        if (rewardLabel != null)
            rewardLabel.text = $"+${ScoreManager.Instance.LastRewardMoney:N0}";

        var playAgainBtn = _resultPopupInstance.Q<Button>("play-again-btn");
        if (playAgainBtn != null)
        {
            if (L != null) playAgainBtn.text = L.GetText("game_play_again");

            // Online mode: "Chơi lại" có nghĩa là VỀ HOME để tìm trận mới (không thể restart room)
            bool isOnline = FirebaseManager.Instance != null
                            && !FirebaseManager.Instance.isOfflineMode
                            && !string.IsNullOrEmpty(FirebaseManager.Instance.CurrentRoomId);

            if (isOnline)
            {
                // "Chơi lại" trong online = tìm trận mới
                playAgainBtn.clicked += () => {
                    FirebaseManager.Instance.LeaveRoom();
                    if (GameManager.Instance != null) GameManager.Instance.LoadHomeScene();
                };
            }
            else
            {
                playAgainBtn.clicked += () => {
                    if (_resultPopupInstance != null)
                    {
                        _resultPopupInstance.RemoveFromHierarchy();
                        _resultPopupInstance = null;
                    }
                    if (GameController.Instance != null) GameController.Instance.RestartGame();
                };
            }
        }

        var backHomeBtn = _resultPopupInstance.Q<Button>("back-home-btn");
        if (backHomeBtn != null)
        {
            if (L != null) backHomeBtn.text = L.GetText("game_back_home");
            backHomeBtn.clicked += () => {
                if (FirebaseManager.Instance != null) FirebaseManager.Instance.LeaveRoom();
                if (GameManager.Instance != null) GameManager.Instance.LoadHomeScene();
                else UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
            };
        }
    }
 
    private void ShowExitConfirmation()
    {
        if (exitPopupTemplate == null) return;
        if (_exitPopupInstance != null) return;
 
        _exitPopupInstance = exitPopupTemplate.Instantiate();
        _exitPopupInstance.style.position = Position.Absolute;
        _exitPopupInstance.style.top = 0;
        _exitPopupInstance.style.bottom = 0;
        _exitPopupInstance.style.left = 0;
        _exitPopupInstance.style.right = 0;
 
        var overlay = _exitPopupInstance.Q<VisualElement>("overlay") ?? _exitPopupInstance.Children().First();
        var popupCard = _exitPopupInstance.Q<VisualElement>("popup") ?? overlay.Children().First();
        UIAnimator.ShowPopupAnim(overlay, popupCard);
 
        var confirmBtn = _exitPopupInstance.Q<Button>("confirm-btn");
        if (confirmBtn != null)
        {
            confirmBtn.clicked += () => {
                UIAnimator.HidePopupAnim(overlay, popupCard, () => {
                    _exitPopupInstance.RemoveFromHierarchy();
                    _exitPopupInstance = null;
                    if (GameController.Instance != null) GameController.Instance.ForcedSurrender();
                });
            };
        }
 
        var cancelBtn = _exitPopupInstance.Q<Button>("cancel-btn");
        if (cancelBtn != null)
        {
            cancelBtn.clicked += () => {
                UIAnimator.HidePopupAnim(overlay, popupCard, () => {
                    _exitPopupInstance.RemoveFromHierarchy();
                    _exitPopupInstance = null;
                });
            };
        }
    }
 
    private void LocalizeHUD()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        if (root == null) return;
        if (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsReady) return;

        var L = LocalizationManager.Instance;

        // Hiển thị ID (hoặc Tên) của người chơi và đối thủ
        if (FirebaseManager.Instance != null)
        {
            string myId = FirebaseManager.Instance.IsAuthenticated 
                ? FirebaseManager.Instance.LocalUserId 
                : (FirebaseManager.Instance.LocalDisplayName ?? "PLAYER");
                
            string oppId = !string.IsNullOrEmpty(FirebaseManager.Instance.OpponentId)
                ? FirebaseManager.Instance.OpponentId
                : (FirebaseManager.Instance.OpponentName ?? "BOT");

            // Cắt ngắn để fit khung UI (UID Firebase rất dài, lấy 10 ký tự đầu)
            if (myId != null && myId.Length > 10) myId = myId.Substring(0, 10);
            if (oppId != null && oppId.Length > 10) oppId = oppId.Substring(0, 10);

            if (_p1Label != null) _p1Label.text = myId;
            if (_p2Label != null) _p2Label.text = oppId;
 
            // Hiển thị Avatar
            if (avatarSprites != null)
            {
                // P1 (Me)
                int myAvatarIdx = PlayerDataManager.Instance?.Data?.avatarIndex ?? 0;
                if (_p1Avatar != null && myAvatarIdx < avatarSprites.Length)
                {
                    _p1Avatar.style.backgroundImage = new StyleBackground(avatarSprites[myAvatarIdx]);
                    _p1Avatar.style.backgroundColor = Color.clear;
                }
 
                // P2 (Opponent)
                int oppAvatarIdx = FirebaseManager.Instance.OpponentAvatarIndex;
                if (_p2Avatar != null && oppAvatarIdx < avatarSprites.Length)
                {
                    _p2Avatar.style.backgroundImage = new StyleBackground(avatarSprites[oppAvatarIdx]);
                    _p2Avatar.style.backgroundColor = Color.clear;
                }
            }
        }
        else
        {
            if (_p1Label != null) _p1Label.text = L.GetText("game_score_me");
            if (_p2Label != null) _p2Label.text = L.GetText("game_score_opp");
        }

        if (_questionText != null && QuizManager.Instance == null)
            _questionText.text = L.GetText("game_loading_question");
    }

    private void UpdateScoreUI(int p1Score, int p2Score)
    {
        if (_p1ScoreLabel != null) _p1ScoreLabel.text = p1Score.ToString();
        if (_p2ScoreLabel != null) _p2ScoreLabel.text = p2Score.ToString();
    }
}
