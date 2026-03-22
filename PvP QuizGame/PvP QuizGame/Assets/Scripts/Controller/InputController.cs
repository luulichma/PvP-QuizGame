using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Xử lý input của Player 1 (người thật).
/// SAU KHI CHỌN: Khoá nút và báo lên LocalMatchProvider.
/// KHÔNG tự chấm điểm hay chuyển câu — GameController mới được làm điều đó
/// sau khi nhận event OnBothPlayersAnswered từ LocalMatchProvider.
///
/// *** SCALE LÊN ONLINE ***
/// Khi Firebase online: không cần sửa file này.
/// Chỉ cần thay LocalMatchProvider bằng FirebaseMatchProvider.
/// </summary>
public class InputController : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static InputController Instance { get; private set; }

    // ==================== EVENTS ====================
    /// <summary>Phát khi GameController xác nhận đáp án — (playerId, answerIndex, isCorrect)</summary>
    public static event Action<int, int, bool> OnAnswerSubmitted;

    // ==================== INSPECTOR ====================
    [Header("Nút đáp án (A=0, B=1, C=2, D=3)")]
    [SerializeField] private Button[] answerButtons = new Button[4];

    [Header("Text hiển thị trên nút")]
    [SerializeField] private Text[] answerTexts = new Text[4];

    [Header("Màu sắc phản hồi")]
    [SerializeField] private Color correctColor  = new Color(0.18f, 0.49f, 0.20f);
    [SerializeField] private Color wrongColor    = new Color(0.72f, 0.11f, 0.11f);
    [SerializeField] private Color defaultColor  = new Color(0.15f, 0.21f, 0.56f);

    [Header("Cài đặt")]
    [SerializeField] private float feedbackDuration = 1.5f;

    // ==================== INTERNAL ====================
    private bool _inputLocked = false;
    private int _localPlayerId = 1;
    private int _myLastAnswer = -1;

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (answerButtons != null)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null) continue;
                int index = i;
                answerButtons[i].onClick.AddListener(() => HandleAnswerClicked(index));
            }
        }

        GameController.OnGameStateChanged += HandleGameStateChanged;
        QuizManager.OnQuestionChanged     += HandleQuestionChanged;
    }

    private void OnDestroy()
    {
        GameController.OnGameStateChanged -= HandleGameStateChanged;
        QuizManager.OnQuestionChanged     -= HandleQuestionChanged;
    }

    // ==================== KEYBOARD FALLBACK (Editor testing) ====================
    private void Update()
    {
        if (_inputLocked || GameController.Instance == null) return;
        if (GameController.Instance.CurrentState != GameState.Playing) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) HandleAnswerClicked(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) HandleAnswerClicked(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) HandleAnswerClicked(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) HandleAnswerClicked(3);
    }

    // ==================== XỬ LÝ INPUT ====================
    private void HandleAnswerClicked(int answerIndex)
    {
        if (_inputLocked) return;
        _inputLocked = true;
        _myLastAnswer = answerIndex;
        SetButtonsInteractable(false);

        Debug.Log($"[InputController] P1 chọn [{answerIndex}]. Đang đợi đối thủ...");

        // Báo lên LocalMatchProvider. Không chấm điểm, không chuyển câu ở đây.
        LocalMatchProvider.Instance?.SubmitAnswerP1(answerIndex);
    }

    // ==================== VISUAL FEEDBACK (Gọi từ GameController) ====================
    /// <summary>Hiển thị phản hồi màu sắc. Được GameController gọi sau khi cả 2 người đã trả lời.</summary>
    public IEnumerator ShowAnswerFeedback(int correctAnswerIndex)
    {
        bool isCorrect = (_myLastAnswer == correctAnswerIndex);

        if (_myLastAnswer >= 0)
            SetButtonColor(_myLastAnswer, isCorrect ? correctColor : wrongColor);

        if (!isCorrect && correctAnswerIndex >= 0)
            SetButtonColor(correctAnswerIndex, correctColor);

        OnAnswerSubmitted?.Invoke(_localPlayerId, _myLastAnswer, isCorrect);

        yield return new WaitForSeconds(feedbackDuration);

        if (answerButtons != null)
        {
            for (int i = 0; i < answerButtons.Length; i++)
                SetButtonColor(i, defaultColor);
        }
    }

    // ==================== HELPERS ====================
    private void SetButtonsInteractable(bool on)
    {
        if (answerButtons == null) return;
        foreach (var btn in answerButtons)
        {
            if (btn != null) btn.interactable = on;
        }
    }

    private void SetButtonColor(int index, Color color)
    {
        if (answerButtons == null || index < 0 || index >= answerButtons.Length) return;
        if (answerButtons[index] == null) return;
        var image = answerButtons[index].GetComponent<Image>();
        if (image != null) image.color = color;
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
        Debug.Log($"[UI-LESS TEST] Câu hỏi mới hiện lên: {question.questionText}");
        
        // Cập nhật text các nút đáp án
        if (answerTexts != null)
        {
            for (int i = 0; i < answerTexts.Length && i < question.answers.Length; i++)
            {
                if (answerTexts[i] != null) answerTexts[i].text = question.answers[i];
                SetButtonColor(i, defaultColor);
                Debug.Log($"[UI-LESS TEST] Đáp án {i}: {question.answers[i]}");
            }
        }

        _inputLocked = false;
        _myLastAnswer = -1;
        SetButtonsInteractable(true);
    }

    /// <summary>Set ID người chơi (gọi từ FirebaseManager sau khi xác thực)</summary>
    public void SetPlayerId(int id) => _localPlayerId = id;
}
