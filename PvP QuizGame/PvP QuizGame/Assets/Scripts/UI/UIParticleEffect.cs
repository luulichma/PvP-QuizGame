using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Hệ thống Particle Effects cho UI Toolkit.
/// Tạo confetti, sparkle, ripple trực tiếp trên VisualElement.
/// Sử dụng DOTween để animate các hạt.
/// </summary>
public class UIParticleEffect : MonoBehaviour
{
    public static UIParticleEffect Instance { get; private set; }

    // Palette confetti
    private static readonly Color[] ConfettiColors = new Color[]
    {
        new Color(1f, 0.84f, 0.28f),    // Gold
        new Color(0f, 0.90f, 0.46f),    // Green
        new Color(0f, 0.90f, 1f),       // Cyan
        new Color(0.70f, 0.53f, 1f),    // Purple
        new Color(1f, 0.32f, 0.42f),    // Pink
        new Color(1f, 0.60f, 0f),       // Orange
        new Color(0.26f, 0.52f, 1f),    // Blue
    };

    // Palette sparkle
    private static readonly Color[] SparkleColors = new Color[]
    {
        new Color(1f, 0.84f, 0.28f),
        new Color(1f, 1f, 1f),
        new Color(0f, 0.90f, 1f),
        new Color(0.70f, 0.53f, 1f),
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Tự động tạo singleton nếu chưa có trên scene.
    /// Gọi từ bất kỳ đâu trước khi dùng Instance.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            var go = new GameObject("[UIParticleEffect]");
            go.AddComponent<UIParticleEffect>();
            Debug.Log("[UIParticleEffect] Auto-initialized singleton.");
        }
    }

    // ==================== CONFETTI BURST ====================

    /// <summary>
    /// Phóng confetti từ giữa màn hình bay tứ phía. Dùng khi THẮNG trận.
    /// </summary>
    public void SpawnConfetti(VisualElement container, int count = 40, float duration = 2.5f)
    {
        if (container == null) return;
        StartCoroutine(ConfettiRoutine(container, count, duration));
    }

    private IEnumerator ConfettiRoutine(VisualElement container, int count, float duration)
    {
        List<VisualElement> particles = new List<VisualElement>();

        for (int i = 0; i < count; i++)
        {
            var p = new VisualElement();
            p.style.position = Position.Absolute;

            // Kích thước random
            float w = Random.Range(8f, 16f);
            float h = Random.Range(12f, 22f);
            p.style.width = w;
            p.style.height = h;

            // Bo góc — tạo hình chữ nhật hoặc tròn random
            bool isRound = Random.value > 0.5f;
            p.style.borderTopLeftRadius = isRound ? w / 2f : 2f;
            p.style.borderTopRightRadius = isRound ? w / 2f : 2f;
            p.style.borderBottomLeftRadius = isRound ? w / 2f : 2f;
            p.style.borderBottomRightRadius = isRound ? w / 2f : 2f;

            // Màu random
            Color c = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
            p.style.backgroundColor = c;

            // Vị trí bắt đầu: trên cùng, random X
            float startX = Random.Range(5f, 95f);
            p.style.left = Length.Percent(startX);
            p.style.top = Length.Percent(Random.Range(-15f, -5f));

            p.style.opacity = 1f;
            p.pickingMode = PickingMode.Ignore;

            container.Add(p);
            particles.Add(p);

            // Animate: rơi xuống + xoay + fade
            float fallDuration = Random.Range(duration * 0.6f, duration);
            float endY = Random.Range(80f, 120f);
            float sway = Random.Range(-20f, 20f);
            float delay = Random.Range(0f, 0.5f);

            float startTop = Random.Range(-15f, -5f);
            float startLeft = startX;

            // Dùng DOTween: animate top% và opacity
            DOTween.To(
                () => startTop,
                y => { if (p.parent != null) p.style.top = Length.Percent(y); },
                endY,
                fallDuration
            ).SetDelay(delay).SetEase(Ease.InQuad);

            DOTween.To(
                () => startLeft,
                x => { if (p.parent != null) p.style.left = Length.Percent(x); },
                startLeft + sway,
                fallDuration
            ).SetDelay(delay).SetEase(Ease.InOutSine);

            // Rotation simulation: scale flip
            int flipCount = Random.Range(2, 6);
            var seq = DOTween.Sequence().SetDelay(delay);
            for (int f = 0; f < flipCount; f++)
            {
                float flipDur = fallDuration / flipCount;
                float scaleX = (f % 2 == 0) ? -1f : 1f;
                seq.Append(
                    DOTween.To(
                        () => 1f,
                        sx => { if (p.parent != null) p.style.scale = new StyleScale(new Scale(new Vector2(sx, 1f))); },
                        scaleX,
                        flipDur
                    ).SetEase(Ease.Linear)
                );
            }

            // Fade out ở nửa sau
            UIAnimator.DOFade(p, 0f, fallDuration * 0.4f)
                .SetDelay(delay + fallDuration * 0.6f);
        }

        yield return new WaitForSeconds(duration + 0.5f);

        // Cleanup
        foreach (var p in particles)
        {
            if (p != null && p.parent != null)
                p.RemoveFromHierarchy();
        }
    }

    // ==================== SPARKLE BURST ====================

    /// <summary>
    /// Hiệu ứng sparkle (tia sáng) tỏa ra từ một điểm. Dùng khi STREAK hoặc trả lời đúng.
    /// </summary>
    public void SpawnSparkle(VisualElement container, float centerXPercent = 50f, float centerYPercent = 50f, int count = 12)
    {
        if (container == null) return;
        StartCoroutine(SparkleRoutine(container, centerXPercent, centerYPercent, count));
    }

    private IEnumerator SparkleRoutine(VisualElement container, float cx, float cy, int count)
    {
        List<VisualElement> particles = new List<VisualElement>();

        for (int i = 0; i < count; i++)
        {
            var p = new VisualElement();
            p.style.position = Position.Absolute;

            float size = Random.Range(6f, 14f);
            p.style.width = size;
            p.style.height = size;
            p.style.borderTopLeftRadius = size / 2f;
            p.style.borderTopRightRadius = size / 2f;
            p.style.borderBottomLeftRadius = size / 2f;
            p.style.borderBottomRightRadius = size / 2f;

            Color c = SparkleColors[Random.Range(0, SparkleColors.Length)];
            p.style.backgroundColor = c;

            p.style.left = Length.Percent(cx);
            p.style.top = Length.Percent(cy);
            p.style.opacity = 1f;
            p.pickingMode = PickingMode.Ignore;

            container.Add(p);
            particles.Add(p);

            // Animate: fly out in random direction
            float angle = (360f / count) * i + Random.Range(-15f, 15f);
            float distance = Random.Range(8f, 25f);
            float rad = angle * Mathf.Deg2Rad;
            float endX = cx + Mathf.Cos(rad) * distance;
            float endY = cy + Mathf.Sin(rad) * distance;
            float dur = Random.Range(0.4f, 0.8f);

            float startX = cx;
            float startY = cy;

            DOTween.To(
                () => startX,
                x => { if (p.parent != null) p.style.left = Length.Percent(x); },
                endX,
                dur
            ).SetEase(Ease.OutCubic);

            DOTween.To(
                () => startY,
                y => { if (p.parent != null) p.style.top = Length.Percent(y); },
                endY,
                dur
            ).SetEase(Ease.OutCubic);

            // Scale down + fade
            DOTween.To(
                () => 1f,
                s => {
                    if (p.parent != null)
                        p.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
                },
                0f,
                dur
            ).SetEase(Ease.InQuad);

            UIAnimator.DOFade(p, 0f, dur * 0.6f).SetDelay(dur * 0.4f);
        }

        yield return new WaitForSeconds(1f);

        foreach (var par in particles)
        {
            if (par != null && par.parent != null)
                par.RemoveFromHierarchy();
        }
    }

    // ==================== RIPPLE EFFECT ====================

    /// <summary>
    /// Hiệu ứng ripple (sóng tròn) tỏa ra khi bấm nút đáp án.
    /// </summary>
    public void SpawnRipple(VisualElement parent, Color color)
    {
        if (parent == null) return;
        StartCoroutine(RippleRoutine(parent, color));
    }

    private IEnumerator RippleRoutine(VisualElement parent, Color color)
    {
        var ring = new VisualElement();
        ring.style.position = Position.Absolute;
        ring.style.width = 40;
        ring.style.height = 40;
        ring.style.borderTopLeftRadius = 20;
        ring.style.borderTopRightRadius = 20;
        ring.style.borderBottomLeftRadius = 20;
        ring.style.borderBottomRightRadius = 20;
        ring.style.borderTopWidth = 3;
        ring.style.borderBottomWidth = 3;
        ring.style.borderLeftWidth = 3;
        ring.style.borderRightWidth = 3;
        ring.style.borderTopColor = color;
        ring.style.borderBottomColor = color;
        ring.style.borderLeftColor = color;
        ring.style.borderRightColor = color;
        ring.style.backgroundColor = Color.clear;
        ring.style.opacity = 0.8f;
        ring.style.alignSelf = Align.Center;
        // Center position
        ring.style.left = Length.Percent(50);
        ring.style.top = Length.Percent(50);
        ring.style.translate = new StyleTranslate(new Translate(new Length(-20), new Length(-20)));
        ring.pickingMode = PickingMode.Ignore;

        parent.Add(ring);

        // Animate: expand + fade out
        DOTween.To(
            () => 1f,
            s => {
                if (ring.parent != null)
                    ring.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
            },
            4f,
            0.6f
        ).SetEase(Ease.OutCubic);

        UIAnimator.DOFade(ring, 0f, 0.6f);

        yield return new WaitForSeconds(0.7f);

        if (ring.parent != null)
            ring.RemoveFromHierarchy();
    }

    // ==================== FLOATING PARTICLES (ambient) ====================

    /// <summary>
    /// Tạo hạt nổi ambient bay nhẹ nhàng trong background. Dùng cho HomeScene.
    /// </summary>
    public void SpawnAmbientParticles(VisualElement container, int count = 15)
    {
        if (container == null) return;
        StartCoroutine(AmbientRoutine(container, count));
    }

    private IEnumerator AmbientRoutine(VisualElement container, int count)
    {
        // Palette bong bóng phát sáng — nhiều màu neon sinh động
        Color[] bubbleColors = new Color[]
        {
            new Color(0.3f, 0.85f, 1f),    // Cyan neon
            new Color(0.6f, 0.4f, 1f),     // Tím lavender
            new Color(0.2f, 1f, 0.7f),     // Xanh mint
            new Color(1f, 0.5f, 0.8f),     // Hồng pastel
            new Color(0.9f, 0.8f, 0.3f),   // Vàng ấm
            new Color(0.4f, 0.6f, 1f),     // Xanh dương sáng
        };

        for (int i = 0; i < count; i++)
        {
            var p = new VisualElement();
            p.style.position = Position.Absolute;

            // === KÍCH THƯỚC TO RÕ RÀNG ===
            float size = Random.Range(14f, 30f);
            p.style.width = size;
            p.style.height = size;
            p.style.borderTopLeftRadius = size / 2f;
            p.style.borderTopRightRadius = size / 2f;
            p.style.borderBottomLeftRadius = size / 2f;
            p.style.borderBottomRightRadius = size / 2f;

            // === MÀU SẮC PHÁT SÁNG MẠNH ===
            Color baseColor = bubbleColors[Random.Range(0, bubbleColors.Length)];
            float alpha = Random.Range(0.35f, 0.7f);
            p.style.backgroundColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            // === GLOW BORDER DÀY & SÁNG ===
            float glowAlpha = Mathf.Min(alpha * 2f, 1f);
            Color glowColor = new Color(baseColor.r * 1.2f, baseColor.g * 1.1f, baseColor.b * 1.2f, glowAlpha);
            p.style.borderTopWidth = 3;
            p.style.borderBottomWidth = 3;
            p.style.borderLeftWidth = 3;
            p.style.borderRightWidth = 3;
            p.style.borderTopColor = glowColor;
            p.style.borderBottomColor = glowColor;
            p.style.borderLeftColor = glowColor;
            p.style.borderRightColor = glowColor;

            // === BẮT ĐẦU TỪ DƯỚI ĐÁY → BAY LÊN ===
            float startX = Random.Range(2f, 98f);
            float startY = Random.Range(85f, 115f); // Bắt đầu từ dưới đáy (hoặc ngoài màn hình)
            p.style.left = Length.Percent(startX);
            p.style.top = Length.Percent(startY);
            p.style.opacity = 0f;
            p.pickingMode = PickingMode.Ignore;

            container.Add(p);

            // === ANIMATION: Bay lên nhanh, rõ ràng ===
            float duration = Random.Range(2.5f, 5.5f);       // Bay nhanh
            float delay = Random.Range(0f, 2.5f);             // Spawn lệch nhau liên tục
            float endY = Random.Range(-20f, 15f);              // Bay hết lên trên (ra khỏi màn hình)
            float driftX = Random.Range(-8f, 8f);              // Lắc ngang nhẹ cho tự nhiên

            float currentY = startY;
            float currentX = startX;

            // Bay lên (trục Y)
            DOTween.To(
                () => currentY,
                y => { if (p.parent != null) { currentY = y; p.style.top = Length.Percent(y); } },
                endY,
                duration
            ).SetDelay(delay).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Restart);

            // Lắc ngang nhẹ (trục X) — tạo cảm giác bong bóng trôi tự nhiên
            DOTween.To(
                () => currentX,
                x => { if (p.parent != null) { currentX = x; p.style.left = Length.Percent(x); } },
                startX + driftX,
                duration * 0.5f
            ).SetDelay(delay).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

            // Pulse opacity: hiện dần → giữ sáng → mờ đi khi gần đỉnh
            DOTween.Sequence()
                .SetDelay(delay)
                .Append(UIAnimator.DOFade(p, Mathf.Min(alpha * 5f, 1f), duration * 0.2f))  // Hiện nhanh
                .AppendInterval(duration * 0.5f)                                             // Giữ sáng lâu
                .Append(UIAnimator.DOFade(p, 0f, duration * 0.3f))                           // Mờ dần
                .SetLoops(-1, LoopType.Restart);
        }

        yield break;
    }
}
