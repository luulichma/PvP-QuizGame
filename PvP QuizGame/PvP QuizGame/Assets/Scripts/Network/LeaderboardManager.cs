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
    public int tier; // [PHASE-2] tier hiện tại của entry này
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
    /// [Seed] Bật/tắt việc trộn fake users vào BXH khi user thật còn ít.
    /// Set false khi đã có nhiều người chơi thật (>= FakeLeaderboardSeeder.MinRealUsers).
    /// </summary>
    public bool useFakeUsersWhenSparse = true;

    /// <summary>
    /// Lấy top 100 người có Điểm Xếp Hạng cao nhất.
    /// [Seed] Nếu user thật &lt; MinRealUsers, trộn fake users vào để BXH không trống.
    /// </summary>
    public async Task<List<LeaderboardEntry>> FetchTopRankPlayersAsync(int limit = 100)
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsConnected)
        {
            Debug.LogWarning("[LeaderboardManager] Firebase chua ket noi!");
            // [Seed] Offline / khong co Firebase -> van tra ve fake users de UI khong trong.
            return useFakeUsersWhenSparse ? BuildFakeOnlyResult(limit) : new List<LeaderboardEntry>();
        }

        var results = new List<LeaderboardEntry>();
        try
        {
            var dbRef = FirebaseManager.Instance.GetRootRef();
            var snapshot = await dbRef.Child("users")
                .OrderByChild("rankPoints")
                .LimitToLast(limit)
                .GetValueAsync();

            if (snapshot.Exists && snapshot.ChildrenCount > 0)
            {
                foreach (var child in snapshot.Children)
                {
                    if (child.Child("isGuest").Value != null)
                    {
                        bool isGuest = false;
                        bool.TryParse(child.Child("isGuest").Value.ToString(), out isGuest);
                        if (isGuest) continue;
                    }

                    int rp = 0, avatar = 0;
                    if (child.Child("rankPoints").Value != null) int.TryParse(child.Child("rankPoints").Value.ToString(), out rp);
                    if (child.Child("avatarIndex").Value != null) int.TryParse(child.Child("avatarIndex").Value.ToString(), out avatar);

                    int tier = PlayerData.ComputeTier(rp);
                    if (child.Child("currentTier").Value != null) int.TryParse(child.Child("currentTier").Value.ToString(), out tier);

                    var entry = new LeaderboardEntry
                    {
                        uid = child.Key,
                        displayName = child.Child("displayName").Value?.ToString() ?? "Unknown",
                        rankPoints = rp,
                        avatarIndex = avatar,
                        tier = tier
                    };
                    results.Add(entry);
                }

                results.Reverse();

                for (int i = 0; i < results.Count; i++)
                {
                    results[i].rank = i + 1;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LeaderboardManager] Loi fetch leaderboard: {ex.Message}");
        }

        if (useFakeUsersWhenSparse && results.Count < FakeLeaderboardSeeder.MinRealUsers)
        {
            MergeFakeUsers(results, limit);
        }

        return results;
    }

    /// <summary>[Seed] Tron fake users vao danh sach ket qua that, re-sort va re-rank.</summary>
    private static void MergeFakeUsers(List<LeaderboardEntry> realResults, int limit)
    {
        var fakes = FakeLeaderboardSeeder.GetFakeEntries();
        realResults.AddRange(fakes);

        realResults.Sort((a, b) => b.rankPoints.CompareTo(a.rankPoints));

        if (realResults.Count > limit)
            realResults.RemoveRange(limit, realResults.Count - limit);

        for (int i = 0; i < realResults.Count; i++)
            realResults[i].rank = i + 1;
    }

    /// <summary>[Seed] Offline fallback: chi tra ve fake users da sort + rank.</summary>
    private static List<LeaderboardEntry> BuildFakeOnlyResult(int limit)
    {
        var fakes = FakeLeaderboardSeeder.GetFakeEntries();
        fakes.Sort((a, b) => b.rankPoints.CompareTo(a.rankPoints));
        if (fakes.Count > limit) fakes.RemoveRange(limit, fakes.Count - limit);
        for (int i = 0; i < fakes.Count; i++) fakes[i].rank = i + 1;
        return fakes;
    }

    /// <summary>
    /// [PHASE-2] Fetch BXH loc theo tier hien tai (Bronze..Legend).
    /// Sort theo rankPoints desc. Tra ve kem rank position cua user trong tier (neu uid khop).
    /// </summary>
    public async Task<List<LeaderboardEntry>> FetchTierLeaderboardAsync(int tier, int limit = 100)
    {
        var all = await FetchTopRankPlayersAsync(500);
        var filtered = new List<LeaderboardEntry>();
        foreach (var e in all)
        {
            if (e.tier == tier) filtered.Add(e);
        }
        if (filtered.Count > limit) filtered.RemoveRange(limit, filtered.Count - limit);
        for (int i = 0; i < filtered.Count; i++) filtered[i].rank = i + 1;
        return filtered;
    }
}
