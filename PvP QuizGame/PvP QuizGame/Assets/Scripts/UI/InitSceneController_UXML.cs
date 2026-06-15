using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// Quản lý UI và luồng khởi tạo của InitScene sử dụng UI Toolkit.
///
/// FLOW:
///   1. Tải Localization (Sheet → Cache → JSON local).
///   2. Init Firebase + SignIn Anonymous + load Cloud Profile.
///   3. Lần đầu chơi (chưa có displayName) → show AuthPopup nhập tên.
///   4. Khi mọi thứ sẵn sàng → load HomeScene.
/// </summary>
public class InitSceneController_UXML : MonoBehaviour
{
    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Auth Popup Template")]
    [Tooltip("Kéo file AuthPopup.uxml vào đây")]
    [SerializeField] private VisualTreeAsset authPopupTemplate;

    [Header("Cài đặt loading")]
    [SerializeField] private float minLoadDuration = 1.2f;
    [SerializeField] private float localizationTimeout = 8f;
    [SerializeField] private float firebaseTimeout = 12f;

    private VisualElement _loadingFill;
    private Label _statusLabel;
    private Label _progressLabel;
    // G-08: Loading tips
    private Label _tipLabel;
    private static readonly string[] _loadingTips = new string[]
    {
        "CPU là viết tắt của Central Processing Unit",
        "Unity hỗ trợ cả 2D và 3D game development",
        "Firebase giúp đồng bộ dữ liệu thời gian thực",
        "Bạn có biết: Pi ≈ 3.14159...",
        "Sông Nile là sông dài nhất thế giới",
        "Sao Thủy là hành tinh gần Mặt Trời nhất",
        "C# là ngôn ngữ chính trong Unity",
        "HTML là viết tắt của HyperText Markup Language",
        "Tổng các góc trong tam giác là 180 độ",
        "Chiến thắng Điện Biên Phủ năm 1954",
    };

    private VisualElement _authPopup;
    private bool _authPopupConfirmed = false;

    // [FIX-LOAD][B3] Popup báo lỗi tải dữ liệu + nút Thử lại
    private VisualElement _loadErrorOverlay;
    private bool _localizationFailedFlag = false;
    // [FIX-LOAD][B5] Quiz data verify
    private bool _quizDataInvalidFlag = false;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        _loadingFill = root.Q<VisualElement>("loading-fill");
        _statusLabel = root.Q<Label>("status-label");
        _progressLabel = root.Q<Label>("progress-label");
        // G-08
        _tipLabel = root.Q<Label>("tip-label");

        UpdateProgressUI(0f);
        SetStatus("init_loading", "Đang khởi tạo...");
        ShowRandomTip();

        LocalizationManager.OnLanguageChanged += OnLocalizationReady;
        // [FIX-LOAD][B3] Bắt event tải fail để hiện popup retry
        LocalizationManager.OnLocalizationFailed += OnLocalizationFailed;
        // [FIX-LOAD][B5] Bắt event quiz data thiếu
        QuizManager.OnQuizDataInvalid += OnQuizDataInvalid;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLocalizationReady;
        LocalizationManager.OnLocalizationFailed -= OnLocalizationFailed;
        QuizManager.OnQuizDataInvalid -= OnQuizDataInvalid;
    }

    private void OnLocalizationFailed()
    {
        Debug.LogWarning("[Init] OnLocalizationFailed → sẽ hiện popup Thử lại.");
        _localizationFailedFlag = true;
    }

    private void OnQuizDataInvalid(int actualCount, int minRequired)
    {
        Debug.LogError($"[Init] OnQuizDataInvalid count={actualCount}/{minRequired} → sẽ hiện popup Thử lại.");
        _quizDataInvalidFlag = true;
    }

    private void Start()
    {
        // Spawn ambient particles — phải gọi ở Start vì UIParticleEffect.AutoInit
        // chạy qua [RuntimeInitializeOnLoadMethod(AfterSceneLoad)] nên Instance
        // chưa có tại thời điểm OnEnable.
        StartCoroutine(SpawnBubblesWhenReady());
        StartCoroutine(InitializationRoutine());
    }

    private IEnumerator SpawnBubblesWhenReady()
    {
        // Đợi tối đa vài frame cho UIParticleEffect singleton sẵn sàng
        int maxWait = 10;
        while (UIParticleEffect.Instance == null && maxWait-- > 0)
            yield return null;

        if (uiDocument == null) yield break;
        var particleLayer = uiDocument.rootVisualElement.Q<VisualElement>("init-particle-layer");
        if (particleLayer != null && UIParticleEffect.Instance != null)
        {
            UIParticleEffect.Instance.SpawnAmbientParticles(particleLayer, 60); // Dense bubbles
            Debug.Log("[Init] Ambient bubbles spawned successfully.");
        }
        else
        {
            Debug.LogWarning($"[Init] Không thể spawn bubbles — particleLayer: {particleLayer != null}, UIParticleEffect: {UIParticleEffect.Instance != null}");
        }
    }

    private void OnLocalizationReady() => SetStatus("init_loading", "Đang khởi tạo hệ thống...");

    private IEnumerator InitializationRoutine()
    {
        // ============ 1. Localization ============
        // [FIX-LOAD][B3] Có thể loop khi user bấm Thử lại
        if (LocalizationManager.Instance != null)
        {
            bool localizationDone = false;
            while (!localizationDone)
            {
                _localizationFailedFlag = false;
                // [FIX-LOAD][B1] Timeout tăng lên để bao trùm 3 retry × ~15s
                float effectiveTimeout = Mathf.Max(localizationTimeout, 45f);
                float waitStart = Time.time;
                while (!LocalizationManager.Instance.IsReady && !_localizationFailedFlag)
                {
                    if (Time.time - waitStart > effectiveTimeout)
                    {
                        Debug.LogWarning("[Init] Localization timeout — coi như failed.");
                        _localizationFailedFlag = true;
                        break;
                    }
                    UpdateProgressUI(Mathf.Clamp01((Time.time - waitStart) / minLoadDuration) * 0.3f);
                    yield return null;
                }

                if (LocalizationManager.Instance.IsReady && !_localizationFailedFlag)
                {
                    localizationDone = true;
                }
                else
                {
                    // [FIX-LOAD][B3] Hiện popup, đợi user bấm Thử lại
                    yield return StartCoroutine(ShowLoadErrorPopupRoutine(isLocalization: true));
                    LocalizationManager.Instance.RetryInit();
                }
            }
        }
        UpdateProgressUI(0.3f);

        // ============ 2. Firebase Init + Auth ============
        var fm = FirebaseManager.Instance;
        if (fm == null)
        {
            Debug.LogWarning("[Init] Không có FirebaseManager — bỏ qua Auth.");
        }
        else
        {
            SetStatus("init_connecting", "Đang kết nối máy chủ...");

            // Đợi Firebase sẵn sàng
            float waitStart = Time.time;
            while (!fm.IsConnected)
            {
                if (Time.time - waitStart > firebaseTimeout)
                {
                    Debug.LogError("[Init] Firebase timeout — vẫn tiếp tục offline.");
                    break;
                }
                UpdateProgressUI(0.3f + Mathf.Clamp01((Time.time - waitStart) / 6f) * 0.3f);
                yield return null;
            }

            UpdateProgressUI(0.6f);

            // Kiểm tra xem có cần hiện Auth Popup không.
            // Firebase SDK lưu auth session riêng (không phải PlayerPrefs).
            // Khi user đăng xuất → ClearData() xóa PlayerPrefs nhưng nếu
            // SignOut() chưa kịp clear SDK session, hoặc khi xóa PlayerPrefs
            // thủ công → Firebase vẫn "authenticated" nhưng local data trống.
            bool needsAuth = !fm.IsAuthenticated;
            
            // Kiểm tra thêm: nếu Firebase nói đã auth nhưng local data trống/mặc định
            // → đây là session cũ không hợp lệ, cần re-auth
            if (!needsAuth && !HasValidLocalSession())
            {
                Debug.LogWarning("[Init] Firebase có session nhưng local data trống — cần đăng nhập lại.");
                fm.SignOut(); // Xóa session Firebase cũ không hợp lệ
                needsAuth = true;
            }

            if (needsAuth)
            {
                yield return StartCoroutine(ShowAuthPopupRoutine());
            }
            else
            {
                // Nếu đã có session hợp lệ, chỉ cần load profile
                SetStatus("init_signing_in", "Đang đồng bộ dữ liệu...");
                Task<bool> loadTask = fm.SyncProfile(); 
                while (!loadTask.IsCompleted) yield return null;
            }
        }

        UpdateProgressUI(0.9f);

        // ============ 3. [FIX-LOAD][B5] Verify quiz data trước khi vào Home ============
        // Tránh trường hợp Localization "ready" nhưng số key q_* lại quá ít → user vào trận mới phát hiện.
        if (QuizManager.Instance != null)
        {
            bool quizOk = false;
            while (!quizOk)
            {
                _quizDataInvalidFlag = false;
                quizOk = QuizManager.Instance.VerifyQuizDataAvailable();
                if (!quizOk)
                {
                    Debug.LogWarning("[Init] Quiz data thiếu — hiện popup Thử lại.");
                    yield return StartCoroutine(ShowLoadErrorPopupRoutine(isLocalization: false));
                    // User bấm Thử lại → reload Localization (nguồn của quiz data)
                    LocalizationManager.Instance?.RetryInit();
                    float waitStart = Time.time;
                    while (LocalizationManager.Instance != null && !LocalizationManager.Instance.IsReady)
                    {
                        if (Time.time - waitStart > 45f) break;
                        yield return null;
                    }
                }
            }
        }

        // ============ 4. Đảm bảo tối thiểu minLoadDuration ============
        yield return new WaitForSeconds(0.3f);

        // ============ 5. Load HomeScene ============
        SetStatus("init_loading_home", "Đang tải sảnh chờ...");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSceneAsync("HomeScene",
                onProgress: (p) => UpdateProgressUI(0.9f + p * 0.1f));
        }
        else
        {
            Debug.LogError("[Init] Không tìm thấy GameManager!");
        }
    }

    // ==================== AUTH POPUP ====================
    private IEnumerator ShowAuthPopupRoutine()
    {
        if (authPopupTemplate == null)
        {
            yield break;
        }

        _authPopup = authPopupTemplate.Instantiate();
        _authPopup.style.position = Position.Absolute;
        _authPopup.style.top = 0; _authPopup.style.bottom = 0;
        _authPopup.style.left = 0; _authPopup.style.right = 0;

        uiDocument.rootVisualElement.Add(_authPopup);

        LocalizeAuthPopup();

        // Containers
        var mainContainer = _authPopup.Q<VisualElement>("main-choice-container");
        var loginContainer = _authPopup.Q<VisualElement>("login-container");
        var regContainer = _authPopup.Q<VisualElement>("register-container");
        var guestContainer = _authPopup.Q<VisualElement>("guest-container");
        var forgotContainer = _authPopup.Q<VisualElement>("forgot-container");
        var errorLabel = _authPopup.Q<Label>("auth-error");

        void ShowContainer(VisualElement container)
        {
            mainContainer.style.display = DisplayStyle.None;
            loginContainer.style.display = DisplayStyle.None;
            regContainer.style.display = DisplayStyle.None;
            guestContainer.style.display = DisplayStyle.None;
            if (forgotContainer != null) forgotContainer.style.display = DisplayStyle.None;
            
            container.style.display = DisplayStyle.Flex;
            if (errorLabel != null) errorLabel.text = "";
        }

        // Đăng ký nhận lỗi từ Firebase
        System.Action<string> authErrorHandler = (msg) => {
            if (errorLabel != null) errorLabel.text = msg;
        };
        FirebaseManager.OnAuthError += authErrorHandler;

        // --- 1. MAIN CHOICE ---
        _authPopup.Q<Button>("goto-login-btn").clicked += () => ShowContainer(loginContainer);
        _authPopup.Q<Button>("goto-register-btn").clicked += () => ShowContainer(regContainer);
        _authPopup.Q<Button>("goto-guest-btn").clicked += () => ShowContainer(guestContainer);

        // --- 2. LOGIN ---
        var loginEmail = _authPopup.Q<TextField>("login-email");
        var loginPass = _authPopup.Q<TextField>("login-password");
        _authPopup.Q<Button>("login-back-btn").clicked += () => ShowContainer(mainContainer);
        _authPopup.Q<Button>("login-confirm-btn").clicked += async () => {
            string email = loginEmail.value.Trim();
            string pass = loginPass.value;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass)) {
                errorLabel.text = LocalizationManager.Instance?.GetText("auth_err_empty", "Vui lòng nhập đầy đủ email và mật khẩu.") ?? "Vui lòng nhập đầy đủ email và mật khẩu."; return;
            }
            errorLabel.text = LocalizationManager.Instance?.GetText("auth_status_logging_in", "Đang đăng nhập...") ?? "Đang đăng nhập...";
            bool success = await FirebaseManager.Instance.SignInWithEmail(email, pass);
            if (success) _authPopupConfirmed = true;
            // Không cần gán text lỗi ở đây vì OnAuthError đã làm
        };
        var forgotPassBtn = _authPopup.Q<Button>("forgot-pass-btn");
        if (forgotPassBtn != null) forgotPassBtn.clicked += () => ShowContainer(forgotContainer);

        // --- 5. FORGOT PASSWORD ---
        var forgotEmailField = _authPopup.Q<TextField>("forgot-email");
        var forgotConfirmBtn = _authPopup.Q<Button>("forgot-confirm-btn");
        var forgotBackBtn = _authPopup.Q<Button>("forgot-back-btn");

        if (forgotBackBtn != null) forgotBackBtn.clicked += () => ShowContainer(loginContainer);
        if (forgotConfirmBtn != null)
        {
            forgotConfirmBtn.clicked += async () => {
                string email = forgotEmailField.value.Trim();
                if (string.IsNullOrEmpty(email)) {
                    errorLabel.text = LocalizationManager.Instance?.GetText("auth_err_email_empty", "Vui lòng nhập email.") ?? "Vui lòng nhập email."; return;
                }
                errorLabel.text = LocalizationManager.Instance?.GetText("auth_status_sending", "Đang gửi yêu cầu...") ?? "Đang gửi yêu cầu...";
                bool success = await FirebaseManager.Instance.SendPasswordResetEmail(email);
                if (success) {
                    errorLabel.text = LocalizationManager.Instance?.GetText("auth_status_email_sent", "Email đặt lại mật khẩu đã được gửi!") ?? "Email đặt lại mật khẩu đã được gửi!";
                    errorLabel.style.color = new Color(0.2f, 0.8f, 0.2f); // Màu xanh
                    // Quay lại login sau 2.5s
                    await Task.Delay(2500);
                    if (forgotContainer.style.display == DisplayStyle.Flex) {
                        ShowContainer(loginContainer);
                        errorLabel.style.color = new Color(1f, 0.32f, 0.32f); // Trả lại màu đỏ
                    }
                }
            };
        }

        // --- 3. REGISTER ---
        var regName = _authPopup.Q<TextField>("reg-display-name");
        var regEmail = _authPopup.Q<TextField>("reg-email");
        var regPass = _authPopup.Q<TextField>("reg-password");
        _authPopup.Q<Button>("reg-back-btn").clicked += () => ShowContainer(mainContainer);
        _authPopup.Q<Button>("reg-confirm-btn").clicked += async () => {
            string name = regName.value.Trim();
            string email = regEmail.value.Trim();
            string pass = regPass.value;
            if (name.Length < 2 || string.IsNullOrEmpty(email) || pass.Length < 6) {
                errorLabel.text = LocalizationManager.Instance?.GetText("auth_err_reg_invalid", "Tên > 2 ký tự, Email hợp lệ, Mật khẩu > 6 ký tự.") ?? "Tên > 2 ký tự, Email hợp lệ, Mật khẩu > 6 ký tự."; return;
            }
            errorLabel.text = LocalizationManager.Instance?.GetText("auth_status_registering", "Đang đăng ký...") ?? "Đang đăng ký...";
            bool success = await FirebaseManager.Instance.SignUpWithEmail(email, pass, name);
            if (success) {
                _authPopupConfirmed = true;
            }
            // Không cần gán text lỗi ở đây vì OnAuthError đã làm
        };

        // --- 4. GUEST ---
        var guestName = _authPopup.Q<TextField>("guest-name-field");
        guestName.value = "Player_" + UnityEngine.Random.Range(1000, 9999);
        _authPopup.Q<Button>("guest-back-btn").clicked += () => ShowContainer(mainContainer);
        _authPopup.Q<Button>("guest-confirm-btn").clicked += async () => {
            string name = guestName.value.Trim();
            if (name.Length < 2) { errorLabel.text = LocalizationManager.Instance?.GetText("auth_err_name_short", "Tên quá ngắn.") ?? "Tên quá ngắn."; return; }
            errorLabel.text = LocalizationManager.Instance?.GetText("auth_status_entering", "Đang vào...") ?? "Đang vào...";
            bool success = await FirebaseManager.Instance.SignInAnonymousAndLoadProfile(name);
            if (success) {
                _authPopupConfirmed = true;
            }
        };

        _authPopupConfirmed = false;
        while (!_authPopupConfirmed) yield return null;

        FirebaseManager.OnAuthError -= authErrorHandler;
        uiDocument.rootVisualElement.Remove(_authPopup);
        _authPopup = null;
    }

    private void LocalizeAuthPopup()
    {
        if (_authPopup == null || LocalizationManager.Instance == null || !LocalizationManager.Instance.IsReady) return;
        var L = LocalizationManager.Instance;

        // Tiêu đề popup (dùng chung cho cả AuthPopup)
        var title = _authPopup.Q<Label>("popup-title");
        if (title != null) title.text = L.GetText("auth_title", "CHỌN CÁCH ĐĂNG NHẬP");

        // Main Choice
        var gotoLoginBtn = _authPopup.Q<Button>("goto-login-btn");
        var gotoRegBtn = _authPopup.Q<Button>("goto-register-btn");
        var gotoGuestBtn = _authPopup.Q<Button>("goto-guest-btn");
        if (gotoLoginBtn != null) gotoLoginBtn.text = L.GetText("auth_btn_goto_login", "ĐĂNG NHẬP BẰNG EMAIL");
        if (gotoRegBtn != null) gotoRegBtn.text = L.GetText("auth_btn_goto_register", "TẠO TÀI KHOẢN");
        if (gotoGuestBtn != null) gotoGuestBtn.text = L.GetText("auth_btn_goto_guest", "CHƠI VỚI TƯ CÁCH KHÁCH");

        // Login
        var loginEmail = _authPopup.Q<TextField>("login-email");
        var loginPass = _authPopup.Q<TextField>("login-password");
        var loginConfirm = _authPopup.Q<Button>("login-confirm-btn");
        var forgotPassBtn = _authPopup.Q<Button>("forgot-pass-btn");
        var loginBack = _authPopup.Q<Button>("login-back-btn");
        if (loginEmail != null) loginEmail.label = L.GetText("auth_lbl_email", "Email");
        if (loginPass != null) loginPass.label = L.GetText("auth_lbl_password", "Mật khẩu");
        if (loginConfirm != null) loginConfirm.text = L.GetText("auth_btn_login", "ĐĂNG NHẬP");
        if (forgotPassBtn != null) forgotPassBtn.text = L.GetText("auth_btn_forgot_password", "Quên mật khẩu?");
        if (loginBack != null) loginBack.text = L.GetText("menu_cancel", "QUAY LẠI");

        // Register
        var regName = _authPopup.Q<TextField>("reg-display-name");
        var regEmail = _authPopup.Q<TextField>("reg-email");
        var regPass = _authPopup.Q<TextField>("reg-password");
        var regConfirm = _authPopup.Q<Button>("reg-confirm-btn");
        var regBack = _authPopup.Q<Button>("reg-back-btn");
        if (regName != null) regName.label = L.GetText("auth_lbl_display_name", "Tên hiển thị");
        if (regEmail != null) regEmail.label = L.GetText("auth_lbl_email", "Email");
        if (regPass != null) regPass.label = L.GetText("auth_lbl_password", "Mật khẩu");
        if (regConfirm != null) regConfirm.text = L.GetText("auth_btn_register", "ĐĂNG KÝ");
        if (regBack != null) regBack.text = L.GetText("menu_cancel", "QUAY LẠI");

        // Guest
        var guestName = _authPopup.Q<TextField>("guest-name-field");
        var guestConfirm = _authPopup.Q<Button>("guest-confirm-btn");
        var guestBack = _authPopup.Q<Button>("guest-back-btn");
        // Label trên textField của Guest: "Nhap ten cua ban:"
        var guestPrompt = _authPopup.Q<VisualElement>("guest-container")?.Q<Label>(); 
        if (guestPrompt != null) guestPrompt.text = L.GetText("auth_lbl_guest_prompt", "Nhập tên của bạn:");
        // guestName không dùng label property
        if (guestConfirm != null) guestConfirm.text = L.GetText("auth_btn_guest_play", "CHƠI THỬ");
        if (guestBack != null) guestBack.text = L.GetText("menu_cancel", "QUAY LẠI");

        // Forgot Password
        var forgotEmail = _authPopup.Q<TextField>("forgot-email");
        var forgotConfirm = _authPopup.Q<Button>("forgot-confirm-btn");
        var forgotBack = _authPopup.Q<Button>("forgot-back-btn");
        var forgotPrompt = _authPopup.Q<VisualElement>("forgot-container")?.Q<Label>();
        if (forgotPrompt != null) forgotPrompt.text = L.GetText("auth_lbl_forgot_prompt", "Nhập email để đặt lại mật khẩu:");
        if (forgotEmail != null) forgotEmail.label = L.GetText("auth_lbl_email", "Email");
        if (forgotConfirm != null) forgotConfirm.text = L.GetText("auth_btn_send_request", "GỬI YÊU CẦU");
        if (forgotBack != null) forgotBack.text = L.GetText("menu_cancel", "QUAY LẠI");
    }

    // ==================== LOAD ERROR POPUP (FIX-LOAD B3) ====================

    /// <summary>
    /// [FIX-LOAD][B3] Hiện popup báo lỗi tải dữ liệu với nút "Thử lại".
    /// Tạo bằng code (không cần UXML asset mới). Coroutine block đến khi user bấm Thử lại.
    /// </summary>
    private IEnumerator ShowLoadErrorPopupRoutine(bool isLocalization)
    {
        if (_loadErrorOverlay != null)
        {
            // Đã có popup → wait tiếp
            bool waitConfirm = true;
            while (waitConfirm) yield return null;
            yield break;
        }

        var root = uiDocument?.rootVisualElement;
        if (root == null) yield break;

        _loadErrorOverlay = new VisualElement { name = "load-error-overlay" };
        _loadErrorOverlay.style.position = Position.Absolute;
        _loadErrorOverlay.style.top = 0; _loadErrorOverlay.style.bottom = 0;
        _loadErrorOverlay.style.left = 0; _loadErrorOverlay.style.right = 0;
        _loadErrorOverlay.style.backgroundColor = new Color(0, 0, 0, 0.75f);
        _loadErrorOverlay.style.justifyContent = Justify.Center;
        _loadErrorOverlay.style.alignItems = Align.Center;

        var box = new VisualElement();
        box.style.width = 480;
        box.style.paddingTop = 32; box.style.paddingBottom = 32;
        box.style.paddingLeft = 28; box.style.paddingRight = 28;
        box.style.backgroundColor = new Color(0.12f, 0.14f, 0.20f, 0.98f);
        box.style.borderTopLeftRadius = 18; box.style.borderTopRightRadius = 18;
        box.style.borderBottomLeftRadius = 18; box.style.borderBottomRightRadius = 18;
        box.style.alignItems = Align.Center;

        var title = new Label(isLocalization ? "Không tải được dữ liệu" : "Dữ liệu câu hỏi thiếu");
        title.style.fontSize = 22;
        title.style.color = new Color(1f, 0.85f, 0.3f);
        title.style.marginBottom = 14;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        box.Add(title);

        var msg = new Label(isLocalization
            ? "Vui lòng kiểm tra kết nối mạng rồi bấm Thử lại."
            : "Ngân hàng câu hỏi chưa đủ. Vui lòng kiểm tra kết nối mạng rồi Thử lại.");
        msg.style.fontSize = 14;
        msg.style.color = new Color(0.9f, 0.9f, 0.9f);
        msg.style.whiteSpace = WhiteSpace.Normal;
        msg.style.marginBottom = 22;
        msg.style.unityTextAlign = TextAnchor.MiddleCenter;
        box.Add(msg);

        var retryBtn = new Button { text = "THỬ LẠI" };
        retryBtn.style.width = 200; retryBtn.style.height = 48;
        retryBtn.style.fontSize = 16;
        retryBtn.style.backgroundColor = new Color(0.15f, 0.55f, 0.95f);
        retryBtn.style.color = Color.white;
        retryBtn.style.borderTopLeftRadius = 8; retryBtn.style.borderTopRightRadius = 8;
        retryBtn.style.borderBottomLeftRadius = 8; retryBtn.style.borderBottomRightRadius = 8;
        retryBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        box.Add(retryBtn);

        _loadErrorOverlay.Add(box);
        root.Add(_loadErrorOverlay);

        // Localize bằng key (nếu có dữ liệu localization fallback)
        var L = LocalizationManager.Instance;
        if (L != null)
        {
            title.text = L.GetText(isLocalization ? "load_err_title" : "load_err_quiz_title", title.text);
            msg.text   = L.GetText(isLocalization ? "load_err_msg"   : "load_err_quiz_msg",   msg.text);
            retryBtn.text = L.GetText("load_err_retry", retryBtn.text);
        }

        bool clicked = false;
        retryBtn.clicked += () => clicked = true;

        while (!clicked) yield return null;

        root.Remove(_loadErrorOverlay);
        _loadErrorOverlay = null;
    }

    // ==================== HELPER ====================
    private void SetStatus(string key, string fallback)
    {
        if (_statusLabel == null) return;
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
            _statusLabel.text = LocalizationManager.Instance.GetText(key, fallback);
        else
            _statusLabel.text = fallback;
    }

    private void UpdateProgressUI(float progress)
    {
        if (_loadingFill != null)
            _loadingFill.style.width = Length.Percent(progress * 100f);
        if (_progressLabel != null)
            _progressLabel.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }

    // G-08: Hiển thị tip ngẫu nhiên khi loading
    private void ShowRandomTip()
    {
        if (_tipLabel == null) return;
        string tip = _loadingTips[UnityEngine.Random.Range(0, _loadingTips.Length)];
        // [Icon Fix] Bỏ prefix emoji 💡 — _tipLabel là plain Label, không render emoji
        // trên build APK. Nếu muốn icon, dùng UIIconHelper.MakeIconLabel("icon-lightbulb", tip).
        _tipLabel.text = tip;
    }

    /// <summary>
    /// Kiểm tra xem có dữ liệu local hợp lệ (PlayerPrefs) hay không.
    /// Firebase SDK lưu auth token riêng biệt với PlayerPrefs, nên có thể xảy ra
    /// trường hợp Firebase nói "đã authenticated" nhưng local data đã bị xóa.
    /// </summary>
    private bool HasValidLocalSession()
    {
        // Nếu PlayerPrefs có "PlayerName" → đã chơi trước đó, session hợp lệ
        return PlayerPrefs.HasKey("PlayerName");
    }
}
