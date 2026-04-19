using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Quản lý UI và luồng khởi tạo của InitScene sử dụng UI Toolkit.
/// </summary>
public class InitSceneController_UXML : MonoBehaviour
{
    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement _loadingFill;
    private Label _statusLabel;
    private Label _progressLabel;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        // Truy vấn các phần tử theo Name trong UXML
        _loadingFill = root.Q<VisualElement>("loading-fill");
        _statusLabel = root.Q<Label>("status-label");
        _progressLabel = root.Q<Label>("progress-label");

        // Khởi tạo trạng thái ban đầu
        UpdateProgressUI(0f);
        if (_statusLabel != null) _statusLabel.text = "Đang khởi tạo hệ thống...";
    }

    private void Start()
    {
        // Bắt đầu tiến trình khởi tạo
        StartCoroutine(InitializationRoutine());
    }

    private IEnumerator InitializationRoutine()
    {
        // 1. Giả lập / Chờ khởi tạo Firebase (Task 2.5)
        if (_statusLabel != null) _statusLabel.text = "Đang kết nối cơ sở dữ liệu (Firebase)...";
        
        float fakeProgress = 0f;
        while (fakeProgress < 1f)
        {
            fakeProgress += Time.deltaTime * 0.5f; // Chạy mất khoảng 2 giây
            UpdateProgressUI(fakeProgress * 0.5f); // Chiếm 50% thanh tiến trình đầu
            yield return null;
        }

        // 2. Chuyển sang MainMenuScene bất đồng bộ
        if (_statusLabel != null) _statusLabel.text = "Đang tải sảnh chờ (Main Menu)...";

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSceneAsync(
                "HomeScene", 
                onProgress: (p) => {
                    UpdateProgressUI(0.5f + (p * 0.5f));
                }
            );
        }
        else
        {
            Debug.LogError("[InitSceneController] Không tìm thấy GameManager!");
        }
    }

    private void UpdateProgressUI(float progress)
    {
        // UI Toolkit dùng % để set độ dài (width) cho fill bar
        if (_loadingFill != null)
        {
            _loadingFill.style.width = Length.Percent(progress * 100f);
        }
        
        if (_progressLabel != null)
        {
            _progressLabel.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
    }
}
