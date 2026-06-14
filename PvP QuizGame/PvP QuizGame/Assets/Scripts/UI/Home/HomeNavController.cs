using System;
using UnityEngine.UIElements;

/// <summary>
/// [UI Refactor] Điều hướng sảnh chính: 4 BOTTOM TAB (Shop / Home / Rank / Achievements)
/// + 2 SUB-TAB trong Achievements panel (Achievements / Daily Quests).
///
/// Rank panel KHÔNG còn sub-tab (vì BXH theo tier dùng tier-filter chips riêng).
/// Season info CHỈ hiển thị trong Rank panel (không trong header/home nữa).
/// </summary>
public class HomeNavController
{
    private readonly VisualElement _homePanel;
    private readonly VisualElement _shopPanel;
    private readonly VisualElement _rankPanel;
    private readonly VisualElement _achievementsPanel;
    private readonly VisualElement _matchmakingPanel;

    // Sub-panels trong achievements-panel
    private readonly VisualElement _subpanelAchievements;
    private readonly VisualElement _subpanelQuests;

    // 4 bottom tab buttons
    private readonly Button _navShopBtn;
    private readonly Button _navHomeBtn;
    private readonly Button _navRankBtn;
    private readonly Button _navAchievementsBtn;

    // Sub-tab buttons (trong Achievements panel)
    private readonly Button _subtabAchievementsBtn;
    private readonly Button _subtabQuestsBtn;

    private readonly UILocalizer _localizer = new UILocalizer();

    private readonly Func<bool> _blockRankForGuest;
    private readonly Action _onGuestBlocked;
    private readonly Action _onShowShop;
    private readonly Action _onShowRank;
    private readonly Action _onShowAchievements;
    private readonly Action _onShowQuests;

    /// <param name="blockRankForGuest">Chặn Rank tab nếu guest (cũng có thể áp Achievements nếu cần).</param>
    /// <param name="onGuestBlocked">Mở popup login khi guest bị chặn.</param>
    public HomeNavController(VisualElement root,
                             Func<bool> blockRankForGuest,
                             Action onGuestBlocked,
                             Action onShowShop,
                             Action onShowRank,
                             Action onShowAchievements,
                             Action onShowQuests)
    {
        _blockRankForGuest = blockRankForGuest;
        _onGuestBlocked = onGuestBlocked;
        _onShowShop = onShowShop;
        _onShowRank = onShowRank;
        _onShowAchievements = onShowAchievements;
        _onShowQuests = onShowQuests;

        _homePanel = root.Q<VisualElement>("home-panel");
        _shopPanel = root.Q<VisualElement>("shop-panel");
        _rankPanel = root.Q<VisualElement>("rank-panel");
        _achievementsPanel = root.Q<VisualElement>("achievements-panel");
        _matchmakingPanel = root.Q<VisualElement>("matchmaking-panel");

        _subpanelAchievements = root.Q<VisualElement>("subpanel-achievements");
        _subpanelQuests = root.Q<VisualElement>("subpanel-quests");

        _navShopBtn = root.Q<Button>("nav-shop-btn");
        _navHomeBtn = root.Q<Button>("nav-home-btn");
        _navRankBtn = root.Q<Button>("nav-rank-btn");
        _navAchievementsBtn = root.Q<Button>("nav-achievements-btn");

        _subtabAchievementsBtn = root.Q<Button>("subtab-achievements-btn");
        _subtabQuestsBtn = root.Q<Button>("subtab-quests-btn");

        // Wire bottom tabs
        if (_navShopBtn != null) _navShopBtn.clicked += () => SwitchBottomTab(0);
        if (_navHomeBtn != null) _navHomeBtn.clicked += () => SwitchBottomTab(1);
        if (_navRankBtn != null) _navRankBtn.clicked += () => SwitchBottomTab(2);
        if (_navAchievementsBtn != null) _navAchievementsBtn.clicked += () => SwitchBottomTab(3);

        // Wire sub-tabs (Achievements panel)
        if (_subtabAchievementsBtn != null) _subtabAchievementsBtn.clicked += () => SwitchAchievementsSubTab(0);
        if (_subtabQuestsBtn != null) _subtabQuestsBtn.clicked += () => SwitchAchievementsSubTab(1);

        // Localization
        _localizer.Bind(l =>
        {
            var lbl = _navShopBtn?.Q<Label>(className: "nav-tab-label");
            if (lbl != null) lbl.text = l.GetText("menu_tab_shop", "SHOP");
        });
        _localizer.Bind(l =>
        {
            var lbl = _navHomeBtn?.Q<Label>(className: "nav-tab-label");
            if (lbl != null) lbl.text = l.GetText("menu_tab_home", "TRANG CHỦ");
        });
        _localizer.Bind(l =>
        {
            var lbl = _navRankBtn?.Q<Label>(className: "nav-tab-label");
            if (lbl != null) lbl.text = l.GetText("menu_tab_rank", "XẾP HẠNG");
        });
        _localizer.Bind(l =>
        {
            var lbl = _navAchievementsBtn?.Q<Label>(className: "nav-tab-label");
            if (lbl != null) lbl.text = l.GetText("menu_tab_achievements", "THÀNH TỰU");
        });
        _localizer.BindButton(_subtabAchievementsBtn, "menu_subtab_achievements", "THÀNH TỰU");
        _localizer.BindButton(_subtabQuestsBtn, "menu_tab_quests", "NHIỆM VỤ");
    }

    public void Attach()
    {
        _localizer.Attach();
        _localizer.Refresh();
    }

    public void Detach() => _localizer.Detach();

    public bool IsMatchmakingVisible =>
        _matchmakingPanel != null && _matchmakingPanel.style.display == DisplayStyle.Flex;

    public void ShowHome() => SwitchBottomTab(1);
    public void ShowMatchmakingPanel() => ShowPanel(_matchmakingPanel);

    public void SwitchBottomTab(int index)
    {
        // Chặn Khách không cho vào Rank (BXH cần đã đăng ký)
        if (index == 2 && _blockRankForGuest != null && _blockRankForGuest())
        {
            _onGuestBlocked?.Invoke();
            return;
        }

        _navShopBtn?.RemoveFromClassList("nav-tab-active");
        _navHomeBtn?.RemoveFromClassList("nav-tab-active");
        _navRankBtn?.RemoveFromClassList("nav-tab-active");
        _navAchievementsBtn?.RemoveFromClassList("nav-tab-active");

        switch (index)
        {
            case 0:
                _navShopBtn?.AddToClassList("nav-tab-active");
                ShowPanel(_shopPanel);
                _onShowShop?.Invoke();
                break;
            case 1:
                _navHomeBtn?.AddToClassList("nav-tab-active");
                ShowPanel(_homePanel);
                break;
            case 2:
                _navRankBtn?.AddToClassList("nav-tab-active");
                ShowPanel(_rankPanel);
                _onShowRank?.Invoke();
                break;
            case 3:
                _navAchievementsBtn?.AddToClassList("nav-tab-active");
                ShowPanel(_achievementsPanel);
                SwitchAchievementsSubTab(0); // Mặc định mở Achievements khi vào tab
                break;
        }
    }

    private void SwitchAchievementsSubTab(int index)
    {
        _subtabAchievementsBtn?.RemoveFromClassList("sub-tab-active");
        _subtabQuestsBtn?.RemoveFromClassList("sub-tab-active");

        if (_subpanelAchievements != null) _subpanelAchievements.style.display = DisplayStyle.None;
        if (_subpanelQuests != null) _subpanelQuests.style.display = DisplayStyle.None;

        if (index == 0)
        {
            _subtabAchievementsBtn?.AddToClassList("sub-tab-active");
            if (_subpanelAchievements != null) _subpanelAchievements.style.display = DisplayStyle.Flex;
            _onShowAchievements?.Invoke();
        }
        else if (index == 1)
        {
            _subtabQuestsBtn?.AddToClassList("sub-tab-active");
            if (_subpanelQuests != null) _subpanelQuests.style.display = DisplayStyle.Flex;
            _onShowQuests?.Invoke();
        }
    }

    private void ShowPanel(VisualElement target)
    {
        if (_homePanel != null) _homePanel.style.display = (_homePanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
        if (_shopPanel != null) _shopPanel.style.display = (_shopPanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
        if (_rankPanel != null) _rankPanel.style.display = (_rankPanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
        if (_achievementsPanel != null) _achievementsPanel.style.display = (_achievementsPanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
        if (_matchmakingPanel != null) _matchmakingPanel.style.display = (_matchmakingPanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
