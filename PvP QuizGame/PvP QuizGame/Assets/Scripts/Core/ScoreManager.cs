using UnityEngine;
using System;

public enum WinResult { Player1Wins, Player2Wins, Draw }

/// <summary>
/// Quản lý điểm số 2 người chơi.
///
/// Online mode: P1 chấm điểm local + push lên Firebase. P2 nhận qua FirebaseMatchProvider.OnOpponentScoreUpdated.
/// Offline mode: tự chấm cả P1 + P2.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public static event Action<int, int> OnScoreChanged;
    // UX-01: Streak tracking
    public static event Action<int> OnStreakChanged; // streak count
    public int CurrentStreak { get; private set; }

    public int Player1Score { get; private set; }
    public int Player2Score { get; private set; }

    private const int CORRECT_POINTS = 10;
    private const int WRONG_POINTS   = 0;

    private const int WIN_XP = 50;
    private const int DRAW_XP = 20;
    private const int LOSE_XP = 10;

    private const int WIN_MONEY = 100;
    private const int DRAW_MONEY = 40;
    private const int LOSE_MONEY = 10;

    public int LastRewardMoney { get; private set; }
    public int LastRewardExp { get; private set; }

    private WinResult? _forcedWinnerResult = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        FirebaseMatchProvider.OnOpponentScoreUpdated += SetOpponentScore;
    }

    private void OnDestroy()
    {
        FirebaseMatchProvider.OnOpponentScoreUpdated -= SetOpponentScore;
    }

    public void AwardRewards()
    {
        if (PlayerDataManager.Instance == null) return;

        var result = GetWinner();
        int expAwarded = 0;
        int moneyAwarded = 0;

        if (result == WinResult.Player1Wins)
        {
            expAwarded = WIN_XP;
            moneyAwarded = WIN_MONEY;
        }
        else if (result == WinResult.Draw)
        {
            expAwarded = DRAW_XP;
            moneyAwarded = DRAW_MONEY;
        }
        else
        {
            // Thua ép buộc (đầu hàng) -> 0 điểm thưởng
            if (_forcedWinnerResult == WinResult.Player2Wins)
            {
                expAwarded = 0;
                moneyAwarded = 0;
            }
            else
            {
                expAwarded = LOSE_XP;
                moneyAwarded = LOSE_MONEY;
            }
        }

        LastRewardExp = expAwarded;
        LastRewardMoney = moneyAwarded;

        PlayerDataManager.Instance.Data.AddExp(expAwarded);
        PlayerDataManager.Instance.Data.AddMoney(moneyAwarded);
        PlayerDataManager.Instance.SaveData();

        Debug.Log($"<color=cyan>[ScoreManager] Kết thúc: +{expAwarded} XP và +{moneyAwarded}$ tiền thưởng!</color>");
    }

    public void SetForcedWinner(WinResult result)
    {
        _forcedWinnerResult = result;
    }

    public void ResetScores()
    {
        Player1Score = 0;
        Player2Score = 0;
        _forcedWinnerResult = null;
        CurrentStreak = 0;
        OnScoreChanged?.Invoke(Player1Score, Player2Score);
    }

    /// <summary>
    /// Chấm điểm + cộng. Online: với playerId=1, push lên Firebase.
    /// </summary>
    public bool CheckAnswer(int playerId, int answerIndex)
    {
        var question = QuizManager.Instance?.CurrentQuestion;
        if (question == null)
        {
            Debug.LogWarning("[ScoreManager] Không có câu hỏi hiện tại.");
            return false;
        }

        bool isCorrect = answerIndex == question.correctAnswerIndex;
        int points = isCorrect ? CORRECT_POINTS : WRONG_POINTS;

        if (points > 0) AddScore(playerId, points);

        // UX-01: Cập nhật streak
        if (playerId == 1)
        {
            if (isCorrect)
            {
                CurrentStreak++;
                OnStreakChanged?.Invoke(CurrentStreak);
            }
            else
            {
                CurrentStreak = 0;
                OnStreakChanged?.Invoke(0);
            }
        }

        // Phát âm thanh đúng/sai cho người chơi hiện tại
        if (playerId == 1 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(isCorrect ? AudioManager.Instance.correctSound : AudioManager.Instance.wrongSound);
        }

        Debug.Log($"[ScoreManager] Player{playerId} chọn [{answerIndex}] — {(isCorrect ? $"ĐÚNG +{points}đ" : "SAI")}. P1:{Player1Score} P2:{Player2Score}");
        return isCorrect;
    }

    /// <summary>
    /// Cập nhật điểm P2 từ Firebase (trong online mode). Không trigger push ngược lại.
    /// </summary>
    public void SetOpponentScore(int opponentScore)
    {
        Player2Score = opponentScore;
        OnScoreChanged?.Invoke(Player1Score, Player2Score);
    }

    public void AddScore(int playerId, int points)
    {
        if (playerId == 1)
        {
            Player1Score += points;
            // Online: push lên Firebase để đối thủ thấy
            if (FirebaseManager.Instance != null
                && !FirebaseManager.Instance.isOfflineMode
                && FirebaseManager.Instance.IsConnected
                && !string.IsNullOrEmpty(FirebaseManager.Instance.CurrentRoomId))
            {
                FirebaseManager.Instance.UpdateMyScore(Player1Score);
            }
        }
        else if (playerId == 2)
        {
            Player2Score += points;
        }
        OnScoreChanged?.Invoke(Player1Score, Player2Score);
    }

    public WinResult GetWinner()
    {
        if (_forcedWinnerResult.HasValue) return _forcedWinnerResult.Value;

        if (Player1Score > Player2Score) return WinResult.Player1Wins;
        if (Player2Score > Player1Score) return WinResult.Player2Wins;
        return WinResult.Draw;
    }
}
