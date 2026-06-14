using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [Header("Data References")]
    [SerializeField] private PlayerData playerData;

    public PlayerData Data => playerData;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    /// <summary>
    /// Lưu dữ liệu vào PlayerPrefs (Sẽ thay bằng Firebase sau này)
    /// </summary>
    public void SaveData()
    {
        if (playerData == null) return;

        PlayerPrefs.SetInt("PlayerLevel", playerData.level);
        PlayerPrefs.SetInt("PlayerExp", playerData.currentExp);
        PlayerPrefs.SetInt("PlayerMoney", playerData.money);
        PlayerPrefs.SetInt("PlayerRankPoints", playerData.rankPoints);
        PlayerPrefs.SetInt("PlayerAvatar", playerData.avatarIndex);
        PlayerPrefs.SetString("PlayerName", playerData.playerName);

        // Achievements
        PlayerPrefs.SetInt("PlayerBotWins", playerData.botWins);
        PlayerPrefs.SetInt("PlayerTotalMoney", playerData.totalMoneyEarned);
        PlayerPrefs.SetInt("PlayerWinStreak", playerData.currentWinStreak);
        PlayerPrefs.SetInt("PlayerHighestStreak", playerData.highestWinStreak);
        PlayerPrefs.SetString("PlayerUnlockedAchievements", string.Join(",", playerData.unlockedAchievements));

        // [PHASE-2] Power-Up inventory
        PlayerPrefs.SetInt("PlayerPU_5050",   playerData.powerUp_5050);
        PlayerPrefs.SetInt("PlayerPU_Time",   playerData.powerUp_extraTime);
        PlayerPrefs.SetInt("PlayerPU_Shield", playerData.powerUp_shield);

        // [PHASE-2] Tier & Season
        PlayerPrefs.SetInt("PlayerCurrentTier",        playerData.currentTier);
        PlayerPrefs.SetInt("PlayerHighestTierSeason",  playerData.highestTierThisSeason);
        PlayerPrefs.SetInt("PlayerLastSeasonProcessed", playerData.lastSeasonProcessed);
        PlayerPrefs.SetString("PlayerSeasonBadges",    playerData.seasonBadges ?? "");

        // [PHASE-2] Daily Quests
        PlayerPrefs.SetString("PlayerDailyQuests", playerData.dailyQuestsData ?? "");

        PlayerPrefs.Save();

        Debug.Log("[PlayerDataManager] Đã lưu dữ liệu người chơi.");
    }

    /// <summary>
    /// Tải dữ liệu từ PlayerPrefs
    /// </summary>
    public void LoadData()
    {
        if (playerData == null)
        {
            Debug.LogError("[PlayerDataManager] Chưa gán PlayerData ScriptableObject!");
            return;
        }

        playerData.level = PlayerPrefs.GetInt("PlayerLevel", 1);
        playerData.currentExp = PlayerPrefs.GetInt("PlayerExp", 0);
        playerData.money = PlayerPrefs.GetInt("PlayerMoney", 0);
        playerData.rankPoints = PlayerPrefs.GetInt("PlayerRankPoints", 0);
        playerData.avatarIndex = PlayerPrefs.GetInt("PlayerAvatar", 0);
        playerData.playerName = PlayerPrefs.GetString("PlayerName", "Player_" + Random.Range(1000, 9999));

        // Achievements
        playerData.botWins = PlayerPrefs.GetInt("PlayerBotWins", 0);
        playerData.totalMoneyEarned = PlayerPrefs.GetInt("PlayerTotalMoney", 0);
        playerData.currentWinStreak = PlayerPrefs.GetInt("PlayerWinStreak", 0);
        playerData.highestWinStreak = PlayerPrefs.GetInt("PlayerHighestStreak", 0);

        string rawAch = PlayerPrefs.GetString("PlayerUnlockedAchievements", "");
        playerData.unlockedAchievements.Clear();
        if (!string.IsNullOrEmpty(rawAch))
        {
            playerData.unlockedAchievements = new System.Collections.Generic.List<string>(rawAch.Split(','));
        }

        // [PHASE-2] Power-Up inventory
        playerData.powerUp_5050     = PlayerPrefs.GetInt("PlayerPU_5050", 0);
        playerData.powerUp_extraTime = PlayerPrefs.GetInt("PlayerPU_Time", 0);
        playerData.powerUp_shield   = PlayerPrefs.GetInt("PlayerPU_Shield", 0);

        // [PHASE-2] Tier & Season
        playerData.currentTier             = PlayerPrefs.GetInt("PlayerCurrentTier", 1);
        playerData.highestTierThisSeason   = PlayerPrefs.GetInt("PlayerHighestTierSeason", 1);
        playerData.lastSeasonProcessed     = PlayerPrefs.GetInt("PlayerLastSeasonProcessed", 0);
        playerData.seasonBadges            = PlayerPrefs.GetString("PlayerSeasonBadges", "");

        // [PHASE-2] Daily Quests
        playerData.dailyQuestsData = PlayerPrefs.GetString("PlayerDailyQuests", "");

        // Self-heal: nếu tier chưa khớp RP (migration từ build cũ) → đồng bộ ngay
        playerData.RecomputeTier();

        Debug.Log($"[PlayerDataManager] Đã tải dữ liệu: {playerData.playerName} - Level {playerData.level} - Tier {playerData.currentTier}");
    }

    /// <summary>
    /// Xóa toàn bộ dữ liệu người chơi (Dùng khi đăng xuất tài khoản guest)
    /// </summary>
    public void ClearData()
    {
        // BUG-09 FIX: Chỉ xóa player data keys, không xóa settings (MusicEnabled, SFXEnabled, SelectedLanguage)
        PlayerPrefs.DeleteKey("PlayerLevel");
        PlayerPrefs.DeleteKey("PlayerExp");
        PlayerPrefs.DeleteKey("PlayerMoney");
        PlayerPrefs.DeleteKey("PlayerRankPoints");
        PlayerPrefs.DeleteKey("PlayerAvatar");
        PlayerPrefs.DeleteKey("PlayerName");

        PlayerPrefs.DeleteKey("PlayerBotWins");
        PlayerPrefs.DeleteKey("PlayerTotalMoney");
        PlayerPrefs.DeleteKey("PlayerWinStreak");
        PlayerPrefs.DeleteKey("PlayerHighestStreak");
        PlayerPrefs.DeleteKey("PlayerUnlockedAchievements");

        // [PHASE-2] xóa power-up, tier, season, quest
        PlayerPrefs.DeleteKey("PlayerPU_5050");
        PlayerPrefs.DeleteKey("PlayerPU_Time");
        PlayerPrefs.DeleteKey("PlayerPU_Shield");
        PlayerPrefs.DeleteKey("PlayerCurrentTier");
        PlayerPrefs.DeleteKey("PlayerHighestTierSeason");
        PlayerPrefs.DeleteKey("PlayerLastSeasonProcessed");
        PlayerPrefs.DeleteKey("PlayerSeasonBadges");
        PlayerPrefs.DeleteKey("PlayerDailyQuests");

        PlayerPrefs.Save();

        // Reset local SO values
        if (playerData != null)
        {
            playerData.Reset();
            playerData.playerName = "Player_" + Random.Range(1000, 9999);
        }

        Debug.Log("[PlayerDataManager] Đã xóa dữ liệu local player (giữ nguyên cài đặt).");
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }
}
