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
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[GameManager]");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Mobile optimization: ép framerate 60fps (Android mặc định 30fps)
        Application.targetFrameRate = 60;
        // Giữ màn hình sáng khi đang chơi PvP (tránh sleep giữa trận)
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    // ==================== CHUYỂN SCENE ====================
    public void LoadInitScene()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade("InitScene");
        else
            SceneManager.LoadScene("InitScene");
    }
    
    public void LoadHomeScene()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade("HomeScene");
        else
            SceneManager.LoadScene("HomeScene");
    }
    
    public void LoadGameplayScene()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadSceneWithFade("GameplayScene");
        else
            SceneManager.LoadScene("GameplayScene");
    }

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
