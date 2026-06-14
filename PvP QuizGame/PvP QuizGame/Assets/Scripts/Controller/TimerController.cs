using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Quản lý bộ đếm ngược thời gian mỗi câu hỏi (mặc định 15 giây).
/// Dùng Coroutine — không block main thread.
/// Attach vào cùng GameObject với GameController.
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
    [SerializeField] private float totalTime = 15f; // BUG-06 FIX: Đổi từ 180f -> 15f (mặc định theo Remote Config)
    public float TotalTime => totalTime;

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

    private void OnEnable()
    {
        // [PHASE-2] Lắng power-up "Thêm giờ"
        PowerUpManager.OnPowerUpUsed += HandlePowerUpUsed;
    }

    private void OnDisable()
    {
        PowerUpManager.OnPowerUpUsed -= HandlePowerUpUsed;
    }

    private void HandlePowerUpUsed(string id)
    {
        if (id != PowerUpManager.PU_TIME) return;
        AddTime(5f);
    }

    // ==================== API CÔNG KHAI ====================
    /// <summary>Bắt đầu đồng hồ đếm ngược từ đầu</summary>
    public void StartTimer()
    {
        StopTimer();

        // Lấy thời gian từ FirebaseManager (Remote Config) cho cả Online/Offline
        if (FirebaseManager.Instance != null)
        {
            totalTime = FirebaseManager.Instance.QuestionDuration;
        }
        
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

    /// <summary>
    /// [PHASE-2] Cộng thêm thời gian cho câu hiện tại (dùng cho Power-Up "Thêm giờ").
    /// Không vượt quá totalTime + 30 (sanity cap), không cộng nếu timer không chạy.
    /// </summary>
    public void AddTime(float seconds)
    {
        if (!IsRunning || seconds <= 0) return;
        RemainingTime = Mathf.Min(RemainingTime + seconds, totalTime + 30f);
        // Phát tick để UI update ngay (TimerArc + label)
        OnTimerTick?.Invoke(RemainingTime);
        Debug.Log($"<color=yellow>[TimerController] +{seconds}s → {RemainingTime}s còn lại</color>");
    }

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
    /// <summary>Trả về thời gian còn lại dạng ss (vì timer luôn < 60s)</summary>
    public string GetFormattedTime()
    {
        int seconds = Mathf.FloorToInt(RemainingTime % 60f);
        return $"{seconds}"; // BUG-06 FIX: Chỉ hiển thị giây, không cần mm:ss
    }
}
