using UnityEngine;
using System;

/// <summary>
/// Kết quả cuối trận
/// </summary>
public enum WinResult { Player1Wins, Player2Wins, Draw }

/// <summary>
/// Quản lý điểm số của 2 người chơi trong trận đấu.
/// Logic: Đúng +10 điểm | Sai +0 điểm.
/// Attach vào cùng GameObject với GameController.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static ScoreManager Instance { get; private set; }

    // ==================== EVENTS ====================
    /// <summary>Phát khi điểm thay đổi — tham số: (p1Score, p2Score)</summary>
    public static event Action<int, int> OnScoreChanged;

    // ==================== ĐIỂM SỐ ====================
    public int Player1Score { get; private set; }
    public int Player2Score { get; private set; }

    private const int CORRECT_POINTS = 10;
    private const int WRONG_POINTS   = 0;

    // ==================== THƯỞNG ====================
    private const int WIN_XP = 50;
    private const int DRAW_XP = 20;
    private const int LOSE_XP = 10;

    private const int WIN_MONEY = 100;
    private const int DRAW_MONEY = 40;
    private const int LOSE_MONEY = 10;

    // ==================== LIFECYCLE ====================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ==================== API CÔNG KHAI ====================
    
    /// <summary>
    /// Tính toán và trao thưởng cho người chơi dựa trên kết quả trận đấu.
    /// </summary>
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
            expAwarded = LOSE_XP;
            moneyAwarded = LOSE_MONEY;
        }

        PlayerDataManager.Instance.Data.AddExp(expAwarded);
        PlayerDataManager.Instance.Data.AddMoney(moneyAwarded);
        PlayerDataManager.Instance.SaveData();

        Debug.Log($"<color=cyan>[ScoreManager] Kết thúc: Nhận {expAwarded} XP và {moneyAwarded}$ tiền thưởng!</color>");
    }

    /// <summary>Reset điểm về 0 — gọi khi bắt đầu trận mới</summary>
    public void ResetScores()
    {
        Player1Score = 0;
        Player2Score = 0;
        OnScoreChanged?.Invoke(Player1Score, Player2Score);
    }

    /// <summary>
    /// Kiểm tra đáp án của người chơi và cộng điểm nếu đúng.
    /// </summary>
    /// <param name="playerId">1 hoặc 2</param>
    /// <param name="answerIndex">Chỉ số đáp án người chơi chọn (0-3)</param>
    /// <returns>true nếu đúng, false nếu sai</returns>
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

        Debug.Log($"[ScoreManager] Player{playerId} chọn [{answerIndex}] — " +
                  $"{(isCorrect ? $"ĐÚNG +{points}đ" : "SAI +0đ")} " +
                  $"| P1:{Player1Score} P2:{Player2Score}");

        return isCorrect;
    }

    /// <summary>Cộng điểm trực tiếp (dùng khi nhận điểm từ Firebase)</summary>
    public void SetOpponentScore(int opponentScore)
    {
        // TODO (Task 2.5): Cập nhật điểm đối thủ từ Firebase
        Player2Score = opponentScore;
        OnScoreChanged?.Invoke(Player1Score, Player2Score);
    }

    /// <summary>Thêm điểm cho một người chơi</summary>
    public void AddScore(int playerId, int points)
    {
        if (playerId == 1) Player1Score += points;
        else if (playerId == 2) Player2Score += points;
        OnScoreChanged?.Invoke(Player1Score, Player2Score);
    }

    /// <summary>Xác định người thắng cuối trận</summary>
    public WinResult GetWinner()
    {
        if (Player1Score > Player2Score) return WinResult.Player1Wins;
        if (Player2Score > Player1Score) return WinResult.Player2Wins;
        return WinResult.Draw;
    }
}
