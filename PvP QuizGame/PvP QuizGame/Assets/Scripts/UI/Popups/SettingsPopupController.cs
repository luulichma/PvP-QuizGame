using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Popup Cài Đặt trên HomeScene — tách từ MainMenuUIController_UXML.OnSettingsClicked()
/// (+ RefreshSettingsPopupLocalization, CloseSettingsPopup, GetLanguageIndex/GetLanguageCode).
/// Behavior giữ nguyên: dropdown ngôn ngữ, toggle nhạc/SFX, nút đăng xuất.
/// Tự re-localize khi đổi ngôn ngữ nhờ UILocalizer trong PopupBase.
/// </summary>
public class SettingsPopupController : PopupBase
{
    private readonly VisualTreeAsset _logoutPopupTemplate;
    private LogoutConfirmPopupController _logoutPopup;

    public SettingsPopupController(VisualTreeAsset template, VisualTreeAsset logoutPopupTemplate, VisualElement parent)
        : base(template, parent, "overlay", "popup")
    {
        _logoutPopupTemplate = logoutPopupTemplate;
    }

    protected override void OnShow(VisualElement root)
    {
        // ---- Localization (tự refresh khi đổi ngôn ngữ) ----
        Localizer.BindLabel(root.Q<Label>("settings-title"), "settings_title");
        Localizer.BindLabel(root.Q<Label>("music-label"), "settings_music");
        Localizer.BindLabel(root.Q<Label>("sfx-label"), "settings_sfx");
        Localizer.BindLabel(root.Q<Label>("language-label"), "settings_language");
        Localizer.BindButton(root.Q<Button>("close-btn"), "settings_close");
        Localizer.BindButton(root.Q<Button>("logout-btn"), "settings_logout");

        // ---- Close ----
        var closeBtn = root.Q<Button>("close-btn");
        if (closeBtn != null) closeBtn.clicked += Close;

        // ---- Dropdown Ngôn ngữ ----
        // Chỉ hiển thị các ngôn ngữ có file JSON local tương ứng.
        // Khi thêm ngôn ngữ mới: tạo StreamingAssets/Localization/<code>.json rồi bổ sung vào đây.
        var langDropdown = root.Q<DropdownField>("language-dropdown");
        if (langDropdown != null && LocalizationManager.Instance != null)
        {
            langDropdown.choices = new System.Collections.Generic.List<string> {
                "Tiếng Việt", "English", "Français", "Italiano", "Deutsch", "Español", "日本語", "한국어"
            };

            langDropdown.index = GetLanguageIndex(LocalizationManager.Instance.CurrentLanguage);

            langDropdown.RegisterValueChangedCallback(evt =>
            {
                string code = GetLanguageCode(evt.newValue);
                LocalizationManager.Instance.SwitchLanguage(code);
                Debug.Log($"[SettingsPopup] Đã chọn ngôn ngữ: {evt.newValue} ({code})");
            });
        }

        // ---- Toggles Âm thanh ----
        var musicToggle = root.Q<Toggle>("music-toggle");
        var sfxToggle = root.Q<Toggle>("sfx-toggle");

        if (AudioManager.Instance != null)
        {
            if (musicToggle != null)
            {
                musicToggle.value = AudioManager.Instance.IsMusicEnabled;
                musicToggle.RegisterValueChangedCallback(evt =>
                    AudioManager.Instance.SetMusicEnabled(evt.newValue));
            }

            if (sfxToggle != null)
            {
                sfxToggle.value = AudioManager.Instance.IsSFXEnabled;
                sfxToggle.RegisterValueChangedCallback(evt =>
                    AudioManager.Instance.SetSFXEnabled(evt.newValue));
            }
        }

        // ---- Đăng xuất ----
        var logoutBtn = root.Q<Button>("logout-btn");
        if (logoutBtn != null)
        {
            logoutBtn.clicked += () =>
            {
                if (_logoutPopup != null && _logoutPopup.IsOpen) return;
                _logoutPopup = new LogoutConfirmPopupController(_logoutPopupTemplate, Parent);
                _logoutPopup.Show();
            };
        }

        Debug.Log("[SettingsPopup] Mở Settings Popup.");
    }

    // ==================== LANGUAGE HELPERS ====================

    public static int GetLanguageIndex(string code)
    {
        return code switch
        {
            "vi" => 0, "en" => 1, "fr" => 2, "it" => 3,
            "de" => 4, "es" => 5, "ja" => 6, "ko" => 7,
            _ => 1
        };
    }

    public static string GetLanguageCode(string name)
    {
        return name switch
        {
            "Tiếng Việt" => "vi", "English" => "en", "Français" => "fr", "Italiano" => "it",
            "Deutsch" => "de", "Español" => "es", "日本語" => "ja", "한국어" => "ko",
            _ => "en"
        };
    }
}
