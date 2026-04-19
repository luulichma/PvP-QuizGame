using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Quản lý UI của màn hình sảnh chính (HomeScene) sử dụng UI Toolkit.
/// </summary>
public class MainMenuUIController_UXML : MonoBehaviour
{
    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;

    // Panels (VisualElements)
    private VisualElement _homePanel;
    private VisualElement _matchmakingPanel;

    // Buttons
    private Button _findMatchBtn;
    private Button _settingsBtn;
    private Button _cancelMatchBtn;

    // Labels
    private Label _moneyLabel;
    private Label _levelTag;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        // Query Panels
        _homePanel = root.Q<VisualElement>("home-panel");
        _matchmakingPanel = root.Q<VisualElement>("matchmaking-panel");

        // Query Buttons & Events
        _findMatchBtn = root.Q<Button>("find-match-btn");
        _settingsBtn = root.Q<Button>("settings-btn");
        _cancelMatchBtn = root.Q<Button>("cancel-match-btn");

        if (_findMatchBtn != null) _findMatchBtn.clicked += OnFindMatchClicked;
        if (_settingsBtn != null) _settingsBtn.clicked += OnSettingsClicked;
        if (_cancelMatchBtn != null) _cancelMatchBtn.clicked += ShowHomePanel;

        // Query Labels
        _moneyLabel = root.Q<Label>("money-label");
        _levelTag = root.Q<Label>("level-tag");

        // Initial State
        ShowHomePanel();
        RefreshPlayerStatsUI();
    }

    private void OnDisable()
    {
        // Unregister events to prevent memory leaks
        if (_findMatchBtn != null) _findMatchBtn.clicked -= OnFindMatchClicked;
        if (_settingsBtn != null) _settingsBtn.clicked -= OnSettingsClicked;
        if (_cancelMatchBtn != null) _cancelMatchBtn.clicked -= ShowHomePanel;
    }

    private void RefreshPlayerStatsUI()
    {
        if (PlayerDataManager.Instance == null) return;
        
        var data = PlayerDataManager.Instance.Data;
        if (_levelTag != null) _levelTag.text = $"LEVEL {data.level}";
        if (_moneyLabel != null) _moneyLabel.text = $"${data.money:N0}";

        Debug.Log($"<color=white>[MainMenuUI] UI Toolkit Updated: Level {data.level}, Money {data.money}$</color>");
    }

    // ==================== ĐIỀU HƯỚNG PANEL ====================
    private void ShowPanel(VisualElement target)
    {
        if (_homePanel != null) _homePanel.style.display = (_homePanel == target) ? DisplayStyle.Flex : DisplayStyle.None;
        if (_matchmakingPanel != null) _matchmakingPanel.style.display = (_matchmakingPanel == target) ? DisplayStyle.Flex : DisplayStyle.None;

        if (target != null)
            Debug.Log($"[MainMenu] Đang hiển thị Panel: {target.name}");
    }
    
    public void ShowHomePanel() => ShowPanel(_homePanel);

    // ==================== XỬ LÝ SỰ KIỆN ====================
    private void OnSettingsClicked()
    {
        // TODO: Show Settings Popup (UIDocument riêng)
        Debug.Log("[MainMenu] Mở Settings Popup...");
    }

    private void OnFindMatchClicked()
    {
        ShowPanel(_matchmakingPanel);
        Debug.Log("[MainMenu] Bắt đầu tìm trận đấu...");
        
        // Giả lập tìm trận trong 2.5 giây
        StartCoroutine(FakeMatchmakingRoutine());
    }

    private IEnumerator FakeMatchmakingRoutine()
    {
        yield return new WaitForSeconds(2.5f);
        OnMatchFoundSuccess();
    }

    private void OnMatchFoundSuccess()
    {
        Debug.Log("[MainMenu] Tìm thấy trận! Chuyển sang GameplayScene...");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGameplayScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayScene");
        }
    }
}
