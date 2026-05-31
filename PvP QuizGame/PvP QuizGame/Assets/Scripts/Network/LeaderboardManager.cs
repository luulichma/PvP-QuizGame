using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

[System.Serializable]
public class LeaderboardEntry
{
    public string uid;
    public string displayName;
    public int rankPoints;
    public int avatarIndex;
    public int rank;
}

/// <summary>
/// Quản lý việc lấy dữ liệu Bảng Xếp Hạng từ Firebase
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Lấy top 100 người có Điểm Xếp Hạng cao nhất
    /// </summary>
    public async Task<List<LeaderboardEntry>> FetchTopRankPlayersAsync(int limit = 100)
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsConnected)
        {
            Debug.LogWarning("[LeaderboardManager] Firebase chưa kết nối!");
            return new List<LeaderboardEntry>();
        }

        var results = new List<LeaderboardEntry>();
        try
        {
            var dbRef = FirebaseManager.Instance.GetRootRef();
            // Firebase hỗ trợ OrderByChild, ta sẽ query trực tiếp trên trường rankPoints
            var snapshot = await dbRef.Child("users")
                .OrderByChild("rankPoints")
                .LimitToLast(limit)
                .GetValueAsync();

            if (snapshot.Exists && snapshot.ChildrenCount > 0)
            {
                // Firebase LimitToLast trả về thứ tự tăng dần (từ điểm thấp đến cao)
                // Cần add vào list rồi đảo ngược để Top 1 (cao điểm nhất) lên đầu
                foreach (var child in snapshot.Children)
                {
                    // FEAT: Lọc bỏ tài khoản Khách (Guest) khỏi Bảng Xếp Hạng
                    if (child.Child("isGuest").Value != null)
                    {
                        bool isGuest = false;
                        bool.TryParse(child.Child("isGuest").Value.ToString(), out isGuest);
                        if (isGuest) continue;
                    }

                    int rp = 0, avatar = 0;
                    if (child.Child("rankPoints").Value != null) int.TryParse(child.Child("rankPoints").Value.ToString(), out rp);
                    if (child.Child("avatarIndex").Value != null) int.TryParse(child.Child("avatarIndex").Value.ToString(), out avatar);
                    
                    var entry = new LeaderboardEntry
                    {
                        uid = child.Key,
                        displayName = child.Child("displayName").Value?.ToString() ?? "Unknown",
                        rankPoints = rp,
                        avatarIndex = avatar
                    };
                    results.Add(entry);
                }

                results.Reverse(); // Từ cao xuống thấp
                
                // Cập nhật thuộc tính rank (1, 2, 3...)
                for (int i = 0; i < results.Count; i++)
                {
                    results[i].rank = i + 1;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LeaderboardManager] Lỗi fetch leaderboard: {ex.Message}");
        }

        return results;
    }
}
