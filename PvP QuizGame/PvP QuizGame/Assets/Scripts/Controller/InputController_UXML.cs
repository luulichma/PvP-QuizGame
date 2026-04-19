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

        var root = uiDocument.rootVisualElement;
        _answerButtons.Clear(); // Dọn dẹp danh sách cũ

        // Query 4 nút đáp án dựa trên Name trong GameplayLayout.uxml
        for (int i = 0; i < 4; i++)
        {
            string btnName = $"ans-{i}";
            var btn = root.Q<Button>(btnName);
            if (btn != null)
            {
                int index = i;
                btn.clicked += () => HandleAnswerClicked(index);
                _answerButtons.Add(btn);
                Debug.Log($"[InputController_UXML] Đã đăng ký sự kiện cho nút: {btnName}");
            }
            else
            {
                Debug.LogWarning($"[InputController_UXML] Không tìm thấy nút: {btnName} trong document.");
            }
        }

        GameController.OnGameStateChanged += HandleGameStateChanged;
        QuizManager.OnQuestionChanged     += HandleQuestionChanged;
    }

    private void OnDisable()
    {
        GameController.OnGameStateChanged -= HandleGameStateChanged;
        QuizManager.OnQuestionChanged     -= HandleQuestionChanged;
        
        foreach (var btn in _answerButtons)
        {
            // Xóa đăng ký để tránh memory leak (UI Toolkit đặc thù)
            // btn.clicked -= ... (Nếu dùng lambda thì khó xóa, ở đây ta dùng OnDisable của MonoBehaviour)
        }
    }

    private void Update()
    {
        if (_inputLocked || GameController.Instance == null) return;
        if (GameController.Instance.CurrentState != GameState.Playing) return;
        
        // Keyboard Fallback
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

        Debug.Log($"[InputController] P1 chọn [{answerIndex}].");
        LocalMatchProvider.Instance?.SubmitAnswerP1(answerIndex);
    }

    public IEnumerator ShowAnswerFeedback(int correctAnswerIndex)
    {
        bool isCorrect = (_myLastAnswer == correctAnswerIndex);

        if (_myLastAnswer >= 0)
            SetButtonColor(_myLastAnswer, isCorrect ? _correctColor : _wrongColor);

        if (!isCorrect && correctAnswerIndex >= 0)
            SetButtonColor(correctAnswerIndex, _correctColor);

        OnAnswerSubmitted?.Invoke(_localPlayerId, _myLastAnswer, isCorrect);

        yield return new WaitForSeconds(1.5f);

        for (int i = 0; i < _answerButtons.Count; i++)
            SetButtonColor(i, _defaultColor);
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
        
        for (int i = 0; i < _answerButtons.Count && i < question.answers.Length; i++)
        {
            _answerButtons[i].text = question.answers[i];
            SetButtonColor(i, _defaultColor);
        }

        _inputLocked = false;
        _myLastAnswer = -1;
        SetButtonsInteractable(true);
    }

    public void SetPlayerId(int id) => _localPlayerId = id;
}
