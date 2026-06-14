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

    // [PHASE-2] Theo economy-design v2.0 §4.1 — RP cố định, KHÔNG nhân Level Multiplier
    private const int WIN_RANK_POINTS = 30;
    private const int DRAW_RANK_POINTS = 10;
    private const int LOSE_RANK_POINTS = -15;
    private const int FORCED_LOSE_RANK_POINTS = -25; // Đầu hàng

    public int LastRewardMoney { get; private set; }
    public int LastRewardExp { get; private set; }
    public int LastRewardRankPoints { get; private set; }

    // [IM] Khi intermission, RP được bù bằng Money+EXP. Dùng cho popup kết quả + toast.
    public int LastIntermissionBonusMoney { get; private set; }
    public int LastIntermissionBonusExp { get; private set; }
    public bool LastWasIntermission { get; private set; }

    private WinResult? _forcedWinnerResult = null;
    private bool _didAnswerWrongThisMatch = false;

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
        int rankPointsAwarded = 0;

        if (result == WinResult.Player1Wins)
        {
            expAwarded = WIN_XP;
            moneyAwarded = WIN_MONEY;
            rankPointsAwarded = WIN_RANK_POINTS;

            // [PHASE-2] Daily Quests
            DailyQuestManager.Instance?.NotifyMatchWon();
            if (!_didAnswerWrongThisMatch)
                DailyQuestManager.Instance?.NotifyPerfectRound();
            
            // Achievement tracking
            bool isOffline = FirebaseManager.Instance != null && FirebaseManager.Instance.isOfflineMode;
            if (isOffline)
            {
                PlayerDataManager.Instance.Data.botWins++;
            }
            else
            {
                PlayerDataManager.Instance.Data.currentWinStreak++;
                if (PlayerDataManager.Instance.Data.currentWinStreak > PlayerDataManager.Instance.Data.highestWinStreak)
                {
                    PlayerDataManager.Instance.Data.highestWinStreak = PlayerDataManager.Instance.Data.currentWinStreak;
                }
            }
            
            if (!_didAnswerWrongThisMatch)
            {
                // Note: To simplify, we track perfect wins regardless of offline/online
                // Thêm field perfectWins vào PlayerData nếu chưa có
                // (Tôi sẽ tạo AchievementManager để check)
                if (AchievementManager.Instance != null)
                {
                    AchievementManager.Instance.RecordPerfectWin();
                }
            }
        }
        else if (result == WinResult.Draw)
        {
            expAwarded = DRAW_XP;
            moneyAwarded = DRAW_MONEY;
            rankPointsAwarded = DRAW_RANK_POINTS;
        }
        else
        {
            // Thua ép buộc (đầu hàng) -> 0 điểm thưởng
            if (_forcedWinnerResult == WinResult.Player2Wins)
            {
                expAwarded = 0;
                moneyAwarded = 0;
                rankPointsAwarded = FORCED_LOSE_RANK_POINTS; // [PHASE-2] -25 RP
            }
            else
            {
                expAwarded = LOSE_XP;
                moneyAwarded = LOSE_MONEY;
                rankPointsAwarded = LOSE_RANK_POINTS;
            }
            
            // Reset chuỗi thắng nếu không phải thắng (kể cả Draw) trong mode Online
            bool isOffline = FirebaseManager.Instance != null && FirebaseManager.Instance.isOfflineMode;
            if (!isOffline)
            {
                PlayerDataManager.Instance.Data.currentWinStreak = 0;
            }
        }

        // [IM] Khi intermission: KHÔNG cộng/trừ RP, bù bằng Money + EXP (tỷ lệ 1 RP = 2$ + 1 XP).
        // Chỉ bù khi RP dương (thắng/hòa). Loss/Surrender = 0 (không phạt RP, không bonus).
        bool isIntermission = SeasonManager.Instance != null && SeasonManager.Instance.IsIntermission;
        int bonusMoney = 0, bonusExp = 0;
        if (isIntermission)
        {
            if (rankPointsAwarded > 0)
            {
                bonusMoney = rankPointsAwarded * 2;
                bonusExp   = rankPointsAwarded * 1;
                moneyAwarded += bonusMoney;
                expAwarded   += bonusExp;
            }
            rankPointsAwarded = 0; // RP bị freeze trong intermission
        }
        LastIntermissionBonusMoney = bonusMoney;
        LastIntermissionBonusExp = bonusExp;
        LastWasIntermission = isIntermission;

        // Áp dụng Hệ số thưởng (Multiplier) dựa trên Level
        int playerLevel = PlayerDataManager.Instance.Data.level;
        float multiplier = 1.0f + (playerLevel * 0.1f);

        // [PHASE-2] Chỉ nhân Money & EXP. RP cố định — KHÔNG nhân multiplier
        // Lý do: RP phải phản ánh kỹ năng thuần túy, không phải thời gian đã chơi.
        if (expAwarded > 0) expAwarded = Mathf.RoundToInt(expAwarded * multiplier);
        if (moneyAwarded > 0) moneyAwarded = Mathf.RoundToInt(moneyAwarded * multiplier);
        // rankPointsAwarded GIỮ NGUYÊN — không nhân multiplier

        LastRewardExp = expAwarded;
        LastRewardMoney = moneyAwarded;
        LastRewardRankPoints = rankPointsAwarded;

        PlayerDataManager.Instance.Data.AddExp(expAwarded);
        PlayerDataManager.Instance.Data.AddMoney(moneyAwarded);
        PlayerDataManager.Instance.Data.AddRankPoints(rankPointsAwarded);

        // [PHASE-2] Cập nhật tier ngay sau khi RP thay đổi (hardcore demotion)
        PlayerDataManager.Instance.Data.RecomputeTier();

        PlayerDataManager.Instance.SaveData();

        // [PHASE-2] Daily Quest: chơi 1 trận (đếm bất kể kết quả)
        DailyQuestManager.Instance?.NotifyMatchPlayed();

        Debug.Log($"<color=cyan>[ScoreManager] Kết thúc: +{expAwarded} XP, +{moneyAwarded}$ tiền, {rankPointsAwarded} RP! (Hệ số: {multiplier}x){(isIntermission ? $" [INTERMISSION bonus: +{bonusMoney}$ +{bonusExp} XP]" : "")}</color>");
        
        // Gọi AchievementManager để check sau khi stats đã cập nhật
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.CheckAchievements();
        }
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
        _didAnswerWrongThisMatch = false;
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
                // [PHASE-2] Daily Quest: đúng 1 câu
                DailyQuestManager.Instance?.NotifyCorrectAnswer();
            }
            else
            {
                // [PHASE-2] Shield logic: nếu shield đang active → giữ streak, consume shield
                bool shieldSaved = PowerUpManager.Instance != null && PowerUpManager.Instance.IsShieldActive;
                if (shieldSaved)
                {
                    PowerUpManager.Instance.ConsumeShield();
                    Debug.Log("<color=cyan>[ScoreManager] Shield cứu streak khỏi 1 lần sai!</color>");
                    // Giữ nguyên CurrentStreak — KHÔNG raise OnStreakChanged
                }
                else
                {
                    CurrentStreak = 0;
                    OnStreakChanged?.Invoke(0);
                }
                _didAnswerWrongThisMatch = true;
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
