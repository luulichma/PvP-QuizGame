using System;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Popup Cài Đặt TRONG trận — tách từ GameplayUIController_UXML.ShowSettingsPopup().
/// Khác SettingsPopupController (Home): không có dropdown ngôn ngữ/logout,
/// thay vào đó có nút "THOÁT GAME" → mở popup xác nhận bỏ cuộc.
/// </summary>
public class GameplaySettingsPopupController : PopupBase
{
    private readonly Action _onQuitRequested;
    private bool _quitRequested;

    /// <param name="onQuitRequested">Gọi sau khi popup đóng vì user bấm "THOÁT GAME" (mở ExitConfirm).</param>
    public GameplaySettingsPopupController(VisualTreeAsset template, VisualElement parent, Action onQuitRequested)
        : base(template, parent, "overlay", "popup")
    {
        _onQuitRequested = onQuitRequested;
    }

    protected override void OnShow(VisualElement root)
    {
        // ---- Localization ----
        Localizer.BindLabel(root.Q<Label>("settings-title"), "settings_title", "CÀI ĐẶT");
        Localizer.BindLabel(root.Q<Label>("music-label"), "settings_music", "Âm nhạc");
        Localizer.BindLabel(root.Q<Label>("sfx-label"), "settings_sfx", "Hiệu ứng");
        Localizer.BindButton(root.Q<Button>("quit-game-btn"), "game_quit_game", "THOÁT GAME");
        Localizer.BindButton(root.Q<Button>("cancel-btn"), "menu_cancel", "HỦY");

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

        // ---- Thoát game: đóng settings xong mới mở ExitConfirm (giữ behavior cũ) ----
        var quitGameBtn = root.Q<Button>("quit-game-btn");
        if (quitGameBtn != null)
        {
            quitGameBtn.clicked += () =>
            {
                _quitRequested = true;
                Close();
            };
        }

        var cancelBtn = root.Q<Button>("cancel-btn");
        if (cancelBtn != null) cancelBtn.clicked += Close;

        OnClosed += () =>
        {
            if (_quitRequested) _onQuitRequested?.Invoke();
        };
    }
}
