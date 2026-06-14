using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Panel & flow Tìm Trận — tách từ MainMenuUIController_UXML
/// (OnFindMatchClicked, OnPracticeClicked, OnCancelMatchClicked, OnMatchFoundFromFirebase,
/// OnMatchmakingError, OnMatchmakingTimeout, OfflineGoToGameplayRoutine, LoadGameplayScene).
///
/// Giữ nguyên các fix cũ:
/// - FIX-CANCEL: flag _isCancelledMatchmaking chặn OnMatchFound fire muộn sau khi cancel.
/// - FIX-CANCEL: lưu coroutine offline để dừng được khi cancel.
/// - UX-06: timeout → toast + về Home.
///
/// Cần MonoBehaviour runner để chạy coroutine (truyền MainMenuUIController vào).
/// Attach()/Detach() phải được gọi từ OnEnable/OnDisable của runner.
/// </summary>
public class MatchmakingPanelController
{
    private readonly MonoBehaviour _runner;
    private readonly HomeNavController _nav;
    private readonly VisualElement _root;

    private readonly Button _findMatchBtn;
    private readonly Button _practiceBtn;
    private readonly Button _cancelMatchBtn;
    private readonly Label _searchingLabel;

    private readonly UILocalizer _localizer = new UILocalizer();

    // FIX-CANCEL: Lưu coroutine offline để có thể dừng khi cancel
    private Coroutine _offlineRoutine;
    // FIX-CANCEL: Flag để block OnMatchFound sau khi đã cancel online matchmaking
    private bool _isCancelledMatchmaking = false;

    public MatchmakingPanelController(VisualElement root, MonoBehaviour runner, HomeNavController nav)
    {
        _root = root;
        _runner = runner;
        _nav = nav;

        _findMatchBtn = root.Q<Button>("find-match-btn");
        _practiceBtn = root.Q<Button>("practice-btn");
        _cancelMatchBtn = root.Q<Button>("cancel-match-btn");
        _searchingLabel = root.Q<Label>("searching-label");

        if (_findMatchBtn != null) _findMatchBtn.clicked += OnFindMatchClicked;
        if (_practiceBtn != null) _practiceBtn.clicked += OnPracticeClicked;
        if (_cancelMatchBtn != null) _cancelMatchBtn.clicked += OnCancelMatchClicked;

        _localizer.BindButton(_findMatchBtn, "menu_find_match");
        _localizer.BindButton(_practiceBtn, "menu_practice");
        _localizer.BindButton(_cancelMatchBtn, "menu_cancel");
        _localizer.BindLabel(_searchingLabel, "menu_searching");
    }

    public void Attach()
    {
        FirebaseManager.OnMatchFound         += OnMatchFoundFromFirebase;
        FirebaseManager.OnMatchmakingError   += OnMatchmakingError;
        FirebaseManager.OnMatchmakingTimeout += OnMatchmakingTimeout; // UX-06
        _localizer.Attach();
        _localizer.Refresh();
    }

    public void Detach()
    {
        FirebaseManager.OnMatchFound         -= OnMatchFoundFromFirebase;
        FirebaseManager.OnMatchmakingError   -= OnMatchmakingError;
        FirebaseManager.OnMatchmakingTimeout -= OnMatchmakingTimeout;
        _localizer.Detach();

        if (_findMatchBtn != null) _findMatchBtn.clicked -= OnFindMatchClicked;
        if (_practiceBtn != null) _practiceBtn.clicked -= OnPracticeClicked;
        if (_cancelMatchBtn != null) _cancelMatchBtn.clicked -= OnCancelMatchClicked;
    }

    /// <summary>Hủy tìm trận (dùng cho cả nút HỦY và nút Back Android).</summary>
    public void CancelMatch() => OnCancelMatchClicked();

    // ==================== HANDLERS ====================

    private void OnFindMatchClicked()
    {
        var fm = FirebaseManager.Instance;
        if (fm == null) return;

        // FEAT-02: Guest → toast cảnh báo thay vì log error
        if (!fm.IsAuthenticated)
        {
            ToastService.ShowInfo(_root, GetTextSafe("menu_login_required", "Bạn cần đăng nhập để tìm trận online."), 3f);
            return;
        }

        // BẮT BUỘC tắt offline mode khi bấm tìm trận thật
        fm.isOfflineMode = false;
        _isCancelledMatchmaking = false;

        // Kiểm tra kết nối Firebase
        if (!fm.IsConnected)
        {
            if (_searchingLabel != null)
                _searchingLabel.text = GetTextSafe("menu_error_connection", "Lỗi kết nối máy chủ.");
            return;
        }

        _nav.ShowMatchmakingPanel();
        Debug.Log($"[Matchmaking] {fm.LocalDisplayName} đang tìm trận thật qua Firebase...");
        fm.StartMatchmaking();
    }

    private void OnPracticeClicked()
    {
        var fm = FirebaseManager.Instance;
        if (fm == null) return;

        // BẮT BUỘC bật offline mode khi bấm đấu máy
        fm.isOfflineMode = true;
        _isCancelledMatchmaking = false;

        Debug.Log("[Matchmaking] Chế độ Đấu với máy — vào trận ngay.");
        _nav.ShowMatchmakingPanel();
        if (_searchingLabel != null)
            _searchingLabel.text = GetTextSafe("menu_preparing", "ĐANG CHUẨN BỊ...");

        // FIX-CANCEL: Lưu coroutine để có thể dừng khi cancel
        if (_offlineRoutine != null) _runner.StopCoroutine(_offlineRoutine);
        _offlineRoutine = _runner.StartCoroutine(OfflineGoToGameplayRoutine());
    }

    private void OnCancelMatchClicked()
    {
        // FIX-CANCEL: Đánh dấu đã cancel để block OnMatchFound nếu nó fire muộn
        _isCancelledMatchmaking = true;

        var fm = FirebaseManager.Instance;
        if (fm != null)
        {
            if (!fm.isOfflineMode)
            {
                // Online: hủy matchmaking Firebase
                fm.CancelMatchmaking();
            }
            else
            {
                // Offline: dừng coroutine chờ vào game
                if (_offlineRoutine != null)
                {
                    _runner.StopCoroutine(_offlineRoutine);
                    _offlineRoutine = null;
                    Debug.Log("[Matchmaking] Đã hủy coroutine offline matchmaking.");
                }
                // Reset offline mode
                fm.isOfflineMode = false;
            }
        }

        _nav.ShowHome();
    }

    private void OnMatchFoundFromFirebase()
    {
        // FIX-CANCEL: Nếu người dùng đã cancel trước khi match tìm thấy → bỏ qua
        if (_isCancelledMatchmaking)
        {
            Debug.Log("[Matchmaking] OnMatchFound bị bỏ qua vì đã cancel matchmaking.");
            return;
        }
        var fm = FirebaseManager.Instance;
        Debug.Log($"[Matchmaking] Đã ghép: {fm?.LocalDisplayName} vs {fm?.OpponentName}. Vào trận!");
        LoadGameplayScene();
    }

    private void OnMatchmakingError(string error)
    {
        Debug.LogError($"[Matchmaking] Matchmaking error: {error}");
        if (_searchingLabel != null)
            _searchingLabel.text = string.Format(GetTextSafe("menu_error_generic", "Lỗi: {0}"), error);

        // Sau 2s quay về Home
        _runner.StartCoroutine(ReturnToHomeAfter(2f));
    }

    // UX-06: Matchmaking timeout handler
    private void OnMatchmakingTimeout()
    {
        ToastService.ShowInfo(_root, GetTextSafe("menu_matchmaking_timeout", "Không tìm thấy đối thủ. Thử lại?"), 4f);
        _nav.ShowHome();
    }

    // ==================== ROUTINES & HELPERS ====================

    private IEnumerator OfflineGoToGameplayRoutine()
    {
        yield return new WaitForSeconds(2f);
        LoadGameplayScene();
    }

    private IEnumerator ReturnToHomeAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _nav.ShowHome();
    }

    private void LoadGameplayScene()
    {
        if (GameManager.Instance != null) GameManager.Instance.LoadGameplayScene();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayScene");
    }

    private static string GetTextSafe(string key, string fallback)
    {
        var l = LocalizationManager.Instance;
        return l != null ? l.GetText(key, fallback) : fallback;
    }
}
