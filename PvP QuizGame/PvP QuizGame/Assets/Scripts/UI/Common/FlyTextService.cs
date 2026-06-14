using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

/// <summary>
/// [UI Refactor] Hiện text "fly" — bay lên + mờ dần trong ~0.7s.
/// Dùng để feedback nhanh các thao tác (mua/bán/dùng power-up...).
///
/// API:
///   FlyTextService.Spawn(targetElement, "+1 ✂️", new Color(0.2f, 1f, 0.6f));
///   FlyTextService.Spawn(targetElement, "Thiếu tiền!", Color.red);
///
/// Lấy ý tưởng từ InputController_UXML.ShowScoreFlyText.
/// Không cần ToastService (chậm 2.5s) cho các action ngắn.
/// </summary>
public static class FlyTextService
{
    /// <summary>
    /// Bắn 1 fly text gần `anchor`. Anchor có thể là button/card.
    /// Fly text được add vào parent của anchor (để position relative).
    /// </summary>
    public static void Spawn(VisualElement anchor, string text, Color color, float duration = 0.8f)
    {
        if (anchor == null || string.IsNullOrEmpty(text)) return;
        var parent = anchor.parent;
        if (parent == null) return;

        var label = new Label(text);
        label.style.position = Position.Absolute;
        label.style.color = color;
        label.style.fontSize = 36;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.opacity = 1f;
        label.pickingMode = PickingMode.Ignore;

        // Position trên đỉnh anchor (~10px trên top)
        label.style.top = anchor.layout.y - 10;
        label.style.left = anchor.layout.x + (anchor.layout.width * 0.5f) - 60;
        label.style.width = 120;

        parent.Add(label);

        // Bay lên + fade out
        var seq = DOTween.Sequence();
        seq.Join(UIAnimator.DOTranslate(label, new Vector2(0, -80), duration).SetEase(Ease.OutCubic));
        seq.Join(UIAnimator.DOScale(label, new Vector2(1.25f, 1.25f), 0.2f).SetEase(Ease.OutBack));
        seq.Join(label.DOFade(0f, duration).SetDelay(duration * 0.3f));
        seq.OnComplete(() =>
        {
            if (label?.parent != null) label.RemoveFromHierarchy();
        });
    }

    /// <summary>Tiện ích — fly text màu xanh (success).</summary>
    public static void SpawnSuccess(VisualElement anchor, string text)
        => Spawn(anchor, text, new Color(0.2f, 1f, 0.5f));

    /// <summary>Tiện ích — fly text màu đỏ (error/failure).</summary>
    public static void SpawnError(VisualElement anchor, string text)
        => Spawn(anchor, text, new Color(1f, 0.35f, 0.45f));
}
