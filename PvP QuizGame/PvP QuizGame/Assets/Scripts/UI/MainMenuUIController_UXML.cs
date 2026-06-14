using System;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

/// <summary>
/// [REFACTOR-P2] Orchestrator UI của màn hình sảnh chính (HomeScene) — UI Toolkit.
/// KHÔNG còn build UI trực tiếp; chỉ khởi tạo và điều phối các sub-controller:
/// - HomeNavController          : bottom tabs + sub-tabs + switch panel
/// - PlayerHeaderController     : tên, avatar, tiền, level, XP bar
/// - MatchmakingPanelController : tìm trận / đấu máy / hủy / timeout
/// - LeaderboardPanelController : bảng xếp hạng
/// - AchievementsPanelController: thành tựu
/// - ShopPanelController        : cửa hàng (placeholder — Phase 2 Bước 3)
/// - SettingsPopup / ProfilePopup / AuthPopup / LogoutPopup (Scripts/UI/Popups)
/// </summary>
public class MainMenuUIController_UXML : MonoBehaviour
{
    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Settings Popup Template")]
    [SerializeField] private VisualTreeAsset settingsPopupTemplate;
    [SerializeField] private VisualTreeAsset logoutPopupTemplate;
    [SerializeField] private VisualTreeAsset profilePopupTemplate;
    [Header("Auth Popup Template")]
    [SerializeField] private VisualTreeAsset authPopupTemplate;
    [Header("Leaderboard & Achievements Templates")]
    [SerializeField] private VisualTreeAsset leaderboardPopupTemplate; // giữ lại để không mất reference Inspector
    [SerializeField] private VisualTreeAsset leaderboardEntryTemplate;
    [SerializeField] private VisualTreeAsset achievementEntryTemplate;

    // ==================== SUB-CONTROLLERS ====================
    private HomeNavController _nav;
    private PlayerHeaderController _header;
    private MatchmakingPanelController _matchmaking;
    private LeaderboardPanelController _leaderboardPanel;
    private AchievementsPanelController _achievementsPanel;
    private ShopPanelController _shopPanel;
    private DailyQuestsPanelController _questsPanel; // [PHASE-2]
    // [UI Refactor] Bỏ IntermissionHeroController (đã dời season info sang Rank panel)
    private RankPanelController _rankPanel;

    // Popups
    private SettingsPopupController _settingsPopup;
    private ProfilePopupController _profilePopup;
    private AuthPopupController _authPopup;
    // [PHASE-2]
    private EndOfSeasonPopupController _seasonPopup;
    private Action<SeasonManager.EndOfSeasonResult> _onSeasonEnded;

    // Nút do orchestrator giữ
    private Button _settingsBtn;
    private Button _openProfileBtn;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmHome);

        // ---- Khởi tạo sub-controllers ----
        _header = new PlayerHeaderController(root);
        _leaderboardPanel = new LeaderboardPanelController(root, leaderboardEntryTemplate);
        _achievementsPanel = new AchievementsPanelController(root, achievementEntryTemplate);
        _shopPanel = new ShopPanelController(root);
        _questsPanel = new DailyQuestsPanelController(root);                            // [PHASE-2]
        _rankPanel = new RankPanelController(root, _leaderboardPanel, this);             // [UI Refactor]

        // [UI Refactor] HomeNavController 4 tab: Shop / Home / Rank / Achievements
        _nav = new HomeNavController(root,
            blockRankForGuest: () => FirebaseManager.Instance != null && FirebaseManager.Instance.IsAnonymous,
            onGuestBlocked: ShowGuestLoginPopup,
            onShowShop: _shopPanel.Refresh,
            onShowRank: _rankPanel.Load,
            onShowAchievements: _achievementsPanel.Load,
            onShowQuests: _questsPanel.Load);
        _matchmaking = new MatchmakingPanelController(root, this, _nav);

        // ---- Nút header ----
        _settingsBtn = root.Q<Button>("settings-btn");
        _openProfileBtn = root.Q<Button>("open-profile-btn");
        if (_settingsBtn != null) _settingsBtn.clicked += OnSettingsClicked;
        if (_openProfileBtn != null) _openProfileBtn.clicked += OnOpenProfileClicked;

        // ---- Attach events (cặp Detach trong OnDisable) ----
        _nav.Attach();
        _shopPanel.Attach();
        _achievementsPanel.Attach();
        _matchmaking.Attach();
        _questsPanel.Attach();  // [PHASE-2]
        _rankPanel.Attach();    // [UI Refactor]

        _nav.ShowHome();
        _header.Refresh();

        // Orbs + background color breathing
        StartBackgroundAnimation();

        // Entry animation: hero bounce in + buttons cascade
        AnimateHomeEntry(root);

        // Localization phần còn lại của orchestrator (hero)
        LocalizationManager.OnLanguageChanged += LocalizeUI;
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
            LocalizeUI();

        // [PHASE-2] Check season — chạy khi đã authenticated. Nếu chưa thì delay sau Auth.
        TriggerSeasonCheck();
        FirebaseManager.OnAuthSuccess += TriggerSeasonCheck;

        // [PHASE-2] Sub EndOfSeason event để hiện popup + refresh header
        _onSeasonEnded = HandleSeasonEnded;
        SeasonManager.OnSeasonEnded += _onSeasonEnded;
    }

    /// <summary>[PHASE-2] Khi mùa kết thúc → hiện popup, refresh header (tier mới).</summary>
    private void HandleSeasonEnded(SeasonManager.EndOfSeasonResult res)
    {
        if (_seasonPopup != null && _seasonPopup.IsOpen) return;
        _seasonPopup = new EndOfSeasonPopupController(uiDocument.rootVisualElement);
        _seasonPopup.Show(res);
        _header?.Refresh();
    }

    /// <summary>[PHASE-2] Fire-and-forget kiểm tra mùa giải.</summary>
    private async void TriggerSeasonCheck()
    {
        if (SeasonManager.Instance == null) return;
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated) return;
        await SeasonManager.Instance.CheckSeasonAsync();
        // [UI Refactor] Sau khi load seasonConfig → refresh header (tier badge có thể đổi sau reset).
        // Rank panel sẽ tự refresh khi user mở tab (qua Load()).
        _header?.Refresh();
    }

    private void OnDisable()
    {
        if (_settingsBtn != null) _settingsBtn.clicked -= OnSettingsClicked;
        if (_openProfileBtn != null) _openProfileBtn.clicked -= OnOpenProfileClicked;

        LocalizationManager.OnLanguageChanged -= LocalizeUI;
        FirebaseManager.OnAuthSuccess -= TriggerSeasonCheck; // [PHASE-2]
        if (_onSeasonEnded != null) SeasonManager.OnSeasonEnded -= _onSeasonEnded; // [PHASE-2]

        _nav?.Detach();
        _shopPanel?.Detach();
        _achievementsPanel?.Detach();
        _matchmaking?.Detach();
        _questsPanel?.Detach(); // [PHASE-2]
        _rankPanel?.Detach();   // [UI Refactor]
    }

    /// <summary>Localize phần UI thuộc orchestrator (hero). Các panel tự localize qua UILocalizer riêng.</summary>
    private void LocalizeUI()
    {
        if (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsReady) return;
        var L = LocalizationManager.Instance;
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        var heroTitle = root.Q<Label>(className: "hero-title");
        if (heroTitle != null) heroTitle.text = L.GetText("menu_hero_title", "PVP QUIZ GAME");

        var heroSubtitle = root.Q<Label>(className: "hero-subtitle");
        if (heroSubtitle != null) heroSubtitle.text = L.GetText("menu_hero_subtitle", "THỬ THÁCH KIẾN THỨC");
    }

    /// <summary>Về tab Trang chủ (public — giữ tương thích API cũ).</summary>
    public void ShowHomePanel() => _nav?.ShowHome();

    private void RefreshPlayerStatsUI() => _header?.Refresh();

    // ==================== AUTH POPUP (GUEST → LOGIN) ====================
    private void ShowGuestLoginPopup()
    {
        if (uiDocument == null) return;
        if (_authPopup != null && _authPopup.IsOpen) return;

        // Ưu tiên template gán trong Inspector; fallback load từ Resources (giữ behavior cũ)
        var template = authPopupTemplate != null
            ? authPopupTemplate
            : Resources.Load<VisualTreeAsset>("UI/AuthPopup");
        if (template == null)
        {
            Debug.LogError("[MainMenu] Không tìm thấy AuthPopup template!");
            return;
        }

        _authPopup = new AuthPopupController(template, uiDocument.rootVisualElement, () =>
        {
            // Auth thành công: refresh player data UI + mở leaderboard ngay
            RefreshPlayerStatsUI();
            _nav?.SwitchBottomTab(2);
        });
        _authPopup.Show();
    }

    // ==================== SETTINGS / PROFILE ====================
    private void OnSettingsClicked()
    {
        if (settingsPopupTemplate == null)
        {
            Debug.LogWarning("[MainMenu] Chưa gán SettingsPopupTemplate trong Inspector!");
            return;
        }
        if (_settingsPopup != null && _settingsPopup.IsOpen) return;

        _settingsPopup = new SettingsPopupController(settingsPopupTemplate, logoutPopupTemplate, uiDocument.rootVisualElement);
        _settingsPopup.Show();
    }

    private void OnOpenProfileClicked()
    {
        if (profilePopupTemplate == null) return;
        if (_profilePopup != null && _profilePopup.IsOpen) return;

        _profilePopup = new ProfilePopupController(profilePopupTemplate, uiDocument.rootVisualElement, RefreshPlayerStatsUI);
        _profilePopup.Show();
    }

    // ==================== UX-08: ANDROID BACK BUTTON ====================
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Nếu đang trong matchmaking panel, hủy matchmaking
            if (_nav != null && _nav.IsMatchmakingVisible)
            {
                _matchmaking?.CancelMatch();
                return;
            }
            // Nếu có inline auth popup, đóng nó (giữ behavior cũ: đóng ngay, không animation)
            if (_authPopup != null && _authPopup.IsOpen)
            {
                _authPopup.CloseImmediate();
                return;
            }
            // Nếu có popup settings, đóng popup
            if (_settingsPopup != null && _settingsPopup.IsOpen)
            {
                _settingsPopup.Close();
                return;
            }
            Application.Quit();
        }
    }

    // ==================== ENTRY ANIMATIONS ====================
    private void AnimateHomeEntry(VisualElement root)
    {
        var header = root.Q<VisualElement>("header");
        if (header != null)
            UIAnimator.DOSlideFromLeft(header, 0.5f, 80f);

        var hero = root.Q<VisualElement>("hero");
        if (hero != null)
        {
            hero.style.scale = new StyleScale(new Scale(new Vector2(0.5f, 0.5f)));
            hero.style.opacity = 0f;
            DOTween.Sequence()
                .SetDelay(0.2f)
                .Append(hero.DOFade(1f, 0.3f))
                .Join(hero.DOScale(Vector2.one, 0.5f).SetEase(Ease.OutBack));
        }

        var buttons = new[] { root.Q<Button>("find-match-btn"), root.Q<Button>("practice-btn") };
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null || btn.style.display == DisplayStyle.None) continue;

            btn.style.opacity = 0f;
            btn.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(50)));

            float delay = 0.35f + i * 0.1f;
            DOTween.Sequence()
                .SetDelay(delay)
                .Append(btn.DOFade(1f, 0.25f))
                .Join(btn.DOTranslate(Vector2.zero, 0.35f).SetEase(Ease.OutBack));
        }
    }

    // ==================== BACKGROUND ANIMATION ====================
    private void StartBackgroundAnimation()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
        var root = uiDocument.rootVisualElement;

        var orb1 = root.Q<VisualElement>("glow-orb-1");
        var orb2 = root.Q<VisualElement>("glow-orb-2");
        var orb3 = root.Q<VisualElement>("glow-orb-3");

        float t = 0f;
        root.schedule.Execute(() =>
        {
            t += Time.deltaTime;

            // 1. Color Breathing (chuyển đổi Hue mượt mà) — màu gốc #0d0221
            float h = Mathf.Lerp(0.70f, 0.85f, (Mathf.Sin(t * 0.3f) + 1f) / 2f);
            root.style.backgroundColor = Color.HSVToRGB(h, 0.9f, 0.15f);

            // 2. Orb Floating Animation (di chuyển vô hạn)
            if (orb1 != null)
                orb1.style.translate = new Translate(Mathf.Sin(t * 0.5f) * 150f, Mathf.Cos(t * 0.4f) * 120f, 0);

            if (orb2 != null)
                orb2.style.translate = new Translate(Mathf.Cos(t * 0.35f) * -180f, Mathf.Sin(t * 0.45f) * 160f, 0);

            if (orb3 != null)
                orb3.style.translate = new Translate(Mathf.Sin(t * 0.6f) * 140f, Mathf.Cos(t * 0.55f) * -140f, 0);

        }).Every(16); // ~60FPS
    }
}
