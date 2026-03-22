using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

/// <summary>
/// Singleton quản lý toàn bộ hệ thống Application.
/// Tồn tại xuyên suốt từ InitScene đến tất cả các scene khác.
/// Xử lý chuyển Scene, Settings chung, v.v.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static GameManager Instance { get; private set; }

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ==================== CHUYỂN SCENE ====================
    public void LoadInitScene() => SceneManager.LoadScene("InitScene");
    
    public void LoadHomeScene() => SceneManager.LoadScene("HomeScene");
    
    public void LoadGameplayScene() => SceneManager.LoadScene("GameplayScene");

    /// <summary>
    /// Chuyển scene bất đồng bộ và trả về tiến trình (0.0 đến 1.0)
    /// </summary>
    public void LoadSceneAsync(string sceneName, Action<float> onProgress, Action onLoaded = null)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, onProgress, onLoaded));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, Action<float> onProgress, Action onLoaded)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Ngăn không cho scene tự động chuyển khi load xong 90% (để làm hiệu ứng mượt nếu cần)
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            // Tiến trình thực tế của Unity chạy từ 0 đến 0.9
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            onProgress?.Invoke(progress);

            // Khi load xong memory
            if (asyncLoad.progress >= 0.9f)
            {
                // Cho phép progress bar chạy đến mốc 1.0 trên UI
                onProgress?.Invoke(1f);
                
                // Kích hoạt chuyển scene
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        onLoaded?.Invoke();
    }
}
