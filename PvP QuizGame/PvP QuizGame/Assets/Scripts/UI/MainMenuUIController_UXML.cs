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

    [Header("Avatar Settings")]
    [SerializeField] private Sprite[] avatarSprites;

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

        ShowHomePanel();
        RefreshPlayerStatsUI();

        // Localization
        LocalizationManager.OnLanguageChanged += LocalizeUI;
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
            LocalizeUI();

        // Firebase Matchmaking events
        FirebaseManager.OnMatchFound        += OnMatchFoundFromFirebase;
        FirebaseManager.OnMatchmakingError  += OnMatchmakingError;
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
        
        // Cập nhật Avatar ở màn hình chính
        if (_avatarElement != null && avatarSprites != null && data.avatarIndex < avatarSprites.Length)
        {
            _avatarElement.style.backgroundImage = new StyleBackground(avatarSprites[data.avatarIndex]);
            _avatarElement.style.backgroundColor = Color.clear;
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

        // BẮT BUỘC Tắt offline mode khi bấm tìm trận thật
        fm.isOfflineMode = false;

        // Online — gọi Firebase Matchmaking thật
        if (!fm.IsConnected || !fm.IsAuthenticated)
        {
            Debug.LogError("[MainMenu] Firebase chưa sẵn sàng — không thể tìm trận.");
            if (_searchingLabel != null) _searchingLabel.text = "Lỗi kết nối máy chủ.";
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
        if (_searchingLabel != null) _searchingLabel.text = $"Lỗi: {error}";
        // Sau 2s quay về Home
        StartCoroutine(ReturnToHomeAfter(2f));
    }

    private IEnumerator ReturnToHomeAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
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
                "Tiếng Việt",   // vi.json  ✓
                "English",      // en.json  ✓
                // "Français",  // fr.json  ← chưa có file → tạm tắt
                // "Italiano",  // it.json  ← chưa có file → tạm tắt
                // "Deutsch",   // de.json  ← chưa có file → tạm tắt
                // "Español",   // es.json  ← chưa có file → tạm tắt
                // "日本語",    // ja.json  ← chưa có file → tạm tắt
                // "한국어",   // ko.json  ← chưa có file → tạm tắt
            };

            // Set giá trị hiện tại — chỉ tìm trong danh sách đã lọc
            string current = LocalizationManager.Instance.CurrentLanguage;
            int idx = GetSupportedLanguageIndex(current);
            langDropdown.index = idx;

            langDropdown.RegisterValueChangedCallback(evt => {
                string code = GetSupportedLanguageCode(evt.newValue);
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

        // ANIMATION
        var overlay = popup.Q<VisualElement>("overlay") ?? popup.Children().First();
        var popupCard = popup.Q<VisualElement>("popup") ?? overlay.Children().First();
        UIAnimator.ShowPopupAnim(overlay, popupCard);

        var titleLabel = popup.Q<Label>("logout-title");
        var msgLabel = popup.Q<Label>("logout-msg");
        var confirmBtn = popup.Q<Button>("confirm-logout-btn");
        var cancelBtn = popup.Q<Button>("cancel-logout-btn");

        var fm = FirebaseManager.Instance;
        var pdm = PlayerDataManager.Instance;
        var L = LocalizationManager.Instance;

        // Cấu hình tin nhắn cảnh báo
        if (fm != null && fm.IsAuthenticated)
        {
            // Kiểm tra xem có phải tài khoản Guest không (thường Guest sẽ không có Email)
            bool isGuest = string.IsNullOrEmpty(fm.LocalUserId) || fm.LocalDisplayName.Contains("Player_"); 
            
            if (isGuest)
            {
                msgLabel.text = "CANH BAO: Ban dang dung tai khoan KHACH. Dang xuat se lam MAT TOAN BO du lieu choi!";
                msgLabel.style.color = Color.red;
            }
            else
            {
                msgLabel.text = "Ban co chac chan muon dang xuat khoi tai khoan nay không?";
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
    private int _selectedAvatarIndex = 0;
    private VisualElement _profilePopup;

    private void OnOpenProfileClicked()
    {
        if (profilePopupTemplate == null) return;
        
        _profilePopup = profilePopupTemplate.Instantiate();
        _profilePopup.style.position = Position.Absolute;
        _profilePopup.style.top = 0; _profilePopup.style.bottom = 0;
        _profilePopup.style.left = 0; _profilePopup.style.right = 0;

        uiDocument.rootVisualElement.Add(_profilePopup);

        // ANIMATION
        var overlay = _profilePopup.Q<VisualElement>("overlay") ?? _profilePopup.Children().First();
        var popupCard = _profilePopup.Q<VisualElement>("popup") ?? overlay.Children().First();
        UIAnimator.ShowPopupAnim(overlay, popupCard);

        var nameInput = _profilePopup.Q<TextField>("name-input");
        var saveBtn = _profilePopup.Q<Button>("save-profile-btn");
        var closeBtn = _profilePopup.Q<Button>("close-profile-btn");

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
        _selectedAvatarIndex = data.avatarIndex;

        // Setup Avatar Grid
        for (int i = 0; i < 8; i++)
        {
            int index = i;
            var avatarBtn = _profilePopup.Q<Button>($"avatar-{index}");
            if (avatarBtn != null)
            {
                if (avatarSprites != null && index < avatarSprites.Length)
                {
                    avatarBtn.style.backgroundImage = new StyleBackground(avatarSprites[index]);
                    avatarBtn.style.backgroundColor = Color.clear;
                }
                
                // Highlight nếu đang chọn
                UpdateAvatarSelectionUI(avatarBtn, index == _selectedAvatarIndex);

                avatarBtn.clicked += () => {
                    _selectedAvatarIndex = index;
                    // Refresh UI highlight
                    for (int j = 0; j < 8; j++) {
                        var b = _profilePopup.Q<Button>($"avatar-{j}");
                        UpdateAvatarSelectionUI(b, j == _selectedAvatarIndex);
                    }
                };
            }
        }

        if (saveBtn != null)
        {
            saveBtn.clicked += async () => {
                string newName = nameInput.value.Trim();
                if (newName.Length < 3) {
                    Debug.LogWarning("[Profile] Tên quá ngắn!");
                    return;
                }

                data.playerName = newName;
                data.avatarIndex = _selectedAvatarIndex;
                PlayerDataManager.Instance.SaveData();

                // Sync Firebase
                if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsConnected)
                {
                    await FirebaseManager.Instance.SaveProfileToCloud();
                }

                RefreshPlayerStatsUI();
                UIAnimator.HidePopupAnim(overlay, popupCard, () => {
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

    private void UpdateAvatarSelectionUI(Button btn, bool isSelected)
    {
        if (btn == null) return;
        Color color = isSelected ? new Color(0f, 0.9f, 1f) : Color.clear;
        float width = isSelected ? 8 : 4;

        btn.style.borderTopColor = color;
        btn.style.borderBottomColor = color;
        btn.style.borderLeftColor = color;
        btn.style.borderRightColor = color;

        btn.style.borderTopWidth = width;
        btn.style.borderBottomWidth = width;
        btn.style.borderLeftWidth = width;
        btn.style.borderRightWidth = width;
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

    // ======== Helpers cho dropdown chỉ hiển thị ngôn ngữ có file JSON ========
    /// <summary>Ánh xạ mã ngôn ngữ → index trong dropdown rút gọn (vi=0, en=1)</summary>
    private int GetSupportedLanguageIndex(string code)
    {
        return code switch {
            "vi" => 0,
            "en" => 1,
            _    => 1 // mặc định English nếu đang dùng ngôn ngữ chưa có file
        };
    }

    /// <summary>Ánh xạ tên hiển thị → mã ngôn ngữ trong dropdown rút gọn</summary>
    private string GetSupportedLanguageCode(string name)
    {
        return name switch {
            "Tiếng Việt" => "vi",
            "English"    => "en",
            _            => "en"
        };
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
        if (logoutBtn != null) logoutBtn.text = L.GetText("settings_logout", "ĐĂNG XUẤT");
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
