using UnityEngine;
using System;
using System.Collections;

public enum GameState
{
    Idle,
    Countdown,
    Playing,
    GameOver
}

/// <summary>
/// Singleton trung tâm điều phối trạng thái TRONG MỘT TRẬN ĐẤU.
/// Attach vào 1 GameObject "GameController" trong màn hình Gameplay.
///
/// THAY ĐỔI PVP: Giờ đây lắng nghe LocalMatchProvider.OnBothPlayersAnswered
/// để chấm điểm & điều hướng câu hỏi mới, thay vì để InputController tự làm.
/// </summary>
public class GameController : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static GameController Instance { get; private set; }

    // ==================== TRẠNG THÁI ====================
    public GameState CurrentState { get; private set; } = GameState.Idle;

    // ==================== EVENTS ====================
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<int> OnCountdownTick;
    public static event Action OnGameOver;

    // ==================== REFERENCES ====================
    [Header("Tham chiếu các Manager")]
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TimerController timerController;

    [Header("Cài đặt PvP")]
    [SerializeField] private float revealDuration = 2.5f; // Thời gian chiếu đáp án xanh/đỏ

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        TimerController.OnTimerEnd            += HandleTimerEnd;
        QuizManager.OnQuestionsExhausted      += HandleQuestionsExhausted;
        LocalMatchProvider.OnBothPlayersAnswered += HandleBothPlayersAnswered;

        StartCoroutine(StartGameDelayed());
    }

    private IEnumerator StartGameDelayed()
    {
        yield return null; // Đợi 1 frame để UI đăng ký event kịp
        StartGame();
    }

    private void OnDestroy()
    {
        TimerController.OnTimerEnd               -= HandleTimerEnd;
        QuizManager.OnQuestionsExhausted         -= HandleQuestionsExhausted;
        LocalMatchProvider.OnBothPlayersAnswered -= HandleBothPlayersAnswered;
    }

    // ==================== ĐIỀU PHỐI STATE ====================
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.Idle:
                scoreManager.ResetScores();
                break;
            case GameState.Countdown:
                StartCoroutine(CountdownRoutine());
                break;
            case GameState.Playing:
                timerController.StartTimer();
                quizManager.StartQuiz();
                break;
            case GameState.GameOver:
                timerController.StopTimer();
                scoreManager.AwardRewards();
                OnGameOver?.Invoke();
                break;
        }
    }

    public void StartGame()   => ChangeState(GameState.Countdown);
    public void RestartGame() { ChangeState(GameState.Idle); ChangeState(GameState.Countdown); }

    // ==================== PVP ANSWER HANDLING ====================
    /// <summary>
    /// Called by LocalMatchProvider khi CẢ 2 người đã nộp bài.
    /// Đây là trung tâm điều phối PvP: chấm điểm, hiển thị feedback, chuyển câu.
    /// </summary>
    private void HandleBothPlayersAnswered(int p1Answer, int p2Answer)
    {
        if (CurrentState != GameState.Playing) return;
        StartCoroutine(RevealAndAdvance(p1Answer, p2Answer));
    }

    private IEnumerator RevealAndAdvance(int p1Answer, int p2Answer)
    {
        var question = quizManager.CurrentQuestion;
        if (question == null) yield break;

        int correctIdx = question.correctAnswerIndex;

        // Chấm điểm
        scoreManager.CheckAnswer(1, p1Answer);
        scoreManager.CheckAnswer(2, p2Answer);

        // Yêu cầu InputController hiển thị animation màu xanh/đỏ
        if (InputController.Instance != null)
            yield return InputController.Instance.ShowAnswerFeedback(correctIdx);
        else
            yield return new WaitForSeconds(revealDuration);

        // Chuyển câu hỏi tiếp theo
        if (CurrentState == GameState.Playing)
            quizManager.NextQuestion();
    }

    // ==================== COROUTINES ====================
    private IEnumerator CountdownRoutine()
    {
        for (int i = 3; i >= 1; i--)
        {
            OnCountdownTick?.Invoke(i);
            yield return new WaitForSeconds(1f);
        }
        ChangeState(GameState.Playing);
    }

    // ==================== EVENT HANDLERS ====================
    private void HandleTimerEnd()         => ChangeState(GameState.GameOver);
    private void HandleQuestionsExhausted() => ChangeState(GameState.GameOver);
}
