using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [REFACTOR-P2] Panel & flow Tim Tran — tach tu MainMenuUIController_UXML.
///
/// Giu nguyen cac fix cu:
/// - FIX-CANCEL: flag _isCancelledMatchmaking chan OnMatchFound fire muon sau khi cancel.
/// - FIX-CANCEL: luu coroutine offline de dung duoc khi cancel.
/// - UX-06: timeout -> toast + ve Home.
/// - [BotFallback] 15s khong tim duoc tran that -> tu chuyen sang dau bot.
///
/// Can MonoBehaviour runner de chay coroutine (truyen MainMenuUIController vao).
/// Attach()/Detach() phai duoc goi tu OnEnable/OnDisable cua runner.
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

    // FIX-CANCEL: Luu coroutine offline de co the dung khi cancel
    private Coroutine _offlineRoutine;
    // FIX-CANCEL: Flag de block OnMatchFound sau khi da cancel online matchmaking
    private bool _isCancelledMatchmaking = false;

    // [BotFallback] Sau N giay khong ghep duoc tran that -> tu fallback sang dau bot.
    // User chua dong -> tranh cho vo tan.
    private const float BOT_FALLBACK_SECONDS = 15f;
    private Coroutine _botFallbackRoutine;

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
        FirebaseManager.OnMatchmakingTimeout += OnMatchmakingTimeout;
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

    /// <summary>Huy tim tran (dung cho ca nut HUY va nut Back Android).</summary>
    public void CancelMatch() => OnCancelMatchClicked();

    // ==================== HANDLERS ====================

    private void OnFindMatchClicked()
    {
        var fm = FirebaseManager.Instance;
        if (fm == null) return;

        if (!fm.IsAuthenticated)
        {
            ToastService.ShowInfo(_root, GetTextSafe("menu_login_required", "Ban can dang nhap de tim tran online."), 3f);
            return;
        }

        fm.isOfflineMode = false;
        _isCancelledMatchmaking = false;

        if (!fm.IsConnected)
        {
            if (_searchingLabel != null)
                _searchingLabel.text = GetTextSafe("menu_error_connection", "Loi ket noi may chu.");
            return;
        }

        _nav.ShowMatchmakingPanel();
        Debug.Log($"[Matchmaking] {fm.LocalDisplayName} dang tim tran that qua Firebase...");
        fm.StartMatchmaking();

        // [BotFallback] Bat dau dem 15s — neu van chua ghep duoc tran that thi
        // tu chuyen sang dau bot de user khong phai cho vo tan.
        if (_botFallbackRoutine != null) _runner.StopCoroutine(_botFallbackRoutine);
        _botFallbackRoutine = _runner.StartCoroutine(BotFallbackRoutine());
    }

    private void OnPracticeClicked()
    {
        var fm = FirebaseManager.Instance;
        if (fm == null) return;

        fm.isOfflineMode = true;
        _isCancelledMatchmaking = false;

        Debug.Log("[Matchmaking] Che do Dau voi may — vao tran ngay.");
        _nav.ShowMatchmakingPanel();
        if (_searchingLabel != null)
            _searchingLabel.text = GetTextSafe("menu_preparing", "DANG CHUAN BI...");

        if (_offlineRoutine != null) _runner.StopCoroutine(_offlineRoutine);
        _offlineRoutine = _runner.StartCoroutine(OfflineGoToGameplayRoutine());
    }

    private void OnCancelMatchClicked()
    {
        _isCancelledMatchmaking = true;

        // [BotFallback] User huy thu cong -> cung phai dung bot-fallback timer.
        StopBotFallbackTimer();

        var fm = FirebaseManager.Instance;
        if (fm != null)
        {
            if (!fm.isOfflineMode)
            {
                fm.CancelMatchmaking();
            }
            else
            {
                if (_offlineRoutine != null)
                {
                    _runner.StopCoroutine(_offlineRoutine);
                    _offlineRoutine = null;
                    Debug.Log("[Matchmaking] Da huy coroutine offline matchmaking.");
                }
                fm.isOfflineMode = false;
            }
        }

        _nav.ShowHome();
    }

    private void OnMatchFoundFromFirebase()
    {
        if (_isCancelledMatchmaking)
        {
            Debug.Log("[Matchmaking] OnMatchFound bi bo qua vi da cancel matchmaking.");
            return;
        }
        // [BotFallback] Da ghep kip nguoi that -> huy bot-fallback timer.
        StopBotFallbackTimer();

        var fm = FirebaseManager.Instance;
        Debug.Log($"[Matchmaking] Da ghep: {fm?.LocalDisplayName} vs {fm?.OpponentName}. Vao tran!");
        LoadGameplayScene();
    }

    private void OnMatchmakingError(string error)
    {
        Debug.LogError($"[Matchmaking] Matchmaking error: {error}");
        // [BotFallback] Co loi tu Firebase -> cung dung timer (do trung coroutine).
        StopBotFallbackTimer();
        if (_searchingLabel != null)
            _searchingLabel.text = string.Format(GetTextSafe("menu_error_generic", "Loi: {0}"), error);

        _runner.StartCoroutine(ReturnToHomeAfter(2f));
    }

    // UX-06: Matchmaking timeout handler (45s hard timeout tu FirebaseManager)
    private void OnMatchmakingTimeout()
    {
        // [BotFallback] Da den day nghia la hard timeout 45s — chac chan bot-fallback
        // 15s da chay (hoac bi skip vi ly do nao do). Van dung timer cho chac.
        StopBotFallbackTimer();
        ToastService.ShowInfo(_root, GetTextSafe("menu_matchmaking_timeout", "Khong tim thay doi thu. Thu lai?"), 4f);
        _nav.ShowHome();
    }

    // ==================== ROUTINES & HELPERS ====================

    private IEnumerator OfflineGoToGameplayRoutine()
    {
        yield return new WaitForSeconds(2f);
        LoadGameplayScene();
    }

    /// <summary>
    /// [BotFallback] Doi BOT_FALLBACK_SECONDS giay. Neu user chua ghep duoc tran that
    /// va chua cancel, huy matchmaking online roi tu chuyen sang dau bot.
    /// User chi thay 1 transition muot — khong phai bam lai "Dau voi may".
    /// </summary>
    private IEnumerator BotFallbackRoutine()
    {
        yield return new WaitForSeconds(BOT_FALLBACK_SECONDS);

        if (_isCancelledMatchmaking) yield break;

        Debug.Log($"[Matchmaking] Qua {BOT_FALLBACK_SECONDS}s chua tim duoc doi thu that -> fallback dau bot.");

        var fm = FirebaseManager.Instance;
        if (fm != null)
        {
            _isCancelledMatchmaking = true;
            fm.CancelMatchmaking();
            fm.isOfflineMode = true;
        }

        // Reset co de OnPracticeClicked path khong bi skip.
        _isCancelledMatchmaking = false;
        _botFallbackRoutine = null;

        if (_searchingLabel != null)
            _searchingLabel.text = GetTextSafe("menu_bot_fallback", "Khong tim duoc doi thu — vao tran voi may...");

        if (_offlineRoutine != null) _runner.StopCoroutine(_offlineRoutine);
        _offlineRoutine = _runner.StartCoroutine(OfflineGoToGameplayRoutine());
    }

    private void StopBotFallbackTimer()
    {
        if (_botFallbackRoutine != null)
        {
            _runner.StopCoroutine(_botFallbackRoutine);
            _botFallbackRoutine = null;
        }
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
