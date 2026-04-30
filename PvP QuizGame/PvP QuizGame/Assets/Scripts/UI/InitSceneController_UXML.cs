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

        UpdateProgressUI(0f);
        SetStatus("init_loading", "Đang khởi tạo...");

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
            Debug.LogWarning("[Init] Chưa gán authPopupTemplate — dùng tên mặc định.");
            PlayerPrefs.SetString("DisplayName", "Player_" + UnityEngine.Random.Range(1000, 9999));
            PlayerPrefs.Save();
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
        var errorLabel = _authPopup.Q<Label>("auth-error");

        void ShowContainer(VisualElement container)
        {
            mainContainer.style.display = DisplayStyle.None;
            loginContainer.style.display = DisplayStyle.None;
            regContainer.style.display = DisplayStyle.None;
            guestContainer.style.display = DisplayStyle.None;
            container.style.display = DisplayStyle.Flex;
            if (errorLabel != null) errorLabel.text = "";
        }

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
            else errorLabel.text = "Đăng nhập thất bại. Kiểm tra lại thông tin.";
        };

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
                PlayerPrefs.SetString("DisplayName", name); PlayerPrefs.Save();
                _authPopupConfirmed = true;
            }
            else errorLabel.text = "Đăng ký thất bại. Email có thể đã tồn tại.";
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
                PlayerPrefs.SetString("DisplayName", name); PlayerPrefs.Save();
                _authPopupConfirmed = true;
            }
            else errorLabel.text = "Không thể kết nối.";
        };

        _authPopupConfirmed = false;
        while (!_authPopupConfirmed) yield return null;

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
}
