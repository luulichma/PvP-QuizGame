using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Linq;
using DG.Tweening;

/// <summary>
/// Quản lý UI của màn hình sảnh chính (HomeScene) sử dụng UI Toolkit.
/// Gọi Firebase Matchmaking thật khi user bấm "Tìm trận".
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
    [SerializeField] private VisualTreeAsset leaderboardPopupTemplate;
    [SerializeField] private VisualTreeAsset leaderboardEntryTemplate;
    [SerializeField] private VisualTreeAsset achievementEntryTemplate;

    // Panels
    private VisualElement _homePanel;
    private VisualElement _shopPanel;
    private VisualElement _leaderboardPanel;
    private VisualElement _matchmakingPanel;

    // Sub-panels (Leaderboard)
    private VisualElement _subpanelLeaderboard;
    private VisualElement _subpanelAchievements;

    // Bottom Nav Tabs
    private Button _navShopBtn;
    private Button _navHomeBtn;
    private Button _navLeaderboardBtn;

    // Sub-tabs
    private Button _subtabLeaderboardBtn;
    private Button _subtabAchievementsBtn;

    // Leaderboard elements
    private Label _leaderboardLoadingLabel;
    private ScrollView _leaderboardScroll;
    private ScrollView _achievementsScroll;

    // Buttons
    private Button _findMatchBtn;
    private Button _practiceBtn;
    private Button _settingsBtn;
    private Button _cancelMatchBtn;
    private Button _openProfileBtn;

    // Profile elements
    private Label _nameLabel;
    private VisualElement _avatarElement;

    // Labels
    private Label _moneyLabel;
    private Label _levelTag;
    private Label _searchingLabel;
    // FIX-CANCEL: Lưu coroutine offline để có thể dừng khi cancel
    private Coroutine _offlineRoutine;
    // FIX-CANCEL: Flag để block OnMatchFound sau khi đã cancel online matchmaking
    private bool _isCancelledMatchmaking = false;
    // G-12: XP Bar
    private VisualElement _xpFill;
    private Label _xpLabel;

    // Particle layer
    private VisualElement _particleLayer;

    // Settings popup instance
    private VisualElement _settingsPopup;

    // Leaderboard popup instance
    private VisualElement _leaderboardPopup;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmHome);
        }

        _homePanel = root.Q<VisualElement>("home-panel");
        _shopPanel = root.Q<VisualElement>("shop-panel");
        _leaderboardPanel = root.Q<VisualElement>("leaderboard-panel");
        _matchmakingPanel = root.Q<VisualElement>("matchmaking-panel");

        _subpanelLeaderboard = root.Q<VisualElement>("subpanel-leaderboard");
        _subpanelAchievements = root.Q<VisualElement>("subpanel-achievements");

        _navShopBtn = root.Q<Button>("nav-shop-btn");
        _navHomeBtn = root.Q<Button>("nav-home-btn");
        _navLeaderboardBtn = root.Q<Button>("nav-leaderboard-btn");

        _subtabLeaderboardBtn = root.Q<Button>("subtab-leaderboard-btn");
        _subtabAchievementsBtn = root.Q<Button>("subtab-achievements-btn");

        _leaderboardLoadingLabel = root.Q<Label>("leaderboard-loading-label");
        _leaderboardScroll = root.Q<ScrollView>("leaderboard-scroll");
        _achievementsScroll = root.Q<ScrollView>("achievements-scroll");

        _findMatchBtn = root.Q<Button>("find-match-btn");
        _practiceBtn = root.Q<Button>("practice-btn");
        _settingsBtn = root.Q<Button>("settings-btn");
        _cancelMatchBtn = root.Q<Button>("cancel-match-btn");
        _openProfileBtn = root.Q<Button>("open-profile-btn");

        if (_findMatchBtn != null) _findMatchBtn.clicked += OnFindMatchClicked;
        if (_practiceBtn != null) _practiceBtn.clicked += OnPracticeClicked;
        if (_settingsBtn != null) _settingsBtn.clicked += OnSettingsClicked;
        if (_cancelMatchBtn != null) _cancelMatchBtn.clicked += OnCancelMatchClicked;
        if (_openProfileBtn != null) _openProfileBtn.clicked += OnOpenProfileClicked;

        // Binds Bottom Nav Tabs
        if (_navShopBtn != null) _navShopBtn.clicked += () => SwitchBottomTab(0);
        if (_navHomeBtn != null) _navHomeBtn.clicked += () => SwitchBottomTab(1);
        if (_navLeaderboardBtn != null) _navLeaderboardBtn.clicked += () => SwitchBottomTab(2);

        // Binds Sub Tabs
        if (_subtabLeaderboardBtn != null) _subtabLeaderboardBtn.clicked += () => SwitchSubTab(0);
        if (_subtabAchievementsBtn != null) _subtabAchievementsBtn.clicked += () => SwitchSubTab(1);

        _moneyLabel = root.Q<Label>("money-label");
        _nameLabel = root.Q<Label>("name-label");
        _avatarElement = root.Q<VisualElement>("avatar");
        _levelTag = root.Q<Label>("level-tag");
        _searchingLabel = root.Q<Label>("searching-label");
        // G-12: XP Bar
        _xpFill = root.Q<VisualElement>("xp-fill");
        _xpLabel = root.Q<Label>("xp-label");

        // Particle layer cho ambient effect
        _particleLayer = root.Q<VisualElement>("home-particle-layer");

        ShowHomePanel();
        RefreshPlayerStatsUI();

        // Swap: Orbs and background color breathing on Home screen
        StartBackgroundAnimation();

        // Entry animation: hero bounce in + buttons cascade
        AnimateHomeEntry(root);

        // Localization
        LocalizationManager.OnLanguageChanged += LocalizeUI;
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
            LocalizeUI();

        // Firebase Matchmaking events
        FirebaseManager.OnMatchFound        += OnMatchFoundFromFirebase;
        FirebaseManager.OnMatchmakingError  += OnMatchmakingError;
        FirebaseManager.OnMatchmakingTimeout += OnMatchmakingTimeout; // UX-06
    }

    private void OnDisable()
    {
        if (_findMatchBtn != null) _findMatchBtn.clicked -= OnFindMatchClicked;
        if (_practiceBtn != null) _practiceBtn.clicked -= OnPracticeClicked;
        if (_settingsBtn != null) _settingsBtn.clicked -= OnSettingsClicked;
        if (_cancelMatchBtn != null) _cancelMatchBtn.clicked -= OnCancelMatchClicked;
        if (_openProfileBtn != null) _openProfileBtn.clicked -= OnOpenProfileClicked;

        LocalizationManager.OnLanguageChanged -= LocalizeUI;
        FirebaseManager.OnMatchFound        -= OnMatchFoundFromFirebase;
        FirebaseManager.OnMatchmakingError  -= OnMatchmakingError;
        FirebaseManager.OnMatchmakingTimeout -= OnMatchmakingTimeout; // UX-06
    }

    private void LocalizeUI()
    {
        if (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsReady) return;
        var L = LocalizationManager.Instance;

        if (_findMatchBtn != null) _findMatchBtn.text = L.GetText("menu_find_match");
        if (_practiceBtn != null) _practiceBtn.text = L.GetText("menu_practice");
        if (_cancelMatchBtn != null) _cancelMatchBtn.text = L.GetText("menu_cancel");
        if (_searchingLabel != null) _searchingLabel.text = L.GetText("menu_searching");

        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            
            var heroTitle = root.Q<Label>(className: "hero-title");
            if (heroTitle != null) heroTitle.text = L.GetText("menu_hero_title", "PVP QUIZ GAME");
            
            var heroSubtitle = root.Q<Label>(className: "hero-subtitle");
            if (heroSubtitle != null) heroSubtitle.text = L.GetText("menu_hero_subtitle", "THỬ THÁCH KIẾN THỨC");
            
            var shopTitle = root.Q<Label>("shop-title-label");
            if (shopTitle != null) shopTitle.text = L.GetText("menu_shop_title", "CỬA HÀNG");
            
            var shopComingSoon = root.Q<Label>("shop-coming-soon-label");
            if (shopComingSoon != null) shopComingSoon.text = L.GetText("menu_shop_coming_soon", "Tính năng đang được phát triển (Coming Soon)");
            
            var navShopBtn = root.Q<Button>("nav-shop-btn");
            if (navShopBtn != null) {
                var lbl = navShopBtn.Q<Label>(className: "nav-tab-label");
                if (lbl != null) lbl.text = L.GetText("menu_tab_shop", "SHOP");
            }
            
            var navHomeBtn = root.Q<Button>("nav-home-btn");
            if (navHomeBtn != null) {
                var lbl = navHomeBtn.Q<Label>(className: "nav-tab-label");
                if (lbl != null) lbl.text = L.GetText("menu_tab_home", "TRANG CHỦ");
            }
            
            var navRankBtn = root.Q<Button>("nav-leaderboard-btn");
            if (navRankBtn != null) {
                var lbl = navRankBtn.Q<Label>(className: "nav-tab-label");
                if (lbl != null) lbl.text = L.GetText("menu_tab_rank", "XẾP HẠNG");
            }
        }

        if (_subtabLeaderboardBtn != null) _subtabLeaderboardBtn.text = L.GetText("menu_subtab_leaderboard", "XẾP HẠNG");
        if (_subtabAchievementsBtn != null) _subtabAchievementsBtn.text = L.GetText("menu_subtab_achievements", "THÀNH TỰU");

        // Force reload achievements with localized text if it's currently showing
        if (_subpanelAchievements != null && _subpanelAchievements.style.display == DisplayStyle.Flex)
        {
            LoadAchievementsData();
        }

        RefreshSettingsPopupLocalization();
    }

    private void RefreshPlayerStatsUI()
    {
        if (PlayerDataManager.Instance == null) return;
        var data = PlayerDataManager.Instance.Data;
        if (_levelTag != null) _levelTag.text = $"LEVEL {data.level}";
        if (_moneyLabel != null) _moneyLabel.text = $"${data.money:N0}";
        if (_nameLabel != null) _nameLabel.text = data.playerName;
        
        // Avatar: dùng Initial Letter thay vì sprite tạm
        if (_avatarElement != null)
        {
            AvatarHelper.SetAvatar(_avatarElement, data.playerName);
        }

        // G-12: XP progress bar
        if (_xpFill != null)
        {
            float expPercent = (float)data.currentExp / Mathf.Max(1, data.GetExpToNextLevel());
            _xpFill.style.width = Length.Percent(Mathf.Clamp(expPercent * 100f, 0f, 100f));
        }
        if (_xpLabel != null)
        {
            _xpLabel.text = $"{data.currentExp} / {data.GetExpToNextLevel()}";
        }

        Debug.Log($"<color=white>[MainMenu] Updated: {data.playerName} | L{data.level} | ${data.money}</color>");
    }

    // ==================== ĐIỀU HƯỚNG PANEL ====================
    private void ShowPanel(VisualElement target)
    {
        if (_homePanel != null) _homePanel.style.display = (_homePanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
        if (_shopPanel != null) _shopPanel.style.display = (_shopPanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
        if (_leaderboardPanel != null) _leaderboardPanel.style.display = (_leaderboardPanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
        if (_matchmakingPanel != null) _matchmakingPanel.style.display = (_matchmakingPanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void ShowHomePanel()
    {
        SwitchBottomTab(1); // Set to Home tab
    }

    private void SwitchBottomTab(int index)
    {
        // FEAT: Chặn Khách không cho vào Leaderboard
        if (index == 2 && FirebaseManager.Instance != null && FirebaseManager.Instance.IsAnonymous)
        {
            ShowGuestLoginPopup();
            return;
        }

        // Update nav buttons visually
        if (_navShopBtn != null) _navShopBtn.RemoveFromClassList("nav-tab-active");
        if (_navHomeBtn != null) _navHomeBtn.RemoveFromClassList("nav-tab-active");
        if (_navLeaderboardBtn != null) _navLeaderboardBtn.RemoveFromClassList("nav-tab-active");

        if (index == 0)
        {
            if (_navShopBtn != null) _navShopBtn.AddToClassList("nav-tab-active");
            ShowPanel(_shopPanel);
        }
        else if (index == 1)
        {
            if (_navHomeBtn != null) _navHomeBtn.AddToClassList("nav-tab-active");
            ShowPanel(_homePanel);
        }
        else if (index == 2)
        {
            if (_navLeaderboardBtn != null) _navLeaderboardBtn.AddToClassList("nav-tab-active");
            ShowPanel(_leaderboardPanel);
            SwitchSubTab(0); // Mặc định mở Leaderboard khi qua tab này
        }
    }

    private void SwitchSubTab(int index)
    {
        if (_subtabLeaderboardBtn != null) _subtabLeaderboardBtn.RemoveFromClassList("sub-tab-active");
        if (_subtabAchievementsBtn != null) _subtabAchievementsBtn.RemoveFromClassList("sub-tab-active");
        
        if (_subpanelLeaderboard != null) _subpanelLeaderboard.style.display = DisplayStyle.None;
        if (_subpanelAchievements != null) _subpanelAchievements.style.display = DisplayStyle.None;

        if (index == 0)
        {
            if (_subtabLeaderboardBtn != null) _subtabLeaderboardBtn.AddToClassList("sub-tab-active");
            if (_subpanelLeaderboard != null) _subpanelLeaderboard.style.display = DisplayStyle.Flex;
            LoadLeaderboardData();
        }
        else if (index == 1)
        {
            if (_subtabAchievementsBtn != null) _subtabAchievementsBtn.AddToClassList("sub-tab-active");
            if (_subpanelAchievements != null) _subpanelAchievements.style.display = DisplayStyle.Flex;
            LoadAchievementsData();
        }
    }

    // ==================== MATCHMAKING ====================
    private void OnFindMatchClicked()
    {
        var fm = FirebaseManager.Instance;
        if (fm == null) return;

        // FEAT-02: Kiểm tra Guest — hiển dialog cảnh báo thay vì log error
        if (!fm.IsAuthenticated)
        {
            var L = LocalizationManager.Instance;
            string msg = L != null
                ? L.GetText("menu_login_required", "Bạn cần đăng nhập để tìm trận online.")
                : "Bạn cần đăng nhập để tìm trận online.";
            ShowInfoToast(msg, 3f);
            return;
        }

        // BẮT BUỘC Tắt offline mode khi bấm tìm trận thật
        fm.isOfflineMode = false;
        _isCancelledMatchmaking = false;

        // Kiểm tra kết nối Firebase
        if (!fm.IsConnected)
        {
            var L = LocalizationManager.Instance;
            string errMsg = L != null
                ? L.GetText("menu_error_connection", "Lỗi kết nối máy chủ.")
                : "Lỗi kết nối máy chủ.";
            if (_searchingLabel != null) _searchingLabel.text = errMsg;
            return;
        }

        ShowPanel(_matchmakingPanel);
        Debug.Log($"[MainMenu] {fm.LocalDisplayName} đang tìm trận thật qua Firebase...");
        fm.StartMatchmaking();
    }

    private void OnPracticeClicked()
    {
        var fm = FirebaseManager.Instance;
        if (fm == null) return;

        // BẮT BUỘC Bật offline mode khi bấm đấu máy
        fm.isOfflineMode = true;
        _isCancelledMatchmaking = false;

        Debug.Log("[MainMenu] Chế độ Đấu với máy — vào trận ngay.");
        ShowPanel(_matchmakingPanel);
        if (_searchingLabel != null)
        {
            if (LocalizationManager.Instance != null)
                _searchingLabel.text = LocalizationManager.Instance.GetText("menu_preparing");
            else
                _searchingLabel.text = "ĐANG CHUẨN BỊ...";
        }
        // FIX-CANCEL: Lưu coroutine để có thể dừng khi cancel
        if (_offlineRoutine != null) StopCoroutine(_offlineRoutine);
        _offlineRoutine = StartCoroutine(OfflineGoToGameplayRoutine());
    }

    private IEnumerator OfflineGoToGameplayRoutine()
    {
        yield return new WaitForSeconds(2f);
        LoadGameplayScene();
    }

    private void OnMatchFoundFromFirebase()
    {
        // FIX-CANCEL: Nếu người dùng đã cancel trước khi match tìm thấy → bỏ qua
        if (_isCancelledMatchmaking)
        {
            Debug.Log("[MainMenu] OnMatchFound bị bỏ qua vì đã cancel matchmaking.");
            return;
        }
        var fm = FirebaseManager.Instance;
        Debug.Log($"[MainMenu] Đã ghép: {fm?.LocalDisplayName} vs {fm?.OpponentName}. Vào trận!");
        LoadGameplayScene();
    }

    private void OnMatchmakingError(string error)
    {
        Debug.LogError($"[MainMenu] Matchmaking error: {error}");
        var L = LocalizationManager.Instance;
        if (_searchingLabel != null)
        {
            string fmt = L != null ? L.GetText("menu_error_generic", "Lỗi: {0}") : "Lỗi: {0}";
            _searchingLabel.text = string.Format(fmt, error);
        }
        // Sau 2s quay về Home
        StartCoroutine(ReturnToHomeAfter(2f));
    }

    private IEnumerator ReturnToHomeAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ShowHomePanel();
    }

    /// <summary>Hiển thị toast notification ngắn trên HomeScene (dùng cho cảnh báo guest, lỗi v.v.)</summary>
    private void ShowInfoToast(string message, float duration = 2.5f)
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        var toast = new UnityEngine.UIElements.Label(message);
        toast.style.position = UnityEngine.UIElements.Position.Absolute;
        toast.style.bottom = 160;
        toast.style.left = 0;
        toast.style.right = 0;
        toast.style.unityTextAlign = TextAnchor.MiddleCenter;
        toast.style.fontSize = 28;
        toast.style.color = Color.white;
        toast.style.backgroundColor = new Color(0.18f, 0.05f, 0.26f, 0.92f);
        toast.style.paddingTop = 18;
        toast.style.paddingBottom = 18;
        toast.style.paddingLeft = 28;
        toast.style.paddingRight = 28;
        toast.style.borderTopLeftRadius = 16;
        toast.style.borderTopRightRadius = 16;
        toast.style.borderBottomLeftRadius = 16;
        toast.style.borderBottomRightRadius = 16;
        toast.style.marginLeft = UnityEngine.UIElements.StyleKeyword.Auto;
        toast.style.marginRight = UnityEngine.UIElements.StyleKeyword.Auto;
        toast.style.maxWidth = new UnityEngine.UIElements.Length(85, UnityEngine.UIElements.LengthUnit.Percent);
        toast.style.whiteSpace = UnityEngine.UIElements.WhiteSpace.Normal;
        toast.style.unityFontStyleAndWeight = FontStyle.Bold;

        root.Add(toast);
        StartCoroutine(RemoveToastAfter(toast, duration));
    }

    private System.Collections.IEnumerator RemoveToastAfter(UnityEngine.UIElements.VisualElement el, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (el != null && el.parent != null) el.RemoveFromHierarchy();
    }

    // UX-06: Matchmaking timeout handler
    private void OnMatchmakingTimeout()
    {
        var L = LocalizationManager.Instance;
        string msg = L != null
            ? L.GetText("menu_matchmaking_timeout", "Không tìm thấy đối thủ. Thử lại?")
            : "Không tìm thấy đối thủ. Thử lại?";
        ShowInfoToast(msg, 4f);
        ShowHomePanel();
    }

    /// <summary>
    /// Hiển thị AuthPopup inline ngay trên HomeScene thay vì redirect về InitScene.
    /// Guest có thể đăng nhập/đăng ký ngay tại đây, hoặc bấm Hủy để quay lại chơi tiếp.
    /// Sau khi auth thành công → refresh UI, không cần chuyển scene.
    /// </summary>
    private VisualElement _inlineAuthPopup;

    private void ShowGuestLoginPopup()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        // Nếu đã mở rồi thì bỏ qua
        if (_inlineAuthPopup != null && _inlineAuthPopup.parent != null) return;

        // Ưu tiên dùng authPopupTemplate (cùng UXML như InitScene)
        if (authPopupTemplate != null)
        {
            _inlineAuthPopup = authPopupTemplate.Instantiate();
        }
        else
        {
            // Fallback: load từ Resources
            var asset = Resources.Load<VisualTreeAsset>("UI/AuthPopup");
            if (asset == null)
            {
                Debug.LogError("[MainMenu] Không tìm thấy AuthPopup template!");
                return;
            }
            _inlineAuthPopup = asset.Instantiate();
        }

        _inlineAuthPopup.style.position = Position.Absolute;
        _inlineAuthPopup.style.top = 0; _inlineAuthPopup.style.bottom = 0;
        _inlineAuthPopup.style.left = 0; _inlineAuthPopup.style.right = 0;
        root.Add(_inlineAuthPopup);

        // Animation
        var overlay = _inlineAuthPopup.Q<VisualElement>("overlay") ?? _inlineAuthPopup.Children().First();
        var popupCard = _inlineAuthPopup.Q<VisualElement>("popup-container") ?? overlay.Children().First();
        UIAnimator.ShowPopupAnim(overlay, popupCard);

        // Localize
        LocalizeInlineAuthPopup();

        // Containers
        var mainContainer = _inlineAuthPopup.Q<VisualElement>("main-choice-container");
        var loginContainer = _inlineAuthPopup.Q<VisualElement>("login-container");
        var regContainer = _inlineAuthPopup.Q<VisualElement>("register-container");
        var guestContainer = _inlineAuthPopup.Q<VisualElement>("guest-container");
        var forgotContainer = _inlineAuthPopup.Q<VisualElement>("forgot-container");
        var errorLabel = _inlineAuthPopup.Q<Label>("auth-error");

        // ẨN nút "Chơi khách" vì user đã là guest rồi
        var gotoGuestBtn = _inlineAuthPopup.Q<Button>("goto-guest-btn");
        if (gotoGuestBtn != null) gotoGuestBtn.style.display = DisplayStyle.None;

        void ShowContainer(VisualElement container)
        {
            if (mainContainer != null) mainContainer.style.display = DisplayStyle.None;
            if (loginContainer != null) loginContainer.style.display = DisplayStyle.None;
            if (regContainer != null) regContainer.style.display = DisplayStyle.None;
            if (guestContainer != null) guestContainer.style.display = DisplayStyle.None;
            if (forgotContainer != null) forgotContainer.style.display = DisplayStyle.None;
            container.style.display = DisplayStyle.Flex;
            if (errorLabel != null) errorLabel.text = "";
        }

        // Đăng ký lắng nghe lỗi auth
        System.Action<string> authErrorHandler = (msg) =>
        {
            if (errorLabel != null) errorLabel.text = msg;
        };
        FirebaseManager.OnAuthError += authErrorHandler;

        // Cleanup helper — đóng popup + gỡ listener
        System.Action closePopup = () =>
        {
            FirebaseManager.OnAuthError -= authErrorHandler;
            UIAnimator.HidePopupAnim(overlay, popupCard, () =>
            {
                if (_inlineAuthPopup != null && _inlineAuthPopup.parent != null)
                    _inlineAuthPopup.RemoveFromHierarchy();
                _inlineAuthPopup = null;
            });
        };

        // Callback khi auth thành công — refresh UI + đóng popup
        System.Action onAuthSuccess = () =>
        {
            FirebaseManager.OnAuthError -= authErrorHandler;
            // Refresh player data UI
            RefreshPlayerStatsUI();
            // Đóng popup
            UIAnimator.HidePopupAnim(overlay, popupCard, () =>
            {
                if (_inlineAuthPopup != null && _inlineAuthPopup.parent != null)
                    _inlineAuthPopup.RemoveFromHierarchy();
                _inlineAuthPopup = null;
            });
            // Mở leaderboard ngay
            SwitchBottomTab(2);
        };

        // --- MAIN CHOICE ---
        _inlineAuthPopup.Q<Button>("goto-login-btn").clicked += () => ShowContainer(loginContainer);
        _inlineAuthPopup.Q<Button>("goto-register-btn").clicked += () => ShowContainer(regContainer);

        // --- Thêm nút HỦY (quay lại HomeScene, vẫn là guest) ---
        // Đổi tiêu đề popup
        var titleLabel = _inlineAuthPopup.Q<Label>("popup-title");
        if (titleLabel != null)
        {
            var L = LocalizationManager.Instance;
            titleLabel.text = L != null
                ? L.GetText("menu_rank_guest_blocked_title", "YÊU CẦU ĐĂNG NHẬP")
                : "YÊU CẦU ĐĂNG NHẬP";
        }

        // Thêm nút HỦY vào main-choice-container (bên dưới cùng)
        var cancelBtn = new Button();
        var Loc = LocalizationManager.Instance;
        cancelBtn.text = Loc != null ? Loc.GetText("menu_cancel", "HỦY") : "HỦY";
        cancelBtn.AddToClassList("btn");
        cancelBtn.AddToClassList("btn-danger");
        cancelBtn.style.width = Length.Percent(100);
        cancelBtn.style.fontSize = 32;
        cancelBtn.style.height = 86;
        cancelBtn.style.borderTopLeftRadius = 22; cancelBtn.style.borderTopRightRadius = 22;
        cancelBtn.style.borderBottomLeftRadius = 22; cancelBtn.style.borderBottomRightRadius = 22;
        cancelBtn.style.marginTop = 16;
        cancelBtn.clicked += closePopup;
        mainContainer?.Add(cancelBtn);

        // --- LOGIN ---
        var loginEmail = _inlineAuthPopup.Q<TextField>("login-email");
        var loginPass = _inlineAuthPopup.Q<TextField>("login-password");
        _inlineAuthPopup.Q<Button>("login-back-btn").clicked += () => ShowContainer(mainContainer);
        _inlineAuthPopup.Q<Button>("login-confirm-btn").clicked += async () =>
        {
            string email = loginEmail.value.Trim();
            string pass = loginPass.value;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                errorLabel.text = Loc?.GetText("auth_err_empty", "Vui lòng nhập đầy đủ email và mật khẩu.") ?? "Vui lòng nhập đầy đủ email và mật khẩu.";
                return;
            }
            errorLabel.text = Loc?.GetText("auth_status_logging_in", "Đang đăng nhập...") ?? "Đang đăng nhập...";

            // Link anonymous → email (nâng cấp tài khoản khách → tài khoản thật)
            bool success = await FirebaseManager.Instance.SignInWithEmail(email, pass);
            if (success) onAuthSuccess();
        };

        // --- FORGOT PASSWORD ---
        var forgotPassBtn = _inlineAuthPopup.Q<Button>("forgot-pass-btn");
        if (forgotPassBtn != null) forgotPassBtn.clicked += () => ShowContainer(forgotContainer);

        var forgotEmailField = _inlineAuthPopup.Q<TextField>("forgot-email");
        var forgotConfirmBtn = _inlineAuthPopup.Q<Button>("forgot-confirm-btn");
        var forgotBackBtn = _inlineAuthPopup.Q<Button>("forgot-back-btn");
        if (forgotBackBtn != null) forgotBackBtn.clicked += () => ShowContainer(loginContainer);
        if (forgotConfirmBtn != null)
        {
            forgotConfirmBtn.clicked += async () =>
            {
                string email = forgotEmailField.value.Trim();
                if (string.IsNullOrEmpty(email))
                {
                    errorLabel.text = Loc?.GetText("auth_err_email_empty", "Vui lòng nhập email.") ?? "Vui lòng nhập email.";
                    return;
                }
                errorLabel.text = Loc?.GetText("auth_status_sending", "Đang gửi yêu cầu...") ?? "Đang gửi yêu cầu...";
                bool success = await FirebaseManager.Instance.SendPasswordResetEmail(email);
                if (success)
                {
                    errorLabel.text = Loc?.GetText("auth_status_email_sent", "Email đặt lại mật khẩu đã được gửi!") ?? "Email đặt lại mật khẩu đã được gửi!";
                    errorLabel.style.color = new Color(0.2f, 0.8f, 0.2f);
                    await System.Threading.Tasks.Task.Delay(2500);
                    if (forgotContainer != null && forgotContainer.style.display == DisplayStyle.Flex)
                    {
                        ShowContainer(loginContainer);
                        errorLabel.style.color = new Color(1f, 0.32f, 0.32f);
                    }
                }
            };
        }

        // --- REGISTER ---
        var regName = _inlineAuthPopup.Q<TextField>("reg-display-name");
        var regEmail = _inlineAuthPopup.Q<TextField>("reg-email");
        var regPass = _inlineAuthPopup.Q<TextField>("reg-password");
        _inlineAuthPopup.Q<Button>("reg-back-btn").clicked += () => ShowContainer(mainContainer);
        _inlineAuthPopup.Q<Button>("reg-confirm-btn").clicked += async () =>
        {
            string name = regName.value.Trim();
            string email = regEmail.value.Trim();
            string pass = regPass.value;
            if (name.Length < 2 || string.IsNullOrEmpty(email) || pass.Length < 6)
            {
                errorLabel.text = Loc?.GetText("auth_err_reg_invalid", "Tên > 2 ký tự, Email hợp lệ, Mật khẩu > 6 ký tự.") ?? "Tên > 2 ký tự, Email hợp lệ, Mật khẩu > 6 ký tự.";
                return;
            }
            errorLabel.text = Loc?.GetText("auth_status_registering", "Đang đăng ký...") ?? "Đang đăng ký...";
            bool success = await FirebaseManager.Instance.SignUpWithEmail(email, pass, name);
            if (success) onAuthSuccess();
        };
    }

    /// <summary>
    /// Localize inline AuthPopup (reuse logic từ InitScene).
    /// </summary>
    private void LocalizeInlineAuthPopup()
    {
        if (_inlineAuthPopup == null || LocalizationManager.Instance == null || !LocalizationManager.Instance.IsReady) return;
        var L = LocalizationManager.Instance;

        var title = _inlineAuthPopup.Q<Label>("popup-title");
        if (title != null) title.text = L.GetText("menu_rank_guest_blocked_title", "YÊU CẦU ĐĂNG NHẬP");

        var gotoLoginBtn = _inlineAuthPopup.Q<Button>("goto-login-btn");
        var gotoRegBtn = _inlineAuthPopup.Q<Button>("goto-register-btn");
        if (gotoLoginBtn != null) gotoLoginBtn.text = L.GetText("auth_btn_goto_login", "ĐĂNG NHẬP BẰNG EMAIL");
        if (gotoRegBtn != null) gotoRegBtn.text = L.GetText("auth_btn_goto_register", "TẠO TÀI KHOẢN");

        var loginEmail = _inlineAuthPopup.Q<TextField>("login-email");
        var loginPass = _inlineAuthPopup.Q<TextField>("login-password");
        var loginConfirm = _inlineAuthPopup.Q<Button>("login-confirm-btn");
        var forgotPassBtn = _inlineAuthPopup.Q<Button>("forgot-pass-btn");
        var loginBack = _inlineAuthPopup.Q<Button>("login-back-btn");
        if (loginEmail != null) loginEmail.label = L.GetText("auth_lbl_email", "Email");
        if (loginPass != null) loginPass.label = L.GetText("auth_lbl_password", "Mật khẩu");
        if (loginConfirm != null) loginConfirm.text = L.GetText("auth_btn_login", "ĐĂNG NHẬP");
        if (forgotPassBtn != null) forgotPassBtn.text = L.GetText("auth_btn_forgot_password", "Quên mật khẩu?");
        if (loginBack != null) loginBack.text = L.GetText("menu_cancel", "QUAY LẠI");

        var regName = _inlineAuthPopup.Q<TextField>("reg-display-name");
        var regEmail = _inlineAuthPopup.Q<TextField>("reg-email");
        var regPass = _inlineAuthPopup.Q<TextField>("reg-password");
        var regConfirm = _inlineAuthPopup.Q<Button>("reg-confirm-btn");
        var regBack = _inlineAuthPopup.Q<Button>("reg-back-btn");
        if (regName != null) regName.label = L.GetText("auth_lbl_display_name", "Tên hiển thị");
        if (regEmail != null) regEmail.label = L.GetText("auth_lbl_email", "Email");
        if (regPass != null) regPass.label = L.GetText("auth_lbl_password", "Mật khẩu");
        if (regConfirm != null) regConfirm.text = L.GetText("auth_btn_register", "ĐĂNG KÝ");
        if (regBack != null) regBack.text = L.GetText("menu_cancel", "QUAY LẠI");

        var forgotPrompt = _inlineAuthPopup.Q<VisualElement>("forgot-container")?.Q<Label>();
        var forgotEmail = _inlineAuthPopup.Q<TextField>("forgot-email");
        var forgotConfirm = _inlineAuthPopup.Q<Button>("forgot-confirm-btn");
        var forgotBack = _inlineAuthPopup.Q<Button>("forgot-back-btn");
        if (forgotPrompt != null) forgotPrompt.text = L.GetText("auth_lbl_forgot_prompt", "Nhập email để đặt lại mật khẩu:");
        if (forgotEmail != null) forgotEmail.label = L.GetText("auth_lbl_email", "Email");
        if (forgotConfirm != null) forgotConfirm.text = L.GetText("auth_btn_send_request", "GỬI YÊU CẦU");
        if (forgotBack != null) forgotBack.text = L.GetText("menu_cancel", "QUAY LẠI");
    }

    private void OnCancelMatchClicked()
    {
        // FIX-CANCEL: Đánh dấu đã cancel để block OnMatchFound nếu nó fire muộn
        _isCancelledMatchmaking = true;

        var fm = FirebaseManager.Instance;
        if (fm != null)
        {
            if (!fm.isOfflineMode)
            {
                // Online: hủy matchmaking Firebase
                fm.CancelMatchmaking();
            }
            else
            {
                // Offline: dừng coroutine chờ vào game
                if (_offlineRoutine != null)
                {
                    StopCoroutine(_offlineRoutine);
                    _offlineRoutine = null;
                    Debug.Log("[MainMenu] Đã hủy coroutine offline matchmaking.");
                }
                // Reset offline mode
                fm.isOfflineMode = false;
            }
        }

        ShowHomePanel();
    }

    private void LoadGameplayScene()
    {
        if (GameManager.Instance != null) GameManager.Instance.LoadGameplayScene();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayScene");
    }

    // ==================== SETTINGS ====================
    private void OnSettingsClicked()
    {
        if (settingsPopupTemplate == null)
        {
            Debug.LogWarning("[MainMenu] Chưa gán SettingsPopupTemplate trong Inspector!");
            return;
        }

        if (_settingsPopup != null && _settingsPopup.parent != null) return;

        _settingsPopup = settingsPopupTemplate.Instantiate();
        
        // FIX: Đảm bảo TemplateContainer chiếm toàn bộ màn hình
        _settingsPopup.style.position = Position.Absolute;
        _settingsPopup.style.top = 0;
        _settingsPopup.style.bottom = 0;
        _settingsPopup.style.left = 0;
        _settingsPopup.style.right = 0;

        uiDocument.rootVisualElement.Add(_settingsPopup);

        // ANIMATION
        var overlay = _settingsPopup.Q<VisualElement>("overlay") ?? _settingsPopup.Children().First();
        var popupCard = _settingsPopup.Q<VisualElement>("popup") ?? overlay.Children().First();
        UIAnimator.ShowPopupAnim(overlay, popupCard);

        // Khởi tạo ngôn ngữ ban đầu cho popup
        RefreshSettingsPopupLocalization();

        var closeBtn = _settingsPopup.Q<Button>("close-btn");
        if (closeBtn != null)
        {
            closeBtn.clicked += CloseSettingsPopup;
        }

        // Dropdown Ngôn ngữ
        // BUG FIX: Chỉ hiển thị các ngôn ngữ có file JSON local tương ứng.
        // Khi thêm ngôn ngữ mới, cần tạo file StreamingAssets/Localization/<code>.json
        // rồi bổ sung vào danh sách này.
        var langDropdown = _settingsPopup.Q<DropdownField>("language-dropdown");
        if (langDropdown != null)
        {
            langDropdown.choices = new System.Collections.Generic.List<string> {
                "Tiếng Việt", "English", "Français", "Italiano", "Deutsch", "Español", "日本語", "한국어"
            };

            // Set giá trị hiện tại
            string current = LocalizationManager.Instance.CurrentLanguage;
            int idx = GetLanguageIndex(current);
            langDropdown.index = idx;

            langDropdown.RegisterValueChangedCallback(evt => {
                string code = GetLanguageCode(evt.newValue);
                LocalizationManager.Instance.SwitchLanguage(code);
                Debug.Log($"[MainMenu] Đã chọn ngôn ngữ: {evt.newValue} ({code})");
            });
        }

        // Toggles Âm thanh
        var musicToggle = _settingsPopup.Q<Toggle>("music-toggle");
        var sfxToggle = _settingsPopup.Q<Toggle>("sfx-toggle");

        if (AudioManager.Instance != null)
        {
            if (musicToggle != null)
            {
                musicToggle.value = AudioManager.Instance.IsMusicEnabled;
                musicToggle.RegisterValueChangedCallback(evt => {
                    AudioManager.Instance.SetMusicEnabled(evt.newValue);
                });
            }

            if (sfxToggle != null)
            {
                sfxToggle.value = AudioManager.Instance.IsSFXEnabled;
                sfxToggle.RegisterValueChangedCallback(evt => {
                    AudioManager.Instance.SetSFXEnabled(evt.newValue);
                });
            }
        }

        var logoutBtn = _settingsPopup.Q<Button>("logout-btn");
        if (logoutBtn != null)
        {
            logoutBtn.clicked += ShowLogoutConfirmation;
        }

        Debug.Log("[MainMenu] Mở Settings Popup.");
    }

    private void ShowLogoutConfirmation()
    {
        if (logoutPopupTemplate == null) return;
        
        var popup = logoutPopupTemplate.Instantiate();
        popup.style.position = Position.Absolute;
        popup.style.top = 0; popup.style.bottom = 0;
        popup.style.left = 0; popup.style.right = 0;

        uiDocument.rootVisualElement.Add(popup);

        // ANIMATION — tên khớp với LogoutConfirmPopup.uxml
        var overlay = popup.Q<VisualElement>("overlay") ?? popup.Children().First();
        var popupCard = popup.Q<VisualElement>("popup-container") ?? overlay.Children().First();
        UIAnimator.ShowPopupAnim(overlay, popupCard);

        var titleLabel = popup.Q<Label>("logout-title");
        var msgLabel = popup.Q<Label>("message-label");
        var confirmBtn = popup.Q<Button>("logout-yes-btn");
        var cancelBtn = popup.Q<Button>("logout-no-btn");

        var fm = FirebaseManager.Instance;
        var pdm = PlayerDataManager.Instance;
        var L = LocalizationManager.Instance;

        if (L != null && L.IsReady)
        {
            if (titleLabel != null) titleLabel.text = L.GetText("logout_confirm_title", "ĐĂNG XUẤT");
            if (confirmBtn != null) confirmBtn.text = L.GetText("logout_confirm_ok", "ĐĂNG XUẤT");
            if (cancelBtn != null) cancelBtn.text = L.GetText("logout_confirm_cancel", "HỦY");
        }

        // Cấu hình tin nhắn cảnh báo
        if (fm != null && fm.IsAuthenticated && msgLabel != null)
        {
            bool isGuest = fm.IsAnonymous;

            if (isGuest)
            {
                msgLabel.text = L != null && L.IsReady ? L.GetText("logout_confirm_msg_guest", "CẢNH BÁO: Đăng xuất sẽ làm MẤT dữ liệu!") : "CẢNH BÁO: Đăng xuất sẽ làm MẤT dữ liệu!";
                msgLabel.style.color = Color.red;
            }
            else
            {
                msgLabel.text = L != null && L.IsReady ? L.GetText("logout_confirm_msg_user", "Bạn có chắc chắn muốn đăng xuất không?") : "Bạn có chắc chắn muốn đăng xuất không?";
                msgLabel.style.color = new Color(0.2f, 0.2f, 0.2f);
            }
        }

        if (confirmBtn != null)
        {
            confirmBtn.clicked += () => {
                Debug.Log("[MainMenu] Thục hiện Đăng xuất...");
                if (fm != null) fm.SignOut();
                if (pdm != null) pdm.ClearData();
                
                // Quay về InitScene
                UnityEngine.SceneManagement.SceneManager.LoadScene("InitScene");
            };
        }

        if (cancelBtn != null)
        {
            cancelBtn.clicked += () => {
                UIAnimator.HidePopupAnim(overlay, popupCard, () => {
                    popup.RemoveFromHierarchy();
                });
            };
        }
    }

    // ==================== PROFILE ====================
    private VisualElement _profilePopup;

    private void OnOpenProfileClicked()
    {
        if (profilePopupTemplate == null) return;
        
        _profilePopup = profilePopupTemplate.Instantiate();
        _profilePopup.style.position = Position.Absolute;
        _profilePopup.style.top = 0; _profilePopup.style.bottom = 0;
        _profilePopup.style.left = 0; _profilePopup.style.right = 0;

        uiDocument.rootVisualElement.Add(_profilePopup);

        // ANIMATION — tên khớp với ProfilePopup.uxml
        var overlay = _profilePopup.Q<VisualElement>("profile-overlay") ?? _profilePopup.Children().First();
        var popupCard = _profilePopup.Q<VisualElement>("profile-container") ?? overlay.Children().First();
        UIAnimator.ShowPopupAnim(overlay, popupCard);

        var nameInput = _profilePopup.Q<TextField>("profile-name-field");
        var saveBtn = _profilePopup.Q<Button>("profile-save-btn");
        var closeBtn = _profilePopup.Q<Button>("profile-close-btn");

        // Localization cho Profile Popup
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
        {
            var L = LocalizationManager.Instance;
            var title = _profilePopup.Q<Label>("profile-title");
            var nameInputLocalize = _profilePopup.Q<TextField>("profile-name-field");

            if (title != null) title.text = L.GetText("profile_title", "HỒ SƠ CÁ NHÂN");
            if (nameInputLocalize != null) nameInputLocalize.label = L.GetText("profile_display_name", "TÊN HIỂN THỊ");
            if (saveBtn != null) saveBtn.text = L.GetText("profile_save", "LƯU THAY ĐỔI");
            if (closeBtn != null) closeBtn.text = L.GetText("menu_cancel", "HỦY");
        }

        var data = PlayerDataManager.Instance.Data;
        if (nameInput != null) nameInput.value = data.playerName;

        // Hiển thị avatar bằng AvatarHelper (initial letter) — nhất quán với GameplayUI
        var profileAvatar = _profilePopup.Q<VisualElement>("profile-avatar");
        if (profileAvatar != null)
            AvatarHelper.SetAvatar(profileAvatar, data.playerName);

        // Cập nhật avatar preview khi user đổi tên
        if (nameInput != null)
        {
            nameInput.RegisterValueChangedCallback(evt => {
                string preview = string.IsNullOrWhiteSpace(evt.newValue) ? "?" : evt.newValue.Trim();
                if (profileAvatar != null)
                    AvatarHelper.SetAvatar(profileAvatar, preview);
            });
        }

        if (saveBtn != null)
        {
            saveBtn.clicked += async () =>
            {
                string newName = nameInput.value.Trim();

                // FEAT-03: Hiển lỗi trên UI thay vì chỉ log warning
                if (newName.Length < 3)
                {
                    var errLabel = _profilePopup.Q<Label>("profile-name-error");
                    if (errLabel == null)
                    {
                        // Tạo label lỗi nếu chưa có trong UXML
                        errLabel = new Label();
                        errLabel.name = "profile-name-error";
                        errLabel.style.color = new Color(0.87f, 0.12f, 0.12f);
                        errLabel.style.fontSize = 24;
                        errLabel.style.marginTop = 8;
                        nameInput.parent?.Add(errLabel);
                    }
                    var L2 = LocalizationManager.Instance;
                    errLabel.text = L2 != null
                        ? L2.GetText("profile_name_too_short", "Tên phải từ 3 ký tự trở lên.")
                        : "Tên phải từ 3 ký tự trở lên.";
                    return;
                }
                else
                {
                    // Xóa lỗi nếu có
                    _profilePopup.Q<Label>("profile-name-error")?.RemoveFromHierarchy();
                }

                // BUG-FIX: Dùng UpdateDisplayName() để đồng bộ cả FirebaseManager.LocalDisplayName,
                // PlayerData.playerName, PlayerPrefs và Firebase cùng một lúc.
                // Nếu chỉ gán data.playerName rồi gọi SaveProfileToCloud(), Firebase sẽ lưu
                // LocalDisplayName cũ (chưa được cập nhật) lên cloud, gây mất tên ở lượt sau.
                if (FirebaseManager.Instance != null)
                {
                    FirebaseManager.Instance.UpdateDisplayName(newName);
                }
                else
                {
                    // Fallback khi không có Firebase (offline)
                    data.playerName = newName;
                    PlayerDataManager.Instance.SaveData();
                }

                RefreshPlayerStatsUI();
                UIAnimator.HidePopupAnim(overlay, popupCard, () =>
                {
                    _profilePopup.RemoveFromHierarchy();
                });
            };
        }

        if (closeBtn != null) closeBtn.clicked += () => {
            UIAnimator.HidePopupAnim(overlay, popupCard, () => {
                _profilePopup.RemoveFromHierarchy();
            });
        };
    }

    // ==================== LEADERBOARD ====================
    private async void LoadLeaderboardData()
    {
        if (leaderboardEntryTemplate == null)
        {
            Debug.LogWarning("[MainMenu] Chưa gán LeaderboardEntryTemplate trong Inspector!");
            return;
        }

        if (_leaderboardLoadingLabel != null) _leaderboardLoadingLabel.style.display = DisplayStyle.Flex;
        if (_leaderboardScroll != null)
        {
            _leaderboardScroll.style.display = DisplayStyle.None;
            _leaderboardScroll.Clear();
        }

        // Fetch data
        if (LeaderboardManager.Instance != null)
        {
            var topPlayers = await LeaderboardManager.Instance.FetchTopRankPlayersAsync(100);
            
            if (_leaderboardLoadingLabel != null) _leaderboardLoadingLabel.style.display = DisplayStyle.None;
            if (_leaderboardScroll != null)
            {
                _leaderboardScroll.style.display = DisplayStyle.Flex;
                
                foreach (var player in topPlayers)
                {
                    var entry = leaderboardEntryTemplate.Instantiate();
                    
                    var rankLabel = entry.Q<Label>("rank-label");
                    if (rankLabel != null)
                    {
                        rankLabel.text = player.rank.ToString();
                        if (player.rank == 1) rankLabel.style.color = new Color(1f, 0.84f, 0f); // Vàng
                        else if (player.rank == 2) rankLabel.style.color = new Color(0.75f, 0.75f, 0.75f); // Bạc
                        else if (player.rank == 3) rankLabel.style.color = new Color(0.8f, 0.5f, 0.2f); // Đồng
                    }

                    var nameLabel = entry.Q<Label>("name-label");
                    if (nameLabel != null)
                    {
                        nameLabel.text = player.displayName;
                        if (FirebaseManager.Instance != null && player.uid == FirebaseManager.Instance.LocalUserId)
                        {
                            nameLabel.style.color = new Color(0.5f, 1f, 0.5f); // Xanh lá
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

                    _leaderboardScroll.Add(entry);
                }
            }
        }
    }

    private void LoadAchievementsData()
    {
        if (achievementEntryTemplate == null)
        {
            Debug.LogWarning("[MainMenu] Chưa gán AchievementEntryTemplate trong Inspector!");
            return;
        }

        if (_achievementsScroll == null) return;
        _achievementsScroll.Clear();

        if (AchievementManager.Instance == null) return;

        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null) return;

        foreach (var ach in AchievementManager.Instance.achievements)
        {
            var entry = achievementEntryTemplate.Instantiate();

            var title = entry.Q<Label>("achievement-title");
            if (title != null) title.text = ach.name;

            var desc = entry.Q<Label>("achievement-desc");
            if (desc != null) desc.text = ach.description;

            var iconElement = entry.Q<VisualElement>("achievement-icon");
            var iconLabel = iconElement != null ? iconElement.Q<Label>() : null;
            if (iconLabel != null) iconLabel.text = ach.iconString;

            var reward = entry.Q<Label>("reward-amount");
            if (reward != null)
            {
                string suffix = ach.rewardType == RewardType.Money ? "$" : " RP";
                reward.text = ach.rewardAmount + suffix;
            }

            var progressFill = entry.Q<VisualElement>("achievement-progress-fill");
            var progressText = entry.Q<Label>("achievement-progress-text");
            var claimBtn = entry.Q<Button>("claim-btn");
            var completedTag = entry.Q<Label>("completed-tag");

            if (pd.unlockedAchievements == null)
                pd.unlockedAchievements = new System.Collections.Generic.List<string>();

            bool isUnlocked = pd.unlockedAchievements.Contains(ach.id);
            int currentProg = AchievementManager.Instance.GetCurrentProgress(ach.id);

            if (progressText != null)
            {
                int displayProg = Mathf.Min(currentProg, ach.targetValue);
                progressText.text = $"{displayProg}/{ach.targetValue}";
            }

            if (progressFill != null)
            {
                float pct = Mathf.Clamp01((float)currentProg / ach.targetValue) * 100f;
                progressFill.style.width = new Length(pct, LengthUnit.Percent);
            }

            if (isUnlocked)
            {
                if (claimBtn != null) claimBtn.style.display = DisplayStyle.None;
                if (completedTag != null)
                {
                    completedTag.style.display = DisplayStyle.Flex;
                    if (LocalizationManager.Instance != null)
                        completedTag.text = LocalizationManager.Instance.GetText("ach_btn_claimed", "ĐÃ NHẬN");
                }
                entry.Q<VisualElement>(className: "glass-panel").style.backgroundColor = new Color(0, 0.9f, 0.46f, 0.15f);
            }
            else
            {
                if (claimBtn != null)
                {
                    if (LocalizationManager.Instance != null)
                        claimBtn.text = LocalizationManager.Instance.GetText("ach_btn_not_reached", "CHƯA ĐẠT");
                    else
                        claimBtn.text = "CHƯA ĐẠT";
                    claimBtn.SetEnabled(false);
                    claimBtn.style.backgroundColor = new Color(1f, 1f, 1f, 0.1f);
                }
                if (completedTag != null) completedTag.style.display = DisplayStyle.None;
            }

            _achievementsScroll.Add(entry);
        }
    }

    // ======== Legacy helpers (giữ lại để tương thích nếu nơi khác gọi) ========
    private int GetLanguageIndex(string code)
    {
        return code switch {
            "vi" => 0, "en" => 1, "fr" => 2, "it" => 3,
            "de" => 4, "es" => 5, "ja" => 6, "ko" => 7,
            _ => 1
        };
    }

    private string GetLanguageCode(string name)
    {
        return name switch {
            "Tiếng Việt" => "vi", "English" => "en", "Français" => "fr", "Italiano" => "it",
            "Deutsch" => "de", "Español" => "es", "日本語" => "ja", "한국어" => "ko",
            _ => "en"
        };
    }

    // UX-08: Android back button
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Nếu đang trong matchmaking panel, hủy matchmaking
            if (_matchmakingPanel != null && _matchmakingPanel.style.display == DisplayStyle.Flex)
            {
                OnCancelMatchClicked();
                return;
            }
            // Nếu có inline auth popup, đóng nó
            if (_inlineAuthPopup != null && _inlineAuthPopup.parent != null)
            {
                _inlineAuthPopup.RemoveFromHierarchy();
                _inlineAuthPopup = null;
                return;
            }
            // Nếu có popup settings/profile/logout, đóng popup
            if (_settingsPopup != null && _settingsPopup.parent != null)
            {
                CloseSettingsPopup();
                return;
            }
            Application.Quit();
        }
    }

    private void RefreshSettingsPopupLocalization()
    {
        if (_settingsPopup == null || LocalizationManager.Instance == null || !LocalizationManager.Instance.IsReady) return;
        var L = LocalizationManager.Instance;

        var titleLabel = _settingsPopup.Q<Label>("settings-title");
        if (titleLabel != null) titleLabel.text = L.GetText("settings_title");
        var musicLabel = _settingsPopup.Q<Label>("music-label");
        if (musicLabel != null) musicLabel.text = L.GetText("settings_music");
        var sfxLabel = _settingsPopup.Q<Label>("sfx-label");
        if (sfxLabel != null) sfxLabel.text = L.GetText("settings_sfx");
        var langLabel = _settingsPopup.Q<Label>("language-label");
        if (langLabel != null) langLabel.text = L.GetText("settings_language");
        var closeBtn = _settingsPopup.Q<Button>("close-btn");
        if (closeBtn != null) closeBtn.text = L.GetText("settings_close");

        var logoutBtn = _settingsPopup.Q<Button>("logout-btn");
        if (logoutBtn != null) logoutBtn.text = L.GetText("settings_logout");
    }

    private void CloseSettingsPopup()
    {
        if (_settingsPopup != null)
        {
            var overlay = _settingsPopup.Q<VisualElement>("overlay") ?? _settingsPopup.Children().First();
            var popupCard = _settingsPopup.Q<VisualElement>("popup") ?? overlay.Children().First();
            UIAnimator.HidePopupAnim(overlay, popupCard, () => {
                _settingsPopup.RemoveFromHierarchy();
                _settingsPopup = null;
            });
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
            hero.style.scale = new StyleScale(new Scale(new UnityEngine.Vector2(0.5f, 0.5f)));
            hero.style.opacity = 0f;
            DOTween.Sequence()
                .SetDelay(0.2f)
                .Append(hero.DOFade(1f, 0.3f))
                .Join(hero.DOScale(UnityEngine.Vector2.one, 0.5f).SetEase(Ease.OutBack));
        }

        var buttons = new[] { _findMatchBtn, _practiceBtn };
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
                .Join(btn.DOTranslate(UnityEngine.Vector2.zero, 0.35f).SetEase(Ease.OutBack));
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

            // 1. Color Breathing (chuyển đổi Hue mượt mà)
            // Màu gốc: #0d0221 (khoảng h=0.72, s=0.94, v=0.13)
            float h = Mathf.Lerp(0.70f, 0.85f, (Mathf.Sin(t * 0.3f) + 1f) / 2f);
            root.style.backgroundColor = Color.HSVToRGB(h, 0.9f, 0.15f);

            // 2. Orb Floating Animation (di chuyển vô hạn)
            if (orb1 != null)
                orb1.style.translate = new Translate(Mathf.Sin(t * 0.5f) * 150f, Mathf.Cos(t * 0.4f) * 120f, 0);
            
            if (orb2 != null)
                orb2.style.translate = new Translate(Mathf.Cos(t * 0.35f) * -180f, Mathf.Sin(t * 0.45f) * 160f, 0);
                
            if (orb3 != null)
                orb3.style.translate = new Translate(Mathf.Sin(t * 0.6f) * 140f, Mathf.Cos(t * 0.55f) * -140f, 0);

        }).Every(16); // Chạy liên tục mỗi 16ms (tương đương ~60FPS)
    }
}
