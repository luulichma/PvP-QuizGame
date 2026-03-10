using UnityEngine;
using System;
using System.Collections;

public enum GameState
{
    Idle,       // Đang chờ bắt đầu
    Countdown,  // Đang đếm ngược 3-2-1 trước trận
    Playing,    // Trận đấu đang diễn ra
    GameOver    // Trận đấu kết thúc
}

/// <summary>
/// Singleton trung tâm điều phối trạng thái TRONG MỘT TRẬN ĐẤU.
/// Attach vào 1 GameObject "GameController" trong màn hình Gameplay.
/// </summary>
public class GameController : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static GameController Instance { get; private set; }

    // ==================== TRẠNG THÁI ====================
    public GameState CurrentState { get; private set; } = GameState.Idle;

    // ==================== EVENTS ====================
    /// <summary>Phát khi trạng thái game thay đổi</summary>
    public static event Action<GameState> OnGameStateChanged;

    /// <summary>Phát khi bắt đầu đếm ngược (giá trị: 3, 2, 1)</summary>
    public static event Action<int> OnCountdownTick;

    /// <summary>Phát khi trận đấu kết thúc</summary>
    public static event Action OnGameOver;

    // ==================== REFERENCES ====================
    [Header("Tham chiếu các Manager")]
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TimerController timerController;

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // KHÔNG dùng DontDestroyOnLoad vì GameController sẽ bị hủy khi rời khỏi màn hình Gameplay
    }

    private void Start()
    {
        // Lắng nghe các sự kiện kết thúc trận
        TimerController.OnTimerEnd += HandleTimerEnd;
        QuizManager.OnQuestionsExhausted += HandleQuestionsExhausted;

        // Vừa load xong Scene thi đấu, hệ thống chờ sự kiện được đăng ký xong (1 frame)
        // và lập tức tiến hành đếm ngược 3-2-1 để vào trận.
        StartCoroutine(StartGameDelayed());
    }

    private IEnumerator StartGameDelayed()
    {
        yield return null; // Đợi 1 frame để tất cả UI kịp đăng ký event
        StartGame();
    }

    private void OnDestroy()
    {
        TimerController.OnTimerEnd -= HandleTimerEnd;
        QuizManager.OnQuestionsExhausted -= HandleQuestionsExhausted;
    }

    // ==================== ĐIỀU PHỐI STATE ====================
    /// <summary>Chuyển sang trạng thái mới và thông báo cho các subscriber</summary>
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
                OnGameOver?.Invoke();
                break;
        }
    }

    /// <summary>Gọi để chuẩn bị vào trận đấu</summary>
    public void StartGame() => ChangeState(GameState.Countdown);

    /// <summary>Gọi từ nút "Chơi lại"</summary>
    public void RestartGame()
    {
        ChangeState(GameState.Idle);
        ChangeState(GameState.Countdown);
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
    private void HandleTimerEnd() => ChangeState(GameState.GameOver);
    private void HandleQuestionsExhausted() => ChangeState(GameState.GameOver);
}
