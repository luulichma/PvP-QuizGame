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
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLocalizationReady;
    }

    private void Start()
    {
        StartCoroutine(InitializationRoutine());
    }

    private void OnLocalizationReady() => SetStatus("init_loading", "Đang khởi tạo hệ thống...");

    private IEnumerator InitializationRoutine()
    {
        // ============ 1. Localization ============
        if (LocalizationManager.Instance != null)
        {
            float waitStart = Time.time;
            while (!LocalizationManager.Instance.IsReady)
            {
                if (Time.time - waitStart > localizationTimeout)
                {
                    Debug.LogWarning("[Init] Localization timeout — vẫn tiếp tục.");
                    break;
                }
                UpdateProgressUI(Mathf.Clamp01((Time.time - waitStart) / minLoadDuration) * 0.3f);
                yield return null;
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

            // Nếu chưa authenticated (không có persisted session), hiện Popup để user chọn
            if (!fm.IsAuthenticated)
            {
                yield return StartCoroutine(ShowAuthPopupRoutine());
            }
            else
            {
                // Nếu đã có session, chỉ cần load profile
                SetStatus("init_signing_in", "Đang đồng bộ dữ liệu...");
                Task<bool> loadTask = fm.SyncProfile(); 
                while (!loadTask.IsCompleted) yield return null;
            }
        }

        UpdateProgressUI(0.9f);

        // ============ 3. Đảm bảo tối thiểu minLoadDuration ============
        yield return new WaitForSeconds(0.3f);

        // ============ 4. Load HomeScene ============
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
                errorLabel.text = "Vui lòng nhập đầy đủ email và mật khẩu."; return;
            }
            errorLabel.text = "Đang đăng nhập...";
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
                    errorLabel.text = "Vui lòng nhập email."; return;
                }
                errorLabel.text = "Đang gửi yêu cầu...";
                bool success = await FirebaseManager.Instance.SendPasswordResetEmail(email);
                if (success) {
                    errorLabel.text = "Email đặt lại mật khẩu đã được gửi!";
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
                errorLabel.text = "Tên > 2 ký tự, Email hợp lệ, Mật khẩu > 6 ký tự."; return;
            }
            errorLabel.text = "Đang đăng ký...";
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
            if (name.Length < 2) { errorLabel.text = "Tên quá ngắn."; return; }
            errorLabel.text = "Đang vào...";
            bool success = await FirebaseManager.Instance.SignInAnonymousAndLoadProfile(name);
            if (success) {
                _authPopupConfirmed = true;
            }
        };

        _authPopupConfirmed = false;
        while (!_authPopupConfirmed) yield return null;

        FirebaseManager.OnAuthError -= authErrorHandler;
        _authPopup.RemoveFromHierarchy();
        _authPopup = null;
    }

    // ==================== UI HELPERS ====================
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
        _tipLabel.text = "💡 " + tip;
    }
}
