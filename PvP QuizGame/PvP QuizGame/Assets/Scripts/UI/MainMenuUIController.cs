using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// Quản lý UI của màn hình sảnh chính (HomeScene).
/// Điều hướng các panel Đăng nhập, Sảnh chính, Cài đặt và Tìm trận (Matchmaking).
/// </summary>
public class MainMenuUIController : MonoBehaviour
{
    [Header("Các màn hình (Panels)")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject matchmakingPanel;

    [Header("Nút tương tác Đăng nhập")]
    [SerializeField] private Button loginSubmitButton;

    [Header("Nút tương tác Sảnh chính")]
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Button settingsButton;
    
    [Header("Nút quay lại (Back To Home)")]
    [SerializeField] private Button[] backToHomeButtons;
    
    [Header("Nút giả lập (Dev Tools)")]
    [Tooltip("Nút này dành cho Dev để test chuyển cảnh tải Gameplay")]
    [SerializeField] private Button devFoundMatchButton; 

    private void Start()
    {
        // Khởi tạo các sự kiện nút bấm chính
        loginSubmitButton?.onClick.AddListener(OnLoginSubmit);
        findMatchButton?.onClick.AddListener(OnFindMatchClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
        
        // Gắn sự kiện "Quay Về" cho toàn bộ mảng các nút (từ Settings, từ Matchmaking về Home)
        if (backToHomeButtons != null)
        {
            foreach (var btn in backToHomeButtons)
            {
                btn?.onClick.AddListener(ShowHomePanel);
            }
        }

        // Nút Dev test chuyển cảnh
        devFoundMatchButton?.onClick.AddListener(OnMatchFoundSuccess);
        resetDataButton?.onClick.AddListener(OnResetDataClicked);

        // TODO (Firebase): Kiểm tra trạng thái lưu đăng nhập
        // Tạm thời hiển thị panel đăng nhập lúc vừa khởi động sảnh
        ShowPanel(loginPanel);
        RefreshPlayerStatsUI();
    }

    private void RefreshPlayerStatsUI()
    {
        if (PlayerDataManager.Instance == null) return;
        
        var data = PlayerDataManager.Instance.Data;
        if (levelText) levelText.text = $"Level: {data.level}";
        if (moneyText) moneyText.text = $"Money: {data.money}$";

        Debug.Log($"<color=white>[MainMenuUI] UI Updated: Level {data.level}, Money {data.money}$</color>");
    }

    private void OnResetDataClicked()
    {
        if (PlayerDataManager.Instance == null) return;
        PlayerDataManager.Instance.Data.Reset();
        PlayerDataManager.Instance.SaveData();
        RefreshPlayerStatsUI();
    }

    // ==================== ĐIỀU HƯỚNG PANEL ====================
    private void ShowPanel(GameObject target)
    {
        // Kiểm tra null từng cái để tránh UnassignedReferenceException khi chưa gán Inspector
        if (loginPanel != null) loginPanel.SetActive(loginPanel == target);
        if (homePanel != null) homePanel.SetActive(homePanel == target);
        if (settingsPanel != null) settingsPanel.SetActive(settingsPanel == target);
        if (matchmakingPanel != null) matchmakingPanel.SetActive(matchmakingPanel == target);

        if (target != null)
            Debug.Log($"[MainMenu] Đang hiển thị Panel: {target.name}");
    }
    
    public void ShowHomePanel() => ShowPanel(homePanel);

    // ==================== XỬ LÝ SỰ KIỆN ====================
    private void OnLoginSubmit()
    {
        // TODO: Hàm này sẽ do Firebase Auth xử lý
        Debug.Log("[MainMenu] Đăng nhập thành công, vào sảnh chính!");
        ShowHomePanel();
    }

    private void OnSettingsClicked()
    {
        ShowPanel(settingsPanel);
    }

    private void OnFindMatchClicked()
    {
        ShowPanel(matchmakingPanel);
        Debug.Log("[MainMenu] Bắt đầu tìm trận đấu (Giả lập)...");
        
        // Giả lập tìm trận trong 2.5 giây rồi vào game
        StartCoroutine(FakeMatchmakingRoutine());
    }

    private IEnumerator FakeMatchmakingRoutine()
    {
        yield return new WaitForSeconds(2.5f);
        OnMatchFoundSuccess();
    }

    // Hàm gọi khi việc tìm phòng (Matchmaking) Firebase thành công
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
