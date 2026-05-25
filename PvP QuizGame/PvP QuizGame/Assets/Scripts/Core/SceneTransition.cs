using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;

/// <summary>
/// UX-05: Singleton quản lý scene transition animation (fade-to-black).
/// Dùng DontDestroyOnLoad canvas overlay để chạy animation xuyên scene.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [SerializeField] private float fadeDuration = 0.5f;

    private UIDocument _uiDocument;
    private VisualElement _fadeOverlay;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Tạo UIDocument runtime
        _uiDocument = gameObject.AddComponent<UIDocument>();

        // Tìm PanelSettings trong Resources, fallback: để null — Unity sẽ dùng default
        var panelSettings = Resources.Load<PanelSettings>("DefaultPanelSettings");
        if (panelSettings != null)
        {
            _uiDocument.panelSettings = panelSettings;
        }
        else
        {
            Debug.LogWarning("[SceneTransition] Không tìm thấy PanelSettings trong Resources. Dùng PanelSettings mặc định của scene.");
        }

        var root = _uiDocument.rootVisualElement;

        _fadeOverlay = new VisualElement();
        _fadeOverlay.name = "scene-fade-overlay";
        _fadeOverlay.style.position = Position.Absolute;
        _fadeOverlay.style.top = 0;
        _fadeOverlay.style.bottom = 0;
        _fadeOverlay.style.left = 0;
        _fadeOverlay.style.right = 0;
        _fadeOverlay.style.backgroundColor = Color.black;
        _fadeOverlay.style.opacity = 0f;
        _fadeOverlay.pickingMode = PickingMode.Ignore;

        root.Add(_fadeOverlay);
    }

    /// <summary>
    /// Fade to black, load scene, then fade in.
    /// </summary>
    public void LoadSceneWithFade(string sceneName, Action onLoaded = null)
    {
        StartCoroutine(FadeRoutine(sceneName, onLoaded));
    }

    private IEnumerator FadeRoutine(string sceneName, Action onLoaded)
    {
        // Fade out (→ black)
        _fadeOverlay.style.opacity = 0f;
        _fadeOverlay.pickingMode = PickingMode.Position;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeOverlay.style.opacity = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        _fadeOverlay.style.opacity = 1f;

        // Load scene
        var asyncOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        asyncOp.allowSceneActivation = true;
        while (!asyncOp.isDone) yield return null;

        onLoaded?.Invoke();

        // Fade in (→ transparent)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeOverlay.style.opacity = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        _fadeOverlay.style.opacity = 0f;
        _fadeOverlay.pickingMode = PickingMode.Ignore;
    }
}
