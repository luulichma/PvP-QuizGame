using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Xử lý input của Player 1 sử dụng UI Toolkit.
/// </summary>
public class InputController_UXML : MonoBehaviour
{
    public static InputController_UXML Instance { get; private set; }

    public static event Action<int, int, bool> OnAnswerSubmitted;

    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;

    private List<Button> _answerButtons = new List<Button>();

    // Lưu lại lambda để có thể unsubscribe đúng cách (tránh memory leak)
    private Dictionary<Button, Action> _buttonHandlers = new Dictionary<Button, Action>();

    // Lưu text gốc của từng nút để khôi phục sau khi hiện "ĐANG ĐỢI..."
    private string[] _originalAnswerTexts = new string[4];

    // Màu sắc phản hồi (Vì USS không hỗ trợ Color dễ dàng qua code, ta dùng StyleColor)
    private readonly Color _correctColor = new Color(0, 0.9f, 0.46f); // var(--color-accent-green)
    private readonly Color _wrongColor = new Color(1f, 0.32f, 0.32f);   // var(--color-accent-red)
    private readonly Color _defaultColor = new Color(0, 0.9f, 1f, 0.15f); // rgba(255,255,255,0.15)

    private bool _inputLocked = false;
    private int _localPlayerId = 1;
    private int _myLastAnswer = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        // Fallback: Nếu không thấy trên cùng GameObject, tìm trong toàn scene
        if (uiDocument == null) uiDocument = UnityEngine.Object.FindAnyObjectByType<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("[InputController_UXML] Không tìm thấy UIDocument trong scene! Các nút bấm sẽ không hoạt động.");
            return;
        }

        // FIX: Có thể UIDocument chưa build xong rootVisualElement khi OnEnable chạy.
        // Thử query luôn, nếu không thấy thì retry vài frame sau.
        QueryButtons();
        if (_answerButtons.Count < 4)
            StartCoroutine(RetryQueryButtonsRoutine());

        GameController.OnGameStateChanged += HandleGameStateChanged;
        QuizManager.OnQuestionChanged     += HandleQuestionChanged;
    }

    /// <summary>
    /// Retry query trong 30 frame (~0.5s) đề phòng UIDocument build chậm.
    /// </summary>
    private IEnumerator RetryQueryButtonsRoutine()
    {
        for (int frame = 0; frame < 30; frame++)
        {
            yield return null;
            if (_answerButtons.Count >= 4) yield break;
            QueryButtons();
        }

        if (_answerButtons.Count < 4)
        {
            Debug.LogError($"[InputController_UXML] Sau 30 frame vẫn KHÔNG tìm thấy đủ nút ans-0..ans-3 (chỉ thấy {_answerButtons.Count}). " +
                           "Kiểm tra: (1) UIDocument có gán đúng GameplayLayout.uxml, (2) Tên nút trong UXML đúng là ans-0/1/2/3, " +
                           "(3) InputController_UXML và UIDocument có cùng GameObject hoặc gán SerializeField đúng.");
        }
    }

    private void OnDisable()
    {
        GameController.OnGameStateChanged -= HandleGameStateChanged;
        QuizManager.OnQuestionChanged     -= HandleQuestionChanged;

        // FIX: Unsubscribe từng lambda đã lưu trong _buttonHandlers
        foreach (var kv in _buttonHandlers)
        {
            if (kv.Key != null) kv.Key.clicked -= kv.Value;
        }
        _buttonHandlers.Clear();
    }

    private void Update()
    {
        if (_inputLocked || GameController.Instance == null) return;
        if (GameController.Instance.CurrentState != GameState.Playing) return;

        // Keyboard Fallback (Legacy Input). Nếu project dùng InputSystem mới,
        // cần đặt Project Settings > Player > Active Input Handling = "Both"
        if (Input.GetKeyDown(KeyCode.Alpha1)) HandleAnswerClicked(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) HandleAnswerClicked(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) HandleAnswerClicked(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) HandleAnswerClicked(3);
    }

    private void HandleAnswerClicked(int answerIndex)
    {
        if (_inputLocked) return;
        _inputLocked = true;
        _myLastAnswer = answerIndex;
        SetButtonsInteractable(false);

        // HIỆU ỨNG PHẢN HỒI NGAY LẬP TỨC: Đổi màu nút thành Vàng để báo hiệu "Đang chờ đối thủ"
        SetButtonColor(answerIndex, Color.yellow);

        // FIX: Localize text "ĐANG ĐỢI..." thay vì hardcode
        if (answerIndex >= 0 && answerIndex < _answerButtons.Count && _answerButtons[answerIndex] != null)
        {
            string waitText = "...";
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
                waitText = LocalizationManager.Instance.GetText("msg_waiting");

            _answerButtons[answerIndex].text = waitText;
        }

        Debug.Log($"[InputController] P1 chọn [{answerIndex}].");

        bool useFirebase = FirebaseManager.Instance != null && !FirebaseManager.Instance.isOfflineMode;

        if (useFirebase && FirebaseMatchProvider.Instance != null)
            FirebaseMatchProvider.Instance.SubmitAnswerP1(answerIndex);
        else
            LocalMatchProvider.Instance?.SubmitAnswerP1(answerIndex);

        // Ghi nhận vào GameController để đợi hết giờ mới chấm
        if (GameController.Instance != null)
            GameController.Instance.SetLocalAnswer(answerIndex);
    }

    public IEnumerator ShowAnswerFeedback(int correctAnswerIndex)
    {
        bool isCorrect = (_myLastAnswer == correctAnswerIndex);

        // Khôi phục text gốc của nút mình đã chọn (đang hiển thị "ĐANG ĐỢI...")
        if (_myLastAnswer >= 0 && _myLastAnswer < _answerButtons.Count)
        {
            if (_originalAnswerTexts[_myLastAnswer] != null && _answerButtons[_myLastAnswer] != null)
                _answerButtons[_myLastAnswer].text = _originalAnswerTexts[_myLastAnswer];
        }

        if (_myLastAnswer >= 0)
        {
            SetButtonColor(_myLastAnswer, isCorrect ? _correctColor : _wrongColor);
            if (!isCorrect)
            {
                UIAnimator.DOShakePosition(_answerButtons[_myLastAnswer], 0.5f);
            }
            else
            {
                _answerButtons[_myLastAnswer].style.scale = new StyleScale(new UnityEngine.UIElements.Scale(new Vector2(1.1f, 1.1f)));
            }
        }

        if (!isCorrect && correctAnswerIndex >= 0)
        {
            SetButtonColor(correctAnswerIndex, _correctColor);
            _answerButtons[correctAnswerIndex].style.scale = new StyleScale(new UnityEngine.UIElements.Scale(new Vector2(1.1f, 1.1f)));
        }

        OnAnswerSubmitted?.Invoke(_localPlayerId, _myLastAnswer, isCorrect);

        yield return new WaitForSeconds(1.5f);

        for (int i = 0; i < _answerButtons.Count; i++)
        {
            SetButtonColor(i, _defaultColor);
            _answerButtons[i].style.scale = new StyleScale(new UnityEngine.UIElements.Scale(Vector2.one));
        }
    }

    private void SetButtonsInteractable(bool on)
    {
        foreach (var btn in _answerButtons)
        {
            if (btn != null) btn.SetEnabled(on);
        }
    }

    private void SetButtonColor(int index, Color color)
    {
        if (index < 0 || index >= _answerButtons.Count) return;
        var btn = _answerButtons[index];
        if (btn != null)
        {
            btn.style.backgroundColor = new StyleColor(color);
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        bool playing = state == GameState.Playing;
        SetButtonsInteractable(playing);
        _inputLocked = !playing;
        _myLastAnswer = -1;
    }

    private void HandleQuestionChanged(QuestionData question)
    {
        if (question == null) return;

        // BỔ SUNG: Nếu danh sách nút bị trống hoặc chưa đủ 4, query lại
        if (_answerButtons == null || _answerButtons.Count < 4)
        {
            QueryButtons();
        }

        if (_answerButtons.Count == 0)
        {
            Debug.LogWarning($"[InputController_UXML] HandleQuestionChanged nhưng _answerButtons rỗng! " +
                             $"Question key = {question.questionText}");
            return;
        }

        Debug.Log($"[InputController_UXML] Cập nhật {_answerButtons.Count} nút cho câu '{question.questionText}'");

        // Cập nhật nội dung cho từng nút
        for (int i = 0; i < _answerButtons.Count; i++)
        {
            if (i < question.answers.Length)
            {
                // LOCALIZATION: Dịch text từ mã Key (Ví dụ: a_it_001_1)
                string ansKey = question.answers[i];
                string ansText = LocalizationManager.Instance != null
                                 ? LocalizationManager.Instance.GetText(ansKey)
                                 : ansKey;

                _answerButtons[i].text = ansText;
                _originalAnswerTexts[i] = ansText; // Lưu để khôi phục sau "ĐANG ĐỢI..."
                _answerButtons[i].style.display = DisplayStyle.Flex;
            }
            else
            {
                // Ẩn các nút thừa nếu câu hỏi có ít hơn 4 đáp án
                _answerButtons[i].style.display = DisplayStyle.None;
            }
            SetButtonColor(i, _defaultColor);
        }

        _inputLocked = false;
        _myLastAnswer = -1;
        SetButtonsInteractable(true);

        // ANIMATION: Hiển thị hiệu ứng lượn sóng cho 4 đáp án
        UIAnimator.AnimateAnswersEntry(_answerButtons);
    }

    private void QueryButtons()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[InputController_UXML] QueryButtons: uiDocument == null");
            return;
        }
        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogWarning("[InputController_UXML] QueryButtons: rootVisualElement == null (UIDocument chưa build xong?)");
            return;
        }

        // Cleanup handlers cũ trước khi query lại
        foreach (var kv in _buttonHandlers)
        {
            if (kv.Key != null) kv.Key.clicked -= kv.Value;
        }
        _buttonHandlers.Clear();
        _answerButtons.Clear();

        for (int i = 0; i < 4; i++)
        {
            var btn = root.Q<Button>($"ans-{i}");
            if (btn != null)
            {
                int index = i;
                Action handler = () => HandleAnswerClicked(index);
                btn.clicked += handler;
                _buttonHandlers[btn] = handler;
                _answerButtons.Add(btn);
            }
        }

        // Reset cache text
        for (int i = 0; i < _originalAnswerTexts.Length; i++)
            _originalAnswerTexts[i] = null;

        if (_answerButtons.Count > 0)
            Debug.Log($"[InputController_UXML] QueryButtons OK — tìm thấy {_answerButtons.Count}/4 nút.");
    }

    public void SetPlayerId(int id) => _localPlayerId = id;
}
