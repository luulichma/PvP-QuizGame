using System;
using UnityEngine;

/// <summary>
/// [PHASE-2] Quản lý sử dụng Power-Up Consumables trong trận.
///
/// Bộ 3 Power-Up: pu_5050 (✂️), pu_time (⏱️), pu_shield (🛡️).
/// Mỗi trận chỉ cho dùng 1 lần/loại (theo economy-design v2.0 §5.2).
/// Logic local-only — không sync sang đối thủ (xem ghi chú plan_note_new_tier_rank §3).
///
/// Caller flow:
///   1. UI button click → PowerUpManager.Use5050() / UseExtraTime() / UseShield()
///   2. Manager validate (đủ count? đã dùng trong câu/trận?) → trừ count + Save → fire event
///   3. Listener thực thi hiệu ứng:
///      - InputController_UXML xử lý OnPowerUpUsed("pu_5050") → ẩn 2 đáp án sai
///      - TimerController xử lý OnPowerUpUsed("pu_time") → AddTime(+5s)
///      - ScoreManager check IsShieldActive khi sai → giữ streak + ConsumeShield()
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    // ==================== IDs (giữ làm hằng để khớp với localization keys & shop) ====================
    public const string PU_5050   = "pu_5050";
    public const string PU_TIME   = "pu_time";
    public const string PU_SHIELD = "pu_shield";

    // ==================== EVENTS ====================
    /// <summary>Power-up vừa được dùng thành công. param = id ("pu_5050"...).</summary>
    public static event Action<string> OnPowerUpUsed;
    /// <summary>Power-up dùng thất bại (hết hoặc đã dùng trong câu/trận). param = (id, reason).</summary>
    public static event Action<string, string> OnPowerUpFailed;
    /// <summary>Shield đã cứu khỏi 1 lần trả lời sai (consume). Phát sau OnPowerUpUsed nếu shield activate.</summary>
    public static event Action OnShieldSaved;

    // ==================== STATE TRONG TRẬN ====================
    private bool _used5050InMatch;
    private bool _usedTimeInMatch;
    private bool _usedShieldInMatch;

    /// <summary>True từ lúc user bấm Shield đến khi answer sai consume nó.</summary>
    public bool IsShieldActive { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Reset state khi vào trận mới (GameController gọi khi countdown bắt đầu).</summary>
    public void ResetForNewMatch()
    {
        _used5050InMatch = false;
        _usedTimeInMatch = false;
        _usedShieldInMatch = false;
        IsShieldActive = false;
        Debug.Log("[PowerUpManager] Reset state cho trận mới.");
    }

    // ==================== PUBLIC API ====================

    public bool CanUse(string id)
    {
        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null) return false;
        if (pd.GetPowerUpCount(id) <= 0) return false;
        return id switch
        {
            PU_5050   => !_used5050InMatch,
            PU_TIME   => !_usedTimeInMatch,
            PU_SHIELD => !_usedShieldInMatch,
            _ => false
        };
    }

    public bool Use5050()   => TryUse(PU_5050);
    public bool UseExtraTime() => TryUse(PU_TIME);
    public bool UseShield() => TryUse(PU_SHIELD);

    private bool TryUse(string id)
    {
        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null)
        {
            OnPowerUpFailed?.Invoke(id, "no_data");
            return false;
        }

        if (pd.GetPowerUpCount(id) <= 0)
        {
            Debug.LogWarning($"[PowerUpManager] {id} = 0 → hết");
            OnPowerUpFailed?.Invoke(id, "empty");
            return false;
        }

        bool already = id switch
        {
            PU_5050   => _used5050InMatch,
            PU_TIME   => _usedTimeInMatch,
            PU_SHIELD => _usedShieldInMatch,
            _ => true
        };
        if (already)
        {
            Debug.LogWarning($"[PowerUpManager] {id} đã được dùng trong trận này");
            OnPowerUpFailed?.Invoke(id, "already_used");
            return false;
        }

        // Trừ inventory + đánh dấu đã dùng trong trận
        pd.AddPowerUp(id, -1);
        switch (id)
        {
            case PU_5050:   _used5050InMatch = true; break;
            case PU_TIME:   _usedTimeInMatch = true; break;
            case PU_SHIELD: _usedShieldInMatch = true; IsShieldActive = true; break;
        }

        PlayerDataManager.Instance.SaveData();

        Debug.Log($"<color=yellow>[PowerUpManager] Đã dùng {id}. Còn lại: {pd.GetPowerUpCount(id)}</color>");
        OnPowerUpUsed?.Invoke(id);
        return true;
    }

    /// <summary>Gọi từ ScoreManager khi answer sai và shield đang active.</summary>
    public void ConsumeShield()
    {
        if (!IsShieldActive) return;
        IsShieldActive = false;
        Debug.Log("<color=cyan>[PowerUpManager] Shield đã cứu 1 lần sai → consume.</color>");
        OnShieldSaved?.Invoke();
    }

    // ==================== STATE QUERY (cho UI) ====================
    public bool HasUsed5050InMatch => _used5050InMatch;
    public bool HasUsedTimeInMatch => _usedTimeInMatch;
    public bool HasUsedShieldInMatch => _usedShieldInMatch;
}
