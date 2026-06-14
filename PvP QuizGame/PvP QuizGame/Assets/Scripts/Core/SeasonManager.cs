using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

/// <summary>
/// [PHASE-2] Quản lý mùa giải (Seasonal Ranked) theo economy-design v2.0 §3.
///
/// Phương án: Client-driven reset (§3.8) — không cần Cloud Functions.
/// Khi vào HomeScene và đã authenticated:
///   1) Đọc /seasonConfig (currentSeason, seasonStartDate, seasonEndDate)
///   2) Nếu UtcNow > seasonEndDate && lastSeasonProcessed < currentSeason:
///      - Snapshot vào /seasonArchive/season_{N}/{uid}
///      - Tính reward theo highestTierThisSeason (bảng §3.4)
///      - Cộng money + power-up, giáng 2 tier, set RP về mốc tier mới
///      - Append badge vào seasonBadges
///      - lastSeasonProcessed = currentSeason
///      - SaveData + SaveProfileToCloud
///      - Fire OnSeasonEnded để UI hiện popup
///
/// Fallback nếu /seasonConfig không tồn tại: dùng default 30 ngày từ ngày tạo file.
/// </summary>
public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }

    // ==================== EVENTS ====================
    public struct EndOfSeasonResult
    {
        public int seasonId;            // mùa vừa kết thúc
        public int highestTier;         // tier cao nhất trong mùa
        public int newTier;             // tier sau giáng
        public int rewardMoney;
        public Dictionary<string, int> rewardPowerUps; // id → qty
        public string badge;            // [Icon Fix] Format mới "T{tier}S{season}", VD "T4S1"
    }
    public static event Action<EndOfSeasonResult> OnSeasonEnded;

    // ==================== STATE ====================
    public int CurrentSeason { get; private set; } = 1;
    public DateTime SeasonStartUtc { get; private set; } = DateTime.UtcNow;
    public DateTime SeasonEndUtc   { get; private set; } = DateTime.UtcNow.AddDays(30);

    // [IM] Intermission state — admin có thể set qua Firebase Console để teaser mùa mới
    public int NextSeasonId { get; private set; } = 0;
    public DateTime? NextSeasonStartUtc { get; private set; } = null;
    public string IntermissionMessage { get; private set; } = "";

    public int DaysLeftInSeason
    {
        get
        {
            var diff = SeasonEndUtc - DateTime.UtcNow;
            return Mathf.Max(0, (int)Math.Ceiling(diff.TotalDays));
        }
    }

    /// <summary>
    /// [IM] True khi mùa cũ đã hết VÀ user đã xử lý reset xong → chờ admin mở mùa mới.
    /// Trong state này: RP không thay đổi, BXH bị freeze, ScoreManager bù bằng Money+EXP.
    /// </summary>
    public bool IsIntermission
    {
        get
        {
            var pd = PlayerDataManager.Instance?.Data;
            if (pd == null) return false;
            return DateTime.UtcNow >= SeasonEndUtc && pd.lastSeasonProcessed >= CurrentSeason;
        }
    }

    /// <summary>[IM] Thời gian còn lại đến khi mùa mới mở (nếu admin đã set nextSeasonStartDate).</summary>
    public TimeSpan? TimeUntilNextSeason
    {
        get
        {
            if (!NextSeasonStartUtc.HasValue) return null;
            var ts = NextSeasonStartUtc.Value - DateTime.UtcNow;
            return ts.TotalSeconds > 0 ? ts : TimeSpan.Zero;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Gọi sau khi Auth thành công (HomeScene). Trả về Task để caller có thể await nếu cần.
    /// Mặc định fire-and-forget — UI sẽ nhận event nếu có reset xảy ra.
    /// </summary>
    public async Task CheckSeasonAsync()
    {
        await LoadSeasonConfigAsync();
        TryProcessEndOfSeason();
    }

    private async Task LoadSeasonConfigAsync()
    {
        var fb = FirebaseManager.Instance;
        if (fb == null || !fb.IsConnected || !fb.IsAuthenticated)
        {
            // Offline: dùng default cấu hình cứng để Home vẫn hiển thị countdown
            return;
        }

        try
        {
            var root = FirebaseManager.Instance.GetRootRef();
            var snap = await root.Child("seasonConfig").GetValueAsync();
            if (!snap.Exists)
            {
                // Tạo mặc định nếu admin chưa setup — không write lên cloud từ client
                Debug.LogWarning("[SeasonManager] /seasonConfig chưa tồn tại — dùng default 30 ngày.");
                return;
            }

            if (snap.Child("currentSeason").Value != null)
                CurrentSeason = int.Parse(snap.Child("currentSeason").Value.ToString());

            if (snap.Child("seasonStartDate").Value != null
                && DateTime.TryParse(snap.Child("seasonStartDate").Value.ToString(),
                    null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var start))
            {
                SeasonStartUtc = start.ToUniversalTime();
            }

            if (snap.Child("seasonEndDate").Value != null
                && DateTime.TryParse(snap.Child("seasonEndDate").Value.ToString(),
                    null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var end))
            {
                SeasonEndUtc = end.ToUniversalTime();
            }

            // [IM] Intermission optional fields
            NextSeasonId = 0;
            NextSeasonStartUtc = null;
            IntermissionMessage = "";

            if (snap.Child("nextSeasonId").Value != null
                && int.TryParse(snap.Child("nextSeasonId").Value.ToString(), out var nsId))
            {
                NextSeasonId = nsId;
            }

            if (snap.Child("nextSeasonStartDate").Value != null
                && DateTime.TryParse(snap.Child("nextSeasonStartDate").Value.ToString(),
                    null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var nsStart))
            {
                NextSeasonStartUtc = nsStart.ToUniversalTime();
            }

            if (snap.Child("intermissionMessage").Value != null)
                IntermissionMessage = snap.Child("intermissionMessage").Value.ToString();

            Debug.Log($"[SeasonManager] Season {CurrentSeason} — kết thúc {SeasonEndUtc:o} (còn {DaysLeftInSeason} ngày). NextSeason={NextSeasonId}, NextStart={NextSeasonStartUtc?.ToString("o") ?? "n/a"}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SeasonManager] Lỗi load seasonConfig: {ex.Message}");
        }
    }

    private void TryProcessEndOfSeason()
    {
        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null) return;

        // Đã hết mùa và chưa xử lý cho user này
        if (DateTime.UtcNow <= SeasonEndUtc) return;
        if (pd.lastSeasonProcessed >= CurrentSeason) return;

        Debug.Log($"<color=magenta>[SeasonManager] Mùa {CurrentSeason} đã kết thúc — bắt đầu xử lý cho user này.</color>");

        int highestTier = Mathf.Clamp(pd.highestTierThisSeason, 1, 5);
        int newTier = Mathf.Max(1, pd.currentTier - 2);

        // Reward theo bảng §3.4
        int rewardMoney = highestTier switch
        {
            1 => 200,
            2 => 500,
            3 => 1000,
            4 => 2000,
            5 => 5000,
            _ => 0
        };
        var rewardPU = new Dictionary<string, int>();
        switch (highestTier)
        {
            case 2:
                rewardPU[PowerUpManager.PU_TIME] = 2; break;
            case 3:
                rewardPU[PowerUpManager.PU_5050] = 2;
                rewardPU[PowerUpManager.PU_TIME] = 2; break;
            case 4:
                rewardPU[PowerUpManager.PU_5050] = 3;
                rewardPU[PowerUpManager.PU_TIME] = 3;
                rewardPU[PowerUpManager.PU_SHIELD] = 2; break;
            case 5:
                rewardPU[PowerUpManager.PU_5050] = 5;
                rewardPU[PowerUpManager.PU_TIME] = 5;
                rewardPU[PowerUpManager.PU_SHIELD] = 5; break;
        }

        // Cộng thưởng
        pd.AddMoney(rewardMoney);
        foreach (var kv in rewardPU) pd.AddPowerUp(kv.Key, kv.Value);

        // RP sau giáng = mốc khởi đầu tier mới (§3.5)
        int newRP = newTier switch
        {
            1 => 0,
            2 => 500,
            3 => 1500,
            4 => 3000,
            5 => 5000,
            _ => 0
        };
        pd.rankPoints = newRP;
        pd.currentTier = newTier;
        pd.highestTierThisSeason = newTier;

        // [Icon Fix] Badge format mới "T{tier}S{season}" — không còn emoji.
        // UI sẽ parse tier để render PNG icon (xem EndOfSeasonPopupController).
        string badge = highestTier > 0 ? $"T{highestTier}S{CurrentSeason}" : "";
        if (!string.IsNullOrEmpty(badge)) pd.AppendSeasonBadge(badge);

        // Đánh dấu đã xử lý
        pd.lastSeasonProcessed = CurrentSeason;

        // Persist local + cloud
        PlayerDataManager.Instance.SaveData();

        // Snapshot lên cloud + push profile
        _ = ArchiveAndSyncAsync(pd, highestTier);

        // Fire UI event
        OnSeasonEnded?.Invoke(new EndOfSeasonResult
        {
            seasonId = CurrentSeason,
            highestTier = highestTier,
            newTier = newTier,
            rewardMoney = rewardMoney,
            rewardPowerUps = rewardPU,
            badge = badge
        });
    }

    private async Task ArchiveAndSyncAsync(PlayerData pd, int highestTier)
    {
        var fb = FirebaseManager.Instance;
        if (fb == null || !fb.IsConnected || !fb.IsAuthenticated) return;

        try
        {
            var root = FirebaseManager.Instance.GetRootRef();
            var archive = new Dictionary<string, object>
            {
                { "finalTier", pd.currentTier },        // Tier sau giáng
                { "finalRP", pd.rankPoints },
                { "highestTier", highestTier },
                { "rewardsClaimed", true },
                { "processedAt", ServerValue.Timestamp }
            };
            await root.Child("seasonArchive").Child($"season_{CurrentSeason}").Child(fb.LocalUserId)
                .UpdateChildrenAsync(archive);

            await fb.SaveProfileToCloud();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SeasonManager] Archive/Sync lỗi: {ex.Message}");
        }
    }
}
