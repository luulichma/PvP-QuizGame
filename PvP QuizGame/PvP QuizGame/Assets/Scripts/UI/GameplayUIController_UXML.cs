using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Linq;
using DG.Tweening;

/// <summary>
/// Điều phối giao diện người dùng TRONG trận đấu (GameplayScene) sử dụng UI Toolkit.
/// Online mode: hiển thị displayName của 2 người chơi.
/// NÂNG CẤP: Modern Glassmorphism + Particle Effects + Haptic Feedback + Animated Timer.
/// </summary>
public class GameplayUIController_UXML : MonoBehaviour
{
    public static GameplayUIController_UXML Instance { get; private set; }

    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;

    [SerializeField] private VisualTreeAsset resultPopupTemplate;
    [SerializeField] private VisualTreeAsset exitPopupTemplate;
    [SerializeField] private VisualTreeAsset settingsPopupTemplate;

    private Label _p1ScoreLabel;
    private Label _p2ScoreLabel;
    private Label _p1Label;
    private Label _p2Label;
    private Label _questionText;
    private Label _questionCounter;
    private Label _timerText;
    private VisualElement _timerFill;
    private VisualElement _timerContainer;
    private VisualElement _p1Avatar;
    private VisualElement _p2Avatar;
    private VisualElement _p1Info;
    private VisualElement _p2Info;
    private Label _p2StatusLabel;

    // Particle layer
    private VisualElement _particleLayer;

    private VisualElement _resultPopupInstance;
    private VisualElement _exitPopupInstance;
    private VisualElement _settingsPopupInstance;

    // BUG-04: Lưu reference để unsubscribe
    private Action _playAgainHandler;
    private Action _backHomeHandler;

    // UX-03: Turn summary state
    private bool _isShowingTurnSummary = false;

    // UX-04: Countdown overlay
    private VisualElement _countdownOverlay;
    private Label _countdownLabel;

    // Score tracking for animation
    private int _lastP1Score = 0;
    private int _lastP2Score = 0;

    // Timer urgent state
    private bool _timerIsUrgent = false;

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
        _timerContainer = root.Q<VisualElement>("timer-container");
        _p1Avatar = root.Q<VisualElement>("p1-avatar");
        _p2Avatar = root.Q<VisualElement>("p2-avatar");
        _p1Info = root.Q<VisualElement>("p1-info");
        _p2Info = root.Q<VisualElement>("p2-info");
        _p2StatusLabel = root.Q<Label>("p2-status");
        _particleLayer = root.Q<VisualElement>("particle-layer");

        var settingsBtn = root.Q<Button>("settings-btn");
        if (settingsBtn != null) settingsBtn.clicked += ShowSettingsPopup;

        GameController.OnGameStateChanged  += HandleGameStateChanged;
        QuizManager.OnQuestionChanged      += HandleQuestionChanged;
        ScoreManager.OnScoreChanged        += HandleScoreChanged;
        TimerController.OnTimerTick        += HandleTimerTick;
        GameController.OnGameOver          += HandleGameOver;
        GameController.OnOpponentLeft      += HandleOpponentLeft;
        GameController.OnOpponentAnswerResult += HandleOpponentAnswerResult;
        GameController.OnCountdownTick     += HandleCountdownTick;
        GameController.OnTurnSummary       += HandleTurnSummary;
        ScoreManager.OnStreakChanged       += HandleStreakChanged;

        LocalizationManager.OnLanguageChanged += LocalizeHUD;
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
            LocalizeHUD();

        // Animate HUD entry
        AnimateHUDEntry(root);
    }

    private void OnDisable()
    {
        GameController.OnGameStateChanged  -= HandleGameStateChanged;
        QuizManager.OnQuestionChanged      -= HandleQuestionChanged;
        ScoreManager.OnScoreChanged        -= HandleScoreChanged;
        TimerController.OnTimerTick        -= HandleTimerTick;
        GameController.OnGameOver          -= HandleGameOver;
        GameController.OnOpponentLeft      -= HandleOpponentLeft;
        GameController.OnOpponentAnswerResult -= HandleOpponentAnswerResult;
        GameController.OnCountdownTick     -= HandleCountdownTick;
        GameController.OnTurnSummary       -= HandleTurnSummary;
        ScoreManager.OnStreakChanged       -= HandleStreakChanged;

        LocalizationManager.OnLanguageChanged -= LocalizeHUD;

        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            if (root != null)
            {
                var settingsBtn = root.Q<Button>("settings-btn");
                if (settingsBtn != null) settingsBtn.clicked -= ShowSettingsPopup;
            }
        }
    }

    /// <summary>Animate HUD elements khi scene mở.</summary>
    private void AnimateHUDEntry(VisualElement root)
    {
        if (_p1Info != null) UIAnimator.DOSlideFromLeft(_p1Info, 0.5f, 80f);
        if (_p2Info != null) UIAnimator.DOSlideFromRight(_p2Info, 0.5f, 80f);
        if (_timerContainer != null) UIAnimator.DOBounceIn(_timerContainer, 0.6f);
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Countdown:
                CreateCountdownOverlay();
                break;
            case GameState.Playing:
                _lastP1Score = 0;
                _lastP2Score = 0;
                UpdateScoreUI(0, 0);
                _timerIsUrgent = false;
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

    // ==================== COUNTDOWN ====================

    private void CreateCountdownOverlay()
    {
        if (uiDocument == null) return;
        _countdownOverlay = new VisualElement();
        _countdownOverlay.AddToClassList("countdown-overlay");

        _countdownLabel = new Label("3");
        _countdownLabel.AddToClassList("countdown-number");

        _countdownOverlay.Add(_countdownLabel);
        uiDocument.rootVisualElement.Add(_countdownOverlay);
    }

    private void HandleCountdownTick(int tick)
    {
        if (_countdownLabel == null) return;

        // Haptic mỗi tick
        HapticFeedback.CountdownTick();

        if (tick == 0)
        {
            _countdownLabel.text = "GO!";
            _countdownLabel.RemoveFromClassList("countdown-number");
            _countdownLabel.AddToClassList("countdown-go");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.countdownGoSound);
            UIAnimator.DOScale(_countdownLabel, new Vector2(0.5f, 0.5f), 0.5f).SetEase(Ease.InBack);
            UIAnimator.DOFade(_countdownLabel, 0f, 0.5f);
        }
        else
        {
            _countdownLabel.text = tick.ToString();
            // Pop animation
            UIAnimator.DOCountdownPop(_countdownLabel, 0.8f);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.countdownTickSound);
        }
    }

    // ==================== QUESTION ====================

    private void HandleQuestionChanged(QuestionData question)
    {
        if (question == null) return;

        if (_questionText != null)
        {
            string qKey = question.questionText;
            _questionText.text = LocalizationManager.Instance != null
                                 ? LocalizationManager.Instance.GetText(qKey)
                                 : qKey;

            // Animation: slide down + fade in cho question card
            var questionCard = _questionText.parent;
            if (questionCard != null)
            {
                questionCard.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(-40)));
                questionCard.style.opacity = 0f;
                questionCard.style.scale = new StyleScale(new Scale(new Vector2(0.95f, 0.95f)));
                UIAnimator.DOFade(questionCard, 1f, 0.25f);
                UIAnimator.DOTranslate(questionCard, Vector2.zero, 0.35f).SetEase(Ease.OutBack);
                UIAnimator.DOScale(questionCard, Vector2.one, 0.35f).SetEase(Ease.OutBack);
            }
        }

        // Reset trạng thái đối thủ
        SetOpponentStatus("game_opp_thinking", "Đang suy nghĩ...");

        // Reset timer urgent state
        _timerIsUrgent = false;
        if (_timerFill != null)
        {
            var cyanColor = new Color(0f, 0.90f, 1f);
            _timerFill.style.borderTopColor = cyanColor;
            _timerFill.style.borderBottomColor = cyanColor;
            _timerFill.style.borderLeftColor = cyanColor;
            _timerFill.style.borderRightColor = cyanColor;
        }

        // Swoosh sound
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

    // ==================== SCORE ====================

    private void HandleScoreChanged(int p1Score, int p2Score)
    {
        // Animated score counter
        if (_p1ScoreLabel != null && p1Score != _lastP1Score)
        {
            UIAnimator.DOCountTo(_p1ScoreLabel, _lastP1Score, p1Score, 0.4f);
            // Pulse player card khi score tăng
            if (p1Score > _lastP1Score && _p1Info != null)
            {
                UIAnimator.DOScale(_p1Info, new Vector2(1.05f, 1.05f), 0.15f).SetEase(Ease.OutBack)
                    .OnComplete(() => UIAnimator.DOScale(_p1Info, Vector2.one, 0.15f));
            }
        }

        if (_p2ScoreLabel != null && p2Score != _lastP2Score)
        {
            UIAnimator.DOCountTo(_p2ScoreLabel, _lastP2Score, p2Score, 0.4f);
            if (p2Score > _lastP2Score && _p2Info != null)
            {
                UIAnimator.DOScale(_p2Info, new Vector2(1.05f, 1.05f), 0.15f).SetEase(Ease.OutBack)
                    .OnComplete(() => UIAnimator.DOScale(_p2Info, Vector2.one, 0.15f));
            }
        }

        _lastP1Score = p1Score;
        _lastP2Score = p2Score;
    }

    // ==================== TIMER ====================

    private void HandleTimerTick(float remaining)
    {
        if (_timerText != null)
        {
            _timerText.text = TimerController.Instance != null
                ? TimerController.Instance.GetFormattedTime()
                : $"{Mathf.CeilToInt(remaining)}s";
        }

        // Animated timer ring
        if (remaining <= 5f && !_timerIsUrgent)
        {
            _timerIsUrgent = true;
            _timerText.style.color = new Color(1f, 0.32f, 0.32f);
            if (_timerFill != null)
            {
                var redColor = new Color(1f, 0.32f, 0.32f);
                _timerFill.style.borderTopColor = redColor;
                _timerFill.style.borderBottomColor = redColor;
                _timerFill.style.borderLeftColor = redColor;
                _timerFill.style.borderRightColor = redColor;
            }
            // Pulse glow trên timer container
            if (_timerContainer != null)
                UIAnimator.DOPulseGlow(_timerContainer, new Color(1f, 0.32f, 0.32f, 0.6f), 0.5f, 10);
        }
        else if (remaining > 5f)
        {
            _timerIsUrgent = false;
            if (_timerText != null) _timerText.style.color = Color.white;
            if (_timerFill != null)
            {
                var cyanColor2 = new Color(0f, 0.90f, 1f);
                _timerFill.style.borderTopColor = cyanColor2;
                _timerFill.style.borderBottomColor = cyanColor2;
                _timerFill.style.borderLeftColor = cyanColor2;
                _timerFill.style.borderRightColor = cyanColor2;
            }
        }

        // Urgent tick sound + haptic
        if (remaining <= 5f && remaining > 0)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.timerUrgentSound);
            HapticFeedback.CountdownTick();
        }

        if (_timerFill != null && TimerController.Instance != null)
        {
            float percent = (remaining / TimerController.Instance.TotalTime) * 100f;
            _timerFill.style.width = Length.Percent(percent);
        }
    }

    // ==================== OPPONENT ====================

    private void HandleOpponentLeft()
    {
        string msg = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText("game_opponent_left", "Đối thủ đã rời trận — Bạn thắng!")
            : "Đối thủ đã rời trận — Bạn thắng!";
        ShowToast(msg, 2.5f);
        HapticFeedback.Medium();
        Debug.LogWarning("[GameplayUI] Đối thủ đã rời trận!");
    }

    private void HandleOpponentAnswerResult(bool isCorrect)
    {
        if (_p2Avatar == null) return;
        SetOpponentStatus("game_opp_answered", "Đã trả lời!");

        var indicator = new Label(isCorrect ? "✅" : "❌");
        indicator.style.position = Position.Absolute;
        indicator.style.fontSize = 40;
        indicator.style.unityTextAlign = TextAnchor.MiddleCenter;
        indicator.style.right = -8;
        indicator.style.bottom = -8;
        indicator.style.width = 50;
        indicator.style.height = 50;
        indicator.style.opacity = 0f;

        _p2Avatar.Add(indicator);
        // Bounce in animation
        UIAnimator.DOBounceIn(indicator, 0.4f);
        StartCoroutine(RemoveAfterDelay(indicator, 2.0f));
    }

    private System.Collections.IEnumerator RemoveAfterDelay(VisualElement el, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (el != null && el.parent != null)
        {
            UIAnimator.DOFade(el, 0f, 0.2f);
            yield return new WaitForSeconds(0.2f);
            if (el.parent != null) el.RemoveFromHierarchy();
        }
    }

    // ==================== TOAST ====================

    private void ShowToast(string message, float duration = 2f)
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        var toast = new Label(message);
        toast.AddToClassList("toast");
        toast.style.opacity = 0f;

        root.Add(toast);

        // Slide up + fade in
        toast.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(20)));
        UIAnimator.DOFade(toast, 1f, 0.2f);
        UIAnimator.DOTranslate(toast, Vector2.zero, 0.3f).SetEase(Ease.OutCubic);

        StartCoroutine(RemoveToastAfter(toast, duration));
    }

    private IEnumerator RemoveToastAfter(VisualElement el, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (el != null && el.parent != null)
        {
            UIAnimator.DOFade(el, 0f, 0.3f);
            UIAnimator.DOTranslate(el, new Vector2(0, -20), 0.3f);
            yield return new WaitForSeconds(0.3f);
            if (el.parent != null) el.RemoveFromHierarchy();
        }
    }

    // ==================== GAME OVER ====================

    private void HandleGameOver()
    {
        if (resultPopupTemplate == null)
            resultPopupTemplate = Resources.Load<VisualTreeAsset>("UI/ResultPopup");

        if (resultPopupTemplate == null)
        {
            Debug.LogError("[GameplayUI] Không tìm thấy resultPopupTemplate!");
            return;
        }

        if (_resultPopupInstance != null)
        {
            _resultPopupInstance.RemoveFromHierarchy();
            _resultPopupInstance = null;
        }

        _resultPopupInstance = resultPopupTemplate.Instantiate();
        uiDocument.rootVisualElement.Add(_resultPopupInstance);
        _resultPopupInstance.style.position = Position.Absolute;
        _resultPopupInstance.style.top = 0;
        _resultPopupInstance.style.bottom = 0;
        _resultPopupInstance.style.left = 0;
        _resultPopupInstance.style.right = 0;

        // ANIMATION: Result Popup
        var overlay = _resultPopupInstance.Q<VisualElement>("result-overlay") ?? _resultPopupInstance.Children().First();
        var popupCard = _resultPopupInstance.Q<VisualElement>("result-container") ?? overlay.Children().First();
        UIAnimator.ShowPopupAnim(overlay, popupCard);

        if (ScoreManager.Instance == null) return;

        WinResult result = ScoreManager.Instance.GetWinner();

        // Haptic feedback dựa theo kết quả
        if (result == WinResult.Player1Wins)
            HapticFeedback.Heavy();
        else if (result == WinResult.Player2Wins)
            HapticFeedback.Medium();
        else
            HapticFeedback.Light();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayResultSound(result == WinResult.Player1Wins);

        // CONFETTI khi thắng!
        if (result == WinResult.Player1Wins)
        {
            var confettiLayer = _resultPopupInstance.Q<VisualElement>("result-particle-layer");
            if (confettiLayer != null && UIParticleEffect.Instance != null)
            {
                UIParticleEffect.Instance.SpawnConfetti(confettiLayer, 50, 3f);
            }
        }

        var L = LocalizationManager.Instance;

        // Trophy icon
        var trophyIcon = _resultPopupInstance.Q<Label>("trophy-icon");
        if (trophyIcon != null)
        {
            string icon = result switch
            {
                WinResult.Player1Wins => "🏆",
                WinResult.Player2Wins => "😢",
                WinResult.Draw        => "🤝",
                _ => "⭐"
            };
            trophyIcon.text = icon;
            trophyIcon.style.fontSize = 52;
            // Bounce in trophy
            var trophyArea = _resultPopupInstance.Q<VisualElement>("trophy-area");
            if (trophyArea != null)
                UIAnimator.DOBounceIn(trophyArea, 0.6f);
        }

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
                WinResult.Draw        => new Color(1f, 0.84f, 0.28f),
                _ => Color.white
            };
        }

        // Localize stat labels
        var yourScoreLbl = _resultPopupInstance.Q<Label>("your-score-label");
        if (yourScoreLbl != null)
            yourScoreLbl.text = L != null ? L.GetText("game_your_score", "Bạn") : "Bạn";

        var oppScoreLbl = _resultPopupInstance.Q<Label>("opp-score-label");
        if (oppScoreLbl != null)
            oppScoreLbl.text = L != null ? L.GetText("game_opp_score", "Đối thủ") : "Đối thủ";

        var rewardLbl = _resultPopupInstance.Q<Label>("reward-label");
        if (rewardLbl != null)
            rewardLbl.text = L != null ? L.GetText("game_reward", "Thưởng") : "Thưởng";

        // Animated score counter
        var p1Final = _resultPopupInstance.Q<Label>("p1-score-final");
        if (p1Final != null)
            UIAnimator.DOCountTo(p1Final, 0, ScoreManager.Instance.Player1Score, 0.8f);

        var p2Final = _resultPopupInstance.Q<Label>("p2-score-final");
        if (p2Final != null)
            UIAnimator.DOCountTo(p2Final, 0, ScoreManager.Instance.Player2Score, 0.8f);

        // Reward — hiển thị cả XP và tiền
        var rewardAmount = _resultPopupInstance.Q<Label>("reward-amount");
        if (rewardAmount != null)
        {
            int money = ScoreManager.Instance.LastRewardMoney;
            int xp = ScoreManager.Instance.LastRewardExp;
            var rewardParent = rewardAmount.parent;

            bool isSurrender = (money == 0 && xp == 0 && result == WinResult.Player2Wins);

            if (isSurrender)
            {
                // Đầu hàng — 0 tiền, 0 XP
                rewardAmount.text = "$0";
                rewardAmount.style.color = new Color(1f, 0.32f, 0.32f, 0.7f);

                var surrenderNote = new Label(
                    L != null
                        ? L.GetText("game_surrender_no_reward", "Đầu hàng — Không nhận được thưởng.")
                        : "Đầu hàng — Không nhận được thưởng."
                );
                surrenderNote.style.fontSize = 20;
                surrenderNote.style.color = new Color(1f, 0.32f, 0.32f, 0.7f);
                surrenderNote.style.unityTextAlign = TextAnchor.MiddleCenter;
                surrenderNote.style.marginTop = 6;
                rewardParent?.Add(surrenderNote);
            }
            else
            {
                // Hiển thị tiền
                rewardAmount.text = $"+${money:N0}";
                rewardAmount.style.color = new Color(1f, 0.84f, 0.28f); // Gold

                // Hiển thị XP bên dưới
                var xpReward = new Label($"+{xp} XP");
                xpReward.style.fontSize = 28;
                xpReward.style.color = new Color(0f, 0.90f, 1f); // Cyan
                xpReward.style.unityFontStyleAndWeight = FontStyle.Bold;
                xpReward.style.unityTextAlign = TextAnchor.MiddleCenter;
                xpReward.style.marginTop = 4;
                rewardParent?.Add(xpReward);
            }
        }

        var playAgainBtn = _resultPopupInstance.Q<Button>("play-again-btn");
        if (playAgainBtn != null)
        {
            if (L != null) playAgainBtn.text = L.GetText("game_play_again");
            if (_playAgainHandler != null) playAgainBtn.clicked -= _playAgainHandler;

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
            if (_backHomeHandler != null) backHomeBtn.clicked -= _backHomeHandler;
            _backHomeHandler = () => {
                if (FirebaseManager.Instance != null) FirebaseManager.Instance.LeaveRoom();
                if (GameManager.Instance != null) GameManager.Instance.LoadHomeScene();
                else UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
            };
            backHomeBtn.clicked += _backHomeHandler;
        }
    }

    // ==================== EXIT CONFIRMATION ====================

    private void ShowSettingsPopup()
    {
        if (settingsPopupTemplate == null) return;
        if (_settingsPopupInstance != null) return;

        _settingsPopupInstance = settingsPopupTemplate.Instantiate();
        uiDocument.rootVisualElement.Add(_settingsPopupInstance);
        _settingsPopupInstance.style.position = Position.Absolute;
        _settingsPopupInstance.style.top = 0;
        _settingsPopupInstance.style.bottom = 0;
        _settingsPopupInstance.style.left = 0;
        _settingsPopupInstance.style.right = 0;

        var overlay = _settingsPopupInstance.Q<VisualElement>("overlay") ?? _settingsPopupInstance.Children().First();
        var popupCard = _settingsPopupInstance.Q<VisualElement>("popup") ?? overlay.Children().First();
        UIAnimator.ShowPopupAnim(overlay, popupCard);

        // Localization
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
        {
            var L = LocalizationManager.Instance;
            var title = _settingsPopupInstance.Q<Label>("settings-title");
            var musicLbl = _settingsPopupInstance.Q<Label>("music-label");
            var sfxLbl = _settingsPopupInstance.Q<Label>("sfx-label");
            var quitBtn = _settingsPopupInstance.Q<Button>("quit-game-btn");
            var cancelBtn = _settingsPopupInstance.Q<Button>("cancel-btn");

            if (title != null) title.text = L.GetText("settings_title", "CÀI ĐẶT");
            if (musicLbl != null) musicLbl.text = L.GetText("settings_music", "Âm nhạc");
            if (sfxLbl != null) sfxLbl.text = L.GetText("settings_sfx", "Hiệu ứng");
            if (quitBtn != null) quitBtn.text = L.GetText("game_quit_game", "THOÁT GAME");
            if (cancelBtn != null) cancelBtn.text = L.GetText("menu_cancel", "HỦY");
        }

        // Toggles Âm thanh
        var musicToggle = _settingsPopupInstance.Q<Toggle>("music-toggle");
        var sfxToggle = _settingsPopupInstance.Q<Toggle>("sfx-toggle");

        if (AudioManager.Instance != null)
        {
            if (musicToggle != null)
            {
                musicToggle.value = AudioManager.Instance.IsMusicEnabled;
                musicToggle.RegisterValueChangedCallback(evt => {
                    AudioManager.Instance.SetMusicEnabled(evt.newValue);
                });
            }

            if (sfxToggle != null)
            {
                sfxToggle.value = AudioManager.Instance.IsSFXEnabled;
                sfxToggle.RegisterValueChangedCallback(evt => {
                    AudioManager.Instance.SetSFXEnabled(evt.newValue);
                });
            }
        }

        // Thoát game button
        var quitGameBtn = _settingsPopupInstance.Q<Button>("quit-game-btn");
        if (quitGameBtn != null)
        {
            quitGameBtn.clicked += () => {
                UIAnimator.HidePopupAnim(overlay, popupCard, () => {
                    _settingsPopupInstance.RemoveFromHierarchy();
                    _settingsPopupInstance = null;
                    ShowExitConfirmation();
                });
            };
        }

        // Hủy button
        var cancelSettingsBtn = _settingsPopupInstance.Q<Button>("cancel-btn");
        if (cancelSettingsBtn != null)
        {
            cancelSettingsBtn.clicked += () => {
                UIAnimator.HidePopupAnim(overlay, popupCard, () => {
                    _settingsPopupInstance.RemoveFromHierarchy();
                    _settingsPopupInstance = null;
                });
            };
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

        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
        {
            var L = LocalizationManager.Instance;
            var title = _exitPopupInstance.Q<Label>("confirm-title");
            var msg = _exitPopupInstance.Q<Label>("confirm-msg");
            if (title != null) title.text = L.GetText("game_exit_title", "BỎ CUỘC?");
            if (msg != null) msg.text = L.GetText("game_exit_msg", "Nếu thoát bây giờ, bạn sẽ bị xử THUA ngay lập tức.");
        }

        HapticFeedback.Light();

        var confirmBtn = _exitPopupInstance.Q<Button>("confirm-btn");
        if (confirmBtn != null)
        {
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
                confirmBtn.text = LocalizationManager.Instance.GetText("game_exit_confirm", "XÁC NHẬN THOÁT");

            confirmBtn.clicked += () => {
                HapticFeedback.Heavy();
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
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
                cancelBtn.text = LocalizationManager.Instance.GetText("menu_cancel", "HỦY");

            cancelBtn.clicked += () => {
                UIAnimator.HidePopupAnim(overlay, popupCard, () => {
                    _exitPopupInstance.RemoveFromHierarchy();
                    _exitPopupInstance = null;
                });
            };
        }
    }

    // UX-08: Android back button
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_exitPopupInstance != null) return;
            if (_resultPopupInstance != null) return;
            ShowExitConfirmation();
        }
    }

    // ==================== LOCALIZATION ====================

    private void LocalizeHUD()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        if (root == null) return;
        if (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsReady) return;

        var L = LocalizationManager.Instance;

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

            if (myName != null && myName.Length > 12 && !myName.Contains(" "))
                myName = myName.Substring(0, 12);
            if (oppName != null && oppName.Length > 12 && !oppName.Contains(" "))
                oppName = oppName.Substring(0, 12);

            if (_p1Label != null) _p1Label.text = myName;
            if (_p2Label != null) _p2Label.text = oppName;

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

    // ==================== TURN SUMMARY ====================

    private void HandleTurnSummary(bool p1Correct, bool p2Correct, int p1Score, int p2Score, bool isLast)
    {
        if (uiDocument == null) return;
        if (_isShowingTurnSummary) return;
        _isShowingTurnSummary = true;

        var root = uiDocument.rootVisualElement;

        var summary = new VisualElement();
        summary.name = "turn-summary-overlay";
        summary.AddToClassList("countdown-overlay");
        summary.style.opacity = 0f;

        var container = new VisualElement();
        container.AddToClassList("turn-summary-card");

        var L = LocalizationManager.Instance;

        // P1 result
        string p1Icon = p1Correct ? "✅" : "❌";
        string p1Text = p1Correct
            ? (L != null ? L.GetText("game_turn_correct", "Đúng") : "Đúng")
            : (L != null ? L.GetText("game_turn_wrong", "Sai") : "Sai");
        var p1Label = new Label($"Bạn: {p1Icon} {p1Text}");
        p1Label.style.fontSize = 36;
        p1Label.style.color = p1Correct ? new Color(0f, 0.9f, 0.46f) : new Color(1f, 0.32f, 0.32f);
        p1Label.style.unityFontStyleAndWeight = FontStyle.Bold;
        p1Label.style.marginBottom = 8;
        container.Add(p1Label);

        // P2 result
        string p2Icon = p2Correct ? "✅" : "❌";
        string p2Text = p2Correct
            ? (L != null ? L.GetText("game_turn_correct", "Đúng") : "Đúng")
            : (L != null ? L.GetText("game_turn_wrong", "Sai") : "Sai");
        var p2LabelEl = new Label($"Đối thủ: {p2Icon} {p2Text}");
        p2LabelEl.style.fontSize = 36;
        p2LabelEl.style.color = p2Correct ? new Color(0f, 0.9f, 0.46f) : new Color(1f, 0.32f, 0.32f);
        p2LabelEl.style.unityFontStyleAndWeight = FontStyle.Bold;
        p2LabelEl.style.marginBottom = 16;
        container.Add(p2LabelEl);

        // Divider
        var divider = new VisualElement();
        divider.AddToClassList("glass-separator");
        container.Add(divider);

        // Score line
        var scoreLabel = new Label($"{p1Score} — {p2Score}");
        scoreLabel.style.fontSize = 46;
        scoreLabel.style.color = Color.white;
        scoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        scoreLabel.style.marginTop = 12;
        scoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        container.Add(scoreLabel);

        summary.Add(container);
        root.Add(summary);

        // Fade in + scale
        UIAnimator.ShowPopupAnim(summary, container);

        StartCoroutine(RemoveTurnSummaryAfter(summary, 2.5f));
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

    // ==================== STREAK ====================

    private void HandleStreakChanged(int streak)
    {
        if (streak >= 2)
        {
            string msg = $"🔥 {streak}x Streak!";
            ShowStreakToast(msg);
            HapticFeedback.Streak(streak);

            // Sparkle effect trên particle layer
            if (_particleLayer != null && UIParticleEffect.Instance != null)
            {
                UIParticleEffect.Instance.SpawnSparkle(_particleLayer, 50f, 50f, 8 + streak * 2);
            }
        }
    }

    private void ShowStreakToast(string message)
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        var badge = new Label(message);
        badge.style.position = Position.Absolute;
        badge.style.top = Length.Percent(35);
        badge.style.left = 0;
        badge.style.right = 0;
        badge.style.unityTextAlign = TextAnchor.MiddleCenter;
        badge.style.fontSize = 44;
        badge.style.color = new Color(1f, 0.84f, 0.28f);
        badge.style.unityFontStyleAndWeight = FontStyle.Bold;
        badge.style.unityTextOutlineWidth = 2;
        badge.style.unityTextOutlineColor = new Color(0f, 0f, 0f, 0.5f);
        badge.pickingMode = PickingMode.Ignore;

        root.Add(badge);

        // Streak flash animation
        UIAnimator.DOStreakFlash(badge, 1.2f).OnComplete(() => {
            if (badge.parent != null) badge.RemoveFromHierarchy();
        });
    }

    // ==================== OPPONENT STATUS ====================

    public void SetOpponentStatus(string statusKey, string fallback)
    {
        if (_p2StatusLabel == null) return;
        string text = LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady
            ? LocalizationManager.Instance.GetText(statusKey, fallback)
            : fallback;
        _p2StatusLabel.text = text;

        _p2StatusLabel.style.opacity = 0f;
        UIAnimator.DOFade(_p2StatusLabel, 1f, 0.3f);
    }
}
