using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Xử lý toàn bộ input của 1 người chơi trên 1 thiết bị.
/// 4 nút đáp án A/B/C/D — có visual feedback xanh/đỏ sau khi chọn.
/// Keyboard fallback: phím 1/2/3/4 để test trong Unity Editor.
/// Attach vào GameObject chứa UI thi đấu.
/// </summary>
public class InputController : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static InputController Instance { get; private set; }

    // ==================== EVENTS ====================
    /// <summary>Phát khi người chơi chọn đáp án — (playerId, answerIndex, isCorrect)</summary>
    public static event Action<int, int, bool> OnAnswerSubmitted;

    // ==================== INSPECTOR ====================
    [Header("Nút đáp án (A=0, B=1, C=2, D=3)")]
    [SerializeField] private Button[] answerButtons = new Button[4];

    [Header("Text hiển thị trên nút")]
    [SerializeField] private Text[] answerTexts = new Text[4];

    [Header("Màu sắc phản hồi")]
    [SerializeField] private Color correctColor  = new Color(0.18f, 0.49f, 0.20f); // Xanh lá
    [SerializeField] private Color wrongColor    = new Color(0.72f, 0.11f, 0.11f); // Đỏ
    [SerializeField] private Color defaultColor  = new Color(0.15f, 0.21f, 0.56f); // Xanh navy
    [SerializeField] private Color disabledColor = new Color(0.4f,  0.4f,  0.4f);  // Xám

    [Header("Cài đặt")]
    [SerializeField] private float feedbackDuration = 1f;

    // ==================== INTERNAL ====================
    private bool _inputLocked = false;
    private int _localPlayerId = 1; // Sẽ được set từ FirebaseManager (Task 2.5)

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Gán sự kiện click cho 4 nút (capture index bằng biến cục bộ)
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.AddListener(() => HandleAnswerClicked(index));
        }

        // Subscribe game events
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        QuizManager.OnQuestionChanged  += HandleQuestionChanged;
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        QuizManager.OnQuestionChanged  -= HandleQuestionChanged;
    }

    // ==================== KEYBOARD FALLBACK ====================
    private void Update()
    {
        if (_inputLocked || GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

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
        SetButtonsInteractable(false);

        bool isCorrect = ScoreManager.Instance.CheckAnswer(_localPlayerId, answerIndex);
        OnAnswerSubmitted?.Invoke(_localPlayerId, answerIndex, isCorrect);

        StartCoroutine(ShowFeedbackRoutine(answerIndex, isCorrect));
    }

    // ==================== VISUAL FEEDBACK ====================
    private IEnumerator ShowFeedbackRoutine(int answerIndex, bool isCorrect)
    {
        // Đổi màu nút được chọn
        SetButtonColor(answerIndex, isCorrect ? correctColor : wrongColor);

        // Highlight đúng đáp án đúng nếu người chơi chọn sai
        if (!isCorrect)
        {
            int correct = QuizManager.Instance.CurrentQuestion.correctAnswerIndex;
            SetButtonColor(correct, correctColor);
        }

        yield return new WaitForSeconds(feedbackDuration);

        // Reset toàn bộ màu nút
        for (int i = 0; i < answerButtons.Length; i++)
            SetButtonColor(i, defaultColor);

        // Mở khoá input và chuyển câu hỏi tiếp
        _inputLocked = false;
        SetButtonsInteractable(true);
        QuizManager.Instance.NextQuestion();
    }

    // ==================== HELPERS ====================
    private void SetButtonsInteractable(bool interactable)
    {
        foreach (var btn in answerButtons)
            btn.interactable = interactable;
    }

    private void SetButtonColor(int index, Color color)
    {
        if (index < 0 || index >= answerButtons.Length) return;
        var image = answerButtons[index].GetComponent<Image>();
        if (image != null) image.color = color;
    }

    // ==================== EVENT HANDLERS ====================
    private void HandleGameStateChanged(GameState state)
    {
        bool isPlaying = state == GameState.Playing;
        SetButtonsInteractable(isPlaying);
        _inputLocked = !isPlaying;
    }

    private void HandleQuestionChanged(QuestionData question)
    {
        if (question == null) return;

        // Cập nhật text các nút đáp án
        for (int i = 0; i < answerTexts.Length && i < question.answers.Length; i++)
        {
            if (answerTexts[i] != null)
                answerTexts[i].text = question.answers[i];
            SetButtonColor(i, defaultColor);
        }

        _inputLocked = false;
        SetButtonsInteractable(true);
    }

    // ==================== CONFIG ====================
    /// <summary>Set ID người chơi (gọi từ FirebaseManager sau khi xác thực)</summary>
    public void SetPlayerId(int id) => _localPlayerId = id;
}
