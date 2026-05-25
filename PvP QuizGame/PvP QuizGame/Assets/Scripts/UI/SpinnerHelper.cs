using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Điều khiển xoay spinner cho matchmaking (G-14).
/// Dùng C# thay vì USS keyframes vì Unity UI Toolkit hỗ trợ keyframes hạn chế.
/// </summary>
public static class SpinnerHelper
{
    /// <summary>
    /// Bắt đầu hiệu ứng xoay cho VisualElement spinner.
    /// Trả về IEnumerator để caller có thể dừng lại.
    /// </summary>
    public static System.Collections.IEnumerator RotateRoutine(VisualElement spinner)
    {
        if (spinner == null) yield break;

        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            // Xoay 360 độ mỗi 0.8 giây
            float angle = (elapsed % 0.8f) / 0.8f * 360f;
            spinner.style.rotate = new Rotate(Angle.Degrees(angle));
            yield return null;
        }
    }
}
