using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Popup Hồ Sơ Cá Nhân — tách từ MainMenuUIController_UXML.OnOpenProfileClicked().
/// Behavior giữ nguyên: đổi tên hiển thị (validate >= 3 ký tự), preview avatar,
/// lưu qua FirebaseManager.UpdateDisplayName() để đồng bộ local + cloud.
/// </summary>
public class ProfilePopupController : PopupBase
{
    private readonly Action _onProfileUpdated;

    /// <param name="onProfileUpdated">Callback sau khi lưu thành công (vd: refresh header sảnh chính).</param>
    public ProfilePopupController(VisualTreeAsset template, VisualElement parent, Action onProfileUpdated)
        : base(template, parent, "profile-overlay", "profile-container")
    {
        _onProfileUpdated = onProfileUpdated;
    }

    protected override void OnShow(VisualElement root)
    {
        var nameInput = root.Q<TextField>("profile-name-field");
        var saveBtn = root.Q<Button>("profile-save-btn");
        var closeBtn = root.Q<Button>("profile-close-btn");
        var profileAvatar = root.Q<VisualElement>("profile-avatar");

        // ---- Localization ----
        Localizer.BindLabel(root.Q<Label>("profile-title"), "profile_title", "HỒ SƠ CÁ NHÂN");
        Localizer.BindFieldLabel(nameInput, "profile_display_name", "TÊN HIỂN THỊ");
        Localizer.BindButton(saveBtn, "profile_save", "LƯU THAY ĐỔI");
        Localizer.BindButton(closeBtn, "menu_cancel", "HỦY");

        var data = PlayerDataManager.Instance != null ? PlayerDataManager.Instance.Data : null;
        if (data != null && nameInput != null) nameInput.value = data.playerName;

        // Avatar initial-letter — nhất quán với GameplayUI
        if (profileAvatar != null && data != null)
            AvatarHelper.SetAvatar(profileAvatar, data.playerName);

        // Preview avatar khi đổi tên
        if (nameInput != null)
        {
            nameInput.RegisterValueChangedCallback(evt =>
            {
                string preview = string.IsNullOrWhiteSpace(evt.newValue) ? "?" : evt.newValue.Trim();
                if (profileAvatar != null)
                    AvatarHelper.SetAvatar(profileAvatar, preview);
            });
        }

        if (saveBtn != null)
        {
            saveBtn.clicked += () =>
            {
                string newName = nameInput != null ? nameInput.value.Trim() : "";

                // FEAT-03: Hiển lỗi trên UI thay vì chỉ log warning
                if (newName.Length < 3)
                {
                    ShowNameError(root, nameInput);
                    return;
                }
                root.Q<Label>("profile-name-error")?.RemoveFromHierarchy();

                // BUG-FIX: Dùng UpdateDisplayName() để đồng bộ FirebaseManager.LocalDisplayName,
                // PlayerData.playerName, PlayerPrefs và Firebase cùng lúc.
                if (FirebaseManager.Instance != null)
                {
                    FirebaseManager.Instance.UpdateDisplayName(newName);
                }
                else if (data != null)
                {
                    // Fallback khi không có Firebase (offline)
                    data.playerName = newName;
                    PlayerDataManager.Instance.SaveData();
                }

                _onProfileUpdated?.Invoke();
                Close();
            };
        }

        if (closeBtn != null) closeBtn.clicked += Close;
    }

    private void ShowNameError(VisualElement root, TextField nameInput)
    {
        var errLabel = root.Q<Label>("profile-name-error");
        if (errLabel == null)
        {
            errLabel = new Label();
            errLabel.name = "profile-name-error";
            errLabel.style.color = new Color(0.87f, 0.12f, 0.12f);
            errLabel.style.fontSize = 24;
            errLabel.style.marginTop = 8;
            nameInput?.parent?.Add(errLabel);
        }
        var l = LocalizationManager.Instance;
        errLabel.text = l != null
            ? l.GetText("profile_name_too_short", "Tên phải từ 3 ký tự trở lên.")
            : "Tên phải từ 3 ký tự trở lên.";
    }
}
