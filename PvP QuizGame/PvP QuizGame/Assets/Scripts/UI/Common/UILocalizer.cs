using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Binding localization tập trung cho UI Toolkit.
/// Thay vì mỗi controller tự viết LocalizeXxx() và tự subscribe OnLanguageChanged,
/// đăng ký (element, key) một lần — UILocalizer tự refresh khi đổi ngôn ngữ.
///
/// Cách dùng:
///   var loc = new UILocalizer();
///   loc.BindLabel(titleLabel, "settings_title");
///   loc.BindButton(closeBtn, "settings_close", "ĐÓNG");
///   loc.Attach();   // subscribe OnLanguageChanged
///   loc.Refresh();  // áp dụng ngay lần đầu
///   ...
///   loc.Detach();   // khi UI bị hủy (tránh leak event)
/// </summary>
public class UILocalizer
{
    private readonly List<Action<LocalizationManager>> _bindings = new List<Action<LocalizationManager>>();
    private bool _attached;

    private static string Get(LocalizationManager l, string key, string fallback)
        => fallback == null ? l.GetText(key) : l.GetText(key, fallback);

    /// <summary>Bind text của Label theo key.</summary>
    public void BindLabel(Label el, string key, string fallback = null)
    {
        if (el == null) return;
        _bindings.Add(l => el.text = Get(l, key, fallback));
    }

    /// <summary>Bind text của Button theo key.</summary>
    public void BindButton(Button el, string key, string fallback = null)
    {
        if (el == null) return;
        _bindings.Add(l => el.text = Get(l, key, fallback));
    }

    /// <summary>Bind label (tiêu đề) của TextField theo key.</summary>
    public void BindFieldLabel(TextField el, string key, string fallback = null)
    {
        if (el == null) return;
        _bindings.Add(l => el.label = Get(l, key, fallback));
    }

    /// <summary>Binding tùy biến (cho trường hợp phức tạp: format chuỗi, đổi màu...).</summary>
    public void Bind(Action<LocalizationManager> apply)
    {
        if (apply == null) return;
        _bindings.Add(apply);
    }

    /// <summary>Áp dụng tất cả binding ngay lập tức (nếu LocalizationManager sẵn sàng).</summary>
    public void Refresh()
    {
        var l = LocalizationManager.Instance;
        if (l == null || !l.IsReady) return;
        foreach (var b in _bindings) b(l);
    }

    /// <summary>Subscribe sự kiện đổi ngôn ngữ.</summary>
    public void Attach()
    {
        if (_attached) return;
        LocalizationManager.OnLanguageChanged += Refresh;
        _attached = true;
    }

    /// <summary>Unsubscribe — BẮT BUỘC gọi khi UI bị hủy.</summary>
    public void Detach()
    {
        if (!_attached) return;
        LocalizationManager.OnLanguageChanged -= Refresh;
        _attached = false;
    }

    /// <summary>Xóa toàn bộ binding (dùng khi rebuild UI).</summary>
    public void Clear() => _bindings.Clear();
}
