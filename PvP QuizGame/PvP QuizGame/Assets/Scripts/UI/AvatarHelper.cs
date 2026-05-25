using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Helper tạo avatar từ chữ cái đầu của tên + màu nền sinh từ hash.
/// Giải pháp Initial Avatar — không cần sprite, luôn đẹp, luôn unique.
/// </summary>
public static class AvatarHelper
{
    private static readonly Color[] _palette = new Color[]
    {
        new Color(0.90f, 0.30f, 0.24f), // Red
        new Color(0.10f, 0.74f, 0.61f), // Teal
        new Color(0.20f, 0.60f, 0.86f), // Blue
        new Color(0.95f, 0.61f, 0.07f), // Orange
        new Color(0.61f, 0.35f, 0.71f), // Purple
        new Color(0.29f, 0.69f, 0.32f), // Green
        new Color(0.95f, 0.42f, 0.49f), // Pink
        new Color(0.17f, 0.80f, 0.80f), // Cyan
    };

    /// <summary>
    /// Gán avatar cho VisualElement dựa trên tên người chơi.
    /// Tạo vòng tròn màu + chữ cái đầu — không cần Sprite.
    /// </summary>
    public static void SetAvatar(VisualElement avatarElement, string playerName)
    {
        if (avatarElement == null) return;

        // Xóa nội dung cũ (nếu có label chữ cái)
        avatarElement.Clear();

        // Xóa background image cũ
        avatarElement.style.backgroundImage = null;

        // Chọn màu từ palette dựa trên hash tên
        int hash = playerName?.GetHashCode() ?? 0;
        Color bgColor = _palette[Mathf.Abs(hash) % _palette.Length];
        avatarElement.style.backgroundColor = bgColor;

        // Tạo label chữ cái đầu
        char firstChar = char.ToUpper(
            !string.IsNullOrEmpty(playerName) ? playerName[0] : '?'
        );
        var label = new Label(firstChar.ToString());
        label.style.fontSize = 52;
        label.style.color = Color.white;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.width = Length.Percent(100);
        label.style.height = Length.Percent(100);
        label.style.marginLeft = 0;
        label.style.marginRight = 0;
        label.style.alignSelf = Align.Center;
        label.style.justifyContent = Justify.Center;

        avatarElement.Add(label);
    }

    /// <summary>
    /// Tạo avatar cho đối thủ khi không có sprite (P2 mặc định)
    /// </summary>
    public static void SetDefaultOpponentAvatar(VisualElement avatarElement)
    {
        if (avatarElement == null) return;
        avatarElement.Clear();
        avatarElement.style.backgroundImage = null;
        avatarElement.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f);

        var label = new Label("?");
        label.style.fontSize = 52;
        label.style.color = Color.white;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.width = Length.Percent(100);
        label.style.height = Length.Percent(100);
        avatarElement.Add(label);
    }

    /// <summary>
    /// Lấy màu nền cho avatar từ tên (dùng để sync giữa các màn hình)
    /// </summary>
    public static Color GetColorForName(string playerName)
    {
        int hash = playerName?.GetHashCode() ?? 0;
        return _palette[Mathf.Abs(hash) % _palette.Length];
    }
}
