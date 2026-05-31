using UnityEngine;

/// <summary>
/// Helper class cho haptic feedback (rung điện thoại) trên Android.
/// Sử dụng Android Vibrator API qua JNI.
/// Hỗ trợ 3 mức: Light, Medium, Heavy.
/// </summary>
public static class HapticFeedback
{
    private static bool _isInitialized = false;
    private static bool _isEnabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject _vibrator;
#endif

    /// <summary>Bật/tắt haptic feedback.</summary>
    public static bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    private static void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HapticFeedback] Không thể khởi tạo Vibrator: {e.Message}");
        }
#endif
    }

    /// <summary>
    /// Rung nhẹ — dùng khi trả lời đúng, bấm nút.
    /// ~20ms
    /// </summary>
    public static void Light()
    {
        if (!_isEnabled) return;
        Vibrate(20);
    }

    /// <summary>
    /// Rung trung bình — dùng khi trả lời sai, hết giờ.
    /// ~50ms
    /// </summary>
    public static void Medium()
    {
        if (!_isEnabled) return;
        Vibrate(50);
    }

    /// <summary>
    /// Rung mạnh — dùng khi thắng/thua trận, surrender.
    /// ~100ms
    /// </summary>
    public static void Heavy()
    {
        if (!_isEnabled) return;
        Vibrate(100);
    }

    /// <summary>
    /// Rung pattern — dùng cho streak hoặc sự kiện đặc biệt.
    /// Ví dụ: 3x streak = 3 rung ngắn liên tiếp.
    /// </summary>
    public static void Pattern(long[] pattern)
    {
        if (!_isEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        Initialize();
        if (_vibrator != null)
        {
            try
            {
                _vibrator.Call("vibrate", pattern, -1); // -1 = no repeat
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticFeedback] Pattern vibrate failed: {e.Message}");
            }
        }
#endif
    }

    /// <summary>
    /// Streak haptic — rung nhịp theo số streak.
    /// </summary>
    public static void Streak(int streakCount)
    {
        if (!_isEnabled || streakCount < 2) return;

        int buzzes = Mathf.Min(streakCount, 5);
        long[] pattern = new long[buzzes * 2 + 1];
        pattern[0] = 0; // start immediately
        for (int i = 0; i < buzzes; i++)
        {
            pattern[i * 2 + 1] = 30;  // vibrate 30ms
            pattern[i * 2 + 2] = 60;  // pause 60ms
        }
        Pattern(pattern);
    }

    /// <summary>
    /// Countdown tick haptic — rung rất nhẹ mỗi giây đếm ngược.
    /// </summary>
    public static void CountdownTick()
    {
        if (!_isEnabled) return;
        Vibrate(15);
    }

    private static void Vibrate(long milliseconds)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Initialize();
        if (_vibrator != null)
        {
            try
            {
                _vibrator.Call("vibrate", milliseconds);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticFeedback] Vibrate failed: {e.Message}");
            }
        }
#else
        // Editor/iOS: không làm gì, chỉ log debug
        // Debug.Log($"[HapticFeedback] Vibrate {milliseconds}ms (editor mock)");
#endif
    }
}
