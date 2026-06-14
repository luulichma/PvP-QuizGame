using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Panel Thành Tựu — tách từ MainMenuUIController_UXML.LoadAchievementsData().
/// Tự reload khi đổi ngôn ngữ nếu panel đang hiển thị (thay logic cũ trong LocalizeUI).
/// </summary>
public class AchievementsPanelController
{
    private readonly VisualTreeAsset _entryTemplate;
    private readonly ScrollView _scroll;
    private readonly VisualElement _subpanel;
    private readonly UILocalizer _localizer = new UILocalizer();

    // [Icon Fix] Tập class icon có thể xuất hiện trên achievement icon — cần clear khi swap.
    private static readonly string[] s_AllAchievementIconClasses = new[]
    {
        "icon-bot", "icon-swords", "icon-coins", "icon-zap", "icon-crown",
        "icon-flame", "icon-brain", "icon-medal", "icon-trophy", "icon-star"
    };
    private static readonly string[] s_AllTintClasses = new[]
    {
        "icon-tint-gold", "icon-tint-silver", "icon-tint-bronze",
        "icon-tint-diamond", "icon-tint-legend", "icon-tint-money",
        "icon-tint-purple", "icon-tint-green", "icon-tint-red", "icon-tint-cyan"
    };

    public AchievementsPanelController(VisualElement root, VisualTreeAsset entryTemplate)
    {
        _entryTemplate = entryTemplate;
        _scroll = root.Q<ScrollView>("achievements-scroll");
        _subpanel = root.Q<VisualElement>("subpanel-achievements");

        // Đổi ngôn ngữ khi panel đang mở → reload toàn bộ entry với text mới
        _localizer.Bind(l => { if (IsVisible) Load(); });
    }

    public bool IsVisible =>
        _subpanel != null && _subpanel.style.display == DisplayStyle.Flex;

    public void Attach() => _localizer.Attach();
    public void Detach() => _localizer.Detach();

    /// <summary>Build danh sách thành tựu từ AchievementManager + tiến độ của người chơi.</summary>
    public void Load()
    {
        if (_entryTemplate == null)
        {
            Debug.LogWarning("[AchievementsPanel] Chưa gán AchievementEntryTemplate trong Inspector!");
            return;
        }

        if (_scroll == null) return;
        _scroll.Clear();

        if (AchievementManager.Instance == null) return;

        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null) return;

        foreach (var ach in AchievementManager.Instance.achievements)
        {
            var entry = _entryTemplate.Instantiate();

            var title = entry.Q<Label>("achievement-title");
            if (title != null) title.text = ach.name;

            var desc = entry.Q<Label>("achievement-desc");
            if (desc != null) desc.text = ach.description;

            // [Icon Fix] Swap icon class trên VisualElement (PNG-based), không dùng text emoji.
            var iconImage = entry.Q<VisualElement>("achievement-icon-image");
            if (iconImage != null)
            {
                UIIconHelper.SwapIconClass(
                    iconImage,
                    ach.iconClass ?? "icon-medal",
                    ach.iconTint  ?? IconTint.Gold,
                    s_AllAchievementIconClasses,
                    s_AllTintClasses);
            }

            var reward = entry.Q<Label>("reward-amount");
            if (reward != null)
            {
                string suffix = ach.rewardType == RewardType.Money ? "$" : " RP";
                reward.text = ach.rewardAmount + suffix;
            }

            var progressFill = entry.Q<VisualElement>("achievement-progress-fill");
            var progressText = entry.Q<Label>("achievement-progress-text");
            var claimBtn = entry.Q<Button>("claim-btn");
            var completedTag = entry.Q<Label>("completed-tag");

            if (pd.unlockedAchievements == null)
                pd.unlockedAchievements = new System.Collections.Generic.List<string>();

            bool isUnlocked = pd.unlockedAchievements.Contains(ach.id);
            int currentProg = AchievementManager.Instance.GetCurrentProgress(ach.id);

            if (progressText != null)
            {
                int displayProg = Mathf.Min(currentProg, ach.targetValue);
                progressText.text = $"{displayProg}/{ach.targetValue}";
            }

            if (progressFill != null)
            {
                float pct = Mathf.Clamp01((float)currentProg / ach.targetValue) * 100f;
                progressFill.style.width = new Length(pct, LengthUnit.Percent);
            }

            if (isUnlocked)
            {
                if (claimBtn != null) claimBtn.style.display = DisplayStyle.None;
                if (completedTag != null)
                {
                    completedTag.style.display = DisplayStyle.Flex;
                    if (LocalizationManager.Instance != null)
                        completedTag.text = LocalizationManager.Instance.GetText("ach_btn_claimed", "ĐÃ NHẬN");
                }
                entry.Q<VisualElement>(className: "glass-panel").style.backgroundColor = new Color(0, 0.9f, 0.46f, 0.15f);
            }
            else
            {
                if (claimBtn != null)
                {
                    if (LocalizationManager.Instance != null)
                        claimBtn.text = LocalizationManager.Instance.GetText("ach_btn_not_reached", "CHƯA ĐẠT");
                    else
                        claimBtn.text = "CHƯA ĐẠT";
                    claimBtn.SetEnabled(false);
                    claimBtn.style.backgroundColor = new Color(1f, 1f, 1f, 0.1f);
                }
                if (completedTag != null) completedTag.style.display = DisplayStyle.None;
            }

            _scroll.Add(entry);
        }
    }
}
