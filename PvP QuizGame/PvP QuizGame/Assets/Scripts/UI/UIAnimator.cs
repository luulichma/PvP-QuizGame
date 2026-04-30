using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// Helper class sử dụng DOTween để tạo hiệu ứng (Animation) cho các thẻ VisualElement của UI Toolkit.
/// </summary>
public static class UIAnimator
{
    // ==================== TWEENERS CƠ BẢN ====================

    /// <summary>
    /// Làm mờ / Tỏ VisualElement.
    /// </summary>
    public static Tweener DOFade(this VisualElement ve, float endValue, float duration)
    {
        float startVal = ve.style.opacity.value;
        return DOTween.To(() => startVal, x => {
            ve.style.opacity = x;
        }, endValue, duration).SetTarget(ve);
    }

    /// <summary>
    /// Phóng to / Thu nhỏ VisualElement.
    /// </summary>
    public static Tweener DOScale(this VisualElement ve, Vector2 endValue, float duration)
    {
        Vector2 startVal = ve.style.scale.value.value;
        return DOTween.To(() => startVal, x => {
            ve.style.scale = new StyleScale(new Scale(x));
        }, endValue, duration).SetTarget(ve);
    }

    /// <summary>
    /// Di chuyển (Translate) VisualElement.
    /// </summary>
    public static Tweener DOTranslate(this VisualElement ve, Vector2 endValue, float duration)
    {
        Vector2 startVal = new Vector2(ve.style.translate.value.x.value, ve.style.translate.value.y.value);
        return DOTween.To(() => startVal, x => {
            ve.style.translate = new StyleTranslate(new Translate(new Length(x.x), new Length(x.y)));
        }, endValue, duration).SetTarget(ve);
    }

    /// <summary>
    /// Hiệu ứng rung lắc (Shake) nhẹ khi trả lời sai.
    /// </summary>
    public static Sequence DOShakePosition(this VisualElement ve, float duration, float strength = 15f)
    {
        Sequence seq = DOTween.Sequence();
        float t = duration / 6f;
        seq.Append(ve.DOTranslate(new Vector2(-strength, 0), t));
        seq.Append(ve.DOTranslate(new Vector2(strength, 0), t * 2));
        seq.Append(ve.DOTranslate(new Vector2(-strength / 2f, 0), t * 2));
        seq.Append(ve.DOTranslate(new Vector2(0, 0), t));
        return seq;
    }


    // ==================== PRE-DEFINED ANIMATIONS CẤP CAO ====================

    /// <summary>
    /// Hiệu ứng xuất hiện Popup (Nảy lên mượt mà).
    /// Yêu cầu thẻ overlay làm tối màu nền phía sau.
    /// </summary>
    public static void ShowPopupAnim(VisualElement overlay, VisualElement popupCard)
    {
        if (overlay != null)
        {
            overlay.style.opacity = 0f;
            overlay.DOFade(1f, 0.2f).SetUpdate(true); // SetUpdate true để popup ko bị ảnh hưởng time scale
        }

        if (popupCard != null)
        {
            popupCard.style.scale = new StyleScale(new Scale(new Vector2(0.3f, 0.3f)));
            popupCard.style.opacity = 0f;
            popupCard.DOFade(1f, 0.2f).SetUpdate(true);
            popupCard.DOScale(Vector2.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    /// <summary>
    /// Hiệu ứng đóng Popup. Cần truyền vào Action callback để Remove khỏi Hierarchy sau khi chạy xong.
    /// </summary>
    public static void HidePopupAnim(VisualElement overlay, VisualElement popupCard, System.Action onComplete)
    {
        float duration = 0.2f;
        Sequence seq = DOTween.Sequence().SetUpdate(true);
        
        if (popupCard != null)
        {
            seq.Join(popupCard.DOScale(new Vector2(0.5f, 0.5f), duration).SetEase(Ease.InBack));
            seq.Join(popupCard.DOFade(0f, duration));
        }

        if (overlay != null)
        {
            seq.Join(overlay.DOFade(0f, duration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// Hiệu ứng xuất hiện nút Đáp Án lượn sóng (Cascade Slide Up).
    /// </summary>
    public static void AnimateAnswersEntry(List<Button> answerBtns)
    {
        for (int i = 0; i < answerBtns.Count; i++)
        {
            var btn = answerBtns[i];
            btn.style.opacity = 0f;
            btn.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(50))); // Dịch xuống 50px
            
            float delay = i * 0.1f;
            
            DOTween.Sequence()
                .SetDelay(delay)
                .Append(btn.DOFade(1f, 0.3f))
                .Join(btn.DOTranslate(Vector2.zero, 0.4f).SetEase(Ease.OutBack));
        }
    }

    /// <summary>
    /// Nhấp nháy màu background (phản hồi đúng/sai).
    /// Lưu ý: Cần lấy màu ban đầu trước, sau đó trả về.
    /// </summary>
    public static Tweener DOColor(this VisualElement ve, Color endColor, float duration)
    {
        Color startColor = ve.style.backgroundColor.value;
        return DOTween.To(() => startColor, x => {
            ve.style.backgroundColor = x;
        }, endColor, duration).SetTarget(ve);
    }
}
