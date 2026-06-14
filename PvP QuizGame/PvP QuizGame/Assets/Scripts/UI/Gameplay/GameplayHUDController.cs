using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

/// <summary>
/// [REFACTOR-P2] HUD trong trận: score, timer arc, tên/avatar 2 người chơi,
/// trạng thái đối thủ, streak toast, turn summary.
/// Tách từ GameplayUIController_UXML.
/// [PHASE-2 HOOK] Tier/Rank plan Bước 2: PowerUp bar (50:50, Thêm giờ, Lá chắn) sẽ cắm vào đây.
///
/// Cần MonoBehaviour runner để chạy coroutine. Attach()/Detach() gọi từ OnEnable/OnDisable.
/// </summary>
public class GameplayHUDController
{
    private readonly VisualElement _root;
    private readonly MonoBehaviour _runner;

    private readonly Label _p1ScoreLabel;
    private readonly Label _p2ScoreLabel;
    private readonly Label _p1Label;
    private readonly Label _p2Label;
    private readonly Label _timerText;
    private readonly VisualElement _timerContainer;
    private readonly TimerArcElement _timerArc;
    private readonly VisualElement _p1Avatar;
    private readonly VisualElement _p2Avatar;
    private readonly VisualElement _p1Info;
    private readonly VisualElement _p2Info;
    private readonly Label _p2StatusLabel;
    private readonly VisualElement _particleLayer;

    private Coroutine _timerRotateCoroutine;

    // UX-03: Turn summary state
    private bool _isShowingTurnSummary = false;

    // Score tracking for animation
    private int _lastP1Score = 0;
    private int _lastP2Score = 0;

    // Timer urgent state
    private bool _timerIsUrgent = false;

    public GameplayHUDController(VisualElement root, MonoBehaviour runner)
    {
        _root = root;
        _runner = runner;

        _p1ScoreLabel = root.Q<Label>("p1-score");
        _p2ScoreLabel = root.Q<Label>("p2-score");
        _p1Label      = root.Q<Label>("p1-label");
        _p2Label      = root.Q<Label>("p2-label");
        _timerText      = root.Q<Label>("timer-text");
        _timerContainer = root.Q<VisualElement>("timer-container");

        // Khởi tạo TimerArcElement và chèn vào sau timer-ring-bg
        if (_timerContainer != null)
        {
            _timerArc = new TimerArcElement { StrokeWidth = 8f };
            _timerArc.ArcColor = new Color(0f, 0.898f, 1f);
            // Insert ở index 1 (sau timer-ring-bg, trước timer-text)
            _timerContainer.Insert(1, _timerArc);
        }

        _p1Avatar = root.Q<VisualElement>("p1-avatar");
        _p2Avatar = root.Q<VisualElement>("p2-avatar");
        _p1Info = root.Q<VisualElement>("p1-info");
        _p2Info = root.Q<VisualElement>("p2-info");
        _p2StatusLabel = root.Q<Label>("p2-status");
        _particleLayer = root.Q<VisualElement>("particle-layer");
    }

    public void Attach()
    {
        if (_timerContainer != null && _timerRotateCoroutine == null)
            _timerRotateCoroutine = _runner.StartCoroutine(RotateTimerSlowly());
    }

    public void Detach()
    {
        if (_timerRotateCoroutine != null)
        {
            _runner.StopCoroutine(_timerRotateCoroutine);
            _timerRotateCoroutine = null;
        }
    }

    /// <summary>Animate HUD elements khi scene mở.</summary>
    public void AnimateEntry()
    {
        if (_p1Info != null) UIAnimator.DOSlideFromLeft(_p1Info, 0.5f, 80f);
        if (_p2Info != null) UIAnimator.DOSlideFromRight(_p2Info, 0.5f, 80f);
        if (_timerContainer != null) UIAnimator.DOBounceIn(_timerContainer, 0.6f);
    }

    /// <summary>Reset HUD khi vào state Playing (trận mới).</summary>
    public void ResetForNewGame()
    {
        _lastP1Score = 0;
        _lastP2Score = 0;
        if (_p1ScoreLabel != null) _p1ScoreLabel.text = "0";
        if (_p2ScoreLabel != null) _p2ScoreLabel.text = "0";
        _timerIsUrgent = false;
    }

    /// <summary>Reset trạng thái đối thủ + timer visuals khi sang câu hỏi mới.</summary>
    public void OnNewQuestion()
    {
        SetOpponentStatus("game_opp_thinking", "Đang suy nghĩ...");

        _timerIsUrgent = false;
        if (_timerArc != null)
        {
            _timerArc.FillAmount = 1f;
            _timerArc.ArcColor  = new Color(0f, 0.898f, 1f);
        }
        if (_timerText != null) _timerText.style.color = Color.white;
    }

    // ==================== SCORE ====================

    public void HandleScoreChanged(int p1Score, int p2Score)
    {
        if (_p1ScoreLabel != null && p1Score != _lastP1Score)
        {
            UIAnimator.DOCountTo(_p1ScoreLabel, _lastP1Score, p1Score, 0.4f);
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

    public void HandleTimerTick(float remaining)
    {
        if (_timerText != null)
        {
            _timerText.text = TimerController.Instance != null
                ? TimerController.Instance.GetFormattedTime()
                : $"{Mathf.CeilToInt(remaining)}";
        }

        if (_timerArc != null && TimerController.Instance != null)
        {
            float fill = remaining / TimerController.Instance.TotalTime;
            _timerArc.FillAmount = fill;

            // Hiệu ứng "vắt kiệt năng lượng": viền mỏng dần
            _timerArc.StrokeWidth = Mathf.Lerp(1.5f, 14f, fill);

            // Blend màu cyan → đỏ trong 5 giây cuối
            if (remaining <= 5f)
            {
                float t = remaining / 5f; // 1 → 0
                _timerArc.ArcColor = Color.Lerp(
                    new Color(1f, 0.32f, 0.32f),
                    new Color(0f, 0.898f, 1f), t);
            }
            else
            {
                _timerArc.ArcColor = new Color(0f, 0.898f, 1f);
            }
        }

        // Urgent state — màu chữ
        if (remaining <= 5f && !_timerIsUrgent)
        {
            _timerIsUrgent = true;
            if (_timerText != null) _timerText.style.color = new Color(1f, 0.32f, 0.32f);
        }
        else if (remaining > 5f && _timerIsUrgent)
        {
            _timerIsUrgent = false;
            if (_timerText != null) _timerText.style.color = Color.white;
        }

        // Urgent tick sound + haptic
        if (remaining <= 5f && remaining > 0)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.timerUrgentSound);
            HapticFeedback.CountdownTick();
        }
    }

    /// <summary>Xoay chậm timer container 18°/giây. Bù ngược timer-text để chữ số không bị nghiêng.</summary>
    private IEnumerator RotateTimerSlowly()
    {
        float angle = 0f;
        while (true)
        {
            angle += 18f * Time.deltaTime;
            if (angle >= 360f) angle -= 360f;

            if (_timerContainer != null)
                _timerContainer.style.rotate = new StyleRotate(new Rotate(angle));
            if (_timerText != null)
                _timerText.style.rotate = new StyleRotate(new Rotate(-angle));

            yield return null;
        }
    }

    // ==================== OPPONENT ====================

    public void HandleOpponentLeft()
    {
        string msg = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText("game_opponent_left", "Đối thủ đã rời trận — Bạn thắng!")
            : "Đối thủ đã rời trận — Bạn thắng!";
        ToastService.Show(_root, msg, 2.5f);
        HapticFeedback.Medium();
        Debug.LogWarning("[GameplayHUD] Đối thủ đã rời trận!");
    }

    public void HandleOpponentAnswerResult(bool isCorrect)
    {
        if (_p2Avatar == null) return;
        SetOpponentStatus("game_opp_answered", "Đã trả lời!");

        // [Icon Fix] PNG icon thay cho emoji ✅/❌ (mất trên build APK).
        var indicator = UIIconHelper.MakeIcon(
            isCorrect ? "icon-check" : "icon-x",
            isCorrect ? IconTint.Green : IconTint.Red,
            sizePx: 50f);
        indicator.style.position = Position.Absolute;
        indicator.style.right = -8;
        indicator.style.bottom = -8;
        indicator.style.opacity = 0f;

        _p2Avatar.Add(indicator);
        UIAnimator.DOBounceIn(indicator, 0.4f);
        _runner.StartCoroutine(RemoveAfterDelay(indicator, 2.0f));
    }

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

    private IEnumerator RemoveAfterDelay(VisualElement el, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (el != null && el.parent != null)
        {
            UIAnimator.DOFade(el, 0f, 0.2f);
            yield return new WaitForSeconds(0.2f);
            if (el.parent != null) el.RemoveFromHierarchy();
        }
    }

    // ==================== PLAYER NAMES / AVATARS ====================

    /// <summary>Cập nhật tên + avatar 2 người chơi (gọi khi enable + khi đổi ngôn ngữ).</summary>
    public void LocalizeNames()
    {
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

            if (_p1Avatar != null) AvatarHelper.SetAvatar(_p1Avatar, myName);
            if (_p2Avatar != null) AvatarHelper.SetAvatar(_p2Avatar, oppName);
        }
        else
        {
            if (_p1Label != null) _p1Label.text = L.GetText("game_score_me");
            if (_p2Label != null) _p2Label.text = L.GetText("game_score_opp");
        }
    }

    // ==================== STREAK ====================

    public void HandleStreakChanged(int streak)
    {
        if (streak >= 2)
        {
            // [Icon Fix] Bỏ emoji 🔥 trong text toast (toast không hỗ trợ inline icon).
            ShowStreakToast($"{streak}x Streak!");
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
        if (_root == null) return;

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

        _root.Add(badge);

        UIAnimator.DOStreakFlash(badge, 1.2f).OnComplete(() =>
        {
            if (badge.parent != null) badge.RemoveFromHierarchy();
        });
    }

    // ==================== TURN SUMMARY (UX-03) ====================

    public void HandleTurnSummary(bool p1Correct, bool p2Correct, int p1Score, int p2Score, bool isLast)
    {
        if (_root == null) return;
        if (_isShowingTurnSummary) return;
        _isShowingTurnSummary = true;

        var summary = new VisualElement();
        summary.name = "turn-summary-overlay";
        summary.AddToClassList("countdown-overlay");
        summary.style.opacity = 0f;

        var container = new VisualElement();
        container.AddToClassList("turn-summary-card");

        var L = LocalizationManager.Instance;

        // [Icon Fix] Dùng PNG icon thay emoji ✅/❌. Tạo row [Label "Bạn:"][icon][Label text].
        string p1Text = p1Correct
            ? (L != null ? L.GetText("game_turn_correct", "Đúng") : "Đúng")
            : (L != null ? L.GetText("game_turn_wrong", "Sai") : "Sai");
        var p1Color = p1Correct ? new Color(0f, 0.9f, 0.46f) : new Color(1f, 0.32f, 0.32f);
        var p1Row = UIIconHelper.MakeIconLabel(
            p1Correct ? "icon-check" : "icon-x",
            $"Bạn: {p1Text}",
            p1Correct ? IconTint.Green : IconTint.Red,
            iconSizePx: 36f, fontSizePx: 36, textColor: p1Color);
        var p1Inner = p1Row.Q<Label>();
        if (p1Inner != null) p1Inner.style.unityFontStyleAndWeight = FontStyle.Bold;
        p1Row.style.marginBottom = 8;
        container.Add(p1Row);

        // P2 result
        string p2Text = p2Correct
            ? (L != null ? L.GetText("game_turn_correct", "Đúng") : "Đúng")
            : (L != null ? L.GetText("game_turn_wrong", "Sai") : "Sai");
        var p2Color = p2Correct ? new Color(0f, 0.9f, 0.46f) : new Color(1f, 0.32f, 0.32f);
        var p2Row = UIIconHelper.MakeIconLabel(
            p2Correct ? "icon-check" : "icon-x",
            $"Đối thủ: {p2Text}",
            p2Correct ? IconTint.Green : IconTint.Red,
            iconSizePx: 36f, fontSizePx: 36, textColor: p2Color);
        var p2Inner = p2Row.Q<Label>();
        if (p2Inner != null) p2Inner.style.unityFontStyleAndWeight = FontStyle.Bold;
        p2Row.style.marginBottom = 16;
        container.Add(p2Row);

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
        _root.Add(summary);

        UIAnimator.ShowPopupAnim(summary, container);

        _runner.StartCoroutine(RemoveTurnSummaryAfter(summary, 2.5f));
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
}
