using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// Helper class sử dụng DOTween để tạo hiệu ứng (Animation) cho các thẻ VisualElement của UI Toolkit.
/// Phiên bản nâng cấp: thêm ScreenShake, PulseGlow, BounceIn, SlideFrom, BreathingEffect, CountdownPop, StreakFlash.
/// </summary>
public static class UIAnimator
{
    // ==================== TWEENERS CƠ BẢN ====================

    /// <summary>Làm mờ / Tỏ VisualElement.</summary>
    public static Tweener DOFade(this VisualElement ve, float endValue, float duration)
    {
        float startVal = ve.style.opacity.value;
        return DOTween.To(() => startVal, x => {
            ve.style.opacity = x;
        }, endValue, duration).SetTarget(ve);
    }

    /// <summary>Phóng to / Thu nhỏ VisualElement.</summary>
    public static Tweener DOScale(this VisualElement ve, Vector2 endValue, float duration)
    {
        Vector2 startVal = ve.style.scale.value.value;
        return DOTween.To(() => startVal, x => {
            ve.style.scale = new StyleScale(new Scale(x));
        }, endValue, duration).SetTarget(ve);
    }

    /// <summary>Di chuyển (Translate) VisualElement.</summary>
    public static Tweener DOTranslate(this VisualElement ve, Vector2 endValue, float duration)
    {
        Vector2 startVal = new Vector2(ve.style.translate.value.x.value, ve.style.translate.value.y.value);
        return DOTween.To(() => startVal, x => {
            ve.style.translate = new StyleTranslate(new Translate(new Length(x.x), new Length(x.y)));
        }, endValue, duration).SetTarget(ve);
    }

    /// <summary>Nhấp nháy màu background.</summary>
    public static Tweener DOColor(this VisualElement ve, Color endColor, float duration)
    {
        Color startColor = ve.style.backgroundColor.value;
        return DOTween.To(() => startColor, x => {
            ve.style.backgroundColor = x;
        }, endColor, duration).SetTarget(ve);
    }

    /// <summary>Animate border color.</summary>
    public static Tweener DOBorderColor(this VisualElement ve, Color endColor, float duration)
    {
        Color startColor = ve.style.borderTopColor.value;
        return DOTween.To(() => startColor, x => {
            ve.style.borderTopColor = x;
            ve.style.borderBottomColor = x;
            ve.style.borderLeftColor = x;
            ve.style.borderRightColor = x;
        }, endColor, duration).SetTarget(ve);
    }

    // ==================== SHAKE EFFECTS ====================

    /// <summary>Hiệu ứng rung lắc (Shake) nhẹ khi trả lời sai.</summary>
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

    /// <summary>
    /// Screen Shake — rung toàn bộ root element.
    /// Mạnh hơn DOShakePosition, kèm cả trục Y.
    /// </summary>
    public static Sequence DOScreenShake(this VisualElement root, float duration = 0.4f, float intensity = 12f)
    {
        Sequence seq = DOTween.Sequence();
        int shakes = Mathf.CeilToInt(duration / 0.05f);
        float stepDur = duration / shakes;

        for (int i = 0; i < shakes; i++)
        {
            float decay = 1f - ((float)i / shakes); // Giảm dần
            float x = Random.Range(-intensity, intensity) * decay;
            float y = Random.Range(-intensity * 0.5f, intensity * 0.5f) * decay;
            seq.Append(root.DOTranslate(new Vector2(x, y), stepDur));
        }
        seq.Append(root.DOTranslate(Vector2.zero, stepDur));
        return seq;
    }

    // ==================== POPUP ANIMATIONS ====================

    /// <summary>Hiệu ứng xuất hiện Popup (Nảy lên mượt mà).</summary>
    public static void ShowPopupAnim(VisualElement overlay, VisualElement popupCard)
    {
        if (overlay != null)
        {
            overlay.style.opacity = 0f;
            overlay.DOFade(1f, 0.25f).SetUpdate(true);
        }

        if (popupCard != null)
        {
            popupCard.style.scale = new StyleScale(new Scale(new Vector2(0.5f, 0.5f)));
            popupCard.style.opacity = 0f;
            popupCard.DOFade(1f, 0.2f).SetUpdate(true);
            popupCard.DOScale(Vector2.one, 0.45f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    /// <summary>Hiệu ứng đóng Popup.</summary>
    public static void HidePopupAnim(VisualElement overlay, VisualElement popupCard, System.Action onComplete)
    {
        float duration = 0.2f;
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (popupCard != null)
        {
            seq.Join(popupCard.DOScale(new Vector2(0.6f, 0.6f), duration).SetEase(Ease.InBack));
            seq.Join(popupCard.DOFade(0f, duration));
        }

        if (overlay != null)
        {
            seq.Join(overlay.DOFade(0f, duration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    // ==================== ANSWER ENTRY ANIMATION ====================

    /// <summary>Hiệu ứng xuất hiện nút Đáp Án lượn sóng (Cascade Slide Up).</summary>
    public static void AnimateAnswersEntry(List<Button> answerBtns)
    {
        for (int i = 0; i < answerBtns.Count; i++)
        {
            var btn = answerBtns[i];
            btn.style.opacity = 0f;
            btn.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(60)));
            btn.style.scale = new StyleScale(new Scale(new Vector2(0.9f, 0.9f)));

            float delay = i * 0.08f;

            DOTween.Sequence()
                .SetDelay(delay)
                .Append(btn.DOFade(1f, 0.25f))
                .Join(btn.DOTranslate(Vector2.zero, 0.35f).SetEase(Ease.OutBack))
                .Join(btn.DOScale(Vector2.one, 0.35f).SetEase(Ease.OutBack));
        }
    }

    // ==================== BOUNCE IN ====================

    /// <summary>
    /// Bounce In — element nhảy vào từ scale 0 với hiệu ứng nảy.
    /// Dùng cho trophy icon, countdown number, streak badge.
    /// </summary>
    public static Sequence DOBounceIn(this VisualElement ve, float duration = 0.5f)
    {
        ve.style.scale = new StyleScale(new Scale(Vector2.zero));
        ve.style.opacity = 0f;

        Sequence seq = DOTween.Sequence();
        seq.Join(ve.DOFade(1f, duration * 0.3f));
        seq.Join(ve.DOScale(new Vector2(1.15f, 1.15f), duration * 0.6f).SetEase(Ease.OutBack));
        seq.Append(ve.DOScale(Vector2.one, duration * 0.4f).SetEase(Ease.InOutSine));
        return seq;
    }

    // ==================== SLIDE FROM SIDE ====================

    /// <summary>
    /// Slide element vào từ bên trái hoặc phải.
    /// </summary>
    public static Sequence DOSlideFromLeft(this VisualElement ve, float duration = 0.4f, float distance = 100f)
    {
        ve.style.translate = new StyleTranslate(new Translate(new Length(-distance), new Length(0)));
        ve.style.opacity = 0f;

        Sequence seq = DOTween.Sequence();
        seq.Join(ve.DOFade(1f, duration * 0.5f));
        seq.Join(ve.DOTranslate(Vector2.zero, duration).SetEase(Ease.OutCubic));
        return seq;
    }

    public static Sequence DOSlideFromRight(this VisualElement ve, float duration = 0.4f, float distance = 100f)
    {
        ve.style.translate = new StyleTranslate(new Translate(new Length(distance), new Length(0)));
        ve.style.opacity = 0f;

        Sequence seq = DOTween.Sequence();
        seq.Join(ve.DOFade(1f, duration * 0.5f));
        seq.Join(ve.DOTranslate(Vector2.zero, duration).SetEase(Ease.OutCubic));
        return seq;
    }

    // ==================== PULSE GLOW ====================

    /// <summary>
    /// Pulse Glow — element phát sáng rồi tắt. Dùng cho timer ring khi sắp hết giờ.
    /// </summary>
    public static Sequence DOPulseGlow(this VisualElement ve, Color glowColor, float duration = 0.6f, int loops = 3)
    {
        Color originalBorder = ve.style.borderTopColor.value;

        Sequence seq = DOTween.Sequence();
        for (int i = 0; i < loops; i++)
        {
            seq.Append(ve.DOBorderColor(glowColor, duration * 0.3f));
            seq.Append(ve.DOBorderColor(originalBorder, duration * 0.3f));
        }
        return seq;
    }

    // ==================== BREATHING EFFECT ====================

    /// <summary>
    /// Breathing Effect — scale nhẹ nhàng lên xuống liên tục.
    /// Dùng cho spinner matchmaking, logo, v.v.
    /// </summary>
    public static Tweener DOBreathe(this VisualElement ve, float scaleAmount = 0.05f, float duration = 1.5f)
    {
        return ve.DOScale(new Vector2(1f + scaleAmount, 1f + scaleAmount), duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // ==================== COUNTDOWN POP ====================

    /// <summary>
    /// Countdown Pop — số đếm ngược xuất hiện to rồi thu nhỏ.
    /// </summary>
    public static Sequence DOCountdownPop(this VisualElement ve, float duration = 0.8f)
    {
        ve.style.scale = new StyleScale(new Scale(new Vector2(1.8f, 1.8f)));
        ve.style.opacity = 1f;

        Sequence seq = DOTween.Sequence();
        seq.Append(ve.DOScale(Vector2.one, duration).SetEase(Ease.OutBack));
        return seq;
    }

    // ==================== STREAK FLASH ====================

    /// <summary>
    /// Streak Flash — hiệu ứng flash sáng nhanh rồi mờ. Dùng cho streak toast.
    /// </summary>
    public static Sequence DOStreakFlash(this VisualElement ve, float duration = 0.8f)
    {
        ve.style.opacity = 0f;
        ve.style.scale = new StyleScale(new Scale(new Vector2(0.6f, 0.6f)));

        Sequence seq = DOTween.Sequence();
        seq.Append(ve.DOFade(1f, duration * 0.15f));
        seq.Join(ve.DOScale(new Vector2(1.2f, 1.2f), duration * 0.2f).SetEase(Ease.OutBack));
        seq.Append(ve.DOScale(Vector2.one, duration * 0.15f).SetEase(Ease.InOutSine));
        seq.AppendInterval(duration * 0.3f);
        seq.Append(ve.DOFade(0f, duration * 0.2f));
        seq.Join(ve.DOTranslate(new Vector2(0, -30), duration * 0.2f));
        return seq;
    }

    // ==================== SCORE COUNTER ANIMATION ====================

    /// <summary>
    /// Animate score number tăng dần từ old → new value.
    /// </summary>
    public static Tweener DOCountTo(this Label label, int fromValue, int toValue, float duration)
    {
        int current = fromValue;
        return DOTween.To(
            () => current,
            x => { current = x; label.text = x.ToString(); },
            toValue,
            duration
        ).SetEase(Ease.OutCubic).SetTarget(label);
    }

    // ==================== TIMER RING ANIMATION ====================

    /// <summary>
    /// Animate timer ring border color: normal → urgent (red pulse).
    /// </summary>
    public static void AnimateTimerUrgent(VisualElement timerRing, float remaining)
    {
        if (remaining <= 5f && remaining > 0f)
        {
            Color urgentColor = new Color(1f, 0.32f, 0.32f); // Red
            Color dimColor = new Color(1f, 0.32f, 0.32f, 0.3f);

            timerRing.DOBorderColor(urgentColor, 0.15f).OnComplete(() =>
            {
                timerRing.DOBorderColor(dimColor, 0.35f);
            });
        }
    }

    // ==================== CORRECT/WRONG BUTTON FEEDBACK ====================

    /// <summary>
    /// Hiệu ứng nút đáp án đúng: pulse glow xanh + scale bounce.
    /// </summary>
    public static Sequence DOCorrectFeedback(this VisualElement btn)
    {
        Color correctColor = new Color(0f, 0.9f, 0.46f);
        Color correctGlow = new Color(0f, 0.9f, 0.46f, 0.4f);

        Sequence seq = DOTween.Sequence();
        seq.Append(btn.DOColor(correctColor, 0.15f));
        seq.Join(btn.DOScale(new Vector2(1.06f, 1.06f), 0.15f).SetEase(Ease.OutBack));
        seq.Append(btn.DOScale(Vector2.one, 0.2f).SetEase(Ease.InOutSine));
        seq.Join(btn.DOBorderColor(correctGlow, 0.2f));
        return seq;
    }

    /// <summary>
    /// Hiệu ứng nút đáp án sai: shake + flash đỏ.
    /// </summary>
    public static Sequence DOWrongFeedback(this VisualElement btn)
    {
        Color wrongColor = new Color(1f, 0.2f, 0.27f);

        Sequence seq = DOTween.Sequence();
        seq.Append(btn.DOColor(wrongColor, 0.1f));
        seq.Append(btn.DOShakePosition(0.4f, 12f));
        return seq;
    }
}
