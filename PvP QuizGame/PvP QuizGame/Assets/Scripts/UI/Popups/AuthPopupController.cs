using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Popup Đăng nhập/Đăng ký inline trên HomeScene — tách từ
/// MainMenuUIController_UXML.ShowGuestLoginPopup() + LocalizeInlineAuthPopup() (~250 dòng).
///
/// Behavior giữ nguyên:
/// - Guest bấm vào Leaderboard → popup yêu cầu đăng nhập (ẩn nút "Chơi khách", thêm nút HỦY).
/// - Login / Register / Quên mật khẩu qua FirebaseManager.
/// - Lắng nghe FirebaseManager.OnAuthError để hiện lỗi (tự gỡ listener khi đóng).
/// - Auth thành công → đóng popup + gọi callback (MainMenu refresh UI + mở leaderboard).
/// </summary>
public class AuthPopupController : PopupBase
{
    private readonly Action _onAuthSuccess;
    private Action<string> _authErrorHandler;

    /// <param name="onAuthSuccess">Gọi sau khi đăng nhập/đăng ký thành công và popup đã đóng.</param>
    public AuthPopupController(VisualTreeAsset template, VisualElement parent, Action onAuthSuccess)
        : base(template, parent, "overlay", "popup-container")
    {
        _onAuthSuccess = onAuthSuccess;
    }

    protected override void OnShow(VisualElement root)
    {
        // ---- Containers ----
        var mainContainer = root.Q<VisualElement>("main-choice-container");
        var loginContainer = root.Q<VisualElement>("login-container");
        var regContainer = root.Q<VisualElement>("register-container");
        var guestContainer = root.Q<VisualElement>("guest-container");
        var forgotContainer = root.Q<VisualElement>("forgot-container");
        var errorLabel = root.Q<Label>("auth-error");

        // ẨN nút "Chơi khách" vì user đã là guest rồi
        var gotoGuestBtn = root.Q<Button>("goto-guest-btn");
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

        // ---- Lắng nghe lỗi auth (gỡ trong OnClose) ----
        _authErrorHandler = msg =>
        {
            if (errorLabel != null) errorLabel.text = msg;
        };
        FirebaseManager.OnAuthError += _authErrorHandler;

        // Auth thành công → đóng popup + callback
        void HandleAuthSuccess()
        {
            Close(); // PopupBase: detach localizer + OnClose (gỡ OnAuthError) + hide anim
            _onAuthSuccess?.Invoke();
        }

        string GetTextSafe(string key, string fallback)
        {
            var l = LocalizationManager.Instance;
            return l != null ? l.GetText(key, fallback) : fallback;
        }

        // ---- MAIN CHOICE ----
        var gotoLoginBtn = root.Q<Button>("goto-login-btn");
        var gotoRegisterBtn = root.Q<Button>("goto-register-btn");
        if (gotoLoginBtn != null) gotoLoginBtn.clicked += () => ShowContainer(loginContainer);
        if (gotoRegisterBtn != null) gotoRegisterBtn.clicked += () => ShowContainer(regContainer);

        // Nút HỦY (quay lại HomeScene, vẫn là guest) — thêm động vào main-choice-container
        var cancelBtn = new Button();
        cancelBtn.AddToClassList("btn");
        cancelBtn.AddToClassList("btn-danger");
        cancelBtn.style.width = Length.Percent(100);
        cancelBtn.style.fontSize = 32;
        cancelBtn.style.height = 86;
        cancelBtn.style.borderTopLeftRadius = 22; cancelBtn.style.borderTopRightRadius = 22;
        cancelBtn.style.borderBottomLeftRadius = 22; cancelBtn.style.borderBottomRightRadius = 22;
        cancelBtn.style.marginTop = 16;
        cancelBtn.clicked += Close;
        mainContainer?.Add(cancelBtn);

        // ---- LOGIN ----
        var loginEmail = root.Q<TextField>("login-email");
        var loginPass = root.Q<TextField>("login-password");
        var loginBackBtn = root.Q<Button>("login-back-btn");
        var loginConfirmBtn = root.Q<Button>("login-confirm-btn");
        if (loginBackBtn != null) loginBackBtn.clicked += () => ShowContainer(mainContainer);
        if (loginConfirmBtn != null)
        {
            loginConfirmBtn.clicked += async () =>
            {
                string email = loginEmail.value.Trim();
                string pass = loginPass.value;
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
                {
                    if (errorLabel != null)
                        errorLabel.text = GetTextSafe("auth_err_empty", "Vui lòng nhập đầy đủ email và mật khẩu.");
                    return;
                }
                if (errorLabel != null)
                    errorLabel.text = GetTextSafe("auth_status_logging_in", "Đang đăng nhập...");

                bool success = await FirebaseManager.Instance.SignInWithEmail(email, pass);
                if (success) HandleAuthSuccess();
            };
        }

        // ---- FORGOT PASSWORD ----
        var forgotPassBtn = root.Q<Button>("forgot-pass-btn");
        if (forgotPassBtn != null) forgotPassBtn.clicked += () => ShowContainer(forgotContainer);

        var forgotEmailField = root.Q<TextField>("forgot-email");
        var forgotConfirmBtn = root.Q<Button>("forgot-confirm-btn");
        var forgotBackBtn = root.Q<Button>("forgot-back-btn");
        if (forgotBackBtn != null) forgotBackBtn.clicked += () => ShowContainer(loginContainer);
        if (forgotConfirmBtn != null)
        {
            forgotConfirmBtn.clicked += async () =>
            {
                string email = forgotEmailField.value.Trim();
                if (string.IsNullOrEmpty(email))
                {
                    if (errorLabel != null)
                        errorLabel.text = GetTextSafe("auth_err_email_empty", "Vui lòng nhập email.");
                    return;
                }
                if (errorLabel != null)
                    errorLabel.text = GetTextSafe("auth_status_sending", "Đang gửi yêu cầu...");

                bool success = await FirebaseManager.Instance.SendPasswordResetEmail(email);
                if (success && errorLabel != null)
                {
                    errorLabel.text = GetTextSafe("auth_status_email_sent", "Email đặt lại mật khẩu đã được gửi!");
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

        // ---- REGISTER ----
        var regName = root.Q<TextField>("reg-display-name");
        var regEmail = root.Q<TextField>("reg-email");
        var regPass = root.Q<TextField>("reg-password");
        var regBackBtn = root.Q<Button>("reg-back-btn");
        var regConfirmBtn = root.Q<Button>("reg-confirm-btn");
        if (regBackBtn != null) regBackBtn.clicked += () => ShowContainer(mainContainer);
        if (regConfirmBtn != null)
        {
            regConfirmBtn.clicked += async () =>
            {
                string name = regName.value.Trim();
                string email = regEmail.value.Trim();
                string pass = regPass.value;
                if (name.Length < 2 || string.IsNullOrEmpty(email) || pass.Length < 6)
                {
                    if (errorLabel != null)
                        errorLabel.text = GetTextSafe("auth_err_reg_invalid", "Tên > 2 ký tự, Email hợp lệ, Mật khẩu > 6 ký tự.");
                    return;
                }
                if (errorLabel != null)
                    errorLabel.text = GetTextSafe("auth_status_registering", "Đang đăng ký...");

                bool success = await FirebaseManager.Instance.SignUpWithEmail(email, pass, name);
                if (success) HandleAuthSuccess();
            };
        }

        // ---- Localization (thay LocalizeInlineAuthPopup — tự refresh khi đổi ngôn ngữ) ----
        Localizer.BindLabel(root.Q<Label>("popup-title"), "menu_rank_guest_blocked_title", "YÊU CẦU ĐĂNG NHẬP");
        Localizer.BindButton(gotoLoginBtn, "auth_btn_goto_login", "ĐĂNG NHẬP BẰNG EMAIL");
        Localizer.BindButton(gotoRegisterBtn, "auth_btn_goto_register", "TẠO TÀI KHOẢN");
        Localizer.BindButton(cancelBtn, "menu_cancel", "HỦY");

        Localizer.BindFieldLabel(loginEmail, "auth_lbl_email", "Email");
        Localizer.BindFieldLabel(loginPass, "auth_lbl_password", "Mật khẩu");
        Localizer.BindButton(loginConfirmBtn, "auth_btn_login", "ĐĂNG NHẬP");
        Localizer.BindButton(forgotPassBtn, "auth_btn_forgot_password", "Quên mật khẩu?");
        Localizer.BindButton(loginBackBtn, "menu_cancel", "QUAY LẠI");

        Localizer.BindFieldLabel(regName, "auth_lbl_display_name", "Tên hiển thị");
        Localizer.BindFieldLabel(regEmail, "auth_lbl_email", "Email");
        Localizer.BindFieldLabel(regPass, "auth_lbl_password", "Mật khẩu");
        Localizer.BindButton(regConfirmBtn, "auth_btn_register", "ĐĂNG KÝ");
        Localizer.BindButton(regBackBtn, "menu_cancel", "QUAY LẠI");

        Localizer.BindLabel(forgotContainer?.Q<Label>(), "auth_lbl_forgot_prompt", "Nhập email để đặt lại mật khẩu:");
        Localizer.BindFieldLabel(forgotEmailField, "auth_lbl_email", "Email");
        Localizer.BindButton(forgotConfirmBtn, "auth_btn_send_request", "GỬI YÊU CẦU");
        Localizer.BindButton(forgotBackBtn, "menu_cancel", "QUAY LẠI");
    }

    protected override void OnClose()
    {
        // Gỡ listener lỗi auth — tránh leak khi đóng bằng bất kỳ đường nào (HỦY, Escape, auth thành công)
        if (_authErrorHandler != null)
        {
            FirebaseManager.OnAuthError -= _authErrorHandler;
            _authErrorHandler = null;
        }
    }
}
