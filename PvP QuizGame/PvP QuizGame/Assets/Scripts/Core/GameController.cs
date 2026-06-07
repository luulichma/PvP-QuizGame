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
    /// <summary>UX-01: Fire sau mỗi câu — thông báo đối thủ đúng hay sai (true = đúng)</summary>
    public static event Action<bool> OnOpponentAnswerResult;
    /// <summary>UX-03: Fire sau mỗi câu — tham số (p1Correct, p2Correct, p1Score, p2Score, isLastQuestion)</summary>
    public static event Action<bool, bool, int, int, bool> OnTurnSummary;

    // BUG-03: Online AFK timeout coroutine
    private Coroutine _afkTimeoutCoroutine = null;
    private const float AFK_TIMEOUT_EXTRA = 5f; // Thêm 5s sau QuestionDuration trước khi auto-submit -1 cho P2

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
    // BUG-02: Lưu answer của P2 cho offline mode
    private int _currentP2Answer = -1;
    // BUG-01: Flag chống reveal trùng
    private bool _isRevealing = false;

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
                _currentP2Answer = -1;
                _isRevealing = false;
                timerController.StartTimer();
                StartCoroutine(StartQuizWithSeed());
                if (AudioManager.Instance != null) 
                    AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmGame);
                // BUG-03: Bắt đầu AFK timeout cho online mode
                if (_isOnline)
                {
                    if (_afkTimeoutCoroutine != null) StopCoroutine(_afkTimeoutCoroutine);
                    _afkTimeoutCoroutine = StartCoroutine(AfkTimeoutRoutine());
                }
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
        // BUG-02: Lưu P2 answer để HandleTimerEnd dùng
        _currentP2Answer = p2Answer;
        StartCoroutine(RevealAndAdvance(p1Answer, p2Answer));
    }

    private IEnumerator RevealAndAdvance(int p1Answer, int p2Answer)
    {
        // BUG-01: Tránh reveal trùng lặp
        if (_isRevealing) yield break;
        _isRevealing = true;

        // BUG-01: Dừng timer ngay khi bắt đầu reveal
        timerController.StopTimer();

        // BUG-03: Hủy AFK timeout
        if (_afkTimeoutCoroutine != null)
        {
            StopCoroutine(_afkTimeoutCoroutine);
            _afkTimeoutCoroutine = null;
        }

        var question = quizManager.CurrentQuestion;
        if (question == null) { _isRevealing = false; yield break; }

        int correctIdx = question.correctAnswerIndex;

        // Chấm điểm cho mình (P1 = local trong cả 2 mode)
        scoreManager.CheckAnswer(1, p1Answer);

        // Offline: chấm điểm cho bot ngay
        // Online: KHÔNG chấm cho P2 ở đây — đợi Firebase scores listener push qua ScoreManager.SetOpponentScore
        if (!_isOnline)
            scoreManager.CheckAnswer(2, p2Answer);

        // UX-01: Thông báo UI đối thủ đúng hay sai
        bool isOpponentCorrect = (p2Answer == correctIdx);
        OnOpponentAnswerResult?.Invoke(isOpponentCorrect);

        if (InputController_UXML.Instance != null)
            yield return InputController_UXML.Instance.ShowAnswerFeedback(correctIdx);
        else
            yield return new WaitForSeconds(revealDuration);

        if (CurrentState == GameState.Playing)
        {
            bool isCorrect = (p1Answer == correctIdx);
            bool hasMore = quizManager.HasMoreQuestions();

            // UX-03: Fire turn summary event before advancing
            OnTurnSummary?.Invoke(isCorrect, isOpponentCorrect, scoreManager.Player1Score, scoreManager.Player2Score, !hasMore);
            yield return new WaitForSeconds(3.0f); // Chờ đủ lâu để người chơi nhìn turn summary popup

            // Reset đáp án cho câu tiếp theo
            _currentLocalAnswer = -1;
            _currentP2Answer = -1;

            if (hasMore)
            {
                quizManager.NextQuestion();
                timerController.StartTimer();

                // BUG-03 FIX: Restart AFK timeout cho câu hỏi tiếp theo
                if (_isOnline)
                {
                    if (_afkTimeoutCoroutine != null) StopCoroutine(_afkTimeoutCoroutine);
                    _afkTimeoutCoroutine = StartCoroutine(AfkTimeoutRoutine());
                }
            }
            else
            {
                ChangeState(GameState.GameOver);
            }
        }

        _isRevealing = false;
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

        // Bất kể chơi online hay offline, nếu đã đăng nhập Firebase thì đẩy profile (level, exp, money) lên cloud
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsConnected && FirebaseManager.Instance.IsAuthenticated)
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
        // UX-04: Fire 0 = "GO!"
        OnCountdownTick?.Invoke(0);
        yield return new WaitForSeconds(0.5f);
        ChangeState(GameState.Playing);
    }

    private void HandleTimerEnd()
    {
        if (CurrentState != GameState.Playing) return;
        
        Debug.Log("[GameController] Hết giờ! Đang chấm điểm...");
        
        // BUG-02: Dùng _currentP2Answer thay vì hardcode -1 để bot offline không bị tính sai
        // Online: nếu P2 chưa trả lời, _currentP2Answer vẫn là -1 (mặc định sai)
        StartCoroutine(RevealAndAdvance(_currentLocalAnswer, _currentP2Answer));
    }

    // BUG-03: AFK timeout cho online mode — nếu P2 không trả lời sau QuestionDuration + 5s, auto submit -1
    private IEnumerator AfkTimeoutRoutine()
    {
        float timeout = timerController.TotalTime + AFK_TIMEOUT_EXTRA;
        yield return new WaitForSeconds(timeout);
        
        if (CurrentState == GameState.Playing)
        {
            Debug.LogWarning("[GameController] P2 (Opponent) không trả lời kịp — auto submit -1.");
            HandleBothPlayersAnswered(_currentLocalAnswer, -1);
        }
    }

    private void HandleQuestionsExhausted() 
    {
        // Không làm gì ở đây, GameController sẽ tự check HasMoreQuestions trong RevealAndAdvance
    }
}
