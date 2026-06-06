# PLAN: UI IMPROVEMENTS — GameplayScene
> Phase 2 · Ưu tiên cao · Tác giả: AI Review

---

## MỤC TIÊU

Hai cải tiến độc lập nhau trong `GameplayScene`:
1. **HUD scaling** — tăng kích thước player/bot info card và chữ trong HUD.
2. **Timer effect** — thay hiệu ứng timer bằng vòng tròn năng lượng cạn kiệt theo cung arc.

---

## CẢI TIẾN 1 — HUD Player/Bot Info Card (Tăng kích thước)

### Phân tích hiện trạng
- `GameplayLayout.uxml` → `p1-info` / `p2-info` dùng class `.player-card`
- Avatar: `width: 64px; height: 64px` (inline style)
- Label tên (p1-label / p2-label): `font-size: 16px`
- Label điểm (p1-score / p2-score): `font-size: 36px`
- `.player-card` trong `GlobalStyles.uss`: `padding: 10px 20px`, `border-radius: 20px`

### Thay đổi cần làm

#### File: `GameplayLayout.uxml`

**Avatar** — tăng từ 64px → 80px:
```xml
<!-- Trước -->
style="width: 64px; height: 64px; border-radius: 32px; margin-right: 12px;"
<!-- Sau -->
style="width: 80px; height: 80px; border-radius: 40px; margin-right: 14px;"
```
Áp dụng cho cả `p1-avatar` và `p2-avatar` (p2 dùng `margin-left` thay `margin-right`).

**Label tên** (p1-label, p2-label) — tăng từ 16px → 20px:
```xml
style="color: rgba(255, 255, 255, 0.5); font-size: 20px; letter-spacing: 1px;"
```

**Label điểm** (p1-score, p2-score) — tăng từ 36px → 44px:
```xml
style="color: white; font-size: 44px; -unity-font-style: bold;"
```

**Label trạng thái đối thủ** (p2-status) — tăng từ 14px → 18px:
```xml
style="color: rgba(255, 255, 255, 0.35); font-size: 18px; -unity-text-align: upper-right;"
```

#### File: `GlobalStyles.uss` — Class `.player-card`

```css
/* Trước */
.player-card {
    padding: 10px 20px;
    border-radius: 20px;
}
/* Sau */
.player-card {
    padding: 14px 24px;
    border-radius: 24px;
}
```

#### File: `GlobalStyles.uss` — Class `.avatar-ring`

```css
/* Thêm/cập nhật */
.avatar-ring {
    border-width: 4px;   /* tăng từ 3px để viền nổi hơn với avatar lớn hơn */
}
```

### Lưu ý
- Sau khi tăng size, HUD có thể bị ép quá ngang trên màn hình nhỏ. Nếu xảy ra, thêm `flex-shrink: 1` cho `p1-info` và `p2-info`.
- `timer-container` giữ nguyên ở giữa, không cần thay đổi layout.

---

## CẢI TIẾN 2 — Timer Effect: Energy Drain Arc

### Phân tích hiện trạng
- Hiệu ứng hiện tại: `_timerFill.style.width = Length.Percent(percent)` → co width ngang rất xấu.
- `timer-ring-fill` chỉ là một `VisualElement` đơn giản với border, không phải arc thật sự.
- `GameplayUIController_UXML.cs` subscribe `OnTimerTick` → `HandleTimerTick`.

### Thiết kế hiệu ứng mới
> *"Vòng tròn chầm chậm xoay. Đường viền phát sáng khuyết dần theo chiều kim đồng hồ — như dải năng lượng đang bị vắt kiệt. Vệt sáng rút ngắn, mỏng dần, cho đến khi chỉ còn là đốm sáng rồi vụt tắt."*

**Kỹ thuật:** Custom `VisualElement` với `generateVisualContent` override → vẽ arc (cung tròn) trực tiếp bằng `MeshGenerationContext` (chuẩn UI Toolkit). Không cần plugin, không cần UGUI.

---

### Bước 1 — Tạo class `TimerArcElement.cs`

**File mới:** `Assets/Scripts/UI/TimerArcElement.cs`

```csharp
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom VisualElement vẽ arc (cung tròn) timer theo kiểu "energy drain".
/// Fill giảm dần theo chiều kim đồng hồ bắt đầu từ đỉnh (12 giờ).
/// </summary>
public class TimerArcElement : VisualElement
{
    // ==================== PROPERTIES ====================
    private float _fillAmount = 1f;  // 0..1
    public float FillAmount
    {
        get => _fillAmount;
        set
        {
            _fillAmount = Mathf.Clamp01(value);
            MarkDirtyRepaint();
        }
    }

    private Color _arcColor = new Color(0f, 0.898f, 1f);   // Cyan mặc định
    public Color ArcColor
    {
        get => _arcColor;
        set { _arcColor = value; MarkDirtyRepaint(); }
    }

    private float _strokeWidth = 8f;
    public float StrokeWidth
    {
        get => _strokeWidth;
        set { _strokeWidth = value; MarkDirtyRepaint(); }
    }

    // ==================== CONSTRUCTOR ====================
    public new class UxmlFactory : UxmlFactory<TimerArcElement, UxmlTraits> { }

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
        if (w <= 0 || h <= 0 || _fillAmount <= 0f) return;

        var painter = ctx.painter2D;
        float cx = w / 2f;
        float cy = h / 2f;
        float radius = Mathf.Min(cx, cy) - _strokeWidth / 2f;

        // Góc bắt đầu: 12 giờ (-90°), chiều kim đồng hồ
        float startAngle = -90f;
        float sweepAngle = 360f * _fillAmount;   // chiều kim đồng hồ
        float endAngle   = startAngle + sweepAngle;

        // Màu với glow: vẽ 3 lớp (outer glow, mid, bright core)
        DrawArcLayer(painter, cx, cy, radius + 3f, startAngle, endAngle,
                     new Color(_arcColor.r, _arcColor.g, _arcColor.b, 0.2f), _strokeWidth + 8f);
        DrawArcLayer(painter, cx, cy, radius, startAngle, endAngle,
                     new Color(_arcColor.r, _arcColor.g, _arcColor.b, 0.65f), _strokeWidth);
        DrawArcLayer(painter, cx, cy, radius, startAngle, endAngle,
                     new Color(_arcColor.r, _arcColor.g, _arcColor.b, 1f), _strokeWidth * 0.45f);

        // Đầu vệt sáng (bright tip) — đốm sáng chạy theo đầu cung
        if (_fillAmount > 0.02f)
        {
            float tipRad = Mathf.Deg2Rad * (endAngle);
            float tipX = cx + radius * Mathf.Cos(tipRad);
            float tipY = cy + radius * Mathf.Sin(tipRad);
            DrawDot(painter, tipX, tipY,
                    new Color(1f, 1f, 1f, 0.9f), _strokeWidth * 0.7f);
        }
    }

    private void DrawArcLayer(Painter2D p, float cx, float cy, float r,
                               float startDeg, float endDeg, Color color, float thickness)
    {
        p.strokeColor = color;
        p.lineWidth = thickness;
        p.lineCap = LineCap.Round;

        p.BeginPath();
        // Chia arc thành các đoạn nhỏ
        int segments = Mathf.CeilToInt(Mathf.Abs(endDeg - startDeg) / 3f);
        segments = Mathf.Max(segments, 1);
        float step = (endDeg - startDeg) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float deg = startDeg + step * i;
            float rad = Mathf.Deg2Rad * deg;
            float x = cx + r * Mathf.Cos(rad);
            float y = cy + r * Mathf.Sin(rad);
            if (i == 0) p.MoveTo(new Vector2(x, y));
            else        p.LineTo(new Vector2(x, y));
        }
        p.Stroke();
    }

    private void DrawDot(Painter2D p, float x, float y, Color color, float radius)
    {
        p.fillColor = color;
        p.BeginPath();
        p.Arc(new Vector2(x, y), radius / 2f, 0f, 360f);
        p.Fill();
    }
}
```

---

### Bước 2 — Cập nhật `GameplayLayout.uxml`

Thay khối `timer-container` hiện tại:
```xml
<!-- XÓA timer-ring-fill cũ, thêm TimerArcElement -->
<ui:VisualElement name="timer-container"
    style="align-items: center; justify-content: center; width: 120px; height: 120px;">

    <!-- Nền tối -->
    <ui:VisualElement name="timer-ring-bg" class="timer-ring"
        style="position: absolute; width: 100%; height: 100%;" />

    <!-- Arc element mới — vẽ bằng code -->
    <!-- Thêm runtime trong GameplayUIController_UXML.cs (xem Bước 3) -->

    <ui:Label name="timer-text" text="15"
        style="color: white; font-size: 38px; -unity-font-style: bold;" />
</ui:VisualElement>
```
> **Lưu ý:** `TimerArcElement` được thêm vào runtime (không phải UXML) vì nó là custom element không có UxmlFactory đơn giản — hoặc có thể đăng ký `UxmlFactory` và thêm thẳng vào UXML. Cả hai đều được.

---

### Bước 3 — Cập nhật `GameplayUIController_UXML.cs`

**Thêm field:**
```csharp
private TimerArcElement _timerArc;
private Coroutine _timerRotateCoroutine;
```

**Trong `OnEnable`, sau khi query `_timerContainer`:**
```csharp
// Xóa timer-ring-fill cũ (không còn dùng)
var oldFill = root.Q<VisualElement>("timer-ring-fill");
oldFill?.RemoveFromHierarchy();

// Tạo TimerArcElement và insert vào timer-container
if (_timerContainer != null)
{
    _timerArc = new TimerArcElement();
    _timerArc.StrokeWidth = 8f;
    _timerArc.ArcColor = new Color(0f, 0.898f, 1f);  // Cyan
    _timerContainer.Insert(0, _timerArc);             // Dưới timer-text

    // Bắt đầu rotation nhẹ
    _timerRotateCoroutine = StartCoroutine(RotateTimerSlowly());
}
```

**Coroutine xoay chậm:**
```csharp
private IEnumerator RotateTimerSlowly()
{
    float angle = 0f;
    while (true)
    {
        angle += 18f * Time.deltaTime;   // 18 độ/giây = 1 vòng / 20 giây
        if (_timerContainer != null)
            _timerContainer.style.rotate = new StyleRotate(new Rotate(angle));
        // Bù ngược timerText để chữ số không bị xoay
        if (_timerText != null)
            _timerText.style.rotate = new StyleRotate(new Rotate(-angle));
        yield return null;
    }
}
```

**Cập nhật `HandleTimerTick`** — thay toàn bộ logic `_timerFill.style.width`:
```csharp
private void HandleTimerTick(float remaining)
{
    // Cập nhật số đếm
    if (_timerText != null)
    {
        _timerText.text = TimerController.Instance != null
            ? TimerController.Instance.GetFormattedTime()
            : $"{Mathf.CeilToInt(remaining)}";
    }

    // Cập nhật arc fill (giảm dần về 0)
    if (_timerArc != null && TimerController.Instance != null)
    {
        float fill = remaining / TimerController.Instance.TotalTime;
        _timerArc.FillAmount = fill;

        // Màu chuyển cyan → đỏ khi còn ≤ 5 giây
        if (remaining <= 5f)
        {
            float t = remaining / 5f;   // 1 → 0
            _timerArc.ArcColor = Color.Lerp(new Color(1f, 0.32f, 0.32f),
                                             new Color(0f, 0.898f, 1f), t);
        }
        else
        {
            _timerArc.ArcColor = new Color(0f, 0.898f, 1f);
        }
    }

    // Urgent state (giữ nguyên logic âm thanh + haptic)
    if (remaining <= 5f && !_timerIsUrgent)
    {
        _timerIsUrgent = true;
        if (_timerText != null) _timerText.style.color = new Color(1f, 0.32f, 0.32f);
        if (_timerContainer != null)
            UIAnimator.DOPulseGlow(_timerContainer, new Color(1f, 0.32f, 0.32f, 0.6f), 0.5f, 10);
    }
    else if (remaining > 5f && _timerIsUrgent)
    {
        _timerIsUrgent = false;
        if (_timerText != null) _timerText.style.color = Color.white;
    }

    if (remaining <= 5f && remaining > 0)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.timerUrgentSound);
        HapticFeedback.CountdownTick();
    }
}
```

**Trong `OnDisable`, dừng coroutine:**
```csharp
if (_timerRotateCoroutine != null)
{
    StopCoroutine(_timerRotateCoroutine);
    _timerRotateCoroutine = null;
}
```

**Khi câu hỏi mới bắt đầu** (`HandleQuestionChanged`), reset arc:
```csharp
// Thay thế đoạn reset _timerFill border color cũ bằng:
if (_timerArc != null)
{
    _timerArc.FillAmount = 1f;
    _timerArc.ArcColor = new Color(0f, 0.898f, 1f);
}
if (_timerText != null) _timerText.style.color = Color.white;
```

---

### Bước 4 — Cập nhật `GlobalStyles.uss` — timer-ring-bg

```css
/* Tăng width timer-container để phù hợp arc mới */
.timer-ring {
    width: 120px;   /* tăng từ 110px */
    height: 120px;
    border-radius: 60px;
    border-width: 3px;   /* giảm để nền mờ hơn, arc nổi hơn */
    border-color: rgba(255, 255, 255, 0.07);
    background-color: rgba(0, 0, 0, 0.35);
}
```

---

## CHECKLIST THỰC HIỆN

| # | Task | File | Status |
|---|------|------|--------|
| 1 | Tăng avatar p1/p2 lên 80px | `GameplayLayout.uxml` | ⬜ |
| 2 | Tăng font label tên lên 20px | `GameplayLayout.uxml` | ⬜ |
| 3 | Tăng font điểm lên 44px | `GameplayLayout.uxml` | ⬜ |
| 4 | Tăng font p2-status lên 18px | `GameplayLayout.uxml` | ⬜ |
| 5 | Tăng padding `.player-card` | `GlobalStyles.uss` | ⬜ |
| 6 | Tạo `TimerArcElement.cs` | Scripts/UI/ | ⬜ |
| 7 | Cập nhật UXML timer-container | `GameplayLayout.uxml` | ⬜ |
| 8 | Thêm `_timerArc` field + khởi tạo | `GameplayUIController_UXML.cs` | ⬜ |
| 9 | Thêm `RotateTimerSlowly` coroutine | `GameplayUIController_UXML.cs` | ⬜ |
| 10 | Refactor `HandleTimerTick` | `GameplayUIController_UXML.cs` | ⬜ |
| 11 | Reset arc trong `HandleQuestionChanged` | `GameplayUIController_UXML.cs` | ⬜ |
| 12 | Dừng coroutine trong `OnDisable` | `GameplayUIController_UXML.cs` | ⬜ |
| 13 | Cập nhật `.timer-ring` size | `GlobalStyles.uss` | ⬜ |

---

## RỦI RO & LƯU Ý

- **`Painter2D` arc**: Unity 2022+ hỗ trợ `painter2D.Arc()` trực tiếp. Nếu dùng Unity 2021 thì phải vẽ thủ công bằng `MoveTo/LineTo` (đã có trong code mẫu trên `DrawArcLayer`).
- **Rotation + text**: Chữ số timer bị xoay ngược nếu quên bù `-angle` cho `_timerText`. Cần kiểm tra kỹ.
- **Flex shrink**: Nếu HUD overflow sau khi tăng card size, thêm `flex-shrink: 1; min-width: 0;` cho `p1-info` / `p2-info`.
- **Tốc độ xoay**: 18°/s = 1 vòng / 20 giây. Nếu cảm thấy quá nhanh/chậm, điều chỉnh hằng số này.
