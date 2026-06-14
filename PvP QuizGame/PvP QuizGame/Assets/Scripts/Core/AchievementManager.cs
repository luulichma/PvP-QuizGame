using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum RewardType { Money, RankPoints }

public class AchievementDef
{
    public string id;
    public string name;
    public string description;
    public int targetValue;
    public int rewardAmount;
    public RewardType rewardType;
    
    // Đổi sang iconClass + iconTint (USS class name) để dùng PNG icon.
    public string iconClass;     // ví dụ "icon-bot", "icon-coins", "icon-crown"
    public string iconTint;      // ví dụ "icon-tint-gold", "icon-tint-money"; nullable.
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    public List<AchievementDef> achievements = new List<AchievementDef>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAchievements();
    }

    private void InitializeAchievements()
    {
        achievements.Add(new AchievementDef { id = "ach_bot_1", name = "Khởi Động Sương Sương", description = "Thắng trận đấu máy đầu tiên.", targetValue = 1, rewardAmount = 50, rewardType = RewardType.Money, iconClass = "icon-bot", iconTint = IconTint.Cyan });
        achievements.Add(new AchievementDef { id = "ach_bot_50", name = "Kẻ Hủy Diệt Máy Móc", description = "Đánh bại Bot 50 lần.", targetValue = 50, rewardAmount = 500, rewardType = RewardType.Money, iconClass = "icon-swords", iconTint = IconTint.Silver });
        achievements.Add(new AchievementDef { id = "ach_money_10k", name = "Phú Hào Mới Nổi", description = "Tổng tiền tích lũy chạm mốc 10,000$.", targetValue = 10000, rewardAmount = 1000, rewardType = RewardType.Money, iconClass = "icon-coins", iconTint = IconTint.Money });
        achievements.Add(new AchievementDef { id = "ach_rank_1k", name = "Bước Chân Thần Tốc", description = "Đạt 1,000 Điểm Xếp Hạng.", targetValue = 1000, rewardAmount = 200, rewardType = RewardType.Money, iconClass = "icon-zap", iconTint = IconTint.Gold });
        achievements.Add(new AchievementDef { id = "ach_rank_5k", name = "Đỉnh Bảng Phong Thần", description = "Đạt 5,000 Điểm Xếp Hạng.", targetValue = 5000, rewardAmount = 1000, rewardType = RewardType.Money, iconClass = "icon-crown", iconTint = IconTint.Legend });
        achievements.Add(new AchievementDef { id = "ach_streak_5", name = "Cỗ Máy Ghi Điểm", description = "Thắng 5 trận Đấu Thường liên tiếp.", targetValue = 5, rewardAmount = 300, rewardType = RewardType.Money, iconClass = "icon-flame", iconTint = IconTint.Red });
        achievements.Add(new AchievementDef { id = "ach_streak_10", name = "Độc Cô Cầu Bại", description = "Thắng 10 trận Đấu Thường liên tiếp.", targetValue = 10, rewardAmount = 1000, rewardType = RewardType.Money, iconClass = "icon-swords", iconTint = IconTint.Gold });
        achievements.Add(new AchievementDef { id = "ach_perfect_1", name = "Trí Tuệ Đỉnh Cao", description = "Chiến thắng một trận đấu mà không trả lời sai câu nào.", targetValue = 1, rewardAmount = 100, rewardType = RewardType.RankPoints, iconClass = "icon-brain", iconTint = IconTint.Purple });
    }

    public void RecordPerfectWin()
    {
        if (PlayerDataManager.Instance != null)
        {
            // Just use a custom stat tracking for perfect win
            // Since we didn't add perfectWins to PlayerData, we can just instantly unlock this achievement if not unlocked.
            var data = PlayerDataManager.Instance.Data;
            if (!data.unlockedAchievements.Contains("ach_perfect_1"))
            {
                UnlockAchievement("ach_perfect_1");
            }
        }
    }

    public void CheckAchievements()
    {
        var data = PlayerDataManager.Instance?.Data;
        if (data == null) return;

        // Check Bot Wins
        if (data.botWins >= 1 && !data.unlockedAchievements.Contains("ach_bot_1")) UnlockAchievement("ach_bot_1");
        if (data.botWins >= 50 && !data.unlockedAchievements.Contains("ach_bot_50")) UnlockAchievement("ach_bot_50");

        // Check Total Money Earned
        if (data.totalMoneyEarned >= 10000 && !data.unlockedAchievements.Contains("ach_money_10k")) UnlockAchievement("ach_money_10k");

        // Check Rank Points
        if (data.rankPoints >= 1000 && !data.unlockedAchievements.Contains("ach_rank_1k")) UnlockAchievement("ach_rank_1k");
        if (data.rankPoints >= 5000 && !data.unlockedAchievements.Contains("ach_rank_5k")) UnlockAchievement("ach_rank_5k");

        // Check Win Streaks
        if (data.highestWinStreak >= 5 && !data.unlockedAchievements.Contains("ach_streak_5")) UnlockAchievement("ach_streak_5");
        if (data.highestWinStreak >= 10 && !data.unlockedAchievements.Contains("ach_streak_10")) UnlockAchievement("ach_streak_10");
    }

    private void UnlockAchievement(string achId)
    {
        var ach = achievements.FirstOrDefault(a => a.id == achId);
        if (ach == null) return;

        PlayerDataManager.Instance.Data.unlockedAchievements.Add(achId);
        PlayerDataManager.Instance.SaveData();

        // Push to Firebase instantly if online
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsAuthenticated)
        {
            _ = FirebaseManager.Instance.SaveProfileToCloud();
        }

        Debug.Log($"<color=yellow>[Achievement] Đã mở khóa thành tựu: {ach.name}!</color>");
        
        // Cấp phần thưởng luôn
        ClaimReward(achId);
    }

    private void ClaimReward(string achId)
    {
        var ach = achievements.FirstOrDefault(a => a.id == achId);
        if (ach == null) return;

        if (ach.rewardType == RewardType.Money)
        {
            PlayerDataManager.Instance.Data.AddMoney(ach.rewardAmount);
        }
        else
        {
            PlayerDataManager.Instance.Data.AddRankPoints(ach.rewardAmount);
        }
        
        PlayerDataManager.Instance.SaveData();
        Debug.Log($"<color=yellow>[Achievement] Đã nhận thưởng thành tựu: {ach.name} (+{ach.rewardAmount} {(ach.rewardType == RewardType.Money ? "$" : "RP")})!</color>");
    }

    // Lấy tiến độ hiển thị lên UI
    public int GetCurrentProgress(string achId)
    {
        var data = PlayerDataManager.Instance?.Data;
        if (data == null) return 0;

        switch (achId)
        {
            case "ach_bot_1":
            case "ach_bot_50":
                return data.botWins;
            case "ach_money_10k":
                return data.totalMoneyEarned;
            case "ach_rank_1k":
            case "ach_rank_5k":
                return data.rankPoints;
            case "ach_streak_5":
            case "ach_streak_10":
                return data.highestWinStreak;
            case "ach_perfect_1":
                return data.unlockedAchievements.Contains("ach_perfect_1") ? 1 : 0;
        }
        return 0;
    }
}
