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
        PlayerPrefs.SetString("PlayerName", playerData.playerName);
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
        playerData.playerName = PlayerPrefs.GetString("PlayerName", "Player_" + Random.Range(1000, 9999));
        
        Debug.Log($"[PlayerDataManager] Đã tải dữ liệu: {playerData.playerName} - Level {playerData.level}");
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }
}
