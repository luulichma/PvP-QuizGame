using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

/// <summary>
/// [REFACTOR-P2] Overlay đếm ngược 3-2-1-GO! đầu trận — tách từ GameplayUIController_UXML
/// (CreateCountdownOverlay + HandleCountdownTick).
/// </summary>
public class CountdownOverlayController
{
    private readonly VisualElement _root;
    private VisualElement _overlay;
    private Label _label;

    public CountdownOverlayController(VisualElement root)
    {
        _root = root;
    }

    /// <summary>Tạo overlay khi vào state Countdown.</summary>
    public void Create()
    {
        if (_root == null) return;

        _overlay = new VisualElement();
        _overlay.AddToClassList("countdown-overlay");

        _label = new Label("3");
        _label.AddToClassList("countdown-number");

        _overlay.Add(_label);
        _root.Add(_overlay);
    }

    /// <summary>Cập nhật số đếm; tick == 0 → "GO!".</summary>
    public void HandleTick(int tick)
    {
        if (_label == null) return;

        // Haptic mỗi tick
        HapticFeedback.CountdownTick();

        if (tick == 0)
        {
            _label.text = "GO!";
            _label.RemoveFromClassList("countdown-number");
            _label.AddToClassList("countdown-go");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.countdownGoSound);
            UIAnimator.DOScale(_label, new Vector2(0.5f, 0.5f), 0.5f).SetEase(Ease.InBack);
            UIAnimator.DOFade(_label, 0f, 0.5f);
        }
        else
        {
            _label.text = tick.ToString();
            UIAnimator.DOCountdownPop(_label, 0.8f);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.countdownTickSound);
        }
    }

    /// <summary>Gỡ overlay khi vào state Playing.</summary>
    public void Remove()
    {
        if (_overlay != null)
        {
            _overlay.RemoveFromHierarchy();
            _overlay = null;
            _label = null;
        }
    }
}
