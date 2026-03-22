using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "QuizGame/PlayerData")]
public class PlayerData : ScriptableObject
{
    public string playerName = "New Player";
    public int level = 1;
    public int currentExp = 0;
    public int money = 0;

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
        Debug.Log($"<color=green>[PlayerData] Nhận được {amount}$ tiền thưởng!</color>");
    }

    /// <summary>
    /// Reset dữ liệu (cho mục đích test)
    /// </summary>
    public void Reset()
    {
        level = 1;
        currentExp = 0;
        money = 0;
    }
}
