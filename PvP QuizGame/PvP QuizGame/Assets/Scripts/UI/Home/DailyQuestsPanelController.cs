using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [PHASE-2] Daily Quests panel — render 4 quest card + nút Claim.
/// Tự subscribe DailyQuestManager.OnQuestsChanged để auto refresh.
/// </summary>
public class DailyQuestsPanelController
{
    private readonly VisualElement _root; // home root để show toast
    private readonly Label _titleLabel;
    private readonly Label _resetLabel;
    private readonly ScrollView _scroll;
    private readonly UILocalizer _localizer = new UILocalizer();

    private Action _onChanged;
    private Action<string, int> _onClaimed;

    public DailyQuestsPanelController(VisualElement root)
    {
        _root = root;
        _titleLabel = root.Q<Label>("quests-title");
        _resetLabel = root.Q<Label>("quests-reset-label");
        _scroll = root.Q<ScrollView>("quests-scroll");

        _localizer.BindLabel(_titleLabel, "quest_title", "NHIỆM VỤ HÀNG NGÀY");
        // _resetLabel update động qua Tick
    }

    public void Attach()
    {
        _localizer.Attach();
        _localizer.Refresh();

        _onChanged = Refresh;
        _onClaimed = (id, reward) => ShowToast($"+{reward}$");
        DailyQuestManager.OnQuestsChanged += _onChanged;
        DailyQuestManager.OnQuestClaimed += _onClaimed;
    }

    public void Detach()
    {
        _localizer.Detach();
        if (_onChanged != null) DailyQuestManager.OnQuestsChanged -= _onChanged;
        if (_onClaimed != null) DailyQuestManager.OnQuestClaimed -= _onClaimed;
    }

    /// <summary>Gọi khi user mở tab → build lại UI.</summary>
    public void Load() => Refresh();

    private void Refresh()
    {
        if (_scroll == null || DailyQuestManager.Instance == null) return;
        _scroll.Clear();

        UpdateResetLabel();

        var l = LocalizationManager.Instance;
        foreach (var id in DailyQuestManager.Instance.AllQuestIds())
        {
            int progress = DailyQuestManager.Instance.GetProgress(id);
            int target = DailyQuestManager.Instance.GetTarget(id);
            int reward = DailyQuestManager.Instance.GetReward(id);
            bool claimed = DailyQuestManager.Instance.IsClaimed(id);
            bool complete = DailyQuestManager.Instance.IsComplete(id);

            var card = new VisualElement();
            card.AddToClassList("quest-card");
            if (claimed) card.AddToClassList("quest-card-claimed");

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            card.Add(headerRow);

            string nameKey = id switch
            {
                DailyQuestManager.QUEST_PLAY_3    => "quest_play_3",
                DailyQuestManager.QUEST_WIN_1     => "quest_win_1",
                DailyQuestManager.QUEST_CORRECT_15 => "quest_correct_15",
                DailyQuestManager.QUEST_PERFECT   => "quest_perfect",
                _ => id
            };
            string fallback = id switch
            {
                DailyQuestManager.QUEST_PLAY_3    => "Chơi 3 trận",
                DailyQuestManager.QUEST_WIN_1     => "Thắng 1 trận",
                DailyQuestManager.QUEST_CORRECT_15 => "Đúng 15 câu",
                DailyQuestManager.QUEST_PERFECT   => "Perfect Round",
                _ => id
            };
            // [Icon Fix] Thay emoji (mất trên build) bằng PNG icon class.
            (string iconCls, string tintCls) = id switch
            {
                DailyQuestManager.QUEST_PLAY_3     => ("icon-gamepad", IconTint.Cyan),
                DailyQuestManager.QUEST_WIN_1     => ("icon-trophy",  IconTint.Gold),
                DailyQuestManager.QUEST_CORRECT_15 => ("icon-check",   IconTint.Green),
                DailyQuestManager.QUEST_PERFECT   => ("icon-star",    IconTint.Gold),
                _ => ("icon-star", IconTint.Gold)
            };

            string questName = (l != null && l.IsReady) ? l.GetText(nameKey, fallback) : fallback;
            var title = UIIconHelper.MakeIconLabel(iconCls, questName, tintCls,
                iconSizePx: 38f, fontSizePx: 32, textColor: Color.white);
            // Lấy Label con bên trong để style bold
            var titleLabel = title.Q<Label>();
            if (titleLabel != null) titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerRow.Add(title);

            string rewardFmt = (l != null && l.IsReady) ? l.GetText("quest_reward", "+{0}$") : "+{0}$";
            var rewardLabel = new Label(string.Format(rewardFmt, reward));
            rewardLabel.style.fontSize = 28;
            rewardLabel.style.color = new Color(1f, 0.82f, 0.4f);
            rewardLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerRow.Add(rewardLabel);

            // Progress bar
            var progressBg = new VisualElement();
            progressBg.AddToClassList("quest-progress-bar");
            card.Add(progressBg);

            var fill = new VisualElement();
            fill.AddToClassList("quest-progress-fill");
            float pct = Mathf.Clamp01((float)progress / Mathf.Max(1, target)) * 100f;
            fill.style.width = Length.Percent(pct);
            progressBg.Add(fill);

            var progressLabel = new Label($"{Mathf.Min(progress, target)} / {target}");
            progressLabel.style.fontSize = 22;
            progressLabel.style.color = new Color(1, 1, 1, 0.5f);
            progressLabel.style.marginTop = 4;
            card.Add(progressLabel);

            // Claim button
            var claimText = (l != null && l.IsReady)
                ? (claimed ? l.GetText("quest_claimed", "ĐÃ NHẬN") : l.GetText("quest_claim", "NHẬN"))
                : (claimed ? "ĐÃ NHẬN" : "NHẬN");
            var btn = new Button(() =>
            {
                if (DailyQuestManager.Instance.TryClaim(id))
                {
                    // OnQuestsChanged → tự refresh
                }
            }) { text = claimText };
            btn.AddToClassList("quest-claim-btn");
            btn.SetEnabled(complete && !claimed);
            btn.style.alignSelf = Align.FlexEnd;
            btn.style.marginTop = 8;
            card.Add(btn);

            _scroll.Add(card);
        }
    }

    private void UpdateResetLabel()
    {
        if (_resetLabel == null || DailyQuestManager.Instance == null) return;
        var ts = DailyQuestManager.Instance.TimeUntilReset;
        string left = $"{ts.Hours:D2}:{ts.Minutes:D2}";
        var l = LocalizationManager.Instance;
        string fmt = (l != null && l.IsReady) ? l.GetText("quest_reset_countdown", "Reset sau: {0}") : "Reset sau: {0}";
        _resetLabel.text = string.Format(fmt, left);
    }

    private void ShowToast(string msg) => ToastService.ShowInfo(_root, msg);
}
