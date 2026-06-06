using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom VisualElement vẽ cung tròn (arc) timer kiểu "energy drain".
/// Fill giảm dần theo chiều kim đồng hồ bắt đầu từ 12 giờ.
/// Hiệu ứng 3 lớp: outer glow mờ → mid → bright core để tạo cảm giác phát sáng.
/// Đầu cung có đốm sáng chạy theo để tăng hiệu ứng "vệt năng lượng".
/// </summary>
public class TimerArcElement : VisualElement
{
    // ==================== PROPERTIES ====================

    private float _fillAmount = 1f;
    /// <summary>Tỉ lệ cung còn lại, 0..1. 1 = đầy, 0 = trống.</summary>
    public float FillAmount
    {
        get => _fillAmount;
        set
        {
            _fillAmount = Mathf.Clamp01(value);
            MarkDirtyRepaint();
        }
    }

    private Color _arcColor = new Color(0f, 0.898f, 1f); // Cyan mặc định
    /// <summary>Màu chủ đạo của cung (glow sẽ tự điều chỉnh alpha theo màu này).</summary>
    public Color ArcColor
    {
        get => _arcColor;
        set { _arcColor = value; MarkDirtyRepaint(); }
    }

    private float _strokeWidth = 8f;
    /// <summary>Độ dày nét vẽ lớp giữa (bright core sẽ mỏng hơn, glow dày hơn).</summary>
    public float StrokeWidth
    {
        get => _strokeWidth;
        set { _strokeWidth = value; MarkDirtyRepaint(); }
    }

    // ==================== UXML FACTORY ====================
    public new class UxmlFactory : UxmlFactory<TimerArcElement, UxmlTraits> { }

    // ==================== CONSTRUCTOR ====================
    public TimerArcElement()
    {
        generateVisualContent += OnGenerateVisualContent;
        style.position = Position.Absolute;
        style.width = Length.Percent(100);
        style.height = Length.Percent(100);
        pickingMode = PickingMode.Ignore;
    }

    // ==================== DRAW ====================
    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        float w = resolvedStyle.width;
        float h = resolvedStyle.height;
        if (w <= 0 || h <= 0 || _fillAmount <= 0.001f) return;

        var painter = ctx.painter2D;
        float cx = w / 2f;
        float cy = h / 2f;
        float radius = Mathf.Min(cx, cy) - _strokeWidth / 2f - 4f;

        // Bắt đầu từ 12 giờ (-90°), quét thuận chiều kim đồng hồ
        float startDeg = -90f;
        float sweepDeg = 360f * _fillAmount;
        float endDeg   = startDeg + sweepDeg;

        // Layer 1: outer glow rộng + mờ
        DrawArc(painter, cx, cy, radius, startDeg, endDeg,
            new Color(_arcColor.r, _arcColor.g, _arcColor.b, 0.18f),
            _strokeWidth + 10f);

        // Layer 2: mid glow
        DrawArc(painter, cx, cy, radius, startDeg, endDeg,
            new Color(_arcColor.r, _arcColor.g, _arcColor.b, 0.60f),
            _strokeWidth);

        // Layer 3: bright core mỏng
        DrawArc(painter, cx, cy, radius, startDeg, endDeg,
            new Color(_arcColor.r, _arcColor.g, _arcColor.b, 1f),
            _strokeWidth * 0.40f);

        // Đốm sáng ở đầu cung (tip)
        if (_fillAmount > 0.015f)
        {
            float tipRad = Mathf.Deg2Rad * endDeg;
            float tipX   = cx + radius * Mathf.Cos(tipRad);
            float tipY   = cy + radius * Mathf.Sin(tipRad);
            DrawDot(painter, tipX, tipY,
                new Color(1f, 1f, 1f, 0.95f),
                _strokeWidth * 0.65f);
        }
    }

    /// <summary>Vẽ một cung tròn từ startDeg đến endDeg (độ, chiều kim đồng hồ).</summary>
    private static void DrawArc(Painter2D painter,
                                 float cx, float cy, float radius,
                                 float startDeg, float endDeg,
                                 Color color, float thickness)
    {
        if (radius <= 0 || thickness <= 0) return;

        painter.strokeColor = color;
        painter.lineWidth   = thickness;
        painter.lineCap     = LineCap.Round;

        // Chia thành các đoạn nhỏ 2° để cung mượt
        int steps = Mathf.Max(Mathf.CeilToInt(Mathf.Abs(endDeg - startDeg) / 2f), 1);
        float stepDeg = (endDeg - startDeg) / steps;

        painter.BeginPath();
        for (int i = 0; i <= steps; i++)
        {
            float deg = startDeg + stepDeg * i;
            float rad = Mathf.Deg2Rad * deg;
            float x   = cx + radius * Mathf.Cos(rad);
            float y   = cy + radius * Mathf.Sin(rad);
            if (i == 0) painter.MoveTo(new Vector2(x, y));
            else        painter.LineTo(new Vector2(x, y));
        }
        painter.Stroke();
    }

    /// <summary>Vẽ đốm tròn nhỏ (tip sáng ở đầu cung).</summary>
    private static void DrawDot(Painter2D painter,
                                 float x, float y, Color color, float dotRadius)
    {
        if (dotRadius <= 0) return;
        painter.fillColor = color;
        painter.BeginPath();
        painter.Arc(new Vector2(x, y), dotRadius / 2f, 0f, 360f);
        painter.Fill();
    }
}
