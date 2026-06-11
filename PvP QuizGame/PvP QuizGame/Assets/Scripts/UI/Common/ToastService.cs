using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

/// <summary>
/// [REFACTOR-P2] Toast notification dùng chung — gộp 2 bản copy-paste
/// từ MainMenuUIController_UXML (ShowInfoToast) và GameplayUIController_UXML (ShowToast).
/// Static class + DOTween delayed call → không cần MonoBehaviour/coroutine.
/// </summary>
public static class ToastService
{
    /// <summary>
    /// Toast kiểu HomeScene: hộp tím đậm, hiện/ẩn tức thì.
    /// (Giữ nguyên style từ MainMenuUIController.ShowInfoToast)
    /// </summary>
    public static void ShowInfo(VisualElement root, string message, float duration = 2.5f)
    {
        if (root == null) return;

        var toast = new Label(message);
        toast.style.position = Position.Absolute;
        toast.style.bottom = 160;
        toast.style.left = 0;
        toast.style.right = 0;
        toast.style.unityTextAlign = TextAnchor.MiddleCenter;
        toast.style.fontSize = 28;
        toast.style.color = Color.white;
        toast.style.backgroundColor = new Color(0.18f, 0.05f, 0.26f, 0.92f);
        toast.style.paddingTop = 18;
        toast.style.paddingBottom = 18;
        toast.style.paddingLeft = 28;
        toast.style.paddingRight = 28;
        toast.style.borderTopLeftRadius = 16;
        toast.style.borderTopRightRadius = 16;
        toast.style.borderBottomLeftRadius = 16;
        toast.style.borderBottomRightRadius = 16;
        toast.style.marginLeft = StyleKeyword.Auto;
        toast.style.marginRight = StyleKeyword.Auto;
        toast.style.maxWidth = new Length(85, LengthUnit.Percent);
        toast.style.whiteSpace = WhiteSpace.Normal;
        toast.style.unityFontStyleAndWeight = FontStyle.Bold;

        root.Add(toast);

        DOVirtual.DelayedCall(duration, () =>
        {
            if (toast.parent != null) toast.RemoveFromHierarchy();
        }, false);
    }

    /// <summary>
    /// Toast kiểu Gameplay: dùng USS class "toast", slide up + fade in/out.
    /// (Giữ nguyên behavior từ GameplayUIController.ShowToast)
    /// </summary>
    public static void Show(VisualElement root, string message, float duration = 2f)
    {
        if (root == null) return;

        var toast = new Label(message);
        toast.AddToClassList("toast");
        toast.style.opacity = 0f;

        root.Add(toast);

        // Slide up + fade in
        toast.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(20)));
        toast.DOFade(1f, 0.2f);
        toast.DOTranslate(Vector2.zero, 0.3f).SetEase(Ease.OutCubic);

        DOVirtual.DelayedCall(duration, () =>
        {
            if (toast.parent == null) return;
            toast.DOFade(0f, 0.3f);
            toast.DOTranslate(new Vector2(0, -20), 0.3f);
            DOVirtual.DelayedCall(0.3f, () =>
            {
                if (toast.parent != null) toast.RemoveFromHierarchy();
            }, false);
        }, false);
    }
}
