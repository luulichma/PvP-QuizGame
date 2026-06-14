using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

/// <summary>
/// [REFACTOR-P2] Popup kết quả trận đấu — tách từ GameplayUIController_UXML.HandleGameOver() (~240 dòng).
/// Behavior giữ nguyên: trophy/title theo kết quả, confetti khi thắng, haptic + sound,
/// animated score, reward (Money/XP/RP) hoặc ghi chú đầu hàng, nút Chơi lại / Về Trang chủ.
/// </summary>
public class ResultPopupController : PopupBase
{
    public ResultPopupController(VisualTreeAsset template, VisualElement parent)
        : base(template, parent, "result-overlay", "result-container") { }

    protected override void OnShow(VisualElement root)
    {
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
            var confettiLayer = root.Q<VisualElement>("result-particle-layer");
            if (confettiLayer != null && UIParticleEffect.Instance != null)
            {
                UIParticleEffect.Instance.SpawnConfetti(confettiLayer, 50, 3f);
            }
        }

        var L = LocalizationManager.Instance;

        // [Icon Fix] Trophy icon — đổi từ Label emoji sang VisualElement PNG, swap class theo kết quả
        var trophyIcon = root.Q<VisualElement>("trophy-icon");
        if (trophyIcon != null)
        {
            // Xoá class icon cũ
            foreach (var c in new[] { "icon-trophy", "icon-frown", "icon-handshake", "icon-star-filled" })
                trophyIcon.RemoveFromClassList(c);

            string iconCls = result switch
            {
                WinResult.Player1Wins => "icon-trophy",
                WinResult.Player2Wins => "icon-frown",
                WinResult.Draw        => "icon-handshake",
                _ => "icon-star-filled"
            };
            trophyIcon.AddToClassList(iconCls);

            var trophyArea = root.Q<VisualElement>("trophy-area");
            if (trophyArea != null)
                UIAnimator.DOBounceIn(trophyArea, 0.6f);
        }

        // Title
        var title = root.Q<Label>("result-title");
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
        var yourScoreLbl = root.Q<Label>("your-score-label");
        if (yourScoreLbl != null)
            yourScoreLbl.text = L != null ? L.GetText("game_your_score", "Bạn") : "Bạn";

        var oppScoreLbl = root.Q<Label>("opp-score-label");
        if (oppScoreLbl != null)
            oppScoreLbl.text = L != null ? L.GetText("game_opp_score", "Đối thủ") : "Đối thủ";

        var rewardLbl = root.Q<Label>("reward-label");
        if (rewardLbl != null)
            rewardLbl.text = L != null ? L.GetText("game_reward", "Thưởng") : "Thưởng";

        // Animated score counter
        var p1Final = root.Q<Label>("p1-score-final");
        if (p1Final != null)
            UIAnimator.DOCountTo(p1Final, 0, ScoreManager.Instance.Player1Score, 0.8f);

        var p2Final = root.Q<Label>("p2-score-final");
        if (p2Final != null)
            UIAnimator.DOCountTo(p2Final, 0, ScoreManager.Instance.Player2Score, 0.8f);

        BuildRewardSection(root, result, L);
        BindActionButtons(root, L);
    }

    // ==================== REWARD ====================
    // [PHASE-2 HOOK] Tier/Rank plan: phần "tính" reward nằm ở ScoreManager (LastRewardMoney/Exp/RankPoints),
    // popup này chỉ "vẽ". Khi thêm reward mới (item, badge mùa...) chỉ cần thêm dòng hiển thị tại đây.
    private void BuildRewardSection(VisualElement root, WinResult result, LocalizationManager L)
    {
        var rewardParent = root.Q<VisualElement>(className: "reward-container");
        if (rewardParent == null) return;

        rewardParent.Clear();

        int money = ScoreManager.Instance.LastRewardMoney;
        int xp = ScoreManager.Instance.LastRewardExp;

        bool isSurrender = (money == 0 && xp == 0 && result == WinResult.Player2Wins);

        if (isSurrender)
        {
            var surrenderNote = new Label(
                L != null
                    ? L.GetText("game_surrender_no_reward", "Đầu hàng — Không nhận được thưởng.")
                    : "Đầu hàng — Không nhận được thưởng."
            );
            surrenderNote.style.fontSize = 28;
            surrenderNote.style.color = new Color(1f, 0.32f, 0.32f, 0.7f);
            surrenderNote.style.unityTextAlign = TextAnchor.MiddleCenter;
            surrenderNote.style.marginTop = 6;
            rewardParent.Add(surrenderNote);
            return;
        }

        // [Icon Fix] Reward rows = [PNG icon][Label] thay cho emoji + text.
        // Tiền
        var moneyReward = UIIconHelper.MakeIconLabel("icon-coins", $"+${money:N0}",
            IconTint.Money, iconSizePx: 40f, fontSizePx: 40, textColor: new Color(1f, 0.84f, 0.28f));
        moneyReward.style.justifyContent = Justify.Center;
        var moneyLbl = moneyReward.Q<Label>();
        if (moneyLbl != null) moneyLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        moneyReward.style.marginTop = 8;
        rewardParent.Add(moneyReward);

        // XP
        var xpReward = UIIconHelper.MakeIconLabel("icon-zap", $"+{xp} XP",
            IconTint.Cyan, iconSizePx: 40f, fontSizePx: 40, textColor: new Color(0f, 0.90f, 1f));
        xpReward.style.justifyContent = Justify.Center;
        var xpLbl = xpReward.Q<Label>();
        if (xpLbl != null) xpLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        xpReward.style.marginTop = 8;
        rewardParent.Add(xpReward);

        // Điểm Xếp Hạng
        int rankPoints = ScoreManager.Instance.LastRewardRankPoints;
        string sign = rankPoints > 0 ? "+" : "";
        var rpColor = rankPoints >= 0 ? new Color(0.85f, 0.44f, 1f) : new Color(1f, 0.32f, 0.32f);
        var rankReward = UIIconHelper.MakeIconLabel("icon-trophy", $"{sign}{rankPoints} RP",
            rankPoints >= 0 ? IconTint.Purple : IconTint.Red,
            iconSizePx: 40f, fontSizePx: 40, textColor: rpColor);
        rankReward.style.justifyContent = Justify.Center;
        var rpLbl = rankReward.Q<Label>();
        if (rpLbl != null) rpLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        rankReward.style.marginTop = 8;
        rewardParent.Add(rankReward);

        // Animation lần lượt
        moneyReward.style.opacity = 0f;
        xpReward.style.opacity = 0f;
        rankReward.style.opacity = 0f;

        moneyReward.style.translate = new StyleTranslate(new Translate(0, 20));
        xpReward.style.translate = new StyleTranslate(new Translate(0, 20));
        rankReward.style.translate = new StyleTranslate(new Translate(0, 20));

        UIAnimator.DOFade(moneyReward, 1f, 0.3f).SetDelay(0.3f);
        UIAnimator.DOTranslate(moneyReward, Vector2.zero, 0.4f).SetDelay(0.3f).SetEase(Ease.OutBack);

        UIAnimator.DOFade(xpReward, 1f, 0.3f).SetDelay(0.5f);
        UIAnimator.DOTranslate(xpReward, Vector2.zero, 0.4f).SetDelay(0.5f).SetEase(Ease.OutBack);

        UIAnimator.DOFade(rankReward, 1f, 0.3f).SetDelay(0.7f);
        UIAnimator.DOTranslate(rankReward, Vector2.zero, 0.4f).SetDelay(0.7f).SetEase(Ease.OutBack);
    }

    // ==================== ACTION BUTTONS ====================
    private void BindActionButtons(VisualElement root, LocalizationManager L)
    {
        var playAgainBtn = root.Q<Button>("play-again-btn");
        if (playAgainBtn != null)
        {
            if (L != null) playAgainBtn.text = L.GetText("game_play_again");

            bool isOnline = FirebaseManager.Instance != null
                            && !FirebaseManager.Instance.isOfflineMode
                            && !string.IsNullOrEmpty(FirebaseManager.Instance.CurrentRoomId);

            if (isOnline)
            {
                playAgainBtn.clicked += () =>
                {
                    FirebaseManager.Instance.LeaveRoom();
                    if (GameManager.Instance != null) GameManager.Instance.LoadHomeScene();
                };
            }
            else
            {
                playAgainBtn.clicked += () =>
                {
                    CloseImmediate(); // giữ behavior cũ: gỡ popup ngay rồi restart
                    if (GameController.Instance != null) GameController.Instance.RestartGame();
                };
            }
        }

        var backHomeBtn = root.Q<Button>("back-home-btn");
        if (backHomeBtn != null)
        {
            if (L != null) backHomeBtn.text = L.GetText("game_back_home");
            backHomeBtn.clicked += () =>
            {
                if (FirebaseManager.Instance != null) FirebaseManager.Instance.LeaveRoom();
                if (GameManager.Instance != null) GameManager.Instance.LoadHomeScene();
                else UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
            };
        }
    }
}
