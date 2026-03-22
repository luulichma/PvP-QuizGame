using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Quản lý UI và luồng khởi tạo của InitScene.
/// Hiển thị thanh tiến trình khi load các thành phần ngầm (Firebase) và load HomeScene.
/// </summary>
public class InitSceneController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text progressText;
    [SerializeField] private Text statusText;

    private void Start()
    {
        // Khởi tạo các giá trị ban đầu cho UI
        if (progressBar != null) progressBar.value = 0f;
        if (progressText != null) progressText.text = "0%";
        if (statusText != null) statusText.text = "Đang khởi tạo hệ thống...";

        // Bắt đầu tiến trình khởi tạo
        StartCoroutine(InitializationRoutine());
    }

    private IEnumerator InitializationRoutine()
    {
        // 1. Giả lập / Chờ khởi tạo Firebase (Task 2.5)
        // Nếu có FirebaseManager thực sự, bạn có thể gọi Task await ở đây
        if (statusText != null) statusText.text = "Đang kết nối cơ sở dữ liệu (Firebase)...";
        
        float fakeProgress = 0f;
        while (fakeProgress < 1f)
        {
            fakeProgress += Time.deltaTime * 0.5f; // Chạy mất khoảng 2 giây
            UpdateProgressUI(fakeProgress * 0.5f); // Chiếm 50% thanh tiến trình đầu
            yield return null;
        }

        // 2. Chuyển sang MainMenuScene bất đồng bộ (Load 50% tiến trình còn lại)
        if (statusText != null) statusText.text = "Đang tải sảnh chờ (Main Menu)...";

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSceneAsync(
                "HomeScene", 
                onProgress: (p) => {
                    // Map tiến trình p (0->1) vào 50% nửa sau của thanh progress bar
                    UpdateProgressUI(0.5f + (p * 0.5f));
                }
            );
        }
        else
        {
            Debug.LogError("[InitSceneController] Không tìm thấy GameManager! Bạn đã cho GameManager vào InitScene chưa?");
        }
    }

    private void UpdateProgressUI(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
        
        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
    }
}
