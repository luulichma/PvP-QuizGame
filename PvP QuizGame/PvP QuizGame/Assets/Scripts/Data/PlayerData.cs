using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "QuizGame/PlayerData")]
public class PlayerData : ScriptableObject
{
    public string playerName = "New Player";
    public int level = 1;
    public int currentExp = 0;
    public int money = 0;
    public int rankPoints = 0;
    public int avatarIndex = 0; // Chỉ số ảnh đại diện (0-9)

    [Header("Achievement Progress")]
    public int botWins = 0;
    public int totalMoneyEarned = 0;
    public int currentWinStreak = 0;
    public int highestWinStreak = 0;
    public System.Collections.Generic.List<string> unlockedAchievements = new System.Collections.Generic.List<string>();

    // ==================== [PHASE-2] POWER-UP INVENTORY ====================
    [Header("Power-Up Inventory (Phase 2)")]
    public int powerUp_5050 = 0;        // ✂️ 50:50
    public int powerUp_extraTime = 0;   // ⏱️ +5s
    public int powerUp_shield = 0;      // 🛡️ Lá chắn

    // ==================== [PHASE-2] TIER & SEASON ====================
    [Header("Tier & Season (Phase 2)")]
    public int currentTier = 1;             // 1=Bronze, 2=Silver, 3=Gold, 4=Diamond, 5=Legend
    public int highestTierThisSeason = 1;   // Tier cao nhất đạt được trong mùa hiện tại
    public int lastSeasonProcessed = 0;     // Season ID cuối cùng đã xử lý reset
    public string seasonBadges = "";        // CSV danh hiệu các mùa trước: "💎S1,🥇S2"

    // ==================== [PHASE-2] DAILY QUESTS ====================
    [Header("Daily Quests (Phase 2)")]
    public string dailyQuestsData = "";     // JSON quest tracking

    /// <summary>
    /// Tính toán lượng Exp cần để lên cấp tiếp theo
    /// Công thức đơn giản: level * 100
    /// </summary>
    public int GetExpToNextLevel()
    {
        return level * 100;
    }

    /// <summary>
    /// Thêm Exp và kiểm tra lên cấp
    /// </summary>
    public void AddExp(int amount)
    {
        currentExp += amount;
        while (currentExp >= GetExpToNextLevel())
        {
            currentExp -= GetExpToNextLevel();
            level++;
            Debug.Log($"<color=yellow>[PlayerData] CHÚC MỪNG! Lên cấp {level}!</color>");
        }
    }

    /// <summary>
    /// Thêm tiền
    /// </summary>
    public void AddMoney(int amount)
    {
        money += amount;
        if (amount > 0) totalMoneyEarned += amount;
        Debug.Log($"<color=green>[PlayerData] Nhận được {amount}$ tiền thưởng!</color>");
    }

    /// <summary>
    /// Trừ tiền (cho Shop). Trả về false nếu không đủ.
    /// </summary>
    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0) return false;
        if (money < amount) return false;
        money -= amount;
        return true;
    }

    /// <summary>
    /// Thêm điểm xếp hạng
    /// </summary>
    public void AddRankPoints(int amount)
    {
        rankPoints += amount;
        // Điểm xếp hạng không thể nhỏ hơn 0
        if (rankPoints < 0) rankPoints = 0;
        Debug.Log($"<color=cyan>[PlayerData] Điểm xếp hạng hiện tại: {rankPoints}</color>");
    }

    // ==================== [PHASE-2] POWER-UP HELPERS ====================

    public int GetPowerUpCount(string id)
    {
        return id switch
        {
            "pu_5050"   => powerUp_5050,
            "pu_time"   => powerUp_extraTime,
            "pu_shield" => powerUp_shield,
            _ => 0
        };
    }

    /// <summary>Cộng power-up theo id. amount âm để trừ.</summary>
    public void AddPowerUp(string id, int amount)
    {
        switch (id)
        {
            case "pu_5050":   powerUp_5050   = Mathf.Max(0, powerUp_5050 + amount);   break;
            case "pu_time":   powerUp_extraTime = Mathf.Max(0, powerUp_extraTime + amount); break;
            case "pu_shield": powerUp_shield = Mathf.Max(0, powerUp_shield + amount); break;
        }
    }

    // ==================== [PHASE-2] TIER HELPERS ====================

    /// <summary>Tính tier từ rankPoints theo bảng economy-design v2.0.</summary>
    public static int ComputeTier(int rp)
    {
        if (rp < 500)  return 1;  // Bronze
        if (rp < 1500) return 2;  // Silver
        if (rp < 3000) return 3;  // Gold
        if (rp < 5000) return 4;  // Diamond
        return 5;                 // Legend
    }

    /// <summary>Cập nhật currentTier + highestTierThisSeason dựa trên rankPoints hiện tại.</summary>
    public void RecomputeTier()
    {
        int newTier = ComputeTier(rankPoints);
        if (newTier != currentTier)
        {
            Debug.Log($"<color=magenta>[PlayerData] Tier: {currentTier} → {newTier} (RP={rankPoints})</color>");
            currentTier = newTier;
        }
        if (currentTier > highestTierThisSeason)
        {
            highestTierThisSeason = currentTier;
        }
    }

    public void AppendSeasonBadge(string badge)
    {
        if (string.IsNullOrEmpty(badge)) return;
        seasonBadges = string.IsNullOrEmpty(seasonBadges) ? badge : seasonBadges + "," + badge;
    }

    /// <summary>
    /// Reset dữ liệu (cho mục đích test)
    /// </summary>
    public void Reset()
    {
        level = 1;
        currentExp = 0;
        money = 0;
        rankPoints = 0;
        avatarIndex = 0;

        botWins = 0;
        totalMoneyEarned = 0;
        currentWinStreak = 0;
        highestWinStreak = 0;
        unlockedAchievements.Clear();

        // Phase 2 fields
        powerUp_5050 = 0;
        powerUp_extraTime = 0;
        powerUp_shield = 0;
        currentTier = 1;
        highestTierThisSeason = 1;
        lastSeasonProcessed = 0;
        seasonBadges = "";
        dailyQuestsData = "";
    }
}
