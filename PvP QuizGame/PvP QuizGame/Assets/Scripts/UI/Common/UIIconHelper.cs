using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [Icon Fix] Helper tạo VisualElement icon + label horizontal container.
/// Thay thế hoàn toàn việc dùng emoji unicode trong text Label
/// (build APK Android không có font emoji fallback → bị mất icon).
///
/// Icon được render bằng PNG (Assets/UI/Icons/) qua USS class
/// `.icon` + `.icon-{name}` + tint tuỳ chọn `.icon-tint-{color}`.
/// Xem GlobalStyles.uss section "PNG ICONS".
/// </summary>
public static class UIIconHelper
{
    /// <summary>Tạo một VisualElement icon đơn lẻ (vuông) với class USS.</summary>
    /// <param name="iconClass">Tên class icon, ví dụ "icon-coins", "icon-trophy".</param>
    /// <param name="tintClass">(Optional) Class tint màu, ví dụ "icon-tint-gold".</param>
    /// <param name="sizePx">Kích thước cạnh tính bằng px.</param>
    public static VisualElement MakeIcon(string iconClass, string tintClass = null, float sizePx = 36f)
    {
        var icon = new VisualElement();
        icon.AddToClassList("icon");
        if (!string.IsNullOrEmpty(iconClass)) icon.AddToClassList(iconClass);
        if (!string.IsNullOrEmpty(tintClass)) icon.AddToClassList(tintClass);
        icon.style.width = sizePx;
        icon.style.height = sizePx;
        icon.style.flexShrink = 0;
        return icon;
    }

    /// <summary>
    /// Tạo một container ngang [icon][text] thay thế cho `new Label("💰 +500$")`.
    /// </summary>
    public static VisualElement MakeIconLabel(
        string iconClass,
        string text,
        string tintClass = null,
        float iconSizePx = 36f,
        int fontSizePx = 28,
        Color? textColor = null)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;

        row.Add(MakeIcon(iconClass, tintClass, iconSizePx));

        var label = new Label(text);
        label.style.marginLeft = 6;
        label.style.fontSize = fontSizePx;
        if (textColor.HasValue) label.style.color = textColor.Value;
        row.Add(label);

        return row;
    }

    /// <summary>
    /// Hoán đổi class icon trên 1 VisualElement có sẵn. Truyền vào danh sách
    /// các icon class có thể có (để xoá trước khi gắn mới) — ngăn rò class cũ.
    /// </summary>
    public static void SwapIconClass(
        VisualElement target,
        string newIconClass,
        string newTintClass,
        string[] allPossibleIconClasses,
        string[] allPossibleTintClasses)
    {
        if (target == null) return;
        if (allPossibleIconClasses != null)
            foreach (var c in allPossibleIconClasses) target.RemoveFromClassList(c);
        if (allPossibleTintClasses != null)
            foreach (var c in allPossibleTintClasses) target.RemoveFromClassList(c);
        if (!string.IsNullOrEmpty(newIconClass)) target.AddToClassList(newIconClass);
        if (!string.IsNullOrEmpty(newTintClass)) target.AddToClassList(newTintClass);
    }
}

/// <summary>Helper struct: dùng để alias màu sắc khi gọi MakeIconLabel.</summary>
public static class IconTint
{
    public const string Gold    = "icon-tint-gold";
    public const string Silver  = "icon-tint-silver";
    public const string Bronze  = "icon-tint-bronze";
    public const string Diamond = "icon-tint-diamond";
    public const string Legend  = "icon-tint-legend";
    public const string Money   = "icon-tint-money";
    public const string Purple  = "icon-tint-purple";
    public const string Green   = "icon-tint-green";
    public const string Red     = "icon-tint-red";
    public const string Cyan    = "icon-tint-cyan";
}
