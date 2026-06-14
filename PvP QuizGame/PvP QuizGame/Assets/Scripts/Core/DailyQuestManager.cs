using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PHASE-2] Daily Quest tracker (economy-design v2.0 §7).
/// Quest list:
///   QUEST_PLAY_3    — Chơi 3 trận            → 50$
///   QUEST_WIN_1     — Thắng 1 trận           → 100$
///   QUEST_CORRECT_15 — Đúng 15 câu           → 75$
///   QUEST_PERFECT   — 1 trận không sai câu nào → 200$
///
/// Reset 00:00 UTC mỗi ngày. Reward claim thủ công (user bấm nút).
/// Lưu state JSON trong PlayerData.dailyQuestsData.
/// </summary>
public class DailyQuestManager : MonoBehaviour
{
    private static DailyQuestManager _instance;
    public static DailyQuestManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DailyQuestManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[DailyQuestManager]");
                    _instance = go.AddComponent<DailyQuestManager>();
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    public const string QUEST_PLAY_3    = "play_3";
    public const string QUEST_WIN_1     = "win_1";
    public const string QUEST_CORRECT_15 = "correct_15";
    public const string QUEST_PERFECT   = "perfect";

    public static event Action OnQuestsChanged;     // bất kỳ progress hoặc reset
    public static event Action<string, int> OnQuestClaimed; // (questId, reward)

    [Serializable]
    public class QuestState
    {
        public int progress;
        public bool claimed;
    }

    [Serializable]
    private class QuestSaveDTO
    {
        public string date; // yyyy-MM-dd UTC
        public List<string> ids = new List<string>();
        public List<int> progresses = new List<int>();
        public List<bool> claims = new List<bool>();
    }

    private readonly Dictionary<string, QuestState> _state = new Dictionary<string, QuestState>();
    private string _currentDateUtc; // yyyy-MM-dd

    // Quest definitions
    private static readonly (string id, int target, int reward)[] Quests = new[]
    {
        (QUEST_PLAY_3,    3,  50),
        (QUEST_WIN_1,     1,  100),
        (QUEST_CORRECT_15, 15, 75),
        (QUEST_PERFECT,   1,  200),
    };

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadOrReset();
    }

    private void Update()
    {
        // Auto reset khi qua ngày mới UTC
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (today != _currentDateUtc)
        {
            LoadOrReset(true);
        }
    }

    // ==================== PUBLIC API ====================

    public int GetProgress(string id) => _state.TryGetValue(id, out var s) ? s.progress : 0;
    public bool IsClaimed(string id) => _state.TryGetValue(id, out var s) && s.claimed;
    public int GetTarget(string id)
    {
        foreach (var q in Quests) if (q.id == id) return q.target;
        return 1;
    }
    public int GetReward(string id)
    {
        foreach (var q in Quests) if (q.id == id) return q.reward;
        return 0;
    }
    public bool IsComplete(string id) => GetProgress(id) >= GetTarget(id);

    public IEnumerable<string> AllQuestIds()
    {
        foreach (var q in Quests) yield return q.id;
    }

    public TimeSpan TimeUntilReset =>
        DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;

    // ==================== TRACKING HOOKS ====================
    public void NotifyMatchPlayed()
    {
        AddProgress(QUEST_PLAY_3, 1);
    }

    public void NotifyMatchWon()
    {
        AddProgress(QUEST_WIN_1, 1);
    }

    public void NotifyCorrectAnswer()
    {
        AddProgress(QUEST_CORRECT_15, 1);
    }

    public void NotifyPerfectRound()
    {
        AddProgress(QUEST_PERFECT, 1);
    }

    private void AddProgress(string id, int amount)
    {
        if (!_state.TryGetValue(id, out var s))
        {
            s = new QuestState();
            _state[id] = s;
        }
        s.progress += amount;
        Persist();
        OnQuestsChanged?.Invoke();
    }

    /// <summary>User bấm "Nhận" — claim reward.</summary>
    public bool TryClaim(string id)
    {
        if (!_state.TryGetValue(id, out var s)) return false;
        if (s.claimed) return false;
        if (s.progress < GetTarget(id)) return false;

        int reward = GetReward(id);
        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null) return false;

        pd.AddMoney(reward);
        s.claimed = true;
        Persist();

        Debug.Log($"<color=green>[DailyQuest] Claimed {id} → +{reward}$</color>");
        OnQuestClaimed?.Invoke(id, reward);
        OnQuestsChanged?.Invoke();

        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsAuthenticated)
            _ = FirebaseManager.Instance.SaveProfileToCloud();
        return true;
    }

    // ==================== PERSIST ====================

    private void LoadOrReset(bool forceReset = false)
    {
        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null) return;

        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        bool needReset = forceReset;
        QuestSaveDTO dto = null;

        if (!string.IsNullOrEmpty(pd.dailyQuestsData))
        {
            try { dto = JsonUtility.FromJson<QuestSaveDTO>(pd.dailyQuestsData); }
            catch { dto = null; }
        }

        if (dto == null || dto.date != today) needReset = true;

        _state.Clear();
        if (needReset)
        {
            foreach (var q in Quests) _state[q.id] = new QuestState();
        }
        else
        {
            for (int i = 0; i < dto.ids.Count; i++)
            {
                _state[dto.ids[i]] = new QuestState
                {
                    progress = i < dto.progresses.Count ? dto.progresses[i] : 0,
                    claimed = i < dto.claims.Count && dto.claims[i]
                };
            }
            // Đảm bảo tất cả quest ID đều có entry
            foreach (var q in Quests)
                if (!_state.ContainsKey(q.id)) _state[q.id] = new QuestState();
        }

        _currentDateUtc = today;

        if (needReset) Persist();
        OnQuestsChanged?.Invoke();
    }

    private void Persist()
    {
        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null) return;

        var dto = new QuestSaveDTO { date = _currentDateUtc };
        foreach (var kv in _state)
        {
            dto.ids.Add(kv.Key);
            dto.progresses.Add(kv.Value.progress);
            dto.claims.Add(kv.Value.claimed);
        }
        pd.dailyQuestsData = JsonUtility.ToJson(dto);
        PlayerDataManager.Instance.SaveData();
    }
}
