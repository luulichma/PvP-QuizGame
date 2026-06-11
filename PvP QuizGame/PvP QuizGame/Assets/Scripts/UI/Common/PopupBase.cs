using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Lớp nền cho mọi popup UI Toolkit.
/// Lo trọn vòng đời: instantiate template → phủ fullscreen → ShowPopupAnim
/// → localize (qua UILocalizer) → HidePopupAnim → remove + cleanup.
///
/// Controller cha KHÔNG build nội dung popup nữa — chỉ:
///   var popup = new XxxPopupController(template, root);
///   popup.Show();
///
/// Subclass override OnShow(root) để query element + wire event.
/// Là plain C# class (không MonoBehaviour) → không đụng Scene/Prefab.
/// </summary>
public abstract class PopupBase
{
    private readonly VisualTreeAsset _template;
    private readonly string _overlayName;
    private readonly string _cardName;

    /// <summary>Root container mà popup được add vào (thường là uiDocument.rootVisualElement).</summary>
    protected VisualElement Parent { get; }

    /// <summary>Instance của popup (TemplateContainer) — null khi chưa mở/đã đóng.</summary>
    protected VisualElement PopupRoot { get; private set; }

    protected VisualElement Overlay { get; private set; }
    protected VisualElement Card { get; private set; }

    /// <summary>Localizer riêng của popup — tự refresh khi đổi ngôn ngữ, tự detach khi đóng.</summary>
    protected UILocalizer Localizer { get; } = new UILocalizer();

    public bool IsOpen => PopupRoot != null && PopupRoot.parent != null;

    /// <summary>Bắn sau khi popup đã đóng hoàn toàn (sau animation).</summary>
    public event Action OnClosed;

    protected PopupBase(VisualTreeAsset template, VisualElement parent,
                        string overlayName = "overlay", string cardName = "popup")
    {
        _template = template;
        Parent = parent;
        _overlayName = overlayName;
        _cardName = cardName;
    }

    /// <summary>Mở popup. Bỏ qua nếu đang mở.</summary>
    public void Show()
    {
        if (IsOpen) return;
        if (_template == null || Parent == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Thiếu template hoặc parent — không thể mở popup.");
            return;
        }

        PopupRoot = _template.Instantiate();

        // TemplateContainer phủ toàn màn hình
        PopupRoot.style.position = Position.Absolute;
        PopupRoot.style.top = 0;
        PopupRoot.style.bottom = 0;
        PopupRoot.style.left = 0;
        PopupRoot.style.right = 0;

        Parent.Add(PopupRoot);

        Overlay = PopupRoot.Q<VisualElement>(_overlayName) ?? PopupRoot.Children().First();
        Card = PopupRoot.Q<VisualElement>(_cardName) ?? Overlay.Children().First();
        UIAnimator.ShowPopupAnim(Overlay, Card);

        OnShow(PopupRoot);

        Localizer.Attach();
        Localizer.Refresh();
    }

    /// <summary>Subclass query element + wire event + đăng ký Localizer tại đây.</summary>
    protected abstract void OnShow(VisualElement root);

    /// <summary>Subclass override nếu cần cleanup thêm (gỡ listener ngoài...).</summary>
    protected virtual void OnClose() { }

    /// <summary>Đóng popup với animation.</summary>
    public void Close()
    {
        if (!IsOpen) return;
        Localizer.Detach();
        OnClose();
        var rootToRemove = PopupRoot;
        PopupRoot = null;
        UIAnimator.HidePopupAnim(Overlay, Card, () =>
        {
            if (rootToRemove != null && rootToRemove.parent != null)
                rootToRemove.RemoveFromHierarchy();
            OnClosed?.Invoke();
        });
    }

    /// <summary>Đóng ngay lập tức, không animation (dùng cho nút Back Android...).</summary>
    public void CloseImmediate()
    {
        if (!IsOpen) return;
        Localizer.Detach();
        OnClose();
        if (PopupRoot.parent != null) PopupRoot.RemoveFromHierarchy();
        PopupRoot = null;
        OnClosed?.Invoke();
    }
}
