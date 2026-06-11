using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Popup xác nhận đăng xuất — tách từ MainMenuUIController_UXML.ShowLogoutConfirmation().
/// Behavior giữ nguyên: cảnh báo mất dữ liệu nếu là Guest, confirm → SignOut + ClearData + về InitScene.
/// </summary>
public class LogoutConfirmPopupController : PopupBase
{
    public LogoutConfirmPopupController(VisualTreeAsset template, VisualElement parent)
        : base(template, parent, "overlay", "popup-container") { }

    protected override void OnShow(VisualElement root)
    {
        var titleLabel = root.Q<Label>("logout-title");
        var msgLabel = root.Q<Label>("message-label");
        var confirmBtn = root.Q<Button>("logout-yes-btn");
        var cancelBtn = root.Q<Button>("logout-no-btn");

        var fm = FirebaseManager.Instance;
        var pdm = PlayerDataManager.Instance;

        Localizer.BindLabel(titleLabel, "logout_confirm_title", "ĐĂNG XUẤT");
        Localizer.BindButton(confirmBtn, "logout_confirm_ok", "ĐĂNG XUẤT");
        Localizer.BindButton(cancelBtn, "logout_confirm_cancel", "HỦY");

        // Tin nhắn cảnh báo: Guest sẽ MẤT dữ liệu khi đăng xuất
        if (fm != null && fm.IsAuthenticated && msgLabel != null)
        {
            bool isGuest = fm.IsAnonymous;
            if (isGuest)
            {
                Localizer.BindLabel(msgLabel, "logout_confirm_msg_guest", "CẢNH BÁO: Đăng xuất sẽ làm MẤT dữ liệu!");
                msgLabel.style.color = Color.red;
            }
            else
            {
                Localizer.BindLabel(msgLabel, "logout_confirm_msg_user", "Bạn có chắc chắn muốn đăng xuất không?");
                msgLabel.style.color = new Color(0.2f, 0.2f, 0.2f);
            }
        }

        if (confirmBtn != null)
        {
            confirmBtn.clicked += () =>
            {
                Debug.Log("[LogoutPopup] Thực hiện Đăng xuất...");
                if (fm != null) fm.SignOut();
                if (pdm != null) pdm.ClearData();
                UnityEngine.SceneManagement.SceneManager.LoadScene("InitScene");
            };
        }

        if (cancelBtn != null) cancelBtn.clicked += Close;
    }
}
