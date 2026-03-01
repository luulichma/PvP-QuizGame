using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Quản lý bộ đếm ngược thời gian trận đấu (mặc định 180 giây).
/// Dùng Coroutine — không block main thread.
/// Attach vào cùng GameObject với GameManager.
/// </summary>
public class TimerController : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static TimerController Instance { get; private set; }

    // ==================== EVENTS ====================
    /// <summary>Phát mỗi giây — tham số: thời gian còn lại (giây)</summary>
    public static event Action<float> OnTimerTick;

    /// <summary>Phát khi đồng hồ về 0</summary>
    public static event Action OnTimerEnd;

    // ==================== INSPECTOR ====================
    [Header("Cài đặt thời gian")]
    [SerializeField] private float totalTime = 180f;

    // ==================== TRẠNG THÁI ====================
    public float RemainingTime { get; private set; }
    public bool IsRunning { get; private set; }

    private Coroutine _timerCoroutine;

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ==================== API CÔNG KHAI ====================
    /// <summary>Bắt đầu đồng hồ đếm ngược từ đầu</summary>
    public void StartTimer()
    {
        StopTimer();
        RemainingTime = totalTime;
        IsRunning = true;
        _timerCoroutine = StartCoroutine(TimerRoutine());
    }

    /// <summary>Dừng hoàn toàn đồng hồ</summary>
    public void StopTimer()
    {
        IsRunning = false;
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
    }

    /// <summary>Tạm dừng đồng hồ (không reset)</summary>
    public void PauseTimer() => IsRunning = false;

    /// <summary>Tiếp tục sau khi tạm dừng</summary>
    public void ResumeTimer() => IsRunning = true;

    // ==================== COROUTINE ====================
    private IEnumerator TimerRoutine()
    {
        while (RemainingTime > 0)
        {
            yield return new WaitForSeconds(1f);

            // Nếu đang pause thì chờ cho đến khi resume
            while (!IsRunning)
                yield return null;

            RemainingTime -= 1f;
            OnTimerTick?.Invoke(RemainingTime);
        }

        IsRunning = false;
        Debug.Log("[TimerController] Hết giờ!");
        OnTimerEnd?.Invoke();
    }

    // ==================== TIỆN ÍCH ====================
    /// <summary>Trả về thời gian còn lại dạng mm:ss</summary>
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(RemainingTime / 60f);
        int seconds = Mathf.FloorToInt(RemainingTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
