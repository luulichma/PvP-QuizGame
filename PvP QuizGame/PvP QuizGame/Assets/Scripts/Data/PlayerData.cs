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
    /// Thêm điểm xếp hạng
    /// </summary>
    public void AddRankPoints(int amount)
    {
        rankPoints += amount;
        // Điểm xếp hạng không thể nhỏ hơn 0
        if (rankPoints < 0) rankPoints = 0;
        Debug.Log($"<color=cyan>[PlayerData] Điểm xếp hạng hiện tại: {rankPoints}</color>");
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
    }
}
