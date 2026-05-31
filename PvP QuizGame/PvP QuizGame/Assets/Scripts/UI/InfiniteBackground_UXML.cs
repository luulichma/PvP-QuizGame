using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Thêm hiệu ứng background chạy lặp vô tận cho UI Toolkit.
/// Yêu cầu Unity 2021.2 trở lên (hỗ trợ backgroundPositionX/Y).
/// </summary>
public class InfiniteBackground_UXML : MonoBehaviour
{
    [Header("Cài đặt Background")]
    [Tooltip("UIDocument chứa UI của Scene")]
    [SerializeField] private UIDocument uiDocument;
    
    [Tooltip("Tên của thẻ VisualElement muốn áp dụng background (thường là 'root')")]
    [SerializeField] private string targetElementName = "root";
    
    [Tooltip("Texture nền cần lặp lại (Nên dùng ảnh Seamless/Pattern)")]
    [SerializeField] private Texture2D backgroundTexture;
    
    [Tooltip("Tốc độ cuộn (X, Y)")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(30f, 30f);
    
    [Tooltip("Làm mờ nền (tint màu tối) để không đè lên chữ")]
    [SerializeField] private Color tintColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    private VisualElement _targetElement;
    private Vector2 _currentPosition;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        // Tìm phần tử root hoặc phần tử nền
        _targetElement = uiDocument.rootVisualElement.Q<VisualElement>(targetElementName);
        
        if (_targetElement != null && backgroundTexture != null)
        {
            // Thiết lập ảnh nền
            _targetElement.style.backgroundImage = new StyleBackground(backgroundTexture);
            
            // Pha thêm màu tối để text nổi bật hơn
            _targetElement.style.unityBackgroundImageTintColor = tintColor;
            
            // QUAN TRỌNG: Thiết lập ảnh lặp lại (Repeat)
            _targetElement.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
        }
    }

    private void Update()
    {
        if (_targetElement == null || backgroundTexture == null) return;

        // Cộng dồn vị trí
        _currentPosition += scrollSpeed * Time.deltaTime;
        
        // Reset khi quá lớn để tránh tràn số học
        if (Mathf.Abs(_currentPosition.x) > 10000f) _currentPosition.x = 0;
        if (Mathf.Abs(_currentPosition.y) > 10000f) _currentPosition.y = 0;

        // Cập nhật vị trí background
        _targetElement.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(_currentPosition.x, LengthUnit.Pixel));
        _targetElement.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, new Length(_currentPosition.y, LengthUnit.Pixel));
    }
}
