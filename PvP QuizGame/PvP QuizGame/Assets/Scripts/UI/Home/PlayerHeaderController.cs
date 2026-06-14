using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [UI Refactor] Header sảnh chính: tên, avatar, tiền, level, XP bar + TIER BADGE LỚN.
///
/// Season info ĐÃ DỜI sang Rank panel (xem RankPanelController).
/// Tier badge giờ là 1 card to (icon 40px + tên tier + RP) thay vì badge nhỏ inline.
/// </summary>
public class PlayerHeaderController
{
    private readonly Label _nameLabel;
    private readonly Label _moneyLabel;
    private readonly Label _levelTag;
    private readonly Label _xpLabel;
    private readonly VisualElement _avatarElement;
    private readonly VisualElement _xpFill;

    // [UI Refactor] Tier badge LỚN
    private readonly VisualElement _tierBadgeCard;
    private readonly VisualElement _tierBadgeIcon;  // [Icon Fix] đổi từ Label (emoji) sang VisualElement (PNG icon)
    private readonly Label _tierBadgeName;
    private readonly Label _tierBadgeRp;

    // [Icon Fix] Tất cả icon-class có thể gắn cho tier-badge-icon (để xoá khi swap)
    private static readonly string[] s_TierIconClasses = new[]
    {
        "icon-award", "icon-gem", "icon-crown",
        "icon-tint-bronze", "icon-tint-silver", "icon-tint-gold",
        "icon-tint-diamond", "icon-tint-legend"
    };

    public PlayerHeaderController(VisualElement root)
    {
        _moneyLabel = root.Q<Label>("money-label");
        _nameLabel = root.Q<Label>("name-label");
        _avatarElement = root.Q<VisualElement>("avatar");
        _levelTag = root.Q<Label>("level-tag");
        _xpFill = root.Q<VisualElement>("xp-fill");
        _xpLabel = root.Q<Label>("xp-label");

        _tierBadgeCard = root.Q<VisualElement>("tier-badge-card");
        _tierBadgeIcon = root.Q<VisualElement>("tier-badge-icon");
        _tierBadgeName = root.Q<Label>("tier-badge-name");
        _tierBadgeRp = root.Q<Label>("tier-badge-rp");
    }

    /// <summary>Cập nhật toàn bộ thông số người chơi trên header.</summary>
    public void Refresh()
    {
        if (PlayerDataManager.Instance == null) return;
        var data = PlayerDataManager.Instance.Data;

        if (_levelTag != null) _levelTag.text = $"LEVEL {data.level}";
        if (_moneyLabel != null) _moneyLabel.text = $"${data.money:N0}";
        if (_nameLabel != null) _nameLabel.text = data.playerName;

        if (_avatarElement != null)
            AvatarHelper.SetAvatar(_avatarElement, data.playerName);

        if (_xpFill != null)
        {
            float expPercent = (float)data.currentExp / Mathf.Max(1, data.GetExpToNextLevel());
            _xpFill.style.width = Length.Percent(Mathf.Clamp(expPercent * 100f, 0f, 100f));
        }
        if (_xpLabel != null)
            _xpLabel.text = $"{data.currentExp} / {data.GetExpToNextLevel()}";

        UpdateTierBadgeLarge(data.currentTier, data.rankPoints);

        Debug.Log($"<color=white>[PlayerHeader] Updated: {data.playerName} | L{data.level} | ${data.money} | Tier {data.currentTier} ({data.rankPoints} RP)</color>");
    }

    /// <summary>[UI Refactor] Tier badge LỚN: icon + tên localized + RP.</summary>
    private void UpdateTierBadgeLarge(int tier, int rp)
    {
        if (_tierBadgeCard == null) return;

        // Reset class màu cũ
        _tierBadgeCard.RemoveFromClassList("tier-bronze");
        _tierBadgeCard.RemoveFromClassList("tier-silver");
        _tierBadgeCard.RemoveFromClassList("tier-gold");
        _tierBadgeCard.RemoveFromClassList("tier-diamond");
        _tierBadgeCard.RemoveFromClassList("tier-legend");

        // [Icon Fix] cls = class màu cho card, iconCls + tintCls = class cho VisualElement icon
        string cls, iconCls, tintCls, fallbackName;
        switch (tier)
        {
            case 1: cls = "tier-bronze";  iconCls = "icon-award"; tintCls = "icon-tint-bronze";  fallbackName = "Đồng"; break;
            case 2: cls = "tier-silver";  iconCls = "icon-award"; tintCls = "icon-tint-silver";  fallbackName = "Bạc"; break;
            case 3: cls = "tier-gold";    iconCls = "icon-award"; tintCls = "icon-tint-gold";    fallbackName = "Vàng"; break;
            case 4: cls = "tier-diamond"; iconCls = "icon-gem";   tintCls = "icon-tint-diamond"; fallbackName = "Kim Cương"; break;
            case 5: cls = "tier-legend";  iconCls = "icon-crown"; tintCls = "icon-tint-legend";  fallbackName = "Huyền Thoại"; break;
            default: cls = "tier-bronze"; iconCls = "icon-award"; tintCls = "icon-tint-bronze";  fallbackName = "Đồng"; break;
        }
        _tierBadgeCard.AddToClassList(cls);

        if (_tierBadgeIcon != null)
        {
            // Xoá hết class icon/tint cũ rồi gắn class mới
            foreach (var c in s_TierIconClasses) _tierBadgeIcon.RemoveFromClassList(c);
            _tierBadgeIcon.AddToClassList(iconCls);
            _tierBadgeIcon.AddToClassList(tintCls);
        }

        var l = LocalizationManager.Instance;
        string tierName = (l != null && l.IsReady) ? l.GetText($"tier_{tier}_name", fallbackName) : fallbackName;
        if (_tierBadgeName != null) _tierBadgeName.text = tierName.ToUpper();
        if (_tierBadgeRp != null) _tierBadgeRp.text = $"{rp:N0} RP";
    }
}
