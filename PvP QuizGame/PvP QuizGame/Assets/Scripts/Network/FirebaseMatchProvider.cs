using UnityEngine;
using System;
using Firebase.Database;
using Firebase.Extensions;

/// <summary>
/// [ONLINE LAYER] Đồng bộ hóa lượt chơi qua Firebase Realtime Database.
///
/// Trách nhiệm:
///  - Listen `rooms/{id}/answers` → khi cả 2 đã ghi → fire OnBothPlayersAnswered.
///  - Listen `rooms/{id}/scores/{oppUid}` → fire OnOpponentScoreUpdated.
///  - Listen `rooms/{id}/state` → khi state==ended → fire OnMatchEnded.
///  - Submit answer của mình lên `answers/{myUid}`.
///
/// Quan trọng: chỉ HOST mới được tăng currentQ và clear answers (xem GameController).
/// </summary>
public class FirebaseMatchProvider : MonoBehaviour
{
    public static FirebaseMatchProvider Instance { get; private set; }

    // Tham số: (p1Answer, p2Answer) — quy ước p1 = local player của client này
    public static event Action<int, int> OnBothPlayersAnswered;
    public static event Action<int> OnOpponentScoreUpdated;
    public static event Action<string> OnMatchEndedByRoom; // winner uid hoặc "draw"

    private DatabaseReference _answersRef;
    private DatabaseReference _scoresRef;
    private DatabaseReference _stateRef;

    private EventHandler<ValueChangedEventArgs> _answersHandler;
    private EventHandler<ValueChangedEventArgs> _scoresHandler;
    private EventHandler<ValueChangedEventArgs> _stateHandler;

    private bool _waitingForAnswers = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        QuizManager.OnQuestionChanged += HandleNewQuestion;
        FirebaseManager.OnMatchFound  += AttachRoomListeners;
    }

    private void OnDestroy()
    {
        QuizManager.OnQuestionChanged -= HandleNewQuestion;
        FirebaseManager.OnMatchFound  -= AttachRoomListeners;
        DetachRoomListeners();
    }

    // ==================== ROOM LISTENERS ====================
    private void AttachRoomListeners()
    {
        DetachRoomListeners(); // đề phòng

        var fm = FirebaseManager.Instance;
        if (fm == null || string.IsNullOrEmpty(fm.CurrentRoomId)) return;
        var roomRef = fm.GetRoomRef();
        if (roomRef == null) return;

        _answersRef = roomRef.Child("answers");
        _scoresRef  = roomRef.Child("scores");
        _stateRef   = roomRef.Child("state");

        _answersHandler = (s, args) => OnAnswersChanged(args);
        _scoresHandler  = (s, args) => OnScoresChanged(args);
        _stateHandler   = (s, args) => OnStateChanged(args);

        _answersRef.ValueChanged += _answersHandler;
        _scoresRef.ValueChanged  += _scoresHandler;
        _stateRef.ValueChanged   += _stateHandler;

        // [DEBUG] Set sẵn _waitingForAnswers ngay khi attach (đề phòng câu hỏi đầu fire OnQuestionChanged
        // trước khi listener kịp attach hoặc miss event)
        _waitingForAnswers = true;

        Debug.Log($"[FirebaseMatchProvider][DEBUG] Đã attach room listeners. Room={fm.CurrentRoomId}, " +
                  $"LocalUid={fm.LocalUserId}, OppUid={fm.OpponentId}, IsHost={fm.IsHost}, _waitingForAnswers=true");
    }

    private void DetachRoomListeners()
    {
        if (_answersRef != null && _answersHandler != null)
            _answersRef.ValueChanged -= _answersHandler;
        if (_scoresRef != null && _scoresHandler != null)
            _scoresRef.ValueChanged -= _scoresHandler;
        if (_stateRef != null && _stateHandler != null)
            _stateRef.ValueChanged -= _stateHandler;

        _answersHandler = null;
        _scoresHandler = null;
        _stateHandler = null;
        _answersRef = null;
        _scoresRef = null;
        _stateRef = null;
    }

    // ==================== HANDLERS ====================
    private void HandleNewQuestion(QuestionData question)
    {
        // Reset cờ chờ — đợi cả 2 answer cho câu mới
        _waitingForAnswers = true;
        Debug.Log($"[FirebaseMatchProvider][DEBUG] HandleNewQuestion → _waitingForAnswers=true. " +
                  $"Question='{question?.questionText}', frame={Time.frameCount}");
    }

    private void OnAnswersChanged(ValueChangedEventArgs args)
    {
        // [DEBUG] Log mọi lần listener fire
        var fmDbg = FirebaseManager.Instance;
        string snapDump = args.Snapshot != null && args.Snapshot.Exists
            ? args.Snapshot.GetRawJsonValue()
            : "(null/empty)";
        Debug.Log($"[FirebaseMatchProvider][DEBUG] OnAnswersChanged FIRE. _waitingForAnswers={_waitingForAnswers}, " +
                  $"snap={snapDump}, frame={Time.frameCount}");

        if (args.DatabaseError != null)
        {
            Debug.LogWarning($"[FirebaseMatchProvider][DEBUG] EARLY-RETURN: DatabaseError={args.DatabaseError.Message}");
            return;
        }
        if (!_waitingForAnswers)
        {
            Debug.Log("[FirebaseMatchProvider][DEBUG] EARLY-RETURN: _waitingForAnswers=false (đã được fire trước đó hoặc chưa set true)");
            return;
        }

        var fm = FirebaseManager.Instance;
        if (fm == null)
        {
            Debug.LogWarning("[FirebaseMatchProvider][DEBUG] EARLY-RETURN: FirebaseManager.Instance null");
            return;
        }

        var snap = args.Snapshot;
        if (snap == null)
        {
            Debug.LogWarning("[FirebaseMatchProvider][DEBUG] EARLY-RETURN: snapshot null");
            return;
        }

        // Chỉ tiếp tục khi CẢ 2 đã ghi đáp án
        bool hasLocal = snap.HasChild(fm.LocalUserId);
        bool hasOpp   = snap.HasChild(fm.OpponentId);
        if (!hasLocal || !hasOpp)
        {
            Debug.Log($"[FirebaseMatchProvider][DEBUG] EARLY-RETURN: chưa đủ 2 đáp án. " +
                      $"LocalUid={fm.LocalUserId} HasChild={hasLocal}, OppUid={fm.OpponentId} HasChild={hasOpp}");
            return;
        }

        string myRaw  = snap.Child(fm.LocalUserId).Value?.ToString();
        string oppRaw = snap.Child(fm.OpponentId).Value?.ToString();
        if (!int.TryParse(myRaw, out int myAns))
        {
            Debug.LogWarning($"[FirebaseMatchProvider][DEBUG] EARLY-RETURN: parse local answer fail. raw='{myRaw}'");
            return;
        }
        if (!int.TryParse(oppRaw, out int oppAns))
        {
            Debug.LogWarning($"[FirebaseMatchProvider][DEBUG] EARLY-RETURN: parse opp answer fail. raw='{oppRaw}'");
            return;
        }

        _waitingForAnswers = false;
        Debug.Log($"[FirebaseMatchProvider][DEBUG] >>> FIRE OnBothPlayersAnswered. Me={myAns}, Opp={oppAns}, frame={Time.frameCount}");

        // Quy ước: param1 = local player (P1), param2 = opponent
        OnBothPlayersAnswered?.Invoke(myAns, oppAns);

        // Host clear answers + advance currentQ (sau 1 độ trễ nhỏ để client kia nhận)
        if (fm.IsHost)
            Invoke(nameof(HostAdvance), 0.1f);
    }

    private void HostAdvance()
    {
        var fm = FirebaseManager.Instance;
        if (fm == null || !fm.IsHost) return;
        // GameController sẽ gọi NextQuestion → QuizManager.OnQuestionChanged sẽ fire
        // Nhưng currentQ trên Firebase cần host tự tăng:
        var qm = QuizManager.Instance;
        if (qm != null) fm.HostAdvanceQuestion(qm.AnsweredCount + 1);
    }

    private void OnScoresChanged(ValueChangedEventArgs args)
    {
        var fmDbg = FirebaseManager.Instance;
        string snapDump = args.Snapshot != null && args.Snapshot.Exists
            ? args.Snapshot.GetRawJsonValue()
            : "(null/empty)";
        Debug.Log($"[FirebaseMatchProvider][DEBUG] OnScoresChanged FIRE. snap={snapDump}, frame={Time.frameCount}");

        if (args.DatabaseError != null)
        {
            Debug.LogWarning($"[FirebaseMatchProvider][DEBUG] OnScoresChanged DatabaseError={args.DatabaseError.Message}");
            return;
        }
        var fm = FirebaseManager.Instance;
        if (fm == null) return;

        var oppNode = args.Snapshot.Child(fm.OpponentId);
        if (oppNode == null || oppNode.Value == null)
        {
            Debug.Log($"[FirebaseMatchProvider][DEBUG] OnScoresChanged: chưa có score cho OppUid={fm.OpponentId}");
            return;
        }
        if (int.TryParse(oppNode.Value.ToString(), out int oppScore))
        {
            Debug.Log($"[FirebaseMatchProvider][DEBUG] >>> FIRE OnOpponentScoreUpdated({oppScore})");
            OnOpponentScoreUpdated?.Invoke(oppScore);
        }
        else
        {
            Debug.LogWarning($"[FirebaseMatchProvider][DEBUG] OnScoresChanged: parse score fail. raw='{oppNode.Value}'");
        }
    }

    private void OnStateChanged(ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;
        if (args.Snapshot.Value == null) return;
        string state = args.Snapshot.Value.ToString();
        if (state == "ended")
        {
            // Đọc winner
            var fm = FirebaseManager.Instance;
            if (fm == null) return;
            fm.GetRoomRef()?.Child("winner").GetValueAsync().ContinueWithOnMainThread(t => {
                if (t.IsFaulted || !t.Result.Exists) return;
                string winner = t.Result.Value.ToString();
                OnMatchEndedByRoom?.Invoke(winner);
            });
        }
    }

    // ==================== API ====================
    /// <summary>
    /// InputController gọi khi P1 (local) chọn đáp án.
    /// </summary>
    public void SubmitAnswerP1(int answerIndex)
    {
        var fm = FirebaseManager.Instance;
        if (fm == null) { Debug.LogWarning("[FirebaseMatchProvider] FirebaseManager null."); return; }
        fm.SubmitMyAnswer(answerIndex);
        Debug.Log($"[FirebaseMatchProvider] Submit Answer = {answerIndex}");
    }
}
