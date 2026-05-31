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
        
        Debug.Log($"[PlayerDataManager] Đã tải dữ liệu: {playerData.playerName} - Level {playerData.level}");
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
        
        PlayerPrefs.Save();
        
        // Reset local SO values
        if (playerData != null)
        {
            playerData.level = 1;
            playerData.currentExp = 0;
            playerData.money = 0;
            playerData.rankPoints = 0;
            playerData.avatarIndex = 0;
            playerData.playerName = "Player_" + Random.Range(1000, 9999);
        }
        
        Debug.Log("[PlayerDataManager] Đã xóa dữ liệu local player (giữ nguyên cài đặt).");
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }
}
