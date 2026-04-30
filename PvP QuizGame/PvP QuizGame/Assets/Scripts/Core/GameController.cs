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
///
/// Online mode: lấy seed từ Firebase room (đồng bộ 2 client). Listen disconnect đối thủ.
/// Offline mode: seed random, dùng MockOpponent.
/// </summary>
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Idle;

    public static event Action<GameState> OnGameStateChanged;
    public static event Action<int> OnCountdownTick;
    public static event Action OnGameOver;
    public static event Action OnOpponentLeft;  // Được fire khi đối thủ rớt mạng

    [Header("Tham chiếu các Manager")]
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TimerController timerController;

    [Header("Cài đặt PvP")]
    [SerializeField] private float revealDuration = 2.5f;

    private bool _subscribedFirebase = false;
    private bool _subscribedLocal = false;
    private bool _isOnline = false;
    private bool _opponentLeft = false;
    private int _currentLocalAnswer = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        TimerController.OnTimerEnd            += HandleTimerEnd;
        QuizManager.OnQuestionsExhausted      += HandleQuestionsExhausted;

        // Xác định mode
        _isOnline = FirebaseManager.Instance != null
                  && !FirebaseManager.Instance.isOfflineMode
                  && FirebaseManager.Instance.IsConnected
                  && FirebaseManager.Instance.IsAuthenticated
                  && !string.IsNullOrEmpty(FirebaseManager.Instance.CurrentRoomId);

        if (_isOnline)
        {
            FirebaseMatchProvider.OnBothPlayersAnswered += HandleBothPlayersAnswered;
            FirebaseMatchProvider.OnMatchEndedByRoom    += HandleMatchEndedByRoom;
            FirebaseManager.OnOpponentDisconnected      += HandleOpponentDisconnected;
            _subscribedFirebase = true;
            Debug.Log($"[GameController] PvP Mode: ONLINE. Room={FirebaseManager.Instance.CurrentRoomId}, Host={FirebaseManager.Instance.IsHost}");
        }
        else
        {
            LocalMatchProvider.OnBothPlayersAnswered += HandleBothPlayersAnswered;
            _subscribedLocal = true;
            Debug.Log("[GameController] PvP Mode: OFFLINE (Local + Bot).");
        }

        StartCoroutine(StartGameDelayed());
    }

    private IEnumerator StartGameDelayed()
    {
        yield return null;

        if (LocalizationManager.Instance != null)
            yield return new WaitUntil(() => LocalizationManager.Instance.IsReady);

        StartGame();
    }

    private void OnDestroy()
    {
        TimerController.OnTimerEnd       -= HandleTimerEnd;
        QuizManager.OnQuestionsExhausted -= HandleQuestionsExhausted;

        if (_subscribedLocal)
            LocalMatchProvider.OnBothPlayersAnswered -= HandleBothPlayersAnswered;
        if (_subscribedFirebase)
        {
            FirebaseMatchProvider.OnBothPlayersAnswered -= HandleBothPlayersAnswered;
            FirebaseMatchProvider.OnMatchEndedByRoom    -= HandleMatchEndedByRoom;
            FirebaseManager.OnOpponentDisconnected      -= HandleOpponentDisconnected;
        }
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.Idle:
                scoreManager.ResetScores();
                _opponentLeft = false;
                break;
            case GameState.Countdown:
                StartCoroutine(CountdownRoutine());
                break;
            case GameState.Playing:
                _currentLocalAnswer = -1;
                timerController.StartTimer();
                StartCoroutine(StartQuizWithSeed());
                if (AudioManager.Instance != null) 
                    AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmGame);
                break;
            case GameState.GameOver:
                timerController.StopTimer();
                StartCoroutine(EndMatchRoutine());
                break;
        }
    }

    private IEnumerator StartQuizWithSeed()
    {
        int seed;
        int questionCount = 10; // Mặc định 10 câu

        if (_isOnline)
        {
            // Lấy seed và questionCount từ Firebase room (đồng bộ 2 client)
            var seedTask = FirebaseManager.Instance.ReadSeedFromRoom();
            var countTask = FirebaseManager.Instance.ReadQuestionCountFromRoom();
            
            while (!seedTask.IsCompleted || !countTask.IsCompleted) yield return null;
            
            seed = seedTask.Result;
            questionCount = countTask.Result;
            
            if (seed < 0)
            {
                Debug.LogWarning("[GameController] Không đọc được seed từ room — fallback random.");
                seed = (int)(System.DateTime.UtcNow.Ticks & 0x7FFFFFFF);
            }
            Debug.Log($"[GameController] Online seed: {seed}, questions: {questionCount}");
        }
        else
        {
            seed = (int)(System.DateTime.UtcNow.Ticks & 0x7FFFFFFF);
            
            // Tính số lượng câu hỏi dựa theo Level của local player
            if (PlayerDataManager.Instance != null && FirebaseManager.Instance != null)
            {
                int myLevel = PlayerDataManager.Instance.Data.level;
                int tier = FirebaseManager.Instance.GetPlayerTier(myLevel);
                questionCount = FirebaseManager.Instance.GetQuestionCountForTier(tier);
            }
            
            Debug.Log($"[GameController] Offline seed: {seed}, questions: {questionCount}");
        }
        quizManager.StartQuiz(seed, questionCount);
    }

    public void StartGame()   => ChangeState(GameState.Countdown);
    public void RestartGame() { ChangeState(GameState.Idle); ChangeState(GameState.Countdown); }

    // ==================== PVP ANSWER HANDLING ====================
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

        // Chấm điểm cho mình (P1 = local trong cả 2 mode)
        scoreManager.CheckAnswer(1, p1Answer);

        // Offline: chấm điểm cho bot ngay
        // Online: KHÔNG chấm cho P2 ở đây — đợi Firebase scores listener push qua ScoreManager.SetOpponentScore
        if (!_isOnline)
            scoreManager.CheckAnswer(2, p2Answer);

        if (InputController_UXML.Instance != null)
            yield return InputController_UXML.Instance.ShowAnswerFeedback(correctIdx);
        else
            yield return new WaitForSeconds(revealDuration);

        if (CurrentState == GameState.Playing)
        {
            // Reset đáp án cho câu tiếp theo
            _currentLocalAnswer = -1;
            
            if (quizManager.HasMoreQuestions())
            {
                quizManager.NextQuestion();
                timerController.StartTimer();
            }
            else
            {
                ChangeState(GameState.GameOver);
            }
        }
    }

    /// <summary>
    /// Gọi khi người chơi chủ động bấm thoát và xác nhận bỏ cuộc.
    /// </summary>
    public void ForcedSurrender()
    {
        if (CurrentState != GameState.Playing) return;
        
        Debug.LogWarning("[GameController] Người chơi đã đầu hàng!");
        
        // Xử thua ngay lập tức cho Player 1
        if (scoreManager != null) 
            scoreManager.SetForcedWinner(WinResult.Player2Wins);
            
        ChangeState(GameState.GameOver);
    }

    public void SetLocalAnswer(int answerIndex)
    {
        _currentLocalAnswer = answerIndex;
        Debug.Log($"[GameController] Đã ghi nhận đáp án local: {answerIndex}. Đợi hết giờ để chấm điểm...");
    }

    // ==================== ONLINE EVENT HANDLERS ====================
    private void HandleMatchEndedByRoom(string winnerUid)
    {
        // Server (host) đã đóng trận → cả 2 client cùng GameOver
        Debug.Log($"[GameController] Room báo trận kết thúc. Winner = {winnerUid}");
        if (CurrentState != GameState.GameOver)
            ChangeState(GameState.GameOver);
    }

    private void HandleOpponentDisconnected()
    {
        if (CurrentState == GameState.GameOver) return;
        Debug.LogWarning("[GameController] Đối thủ rớt mạng — bạn thắng!");
        _opponentLeft = true;
        
        // Luôn xử thắng cho người ở lại
        if (scoreManager != null) scoreManager.SetForcedWinner(WinResult.Player1Wins);

        OnOpponentLeft?.Invoke();
        ChangeState(GameState.GameOver);
    }

    // ==================== END MATCH ====================
    private IEnumerator EndMatchRoutine()
    {
        // Online: nếu là host và trận chưa được đánh dấu ended → host quyết định winner
        if (_isOnline && FirebaseManager.Instance != null && FirebaseManager.Instance.IsHost)
        {
            string winner = "draw";
            if (_opponentLeft)
            {
                winner = FirebaseManager.Instance.LocalUserId; // mình thắng do đối thủ rời
            }
            else
            {
                var result = scoreManager.GetWinner();
                if (result == WinResult.Player1Wins) winner = FirebaseManager.Instance.LocalUserId;
                else if (result == WinResult.Player2Wins) winner = FirebaseManager.Instance.OpponentId;
            }

            var task = FirebaseManager.Instance.HostEndMatch(winner);
            while (!task.IsCompleted) yield return null;
        }

        scoreManager.AwardRewards();

        // Online: đẩy profile (level, exp, money) lên cloud
        if (_isOnline && FirebaseManager.Instance != null)
        {
            var saveTask = FirebaseManager.Instance.SaveProfileToCloud();
            while (!saveTask.IsCompleted) yield return null;
        }

        OnGameOver?.Invoke();
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

    private void HandleTimerEnd()
    {
        if (CurrentState != GameState.Playing) return;
        
        Debug.Log("[GameController] Hết giờ! Đang chấm điểm...");
        
        // Khi hết giờ mới thực hiện chấm điểm và hiện feedback
        // P2 (đối thủ) trong mode online sẽ được ScoreManager tự cập nhật qua Firebase
        // Ở đây ta chỉ cần kích hoạt Reveal cho P1
        StartCoroutine(RevealAndAdvance(_currentLocalAnswer, -1)); // -1 cho p2 vì p2 đã có sync riêng
    }

    private void HandleQuestionsExhausted() 
    {
        // Không làm gì ở đây, GameController sẽ tự check HasMoreQuestions trong RevealAndAdvance
    }
}
