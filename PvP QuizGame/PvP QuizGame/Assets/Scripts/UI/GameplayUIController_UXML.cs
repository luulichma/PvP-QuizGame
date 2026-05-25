using System;
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
    // UX-02: Opponent status indicator
    private Label _p2StatusLabel;

    private VisualElement _resultPopupInstance;
    private VisualElement _exitPopupInstance;

    // BUG-04: Lưu reference để unsubscribe
    private Action _playAgainHandler;
    private Action _backHomeHandler;

    // UX-03: Turn summary state
    private bool _isShowingTurnSummary = false;

    // UX-04: Countdown overlay
    private VisualElement _countdownOverlay;
    private Label _countdownLabel;

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
        _timerFill = root.Q<VisualElement>("timer-ring-fill");
        _p1Avatar = root.Q<VisualElement>("p1-avatar");
        _p2Avatar = root.Q<VisualElement>("p2-avatar");
        // UX-02: Status label cho đối thủ
        _p2StatusLabel = root.Q<Label>("p2-status");
 
        var exitBtn = root.Q<Button>("exit-btn");
        if (exitBtn != null) exitBtn.clicked += ShowExitConfirmation;

        GameController.OnGameStateChanged  += HandleGameStateChanged;
        QuizManager.OnQuestionChanged      += HandleQuestionChanged;
        ScoreManager.OnScoreChanged        += HandleScoreChanged;
        TimerController.OnTimerTick        += HandleTimerTick;
        GameController.OnGameOver          += HandleGameOver;
        GameController.OnOpponentLeft      += HandleOpponentLeft;
        GameController.OnOpponentAnswerResult += HandleOpponentAnswerResult; // UX-01
        GameController.OnCountdownTick     += HandleCountdownTick; // UX-04
        GameController.OnTurnSummary       += HandleTurnSummary; // UX-03
        ScoreManager.OnStreakChanged       += HandleStreakChanged; // UX-01

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
        GameController.OnOpponentAnswerResult -= HandleOpponentAnswerResult; // UX-01
        GameController.OnCountdownTick     -= HandleCountdownTick; // UX-04
        GameController.OnTurnSummary       -= HandleTurnSummary; // UX-03
        ScoreManager.OnStreakChanged       -= HandleStreakChanged; // UX-01

        LocalizationManager.OnLanguageChanged -= LocalizeHUD;

        // BUG-13: Unsubscribe exitBtn handler
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            if (root != null)
            {
                var exitBtn = root.Q<Button>("exit-btn");
                if (exitBtn != null) exitBtn.clicked -= ShowExitConfirmation;
            }
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Countdown:
                // UX-04: Tạo countdown overlay
                CreateCountdownOverlay();
                break;
            case GameState.Playing:
                UpdateScoreUI(0, 0);
                // UX-04: Ẩn countdown overlay khi bắt đầu chơi
                if (_countdownOverlay != null)
                {
                    _countdownOverlay.RemoveFromHierarchy();
                    _countdownOverlay = null;
                }
                if (_resultPopupInstance != null)
                {
                    _resultPopupInstance.RemoveFromHierarchy();
                    _resultPopupInstance = null;
                }
                break;
        }
    }

    // UX-04: Countdown visual 3-2-1-GO!
    private void CreateCountdownOverlay()
    {
        if (uiDocument == null) return;
        _countdownOverlay = new VisualElement();
        _countdownOverlay.style.position = Position.Absolute;
        _countdownOverlay.style.top = 0;
        _countdownOverlay.style.bottom = 0;
        _countdownOverlay.style.left = 0;
        _countdownOverlay.style.right = 0;
        _countdownOverlay.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        _countdownOverlay.style.alignItems = Align.Center;
        _countdownOverlay.style.justifyContent = Justify.Center;

        _countdownLabel = new Label("3");
        _countdownLabel.style.fontSize = 200;
        _countdownLabel.style.color = Color.white;
        _countdownLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _countdownLabel.style.unityTextOutlineWidth = 4;
        _countdownLabel.style.unityTextOutlineColor = new Color(0.5f, 0, 0.5f);

        _countdownOverlay.Add(_countdownLabel);
        uiDocument.rootVisualElement.Add(_countdownOverlay);
    }

    private void HandleCountdownTick(int tick)
    {
        if (_countdownLabel == null) return;
        if (tick == 0)
        {
            // UX-04: "GO!"
            _countdownLabel.text = "GO!";
            _countdownLabel.style.color = new Color(0f, 0.9f, 0.46f);
            _countdownLabel.style.fontSize = 150;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.countdownGoSound);
            UIAnimator.DOScale(_countdownLabel, new Vector2(0.5f, 0.5f), 0.5f).SetEase(Ease.InBack);
        }
        else
        {
            _countdownLabel.text = tick.ToString();
            _countdownLabel.style.color = Color.white;
            _countdownLabel.style.fontSize = 200;
            _countdownLabel.style.scale = new StyleScale(new Scale(new Vector2(1.5f, 1.5f)));
            _countdownLabel.style.opacity = 1f;
            // Scale animation: big → normal
            UIAnimator.DOScale(_countdownLabel, Vector2.one, 0.8f).SetEase(Ease.OutBack);
            // Play tick sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.countdownTickSound);
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

        // UX-02: Reset trạng thái đối thủ khi câu mới
        SetOpponentStatus("game_opp_thinking", "Đang suy nghĩ...");

        // UX-07: Swoosh sound khi câu mới xuất hiện
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.swooshSound);

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

        // UX-07: Tick-tock khi timer <= 5 giây
        if (remaining <= 5f && remaining > 0)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.timerUrgentSound);
        }

        if (_timerFill != null && TimerController.Instance != null)
        {
            float percent = (remaining / TimerController.Instance.TotalTime) * 100f;
            _timerFill.style.width = Length.Percent(percent);
        }
    }

    private void HandleOpponentLeft()
    {
        // UX-03: Hiện toast thông báo ngắn trước khi Result Popup xuất hiện
        string msg = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText("game_opponent_left", "Đối thủ đã rời trận — Bạn thắng!")
            : "Đối thủ đã rời trận — Bạn thắng!";
        ShowToast(msg, 2.5f);
        Debug.LogWarning("[GameplayUI] Đối thủ đã rời trận!");
    }

    /// <summary>UX-01: Hiển indicator ✅/❌ trên card P2 sau mỗi câu hỏi. UX-02: Cập nhật trạng thái đối thủ.</summary>
    private void HandleOpponentAnswerResult(bool isCorrect)
    {
        if (_p2Avatar == null) return;

        // UX-02: Cập nhật trạng thái "Đã trả lời!"
        SetOpponentStatus("game_opp_answered", "Đã trả lời!");

        // Tạo label indicator tạm thời chồng lên avatar P2
        var indicator = new Label(isCorrect ? "✅" : "❌");
        indicator.style.position = Position.Absolute;
        indicator.style.fontSize = 48;
        indicator.style.unityTextAlign = TextAnchor.MiddleCenter;
        // Đặt indicator phía dưới-phải avatar
        indicator.style.right = -10;
        indicator.style.bottom = -10;
        indicator.style.width = 60;
        indicator.style.height = 60;

        _p2Avatar.Add(indicator);
        StartCoroutine(RemoveAfterDelay(indicator, 2.0f));
    }

    private System.Collections.IEnumerator RemoveAfterDelay(VisualElement el, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (el != null && el.parent != null)
            el.RemoveFromHierarchy();
    }

    /// <summary>UX-03: Hiển toast message nổi trên màn hình, tự mất sau `duration` giây.</summary>
    private void ShowToast(string message, float duration = 2f)
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        var toast = new Label(message);
        toast.style.position = Position.Absolute;
        toast.style.bottom = 200;
        toast.style.left = 0;
        toast.style.right = 0;
        toast.style.unityTextAlign = TextAnchor.MiddleCenter;
        toast.style.fontSize = 32;
        toast.style.color = Color.white;
        toast.style.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
        toast.style.paddingTop = 20;
        toast.style.paddingBottom = 20;
        toast.style.paddingLeft = 30;
        toast.style.paddingRight = 30;
        toast.style.borderTopLeftRadius = 16;
        toast.style.borderTopRightRadius = 16;
        toast.style.borderBottomLeftRadius = 16;
        toast.style.borderBottomRightRadius = 16;
        toast.style.marginLeft = StyleKeyword.Auto;
        toast.style.marginRight = StyleKeyword.Auto;
        toast.style.maxWidth = new Length(80, LengthUnit.Percent);
        toast.style.whiteSpace = WhiteSpace.Normal;
        toast.style.unityFontStyleAndWeight = FontStyle.Bold;

        root.Add(toast);
        StartCoroutine(RemoveAfterDelay(toast, duration));
    }

    private void HandleGameOver()
    {
        // BUG-01 FIX: Nếu template chưa gán, thử tải từ Resources (bản Build có thể không gán SerializeField)
        if (resultPopupTemplate == null)
        {
            resultPopupTemplate = Resources.Load<VisualTreeAsset>("UI/ResultPopup");
        }

        if (resultPopupTemplate == null)
        {
            Debug.LogError("[GameplayUI] Không tìm thấy resultPopupTemplate! Kiểm tra Inspector hoặc Resources/UI/ResultPopup.");
            return;
        }

        if (_resultPopupInstance != null)
        {
            _resultPopupInstance.RemoveFromHierarchy();
            _resultPopupInstance = null;
        }

        _resultPopupInstance = resultPopupTemplate.Instantiate();
        uiDocument.rootVisualElement.Add(_resultPopupInstance);
        
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

        // BUG-08 FIX: Localize stat labels trong ResultPopup
        var yourScoreLbl = _resultPopupInstance.Q<Label>("your-score-label");
        if (yourScoreLbl != null)
            yourScoreLbl.text = L != null ? L.GetText("game_your_score", "Điểm của bạn") : "Điểm của bạn";

        var oppScoreLbl = _resultPopupInstance.Q<Label>("opp-score-label");
        if (oppScoreLbl != null)
            oppScoreLbl.text = L != null ? L.GetText("game_opp_score", "Điểm đối thủ") : "Điểm đối thủ";

        var rewardLbl = _resultPopupInstance.Q<Label>("reward-label");
        if (rewardLbl != null)
            rewardLbl.text = L != null ? L.GetText("game_reward", "Tiền thưởng") : "Tiền thưởng";

        var p1Final = _resultPopupInstance.Q<Label>("p1-score-final");
        if (p1Final != null) p1Final.text = ScoreManager.Instance.Player1Score.ToString();

        var p2Final = _resultPopupInstance.Q<Label>("p2-score-final");
        if (p2Final != null) p2Final.text = ScoreManager.Instance.Player2Score.ToString();

        // Reward thực tế từ ScoreManager
        var rewardLabel = _resultPopupInstance.Q<Label>("reward-amount");
        if (rewardLabel != null)
        {
            int money = ScoreManager.Instance.LastRewardMoney;
            rewardLabel.text = $"+${money:N0}";

            // FEAT-04: Nếu reward = 0 do đầu hàng, thêm label giải thích
            if (money == 0 && result == WinResult.Player2Wins)
            {
                var surrenderNote = new Label(
                    L != null
                        ? L.GetText("game_surrender_no_reward", "Đầu hàng — Không nhận được thưởng.")
                        : "Đầu hàng — Không nhận được thưởng."
                );
                surrenderNote.style.fontSize = 22;
                surrenderNote.style.color = new Color(0.7f, 0.3f, 0.3f);
                surrenderNote.style.unityTextAlign = TextAnchor.MiddleCenter;
                surrenderNote.style.marginTop = 6;
                rewardLabel.parent?.Add(surrenderNote);
            }
        }

        var playAgainBtn = _resultPopupInstance.Q<Button>("play-again-btn");
        if (playAgainBtn != null)
        {
            if (L != null) playAgainBtn.text = L.GetText("game_play_again");

            // BUG-04: Unregister handler cũ trước khi đăng ký mới
            if (_playAgainHandler != null) playAgainBtn.clicked -= _playAgainHandler;

            // Online mode: "Chơi lại" có nghĩa là VỀ HOME để tìm trận mới (không thể restart room)
            bool isOnline = FirebaseManager.Instance != null
                            && !FirebaseManager.Instance.isOfflineMode
                            && !string.IsNullOrEmpty(FirebaseManager.Instance.CurrentRoomId);

            if (isOnline)
            {
                _playAgainHandler = () => {
                    FirebaseManager.Instance.LeaveRoom();
                    if (GameManager.Instance != null) GameManager.Instance.LoadHomeScene();
                };
            }
            else
            {
                _playAgainHandler = () => {
                    if (_resultPopupInstance != null)
                    {
                        _resultPopupInstance.RemoveFromHierarchy();
                        _resultPopupInstance = null;
                    }
                    if (GameController.Instance != null) GameController.Instance.RestartGame();
                };
            }
            playAgainBtn.clicked += _playAgainHandler;
        }

        var backHomeBtn = _resultPopupInstance.Q<Button>("back-home-btn");
        if (backHomeBtn != null)
        {
            if (L != null) backHomeBtn.text = L.GetText("game_back_home");

            // BUG-04: Unregister handler cũ
            if (_backHomeHandler != null) backHomeBtn.clicked -= _backHomeHandler;
            _backHomeHandler = () => {
                if (FirebaseManager.Instance != null) FirebaseManager.Instance.LeaveRoom();
                if (GameManager.Instance != null) GameManager.Instance.LoadHomeScene();
                else UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
            };
            backHomeBtn.clicked += _backHomeHandler;
        }
    }
 
    private void ShowExitConfirmation()
    {
        if (exitPopupTemplate == null) return;
        if (_exitPopupInstance != null) return;
 
        _exitPopupInstance = exitPopupTemplate.Instantiate();
        uiDocument.rootVisualElement.Add(_exitPopupInstance);
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
 
    // UX-08: Android back button — show exit confirmation
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Nếu đang có popup, đóng popup trước
            if (_exitPopupInstance != null)
            {
                // Do nothing — exit popup đã hiển thị, user có thể bấm cancel
                return;
            }
            if (_resultPopupInstance != null)
            {
                // Result popup đang hiển thị — không xử lý
                return;
            }
            ShowExitConfirmation();
        }
    }

    private void LocalizeHUD()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        if (root == null) return;
        if (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsReady) return;

        var L = LocalizationManager.Instance;

        // UX-02 FIX: Dùng DisplayName thay vì UID Firebase
        if (FirebaseManager.Instance != null)
        {
            string myName = FirebaseManager.Instance.LocalDisplayName
                ?? (FirebaseManager.Instance.IsAuthenticated
                    ? FirebaseManager.Instance.LocalUserId
                    : "PLAYER");
                
            string oppName = FirebaseManager.Instance.OpponentName
                ?? (!string.IsNullOrEmpty(FirebaseManager.Instance.OpponentId)
                    ? FirebaseManager.Instance.OpponentId
                    : "BOT");

            // Chỉ cắt ngắn nếu là UID (có dấu gạch dưới hoặc quá dài), DisplayName thì hiển nguyên
            if (myName != null && myName.Length > 12 && !myName.Contains(" "))
                myName = myName.Substring(0, 12);
            if (oppName != null && oppName.Length > 12 && !oppName.Contains(" "))
                oppName = oppName.Substring(0, 12);

            if (_p1Label != null) _p1Label.text = myName;
            if (_p2Label != null) _p2Label.text = oppName;
 
            // Hiển thị Avatar (Initial Letter thay vì sprite)
            if (_p1Avatar != null)
                AvatarHelper.SetAvatar(_p1Avatar, myName);
            if (_p2Avatar != null)
                AvatarHelper.SetAvatar(_p2Avatar, oppName);
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

    // UX-03: Turn summary — hiển thị overlay kết quả câu vừa rồi
    private void HandleTurnSummary(bool p1Correct, bool p2Correct, int p1Score, int p2Score, bool isLast)
    {
        if (uiDocument == null) return;
        if (_isShowingTurnSummary) return;
        _isShowingTurnSummary = true;

        var root = uiDocument.rootVisualElement;

        var summary = new VisualElement();
        summary.name = "turn-summary-overlay";
        summary.style.position = Position.Absolute;
        summary.style.top = 0;
        summary.style.bottom = 0;
        summary.style.left = 0;
        summary.style.right = 0;
        summary.style.backgroundColor = new Color(0, 0, 0, 0.6f);
        summary.style.alignItems = Align.Center;
        summary.style.justifyContent = Justify.Center;
        summary.style.opacity = 0f;

        var container = new VisualElement();
        container.style.backgroundColor = new Color(0.1f, 0.02f, 0.15f, 0.95f);
        container.style.borderTopLeftRadius = 24;
        container.style.borderTopRightRadius = 24;
        container.style.borderBottomLeftRadius = 24;
        container.style.borderBottomRightRadius = 24;
        container.style.paddingTop = 30;
        container.style.paddingBottom = 30;
        container.style.paddingLeft = 50;
        container.style.paddingRight = 50;
        container.style.alignItems = Align.Center;
        container.style.minWidth = 400;

        var L = LocalizationManager.Instance;

        // P1 result
        string p1Icon = p1Correct ? "✅" : "❌";
        string p1Text = p1Correct
            ? (L != null ? L.GetText("game_turn_correct", "Đúng") : "Đúng")
            : (L != null ? L.GetText("game_turn_wrong", "Sai") : "Sai");
        var p1Label = new Label($"Bạn: {p1Icon} {p1Text}");
        p1Label.style.fontSize = 40;
        p1Label.style.color = p1Correct ? new Color(0f, 0.9f, 0.46f) : new Color(1f, 0.32f, 0.32f);
        p1Label.style.unityFontStyleAndWeight = FontStyle.Bold;
        p1Label.style.marginBottom = 10;
        container.Add(p1Label);

        // P2 result
        string p2Icon = p2Correct ? "✅" : "❌";
        string p2Text = p2Correct
            ? (L != null ? L.GetText("game_turn_correct", "Đúng") : "Đúng")
            : (L != null ? L.GetText("game_turn_wrong", "Sai") : "Sai");
        var p2Label = new Label($"Đối thủ: {p2Icon} {p2Text}");
        p2Label.style.fontSize = 40;
        p2Label.style.color = p2Correct ? new Color(0f, 0.9f, 0.46f) : new Color(1f, 0.32f, 0.32f);
        p2Label.style.unityFontStyleAndWeight = FontStyle.Bold;
        p2Label.style.marginBottom = 20;
        container.Add(p2Label);

        // Score line
        var scoreLabel = new Label($"{p1Score} — {p2Score}");
        scoreLabel.style.fontSize = 50;
        scoreLabel.style.color = Color.white;
        scoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        scoreLabel.style.marginTop = 10;
        container.Add(scoreLabel);

        summary.Add(container);
        root.Add(summary);

        // Fade in
        UIAnimator.DOFade(summary, 1f, 0.2f);

        // Auto remove — khớp với WaitForSeconds(1.0f) trong GameController.RevealAndAdvance
        StartCoroutine(RemoveTurnSummaryAfter(summary, 0.8f));
    }

    private IEnumerator RemoveTurnSummaryAfter(VisualElement el, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (el != null && el.parent != null)
        {
            UIAnimator.DOFade(el, 0f, 0.2f);
            yield return new WaitForSeconds(0.2f);
            el.RemoveFromHierarchy();
            _isShowingTurnSummary = false;
        }
    }

    // UX-01: Streak counter — hiển thị toast "2x Streak!" khi correct streak >= 2
    private void HandleStreakChanged(int streak)
    {
        if (streak >= 2)
        {
            string msg = $"{streak}x Streak!";
            ShowToast(msg, 1.5f);
        }
    }

    // UX-02: Set trạng thái đối thủ real-time
    public void SetOpponentStatus(string statusKey, string fallback)
    {
        if (_p2StatusLabel == null) return;
        string text = LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady
            ? LocalizationManager.Instance.GetText(statusKey, fallback)
            : fallback;
        _p2StatusLabel.text = text;

        // Animation fade-in
        _p2StatusLabel.style.opacity = 0f;
        UIAnimator.DOFade(_p2StatusLabel, 1f, 0.3f);
    }
}
