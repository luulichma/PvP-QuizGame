using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Xử lý input của Player 1 sử dụng UI Toolkit.
/// NÂNG CẤP: Ripple effect, Screen Shake, Haptic Feedback, cải tiến visual feedback.
/// </summary>
public class InputController_UXML : MonoBehaviour
{
    public static InputController_UXML Instance { get; private set; }

    public static event Action<int, int, bool> OnAnswerSubmitted;

    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;

    private List<Button> _answerButtons = new List<Button>();
    private Dictionary<Button, Action> _buttonHandlers = new Dictionary<Button, Action>();
    private string[] _originalAnswerTexts = new string[4];

    // Màu sắc phản hồi — Modern Glassmorphism palette
    private readonly Color _correctColor = new Color(0f, 0.9f, 0.46f);
    private readonly Color _wrongColor = new Color(1f, 0.2f, 0.27f);
    private readonly Color _selectedColor = new Color(1f, 0.84f, 0.28f, 0.8f); // Gold
    private readonly Color _defaultColor = new Color(0, 0.9f, 1f, 0.15f);

    // Original button colors
    private static readonly Color[] AnswerBaseColors = new Color[]
    {
        new Color(0.90f, 0.22f, 0.20f, 0.85f), // Red
        new Color(0.12f, 0.53f, 0.90f, 0.85f), // Blue
        new Color(1f, 0.70f, 0f, 0.85f),        // Yellow
        new Color(0.18f, 0.49f, 0.20f, 0.85f),  // Green
    };

    private bool _inputLocked = false;
    private int _localPlayerId = 1;
    private int _myLastAnswer = -1;

    // [PHASE-2] Tracking indexes đã bị ẩn bởi 50:50 cho câu hiện tại
    private readonly HashSet<int> _eliminatedByFiftyFifty = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) uiDocument = UnityEngine.Object.FindAnyObjectByType<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("[InputController_UXML] Không tìm thấy UIDocument!");
            return;
        }

        QueryButtons();
        if (_answerButtons.Count < 4)
            StartCoroutine(RetryQueryButtonsRoutine());

        GameController.OnGameStateChanged += HandleGameStateChanged;
        QuizManager.OnQuestionChanged     += HandleQuestionChanged;

        // [PHASE-2] Listen 50:50
        PowerUpManager.OnPowerUpUsed += HandlePowerUpUsed;
    }

    private IEnumerator RetryQueryButtonsRoutine()
    {
        for (int frame = 0; frame < 30; frame++)
        {
            yield return null;
            if (_answerButtons.Count >= 4) yield break;
            QueryButtons();
        }

        if (_answerButtons.Count < 4)
            Debug.LogError($"[InputController_UXML] Chỉ tìm thấy {_answerButtons.Count}/4 nút.");
    }

    private void OnDisable()
    {
        GameController.OnGameStateChanged -= HandleGameStateChanged;
        QuizManager.OnQuestionChanged     -= HandleQuestionChanged;
        PowerUpManager.OnPowerUpUsed      -= HandlePowerUpUsed; // [PHASE-2]

        foreach (var kv in _buttonHandlers)
        {
            if (kv.Key != null) kv.Key.clicked -= kv.Value;
        }
        _buttonHandlers.Clear();
    }

    // [PHASE-2] 50:50 — ẩn 2 đáp án sai khi user dùng power-up
    private void HandlePowerUpUsed(string id)
    {
        if (id != PowerUpManager.PU_5050) return;
        var question = QuizManager.Instance?.CurrentQuestion;
        if (question == null || _answerButtons.Count < 4) return;

        int correct = question.correctAnswerIndex;

        // Lấy danh sách index sai còn đang sống (chưa bị ẩn từ trước)
        var wrongIndexes = new List<int>();
        for (int i = 0; i < _answerButtons.Count; i++)
        {
            if (i == correct) continue;
            if (_eliminatedByFiftyFifty.Contains(i)) continue;
            if (_answerButtons[i] == null) continue;
            if (_answerButtons[i].style.display == DisplayStyle.None) continue;
            wrongIndexes.Add(i);
        }

        // Random 2 trong 3 đáp án sai để ẩn
        // Fisher-Yates partial shuffle
        for (int i = 0; i < wrongIndexes.Count; i++)
        {
            int swap = UnityEngine.Random.Range(i, wrongIndexes.Count);
            (wrongIndexes[i], wrongIndexes[swap]) = (wrongIndexes[swap], wrongIndexes[i]);
        }

        int eliminateCount = Mathf.Min(2, wrongIndexes.Count);
        for (int i = 0; i < eliminateCount; i++)
        {
            int idx = wrongIndexes[i];
            _eliminatedByFiftyFifty.Add(idx);
            var btn = _answerButtons[idx];
            if (btn == null) continue;
            btn.SetEnabled(false);
            if (!btn.ClassListContains("answer-eliminated"))
                btn.AddToClassList("answer-eliminated");
            
            // [FIX] Inline opacity from entry animation overrides USS. We must explicitly animate it out.
            btn.DOFade(0f, 0.3f);
            UIAnimator.DOScale(btn, new Vector2(0.8f, 0.8f), 0.3f);
        }

        Debug.Log($"[InputController_UXML] 50:50 ẩn {eliminateCount} đáp án sai (correct={correct}).");
    }

    private void Update()
    {
        if (_inputLocked || GameController.Instance == null) return;
        if (GameController.Instance.CurrentState != GameState.Playing) return;

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

        // HAPTIC: light tap khi chọn đáp án
        HapticFeedback.Light();

        // RIPPLE EFFECT trên nút đã chọn
        if (answerIndex >= 0 && answerIndex < _answerButtons.Count)
        {
            if (UIParticleEffect.Instance != null)
                UIParticleEffect.Instance.SpawnRipple(_answerButtons[answerIndex], _selectedColor);
        }

        // Visual: highlight nút đã chọn với border glow vàng
        SetButtonSelected(answerIndex);

        // Text "ĐANG ĐỢI..."
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

        if (GameController.Instance != null)
            GameController.Instance.SetLocalAnswer(answerIndex);
    }

    private void SetButtonSelected(int index)
    {
        if (index < 0 || index >= _answerButtons.Count) return;
        var btn = _answerButtons[index];
        if (btn == null) return;

        // Glow border animation
        btn.style.borderLeftColor = _selectedColor;
        btn.style.borderRightColor = _selectedColor;
        btn.style.borderLeftWidth = 4;
        btn.style.borderRightWidth = 4;

        // Subtle scale
        UIAnimator.DOScale(btn, new Vector2(1.03f, 1.03f), 0.15f).SetEase(Ease.OutBack);
    }

    public IEnumerator ShowAnswerFeedback(int correctAnswerIndex)
    {
        bool isCorrect = (_myLastAnswer == correctAnswerIndex);

        // Restore text gốc
        if (_myLastAnswer >= 0 && _myLastAnswer < _answerButtons.Count)
        {
            if (_originalAnswerTexts[_myLastAnswer] != null && _answerButtons[_myLastAnswer] != null)
                _answerButtons[_myLastAnswer].text = _originalAnswerTexts[_myLastAnswer];
        }

        if (_myLastAnswer >= 0 && _myLastAnswer < _answerButtons.Count)
        {
            if (isCorrect)
            {
                // CORRECT: Pulse glow xanh + haptic
                UIAnimator.DOCorrectFeedback(_answerButtons[_myLastAnswer]);
                HapticFeedback.Light();
            }
            else
            {
                // WRONG: Shake + flash đỏ + screen shake + haptic
                UIAnimator.DOWrongFeedback(_answerButtons[_myLastAnswer]);
                HapticFeedback.Medium();

                // SCREEN SHAKE: rung toàn bộ root
                if (uiDocument != null)
                {
                    var root = uiDocument.rootVisualElement;
                    if (root != null)
                        UIAnimator.DOScreenShake(root, 0.35f, 8f);
                }
            }
        }

        // Highlight đáp án đúng nếu user chọn sai
        if (!isCorrect && correctAnswerIndex >= 0 && correctAnswerIndex < _answerButtons.Count)
        {
            UIAnimator.DOCorrectFeedback(_answerButtons[correctAnswerIndex]);
        }

        // Score fly text khi đúng
        if (isCorrect && _myLastAnswer >= 0 && _myLastAnswer < _answerButtons.Count)
        {
            ShowScoreFlyText(_answerButtons[_myLastAnswer].parent ?? _answerButtons[_myLastAnswer], "+10");

            // Sparkle nhỏ trên nút đúng
            if (UIParticleEffect.Instance != null)
            {
                var particleLayer = uiDocument?.rootVisualElement?.Q<VisualElement>("particle-layer");
                if (particleLayer != null)
                    UIParticleEffect.Instance.SpawnSparkle(particleLayer, 50f, 75f, 8);
            }
        }

        OnAnswerSubmitted?.Invoke(_localPlayerId, _myLastAnswer, isCorrect);

        yield return new WaitForSeconds(1.5f);

        // Reset tất cả nút về trạng thái gốc
        for (int i = 0; i < _answerButtons.Count; i++)
        {
            if (_answerButtons[i] == null) continue;

            _answerButtons[i].style.backgroundColor = (i < AnswerBaseColors.Length) ? AnswerBaseColors[i] : _defaultColor;
            _answerButtons[i].style.scale = new StyleScale(new Scale(Vector2.one));

            // Reset border glow
            _answerButtons[i].style.borderLeftWidth = 4;
            _answerButtons[i].style.borderRightWidth = 0;
            _answerButtons[i].style.borderLeftColor = GetAnswerGlowColor(i);
            _answerButtons[i].style.borderRightColor = Color.clear;
        }
    }

    private Color GetAnswerGlowColor(int index)
    {
        return index switch
        {
            0 => new Color(1f, 0.32f, 0.32f, 0.5f),
            1 => new Color(0.27f, 0.54f, 1f, 0.5f),
            2 => new Color(1f, 0.84f, 0f, 0.5f),
            3 => new Color(0f, 0.90f, 0.46f, 0.5f),
            _ => Color.clear
        };
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
            btn.style.backgroundColor = new StyleColor(color);
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

        if (_answerButtons == null || _answerButtons.Count < 4)
            QueryButtons();

        if (_answerButtons.Count == 0)
        {
            Debug.LogWarning($"[InputController_UXML] HandleQuestionChanged nhưng _answerButtons rỗng!");
            return;
        }

        Debug.Log($"[InputController_UXML] Cập nhật {_answerButtons.Count} nút cho câu '{question.questionText}'");

        // [PHASE-2] Reset 50:50 eliminated state cho câu mới
        _eliminatedByFiftyFifty.Clear();

        for (int i = 0; i < _answerButtons.Count; i++)
        {
            if (i < question.answers.Length)
            {
                string ansKey = question.answers[i];
                string ansText = LocalizationManager.Instance != null
                                 ? LocalizationManager.Instance.GetText(ansKey)
                                 : ansKey;

                _answerButtons[i].text = ansText;
                _originalAnswerTexts[i] = ansText;
                _answerButtons[i].style.display = DisplayStyle.Flex;

                // Restore base color
                _answerButtons[i].style.backgroundColor = (i < AnswerBaseColors.Length) ? AnswerBaseColors[i] : _defaultColor;
                _answerButtons[i].style.borderLeftWidth = 4;
                _answerButtons[i].style.borderRightWidth = 0;
                _answerButtons[i].style.borderLeftColor = GetAnswerGlowColor(i);
                _answerButtons[i].style.borderRightColor = Color.clear;

                // [PHASE-2] Bỏ class "answer-eliminated" còn sót từ câu trước
                _answerButtons[i].RemoveFromClassList("answer-eliminated");
            }
            else
            {
                _answerButtons[i].style.display = DisplayStyle.None;
            }
        }

        _inputLocked = false;
        _myLastAnswer = -1;
        SetButtonsInteractable(true);

        // CASCADE ANIMATION
        UIAnimator.AnimateAnswersEntry(_answerButtons);
    }

    private void QueryButtons()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        if (root == null) return;

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

        for (int i = 0; i < _originalAnswerTexts.Length; i++)
            _originalAnswerTexts[i] = null;

        if (_answerButtons.Count > 0)
            Debug.Log($"[InputController_UXML] QueryButtons OK — {_answerButtons.Count}/4 nút.");
    }

    public void SetPlayerId(int id) => _localPlayerId = id;

    // Score fly text — "+10" bay lên
    private void ShowScoreFlyText(VisualElement parent, string text)
    {
        if (parent == null || uiDocument == null) return;

        var flyLabel = new Label(text);
        flyLabel.AddToClassList("score-fly");
        flyLabel.style.top = -30;
        flyLabel.style.left = Length.Percent(50);
        flyLabel.style.opacity = 1f;
        flyLabel.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(0)));
        flyLabel.pickingMode = PickingMode.Ignore;

        parent.Add(flyLabel);

        var seq = DOTween.Sequence();
        seq.Join(flyLabel.DOFade(0f, 0.9f).SetDelay(0.1f));
        seq.Join(UIAnimator.DOTranslate(flyLabel, new Vector2(0, -70), 0.9f).SetEase(Ease.OutCubic));
        seq.Join(UIAnimator.DOScale(flyLabel, new Vector2(1.3f, 1.3f), 0.3f).SetEase(Ease.OutBack));
        seq.OnComplete(() => {
            if (flyLabel?.parent != null) flyLabel.RemoveFromHierarchy();
        });
    }
}
