using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Panel Bảng Xếp Hạng — tách từ MainMenuUIController_UXML.LoadLeaderboardData().
/// [PHASE-2 HOOK] Tier/Rank plan Bước 4: filter theo currentTier của mùa sẽ sửa ở LeaderboardManager,
/// UI này không cần đổi.
/// </summary>
public class LeaderboardPanelController
{
    private readonly VisualTreeAsset _entryTemplate;
    private readonly Label _loadingLabel;
    private readonly ScrollView _scroll;

    public LeaderboardPanelController(VisualElement root, VisualTreeAsset entryTemplate)
    {
        _entryTemplate = entryTemplate;
        _loadingLabel = root.Q<Label>("leaderboard-loading-label");
        _scroll = root.Q<ScrollView>("leaderboard-scroll");
    }

    /// <summary>
    /// [UI Refactor] Wrapper — load tier hiện tại của user. Giữ tương thích API cũ.
    /// </summary>
    public void Load() => LoadInternal(PlayerDataManager.Instance?.Data?.currentTier ?? 1);

    /// <summary>Fetch BXH cho tier chỉ định (1=Bronze...5=Legend). Gọi từ RankPanelController khi user chuyển tier filter.</summary>
    public void Load(int tier) => LoadInternal(tier);

    private async void LoadInternal(int tier)
    {
        if (_entryTemplate == null)
        {
            Debug.LogWarning("[LeaderboardPanel] Chưa gán LeaderboardEntryTemplate trong Inspector!");
            return;
        }

        // [IM] Intermission → freeze BXH, hiển thị message, không fetch cloud
        if (SeasonManager.Instance != null && SeasonManager.Instance.IsIntermission)
        {
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.None;
                _scroll.Clear();
            }
            if (_loadingLabel != null)
            {
                _loadingLabel.style.display = DisplayStyle.Flex;
                var l = LocalizationManager.Instance;
                _loadingLabel.text = (l != null && l.IsReady)
                    ? l.GetText("leaderboard_intermission", "BXH đang reset — chờ mùa mới")
                    : "BXH đang reset — chờ mùa mới";
            }
            return;
        }

        if (_loadingLabel != null) _loadingLabel.style.display = DisplayStyle.Flex;
        if (_scroll != null)
        {
            _scroll.style.display = DisplayStyle.None;
            _scroll.Clear();
        }

        if (LeaderboardManager.Instance == null) return;

        // [PHASE-2] Filter theo tier (Seasonal BXH).
        var topPlayers = await LeaderboardManager.Instance.FetchTierLeaderboardAsync(tier, 100);

        if (_loadingLabel != null) _loadingLabel.style.display = DisplayStyle.None;
        if (_scroll == null) return;

        _scroll.style.display = DisplayStyle.Flex;

        foreach (var player in topPlayers)
        {
            var entry = _entryTemplate.Instantiate();

            var rankLabel = entry.Q<Label>("rank-label");
            if (rankLabel != null)
            {
                rankLabel.text = player.rank.ToString();
                if (player.rank == 1) rankLabel.style.color = new Color(1f, 0.84f, 0f);        // Vàng
                else if (player.rank == 2) rankLabel.style.color = new Color(0.75f, 0.75f, 0.75f); // Bạc
                else if (player.rank == 3) rankLabel.style.color = new Color(0.8f, 0.5f, 0.2f);    // Đồng
            }

            var nameLabel = entry.Q<Label>("name-label");
            if (nameLabel != null)
            {
                nameLabel.text = player.displayName;
                if (FirebaseManager.Instance != null && player.uid == FirebaseManager.Instance.LocalUserId)
                {
                    nameLabel.style.color = new Color(0.5f, 1f, 0.5f); // Xanh lá — chính mình
                }
            }

            var pointsLabel = entry.Q<Label>("points-label");
            if (pointsLabel != null) pointsLabel.text = player.rankPoints.ToString();

            var pointsText = entry.Q<Label>("points-text");
            if (pointsText != null && LocalizationManager.Instance != null)
            {
                pointsText.text = LocalizationManager.Instance.GetText("leaderboard_points", "Điểm");
            }

            var avatarNode = entry.Q<VisualElement>("avatar");
            if (avatarNode != null) AvatarHelper.SetAvatar(avatarNode, player.displayName);

            _scroll.Add(entry);
        }
    }
}
