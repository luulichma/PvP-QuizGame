using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý UI của màn hình sảnh chính (MainMenuScene).
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

        // TODO (Firebase): Kiểm tra trạng thái lưu đăng nhập
        // Tạm thời hiển thị panel đăng nhập lúc vừa khởi động sảnh
        ShowPanel(loginPanel);
    }

    // ==================== ĐIỀU HƯỚNG PANEL ====================
    private void ShowPanel(GameObject target)
    {
        loginPanel?.SetActive(loginPanel == target);
        homePanel?.SetActive(homePanel == target);
        settingsPanel?.SetActive(settingsPanel == target);
        matchmakingPanel?.SetActive(matchmakingPanel == target);
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
        Debug.Log("[MainMenu] Bắt đầu tìm trận đấu trên Firebase Realtime DB...");
        // TODO: Gọi FirebaseManager.Instance.JoinOrCreateRoom()
    }

    // Hàm gọi khi việc tìm phòng (Matchmaking) Firebase thành công
    private void OnMatchFoundSuccess()
    {
        Debug.Log("[MainMenu] Tìm thấy trận! Chuyển sang GameplayScene...");
        if (GameManager.Instance != null)
        {
            // Có thể làm một popup tiến trình tải trận đấu ở đây
            GameManager.Instance.LoadSceneAsync("GameplayScene", progress => {
                // UIController có thể nghe progress để cập nhật vòng quay xoay chữ "Vào trận thôi..."
            });
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayScene");
        }
    }
}
