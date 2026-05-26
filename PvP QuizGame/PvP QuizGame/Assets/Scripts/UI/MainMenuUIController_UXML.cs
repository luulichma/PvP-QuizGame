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

    // Panels
    private VisualElement _homePanel;
    private VisualElement _matchmakingPanel;

    // Buttons
    private Button _findMatchBtn;
    private Button _practiceBtn;
    private Button _settingsBtn;
    private Button _cancelMatchBtn;
    private Button _leaderboardBtn;
    private Button _openProfileBtn;

    // Profile elements
    private Label _nameLabel;
    private VisualElement _avatarElement;

    // Labels
    private Label _moneyLabel;
    private Label _levelTag;
    private Label _searchingLabel;
    // G-12: XP Bar
    private VisualElement _xpFill;
    private Label _xpLabel;

    // Settings popup instance
    private VisualElement _settingsPopup;

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
        _matchmakingPanel = root.Q<VisualElement>("matchmaking-panel");

        _findMatchBtn = root.Q<Button>("find-match-btn");
        _practiceBtn = root.Q<Button>("practice-btn");
        _settingsBtn = root.Q<Button>("settings-btn");
        _cancelMatchBtn = root.Q<Button>("cancel-match-btn");
        _leaderboardBtn = root.Q<Button>("leaderboard-btn");
        _openProfileBtn = root.Q<Button>("open-profile-btn");

        if (_findMatchBtn != null) _findMatchBtn.clicked += OnFindMatchClicked;
        if (_practiceBtn != null) _practiceBtn.clicked += OnPracticeClicked;
        if (_settingsBtn != null) _settingsBtn.clicked += OnSettingsClicked;
        if (_cancelMatchBtn != null) _cancelMatchBtn.clicked += OnCancelMatchClicked;
        if (_openProfileBtn != null) _openProfileBtn.clicked += OnOpenProfileClicked;
        if (_leaderboardBtn != null) _leaderboardBtn.style.display = DisplayStyle.None; // ẩn

        _moneyLabel = root.Q<Label>("money-label");
        _nameLabel = root.Q<Label>("name-label");
        _avatarElement = root.Q<VisualElement>("avatar");
        _levelTag = root.Q<Label>("level-tag");
        _searchingLabel = root.Q<Label>("searching-label");
        // G-12: XP Bar
        _xpFill = root.Q<VisualElement>("xp-fill");
        _xpLabel = root.Q<Label>("xp-label");

        ShowHomePanel();
        RefreshPlayerStatsUI();

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
        if (_leaderboardBtn != null) _leaderboardBtn.text = L.GetText("menu_leaderboard");
        if (_searchingLabel != null) _searchingLabel.text = L.GetText("menu_searching");

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
        if (_matchmakingPanel != null) _matchmakingPanel.style.display = (_matchmakingPanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void ShowHomePanel() => ShowPanel(_homePanel);

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

        Debug.Log("[MainMenu] Chế độ Đấu với máy — vào trận ngay.");
        ShowPanel(_matchmakingPanel);
        if (_searchingLabel != null)
        {
            if (LocalizationManager.Instance != null)
                _searchingLabel.text = LocalizationManager.Instance.GetText("menu_preparing");
            else
                _searchingLabel.text = "ĐANG CHUẨN BỊ...";
        }
        StartCoroutine(OfflineGoToGameplayRoutine());
    }

    private IEnumerator OfflineGoToGameplayRoutine()
    {
        yield return new WaitForSeconds(2f);
        LoadGameplayScene();
    }

    private void OnMatchFoundFromFirebase()
    {
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

    private void OnCancelMatchClicked()
    {
        var fm = FirebaseManager.Instance;
        if (fm != null && !fm.isOfflineMode) fm.CancelMatchmaking();
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

        if (titleLabel != null) titleLabel.text = L.GetText("logout_confirm_title", "ĐĂNG XUẤT");
        if (confirmBtn != null) confirmBtn.text = L.GetText("logout_confirm_ok", "ĐĂNG XUẤT");
        if (cancelBtn != null) cancelBtn.text = L.GetText("logout_confirm_cancel", "HỦY");

        // Cấu hình tin nhắn cảnh báo
        if (fm != null && fm.IsAuthenticated && msgLabel != null)
        {
            // BUG-10 FIX: Dùng IsAnonymous (từ FirebaseManager) thay vì dựa vào tên
            bool isGuest = fm.IsAnonymous;

            if (isGuest)
            {
                msgLabel.text = L.GetText("logout_confirm_msg_guest", "CẢNH BÁO: Đăng xuất sẽ làm MẤT dữ liệu!");
                msgLabel.style.color = Color.red;
            }
            else
            {
                msgLabel.text = L.GetText("logout_confirm_msg_user", "Bạn có chắc chắn muốn đăng xuất không?");
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
            var avatarLbl = _profilePopup.Q<Label>("profile-avatar-lbl");
            var nameLbl = _profilePopup.Q<Label>("profile-name-lbl");

            if (title != null) title.text = L.GetText("profile_title", "HỒ SƠ CÁ NHÂN");
            if (avatarLbl != null) avatarLbl.text = L.GetText("profile_select_avatar", "CHỌN ẢNH ĐẠI DIỆN");
            if (nameLbl != null) nameLbl.text = L.GetText("profile_display_name", "TÊN HIỂN THỊ");
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

                data.playerName = newName;
                PlayerDataManager.Instance.SaveData();

                // Sync Firebase
                if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsConnected)
                {
                    await FirebaseManager.Instance.SaveProfileToCloud();
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

    // Note: Cấu trúc SwitchLanguage mới đã hỗ trợ tải trực tiếp từ Sheet 
    // nên không cần lọc danh sách ngôn ngữ ở đây nữa.


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


}
