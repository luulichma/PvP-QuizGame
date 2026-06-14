using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Popup xác nhận bỏ cuộc — tách từ GameplayUIController_UXML.ShowExitConfirmation().
/// Confirm → đóng popup (animation) rồi GameController.ForcedSurrender() (xử thua ngay).
/// </summary>
public class ExitConfirmPopupController : PopupBase
{
    private bool _confirmed;

    public ExitConfirmPopupController(VisualTreeAsset template, VisualElement parent)
        : base(template, parent, "overlay", "popup") { }

    protected override void OnShow(VisualElement root)
    {
        Localizer.BindLabel(root.Q<Label>("confirm-title"), "game_exit_title", "BỎ CUỘC?");
        Localizer.BindLabel(root.Q<Label>("confirm-msg"), "game_exit_msg",
            "Nếu thoát bây giờ, bạn sẽ bị xử THUA ngay lập tức.");
        Localizer.BindButton(root.Q<Button>("confirm-btn"), "game_exit_confirm", "XÁC NHẬN THOÁT");
        Localizer.BindButton(root.Q<Button>("cancel-btn"), "menu_cancel", "HỦY");

        HapticFeedback.Light();

        var confirmBtn = root.Q<Button>("confirm-btn");
        if (confirmBtn != null)
        {
            confirmBtn.clicked += () =>
            {
                HapticFeedback.Heavy();
                _confirmed = true;
                Close();
            };
        }

        var cancelBtn = root.Q<Button>("cancel-btn");
        if (cancelBtn != null) cancelBtn.clicked += Close;

        OnClosed += () =>
        {
            if (_confirmed && GameController.Instance != null)
                GameController.Instance.ForcedSurrender();
        };
    }
}
