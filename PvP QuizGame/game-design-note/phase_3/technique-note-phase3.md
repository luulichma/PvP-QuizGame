# 🔧 Technique Note — Phase 3: Rogue-like Tower Implementation

> **Phiên bản:** 1.0  
> **Ngày tạo:** 2026-06-18  
> **Tác giả:** Solo Dev  
> **Trọng tâm:** Kiến trúc kỹ thuật, Firebase Schema, và kế hoạch triển khai cho Phase 3

---

## 1. Tổng quan kiến trúc Phase 3

### 1.1. Module mới cần phát triển

```
Phase 3 Modules:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                                                                
  ┌────────────────────────────────────────────────────────┐    
  │                    ROGUE-LIKE CORE                      │    
  │                                                        │    
  │  ┌──────────┐  ┌──────────┐  ┌──────────┐             │    
  │  │ RunMgr   │  │ MapGen   │  │ FloorMgr │             │    
  │  │          │  │          │  │          │             │    
  │  │ Run state│  │ Bản đồ  │  │ Logic từng│             │    
  │  │ HP, Gold │  │ nhánh    │  │ phòng    │             │    
  │  └──────────┘  └──────────┘  └──────────┘             │    
  │                                                        │    
  │  ┌──────────┐  ┌──────────┐  ┌──────────┐             │    
  │  │ BossMgr  │  │ RelicMgr │  │ EventMgr │             │    
  │  │          │  │          │  │          │             │    
  │  │ Boss AI  │  │ Passive  │  │ Random   │             │    
  │  │ Gimmick  │  │ buffs    │  │ events   │             │    
  │  └──────────┘  └──────────┘  └──────────┘             │    
  └────────────────────────────────────────────────────────┘    
                                                                
  ┌────────────────────────────────────────────────────────┐    
  │                   META-PROGRESSION                      │    
  │                                                        │    
  │  ┌──────────────┐  ┌──────────────┐                    │    
  │  │ MetaUpgradeMgr│  │ MetaCrystalMgr│                   │    
  │  │              │  │              │                    │    
  │  │ Cây nâng cấp │  │ Thu/chi 💎   │                    │    
  │  └──────────────┘  └──────────────┘                    │    
  └────────────────────────────────────────────────────────┘    
                                                                
  ┌────────────────────────────────────────────────────────┐    
  │                GHOST RECORD SYSTEM                      │    
  │                                                        │    
  │  ┌──────────────┐  ┌──────────────┐                    │    
  │  │ GhostRecorder│  │ GhostReplayer│                    │    
  │  │              │  │              │                    │    
  │  │ Ghi hành vi  │  │ Phát lại     │                    │    
  │  │ người thật   │  │ khi ghép bot │                    │    
  │  └──────────────┘  └──────────────┘                    │    
  └────────────────────────────────────────────────────────┘    
                                                                
  ┌────────────────────────────────────────────────────────┐    
  │              CẢI TIẾN HỆ THỐNG CÂU HỎI                 │    
  │                                                        │    
  │  ┌──────────────┐  ┌──────────────┐  ┌──────────┐     │    
  │  │ QuestionType │  │ ElementSystem│  │ ComboMgr │     │    
  │  │              │  │              │  │          │     │    
  │  │ MC, T/F,     │  │ Hỏa, Thủy,  │  │ Streak + │     │    
  │  │ Order, Match │  │ Lôi, Mộc, Thổ│  │ Multiplier│     │    
  │  └──────────────┘  └──────────────┘  └──────────┘     │    
  └────────────────────────────────────────────────────────┘    
```

### 1.2. Relationship với code hiện tại

```
CÁC FILE HIỆN TẠI CẦN SỬA ĐỔI:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Scripts/Data/PlayerData.cs
  → Thêm: metaCrystals, metaUpgrades, runHistory, ghostRecordFlag
  → Thêm: relicInventory (chỉ trong run)

Scripts/Core/PlayerDataManager.cs
  → Thêm Save/Load cho Meta Crystals, Meta Upgrades

Scripts/Core/ScoreManager.cs
  → Thêm: Combo system, Element bonus damage
  → Sửa: Scoring logic cho Boss battle

Scripts/Core/PowerUpManager.cs
  → Sửa: Logic "không giới hạn trong run, 1 loại/câu"
  → Thêm: pu_heal, pu_double (power-up mới chỉ trong run)

Scripts/Core/QuizManager.cs (hoặc tương đương)
  → Thêm: Hỗ trợ nhiều loại câu hỏi (Order, Match, Fill-in)
  → Thêm: Element tagging cho câu hỏi

Scripts/Network/FirebaseManager.cs
  → Thêm: Ghost Record CRUD
  → Thêm: Meta Crystal cloud sync
  → Thêm: Run history logging

Scripts/Network/MatchmakingManager.cs (hoặc logic ghép trận)
  → Sửa: Bỏ thông báo "chuyển sang bot"
  → Thêm: Tích hợp Ghost Record khi timeout

UI/Layouts/HomeLayout.uxml
  → Thêm: Nút "Leo Tháp" (nổi bật, trung tâm)
  → Sửa: Nút PvP Ranked nhỏ hơn, vị trí phụ
  → Thêm: Panel Meta Upgrade

UI/Layouts/GameplayLayout.uxml
  → Thêm: Boss HP bar
  → Thêm: Player HP (hearts)
  → Thêm: Combo counter
  → Thêm: Relic display
  → Sửa: Power-up area (thêm pu_heal, pu_double)
```

---

## 2. Firebase Schema mới

### 2.1. Schema tổng thể (bổ sung Phase 3)

```
/users/{uid}:
  ... (giữ nguyên fields Phase 1–2)
  
  # ===== META-PROGRESSION (Phase 3) =====
  metaCrystals: int                       # 💎 Tổng Meta Crystals hiện có
  metaUpgrades:                           # Cây nâng cấp vĩnh viễn
    M01_startHP: int (0–2)                # Level nâng cấp HP khởi đầu
    M02_shopDiscount: int (0–3)           # Level giảm giá shop
    M03_treasureBonus: int (0–3)          # Level rương hào phóng
    M04_relicRarity: int (0–2)            # Level tỉ lệ Relic xịn
    M05_bagExpansion: int (0–2)           # Level túi đồ mở rộng
    M06_comboMaster: int (0–2)            # Level Combo Master
    M07_trueVision: int (0–1)             # Level Mắt Thần

  # ===== RUN HISTORY (Phase 3) =====
  bestFloor: int                          # Floor cao nhất từng đạt
  totalRuns: int                          # Tổng số run đã chơi
  totalTowerClears: int                   # Số lần chinh phục tháp
  currentDifficulty: int                  # Độ khó hiện tại (tăng sau mỗi lần clear)

  # ===== GHOST RECORD FLAG =====
  ghostRecordConsent: bool                # Đồng ý ghi ghost (mặc định true)


/ghostRecords/{tier}:
  # Pool ghost records theo tier để ghép trận
  {recordId}:
    uid: string                           # UID người chơi gốc (ẩn danh)
    playerName: string                    # Tên hiển thị
    avatarIndex: int                      # Avatar
    tier: int                             # Tier khi ghi
    recordDate: string                    # Ngày ghi (ISO 8601)
    questionSeed: int                     # Seed câu hỏi (để đảm bảo câu hỏi giống)
    answers: [                            # Mảng hành vi từng câu
      {
        questionIndex: int
        thinkTimeMs: int                  # Thời gian suy nghĩ (ms)
        answerIndex: int                  # Đáp án đã chọn
        isCorrect: bool                   # Kết quả
        powerUpUsed: string|null          # Power-up đã dùng (nếu có)
      }
    ]


/runLeaderboard:                          # BXH leo tháp (optional)
  {uid}:
    playerName: string
    bestFloor: int
    totalClears: int
    highestDifficulty: int
```

### 2.2. Quy tắc Firebase Security Rules (bổ sung)

```javascript
// Ghost Records — Chỉ server/owner ghi, ai cũng đọc được
"ghostRecords": {
  "$tier": {
    ".read": true,
    "$recordId": {
      ".write": "auth != null && newData.child('uid').val() === auth.uid"
    }
  }
},

// Meta Upgrades — Chỉ owner đọc/ghi
"users": {
  "$uid": {
    "metaCrystals": {
      ".read": "$uid === auth.uid",
      ".write": "$uid === auth.uid",
      ".validate": "newData.isNumber() && newData.val() >= 0"
    },
    "metaUpgrades": {
      ".read": "$uid === auth.uid",
      ".write": "$uid === auth.uid"
    }
  }
}
```

---

## 3. Thiết kế kỹ thuật chi tiết

### 3.1. RunManager — Quản lý trạng thái Run

```csharp
// === Scripts/Roguelike/RunManager.cs ===

/// <summary>
/// Singleton quản lý toàn bộ trạng thái của 1 lượt chơi Rogue-like.
/// Tạo khi bắt đầu run, hủy khi run kết thúc.
/// </summary>
public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    // === RUN STATE ===
    public int CurrentFloor { get; private set; }
    public int CurrentZone => (CurrentFloor - 1) / 5 + 1;  // Zone 1: F1-5, Zone 2: F6-10, Zone 3: F11-15
    public int PlayerHP { get; private set; }
    public int MaxHP { get; private set; }
    public int RunGold { get; private set; }
    public int MetaCrystalsEarned { get; private set; }
    public int ComboCount { get; private set; }
    public int DifficultyLevel { get; private set; }  // Tăng sau mỗi lần clear

    // === INVENTORY TRONG RUN ===
    public Dictionary<string, int> RunPowerUps { get; private set; }  // Power-up kiếm + mang vào
    public List<RelicData> ActiveRelics { get; private set; }         // Tối đa 5

    // === EVENTS ===
    public event Action<int> OnHPChanged;
    public event Action<int> OnGoldChanged;
    public event Action OnRunStarted;
    public event Action<RunResult> OnRunEnded;
    public event Action<int> OnFloorCompleted;
    public event Action<int> OnComboChanged;

    // === CORE METHODS ===
    public void StartNewRun(int difficulty);
    public void TakeDamage(int amount);    // Gọi khi trả lời sai
    public void HealHP(int amount);        // Từ Rest Room, Relic, Event
    public void AddGold(int amount);       // Từ trả lời đúng, Rương
    public void SpendGold(int amount);     // Tại Shop trong run
    public void AddMetaCrystals(int amount);
    public void IncrementCombo();
    public void ResetCombo();
    public void AddRelic(RelicData relic);
    public void CompleteFloor();
    public void EndRun(bool victory);      // Kết thúc run (thắng/thua)
    
    // === HELPER ===
    public float GetComboMultiplier()
    {
        if (ComboCount >= 10) return 3.0f;
        if (ComboCount >= 5) return 2.0f;
        if (ComboCount >= 3) return 1.5f;
        return 1.0f;
    }

    public int GetBaseHP()
    {
        int metaBonus = MetaUpgradeManager.Instance.GetUpgradeLevel("M01_startHP");
        return 3 + metaBonus;  // Base 3, max 5
    }
}

/// <summary>
/// Kết quả sau mỗi run.
/// </summary>
public class RunResult
{
    public bool Victory;
    public int FloorsCompleted;
    public int BossesDefeated;
    public int MetaCrystalsEarned;
    public int RunGoldEarned;
    public int MoneyEarned;
    public int HighestCombo;
    public float TimePlayed;
    public List<RelicData> RelicsCollected;
}
```

### 3.2. MapGenerator — Tạo bản đồ nhánh

```csharp
// === Scripts/Roguelike/MapGenerator.cs ===

/// <summary>
/// Tạo bản đồ nhánh kiểu Slay the Spire cho mỗi Zone.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    [Header("Map Config")]
    public int rowsPerZone = 5;           // 5 tầng/zone
    public int minNodesPerRow = 2;        // Tối thiểu 2 nhánh
    public int maxNodesPerRow = 4;        // Tối đa 4 nhánh
    
    /// <summary>
    /// Tạo bản đồ cho 1 Zone.
    /// </summary>
    public MapData GenerateZoneMap(int zoneNumber, int difficulty)
    {
        // Thuật toán:
        // 1. Row cuối luôn là 1 node BOSS
        // 2. Row 1 (start) có 2–3 node
        // 3. Row 2–4 có 2–4 node, random loại phòng
        // 4. Tạo connections giữa các row (mỗi node connect 1–2 node row tiếp)
        // 5. Đảm bảo không có node "cô đơn" (orphan)
    }

    /// <summary>
    /// Xác định loại phòng cho mỗi node.
    /// </summary>
    private RoomType GetRandomRoomType(int row, int zone, int difficulty)
    {
        // Tỉ lệ phòng theo Zone:
        //
        // Zone 1: Quiz 50%, Treasure 20%, Event 10%, Shop 10%, Rest 10%
        // Zone 2: Quiz 45%, Treasure 15%, Event 15%, Shop 10%, Rest 10%, Elite 5%
        // Zone 3: Quiz 40%, Treasure 10%, Event 15%, Shop 10%, Rest 10%, Elite 15%
        //
        // Đảm bảo:
        // - Row cuối luôn là BOSS
        // - Mỗi Zone có ít nhất 1 Shop và 1 Rest
        // - Elite chỉ xuất hiện từ Zone 2+
    }
}

public enum RoomType
{
    Quiz,       // 🎯 Câu hỏi
    Treasure,   // 💰 Rương
    Event,      // ❓ Sự kiện
    Shop,       // 🏪 Shop
    Rest,       // 🛏 Nghỉ ngơi
    Elite,      // ⚔️ Elite (mini-boss mạnh)
    Boss        // 👹 Boss
}

public class MapData
{
    public int ZoneNumber;
    public List<MapRow> Rows;       // Mỗi row = 1 hàng ngang trên bản đồ
}

public class MapRow
{
    public int RowIndex;
    public List<MapNode> Nodes;
}

public class MapNode
{
    public string NodeId;
    public RoomType Type;
    public QuestionElement Element;         // Nguyên tố câu hỏi (nếu là Quiz)
    public List<string> ConnectedNodeIds;   // Các node ở row tiếp theo mà node này nối tới
    public bool IsCompleted;
    public bool IsCurrentPosition;
}
```

### 3.3. BossManager — Quản lý trận đánh Boss

```csharp
// === Scripts/Roguelike/BossManager.cs ===

/// <summary>
/// Quản lý trận đánh Boss với Gimmick riêng cho mỗi Boss.
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [SerializeField] private List<BossConfig> bossConfigs;
    
    private BossConfig currentBoss;
    private int bossHP;
    private int bossMaxHP;
    private IBossGimmick activeGimmick;

    public event Action<int, int> OnBossHPChanged;  // (currentHP, maxHP)
    public event Action OnBossDefeated;
    public event Action OnPlayerDefeated;

    /// <summary>
    /// Bắt đầu trận Boss.
    /// </summary>
    public void StartBossBattle(int zoneNumber, int difficulty)
    {
        currentBoss = GetBossForZone(zoneNumber);
        bossMaxHP = Mathf.RoundToInt(currentBoss.baseHP * (1 + difficulty * 0.2f));
        bossHP = bossMaxHP;
        
        // Kích hoạt Gimmick
        activeGimmick = CreateGimmick(currentBoss.gimmickType);
        activeGimmick.Initialize(currentBoss);
        activeGimmick.Activate();
    }

    /// <summary>
    /// Xử lý khi người chơi trả lời đúng → gây damage lên Boss.
    /// </summary>
    public void OnCorrectAnswer(QuestionElement element)
    {
        float comboMultiplier = RunManager.Instance.GetComboMultiplier();
        float elementMultiplier = (element == currentBoss.weakness) ? 2.0f : 1.0f;
        
        int baseDamage = 10;
        int totalDamage = Mathf.RoundToInt(baseDamage * comboMultiplier * elementMultiplier);
        
        bossHP = Mathf.Max(0, bossHP - totalDamage);
        OnBossHPChanged?.Invoke(bossHP, bossMaxHP);
        
        if (bossHP <= 0)
        {
            activeGimmick.Deactivate();
            OnBossDefeated?.Invoke();
        }
    }

    /// <summary>
    /// Xử lý khi người chơi trả lời sai → mất HP.
    /// </summary>
    public void OnWrongAnswer()
    {
        RunManager.Instance.TakeDamage(1);
        
        if (RunManager.Instance.PlayerHP <= 0)
        {
            activeGimmick.Deactivate();
            OnPlayerDefeated?.Invoke();
        }
    }
}

/// <summary>
/// Interface cho Boss Gimmick. Mỗi Boss implement khác nhau.
/// </summary>
public interface IBossGimmick
{
    void Initialize(BossConfig config);
    void Activate();                          // Bật gimmick khi bắt đầu trận
    void Deactivate();                        // Tắt gimmick khi kết thúc
    void OnQuestionDisplayed(QuestionUI ui);  // Hook vào mỗi câu hỏi hiện lên
    void OnAnswerSubmitted();                 // Hook sau khi trả lời
}

// === CÁC GIMMICK CỤ THỂ ===

/// <summary>
/// Giáo Sư Ảo Ảnh — Che 1 đáp án, mở khóa sau 3 câu.
/// </summary>
public class IllusionGimmick : IBossGimmick
{
    private int questionsAnswered = 0;
    
    public void OnQuestionDisplayed(QuestionUI ui)
    {
        // Chọn random 1 đáp án (không phải đáp án đúng) và blur text
        int hiddenIndex = GetRandomWrongAnswerIndex();
        ui.BlurAnswer(hiddenIndex);
        
        questionsAnswered++;
        if (questionsAnswered % 3 == 0)
        {
            // Mở khóa: reveal đáp án bị che
            ui.RevealAllAnswers();
        }
    }
}

/// <summary>
/// Vua Thời Gian — Timer siêu ngắn (5s thay vì 10–15s).
/// </summary>
public class TimeKingGimmick : IBossGimmick
{
    public void Activate()
    {
        TimerController.Instance.SetCustomDuration(5f);  // Override timer
    }
    
    public void Deactivate()
    {
        TimerController.Instance.ResetToDefault();
    }
}

/// <summary>
/// Giáo Sư Hỗn Loạn — Xáo trộn vị trí đáp án mỗi 2 giây.
/// </summary>
public class ChaosGimmick : IBossGimmick
{
    private Coroutine shuffleCoroutine;
    
    public void Activate()
    {
        shuffleCoroutine = StartCoroutine(ShuffleLoop());
    }
    
    private IEnumerator ShuffleLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            // Hoán đổi vị trí visual của 4 nút đáp án
            // (chỉ đổi vị trí UI, KHÔNG đổi mapping index → đáp án đúng vẫn đúng)
            GameplayUIController.Instance.ShuffleAnswerPositions();
        }
    }
}

/// <summary>
/// Vua Nghịch Lý — Đảo ngược luật: chọn SAI = gây damage.
/// </summary>
public class ParadoxGimmick : IBossGimmick
{
    public bool IsReversed { get; private set; } = true;
    private int questionCount = 0;
    
    public void OnQuestionDisplayed(QuestionUI ui)
    {
        questionCount++;
        
        // Mỗi 5 câu, đảo lại 2 câu rồi switch lại
        if (questionCount % 7 <= 1)  // Câu 6, 7 = bình thường
            IsReversed = false;
        else
            IsReversed = true;
        
        // Hiển thị indicator rõ ràng cho người chơi
        ui.ShowRuleIndicator(IsReversed ? "CHỌN ĐÁP ÁN SAI!" : "CHỌN ĐÁP ÁN ĐÚNG");
    }
}
```

### 3.4. RelicManager — Quản lý Thánh tích

```csharp
// === Scripts/Roguelike/RelicManager.cs ===

/// <summary>
/// Quản lý Relic trong 1 run. Reset khi run kết thúc.
/// </summary>
public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }
    
    public const int MAX_RELICS = 5;
    
    [SerializeField] private List<RelicConfig> allRelics;
    private List<RelicConfig> activeRelics = new List<RelicConfig>();
    
    public event Action<RelicConfig> OnRelicAdded;
    public event Action<RelicConfig> OnRelicRemoved;

    /// <summary>
    /// Thêm Relic vào run hiện tại.
    /// </summary>
    public bool AddRelic(RelicConfig relic)
    {
        if (activeRelics.Count >= MAX_RELICS) return false;
        activeRelics.Add(relic);
        relic.OnActivate(RunManager.Instance);
        OnRelicAdded?.Invoke(relic);
        return true;
    }

    /// <summary>
    /// Gọi khi người chơi trả lời đúng — kích hoạt effect Relic liên quan.
    /// </summary>
    public void NotifyCorrectAnswer(int currentCombo)
    {
        foreach (var relic in activeRelics)
        {
            relic.OnCorrectAnswer(RunManager.Instance, currentCombo);
        }
    }

    /// <summary>
    /// Gọi khi người chơi trả lời sai — kích hoạt effect Relic liên quan.
    /// </summary>  
    public bool NotifyWrongAnswer()
    {
        bool damageBlocked = false;
        foreach (var relic in activeRelics)
        {
            if (relic.OnWrongAnswer(RunManager.Instance))
                damageBlocked = true;
        }
        return damageBlocked;
    }

    /// <summary>
    /// Hiện UI chọn 1 trong 3 Relic.
    /// </summary>
    public void OfferRelicChoice(Action<RelicConfig> onChosen)
    {
        var choices = GetRandomRelics(3);
        // Show UI popup với 3 lựa chọn
        RelicChoiceUI.Instance.Show(choices, (chosen) =>
        {
            if (chosen != null) AddRelic(chosen);
            onChosen?.Invoke(chosen);
        });
    }

    private List<RelicConfig> GetRandomRelics(int count)
    {
        // Lọc bỏ Relic đã có
        // Áp dụng Meta Upgrade M04 (tăng tỉ lệ Rare)
        // Random theo weighted rarity
        int rarityBonus = MetaUpgradeManager.Instance.GetUpgradeLevel("M04_relicRarity");
        // ...
    }

    /// <summary>
    /// Reset toàn bộ khi run kết thúc.
    /// </summary>
    public void ClearAllRelics()
    {
        foreach (var relic in activeRelics)
            relic.OnDeactivate();
        activeRelics.Clear();
    }
}

/// <summary>
/// Config cho mỗi Relic. Dùng ScriptableObject.
/// </summary>
[CreateAssetMenu(menuName = "Roguelike/Relic Config")]
public class RelicConfig : ScriptableObject
{
    public string relicId;
    public string relicName;
    public string emoji;
    public string description;
    public RelicRarity rarity;
    
    // Override trong subclass hoặc dùng delegate
    public virtual void OnActivate(RunManager run) { }
    public virtual void OnDeactivate() { }
    public virtual void OnCorrectAnswer(RunManager run, int combo) { }
    public virtual bool OnWrongAnswer(RunManager run) { return false; }  // return true = block damage
    public virtual void OnFloorComplete(RunManager run) { }
    public virtual void OnBossDefeated(RunManager run) { }
}

public enum RelicRarity
{
    Common,     // 60% drop rate
    Uncommon,   // 30% drop rate
    Rare        // 10% drop rate (tăng bởi Meta M04)
}
```

### 3.5. GhostRecordSystem — Giả lập PvP

```csharp
// === Scripts/Network/GhostRecordSystem.cs ===

/// <summary>
/// Ghi lại hành vi người chơi trong trận PvP để dùng làm Ghost.
/// </summary>
public class GhostRecorder : MonoBehaviour
{
    private GhostRecordData currentRecord;
    private bool isRecording;

    /// <summary>
    /// Bắt đầu ghi khi trận PvP bắt đầu (chỉ ghi trận với người thật).
    /// </summary>
    public void StartRecording(string playerName, int avatarIndex, int tier, int seed)
    {
        currentRecord = new GhostRecordData
        {
            uid = FirebaseManager.Instance.CurrentUserId,
            playerName = playerName,
            avatarIndex = avatarIndex,
            tier = tier,
            questionSeed = seed,
            recordDate = DateTime.UtcNow.ToString("o"),
            answers = new List<GhostAnswerData>()
        };
        isRecording = true;
    }

    /// <summary>
    /// Ghi lại mỗi hành động trả lời.
    /// </summary>
    public void RecordAnswer(int questionIndex, int answerIndex, bool isCorrect, 
                              int thinkTimeMs, string powerUpUsed)
    {
        if (!isRecording) return;
        currentRecord.answers.Add(new GhostAnswerData
        {
            questionIndex = questionIndex,
            answerIndex = answerIndex,
            isCorrect = isCorrect,
            thinkTimeMs = thinkTimeMs,
            powerUpUsed = powerUpUsed
        });
    }

    /// <summary>
    /// Upload ghost record lên Firebase khi trận kết thúc.
    /// </summary>
    public async void FinishAndUpload()
    {
        if (!isRecording || currentRecord == null) return;
        isRecording = false;
        
        // Chỉ upload nếu trận có đủ câu trả lời
        if (currentRecord.answers.Count >= 5)
        {
            string path = $"ghostRecords/tier_{currentRecord.tier}/{Guid.NewGuid()}";
            await FirebaseManager.Instance.SetValueAsync(path, currentRecord.ToDict());
        }
    }
}

/// <summary>
/// Phát lại Ghost Record như đối thủ trong trận PvP.
/// </summary>
public class GhostReplayer : MonoBehaviour
{
    private GhostRecordData ghostData;
    private int currentQuestionIndex;
    private Coroutine replayCoroutine;

    /// <summary>
    /// Load ghost record phù hợp tier từ Firebase.
    /// </summary>
    public async Task<bool> LoadGhost(int tier)
    {
        // Query /ghostRecords/tier_{tier} 
        // Random chọn 1 record
        // Trả về false nếu không có record nào (fallback sang bot random)
    }

    /// <summary>
    /// Bắt đầu mô phỏng ghost cho câu hỏi tiếp theo.
    /// </summary>
    public void SimulateNextAnswer(int questionIndex, Action<int, bool, string> onGhostAnswered)
    {
        if (questionIndex >= ghostData.answers.Count)
        {
            // Hết dữ liệu → fallback random
            FallbackRandomAnswer(onGhostAnswered);
            return;
        }

        var answerData = ghostData.answers[questionIndex];
        
        // Đợi đúng thời gian suy nghĩ rồi mới submit
        replayCoroutine = StartCoroutine(DelayedAnswer(
            answerData.thinkTimeMs,
            answerData.answerIndex,
            answerData.isCorrect,
            answerData.powerUpUsed,
            onGhostAnswered
        ));
    }

    private IEnumerator DelayedAnswer(int delayMs, int answer, bool correct, 
                                       string powerUp, Action<int, bool, string> callback)
    {
        // Thêm ±500ms random để tự nhiên hơn
        float delay = (delayMs + UnityEngine.Random.Range(-500, 500)) / 1000f;
        delay = Mathf.Max(0.5f, delay);  // Tối thiểu 0.5s
        
        yield return new WaitForSeconds(delay);
        callback?.Invoke(answer, correct, powerUp);
    }
}

[System.Serializable]
public class GhostRecordData
{
    public string uid;
    public string playerName;
    public int avatarIndex;
    public int tier;
    public string recordDate;
    public int questionSeed;
    public List<GhostAnswerData> answers;
    
    public Dictionary<string, object> ToDict() { /* ... */ }
}

[System.Serializable]
public class GhostAnswerData
{
    public int questionIndex;
    public int thinkTimeMs;
    public int answerIndex;
    public bool isCorrect;
    public string powerUpUsed;
}
```

### 3.6. MetaUpgradeManager — Quản lý nâng cấp vĩnh viễn

```csharp
// === Scripts/Roguelike/MetaUpgradeManager.cs ===

/// <summary>
/// Quản lý cây nâng cấp Meta-Progression.
/// Dữ liệu được lưu vĩnh viễn (PlayerPrefs + Firebase).
/// </summary>
public class MetaUpgradeManager : MonoBehaviour
{
    public static MetaUpgradeManager Instance { get; private set; }

    [SerializeField] private List<MetaUpgradeConfig> upgradeConfigs;

    /// <summary>
    /// Lấy level hiện tại của 1 nâng cấp.
    /// </summary>
    public int GetUpgradeLevel(string upgradeId)
    {
        return PlayerDataManager.Instance.GetMetaUpgradeLevel(upgradeId);
    }

    /// <summary>
    /// Nâng cấp 1 meta upgrade, trừ Meta Crystals.
    /// </summary>
    public bool TryUpgrade(string upgradeId)
    {
        var config = upgradeConfigs.Find(c => c.upgradeId == upgradeId);
        if (config == null) return false;

        int currentLevel = GetUpgradeLevel(upgradeId);
        if (currentLevel >= config.maxLevel) return false;

        int cost = config.costPerLevel[currentLevel];
        if (PlayerDataManager.Instance.MetaCrystals < cost) return false;

        PlayerDataManager.Instance.SpendMetaCrystals(cost);
        PlayerDataManager.Instance.SetMetaUpgradeLevel(upgradeId, currentLevel + 1);
        PlayerDataManager.Instance.SaveAndSync();

        return true;
    }
}

[CreateAssetMenu(menuName = "Roguelike/Meta Upgrade Config")]
public class MetaUpgradeConfig : ScriptableObject
{
    public string upgradeId;       // "M01_startHP"
    public string upgradeName;     // "Sức khỏe khởi đầu"
    public string emoji;           // "❤️"
    public string description;     // "+1 HP lúc bắt đầu run"
    public int maxLevel;           // 2
    public int[] costPerLevel;     // [100, 250]
    public string[] effectDescription;  // ["3 → 4 HP", "4 → 5 HP"]
}
```

### 3.7. Mở rộng Question System

```csharp
// === Scripts/Data/QuestionData.cs (Mở rộng) ===

/// <summary>
/// Enum cho loại câu hỏi mới.
/// </summary>
public enum QuestionType
{
    MultipleChoice,     // Trắc nghiệm 4 đáp án (như hiện tại)
    TrueFalse,          // Đúng/Sai
    Ordering,           // Sắp xếp thứ tự
    Matching,           // Nối cặp
    FillIn              // Điền từ
}

/// <summary>
/// Enum cho nguyên tố câu hỏi.
/// </summary>
public enum QuestionElement
{
    None = 0,
    Fire = 1,       // 🔥 Lịch sử
    Water = 2,      // 🌊 Địa lý
    Thunder = 3,    // ⚡ Khoa học
    Wood = 4,       // 🌿 Văn học
    Earth = 5       // 🪨 Đời sống
}

/// <summary>
/// Cấu trúc câu hỏi mở rộng (tương thích ngược với câu hỏi cũ).
/// </summary>
[System.Serializable]
public class QuestionDataExtended
{
    public string questionId;
    public QuestionType type;               // Loại câu hỏi
    public QuestionElement element;         // Nguyên tố
    public int difficulty;                  // 1–5
    public string questionText;             // Nội dung câu hỏi
    
    // === MULTIPLE CHOICE / TRUE-FALSE ===
    public List<string> answers;            // Danh sách đáp án
    public int correctAnswerIndex;          // Index đáp án đúng
    
    // === ORDERING ===
    public List<string> orderItems;         // Các mục cần sắp xếp
    public List<int> correctOrder;          // Thứ tự đúng (index)
    
    // === MATCHING ===
    public List<string> matchLeft;          // Cột trái (VD: quốc gia)
    public List<string> matchRight;         // Cột phải (VD: thủ đô)
    public List<int> correctMatchMapping;   // matchLeft[i] nối với matchRight[correctMatchMapping[i]]
    
    // === FILL-IN ===
    public string correctFillAnswer;        // Đáp án đúng (text)
    public List<string> acceptedVariants;   // Các biến thể chấp nhận (VD: "sắt", "Sắt", "Fe")
}
```

---

## 4. Cấu trúc thư mục mới

```
Assets/Scripts/
├── Core/                           # (Hiện có)
│   ├── PlayerDataManager.cs        # Sửa: thêm Meta fields
│   ├── ScoreManager.cs             # Sửa: Combo, Element bonus
│   ├── PowerUpManager.cs           # Sửa: logic mới cho Rogue-like
│   ├── ShopManager.cs              # Giữ nguyên
│   └── ...
│
├── Roguelike/                      # (MỚI — Toàn bộ module Rogue-like)
│   ├── Core/
│   │   ├── RunManager.cs           # Singleton quản lý run state
│   │   ├── MapGenerator.cs         # Tạo bản đồ nhánh
│   │   ├── FloorManager.cs         # Logic từng phòng/tầng
│   │   └── RunResultCalculator.cs  # Tính toán kết quả run
│   │
│   ├── Boss/
│   │   ├── BossManager.cs          # Quản lý trận Boss
│   │   ├── BossConfig.cs           # ScriptableObject config Boss
│   │   └── Gimmicks/
│   │       ├── IBossGimmick.cs     # Interface
│   │       ├── IllusionGimmick.cs  # Giáo Sư Ảo Ảnh
│   │       ├── TimeKingGimmick.cs  # Vua Thời Gian
│   │       ├── ChaosGimmick.cs     # Giáo Sư Hỗn Loạn
│   │       └── ParadoxGimmick.cs   # Vua Nghịch Lý
│   │
│   ├── Relic/
│   │   ├── RelicManager.cs         # Quản lý Relic trong run
│   │   ├── RelicConfig.cs          # ScriptableObject config Relic
│   │   └── Relics/                 # Các Relic cụ thể
│   │       ├── ScholarCupRelic.cs
│   │       ├── TelescopeRelic.cs
│   │       ├── HourglassRelic.cs
│   │       ├── AncientBookRelic.cs
│   │       ├── KnowledgeScytheRelic.cs
│   │       ├── ReflectArmorRelic.cs
│   │       └── GreedGemRelic.cs
│   │
│   ├── Event/
│   │   ├── EventManager.cs         # Quản lý sự kiện ngẫu nhiên
│   │   └── EventConfig.cs          # ScriptableObject config Event
│   │
│   ├── Meta/
│   │   ├── MetaUpgradeManager.cs   # Cây nâng cấp vĩnh viễn
│   │   ├── MetaCrystalManager.cs   # Thu/chi Meta Crystals
│   │   └── MetaUpgradeConfig.cs    # ScriptableObject
│   │
│   └── UI/
│       ├── MapUIController.cs      # UI bản đồ nhánh
│       ├── BossUIController.cs     # UI trận Boss
│       ├── RelicChoiceUI.cs        # UI chọn Relic
│       ├── RunResultUI.cs          # UI kết quả run
│       ├── MetaUpgradeUI.cs        # UI nâng cấp Meta
│       └── InRunShopUI.cs          # UI Shop trong run
│
├── Network/                        # (Hiện có)
│   ├── FirebaseManager.cs          # Sửa: Ghost Record, Meta sync
│   ├── GhostRecorder.cs            # MỚI: Ghi hành vi
│   ├── GhostReplayer.cs            # MỚI: Phát lại Ghost
│   └── ...
│
├── Data/                           # (Hiện có)
│   ├── PlayerData.cs               # Sửa: thêm Meta fields
│   ├── QuestionData.cs             # Sửa: thêm QuestionType, Element
│   └── ...
│
└── UI/
    └── Layouts/
        ├── HomeLayout.uxml         # Sửa: Nút Leo Tháp, Meta panel
        ├── GameplayLayout.uxml     # Sửa: Boss HP, Player HP, Combo
        ├── MapLayout.uxml          # MỚI: Bản đồ nhánh
        ├── BossLayout.uxml         # MỚI: UI trận Boss
        ├── MetaUpgradeLayout.uxml  # MỚI: UI nâng cấp
        └── RunResultLayout.uxml   # MỚI: Kết quả run
```

---

## 5. Kế hoạch triển khai (Phân phase nhỏ)

### Phase 3.1: Nền tảng Rogue-like (Sprint 1 — ~2 tuần)

```
[ ] Tạo thư mục Scripts/Roguelike/ và cấu trúc cơ bản
[ ] Implement RunManager (state management)
[ ] Implement MapGenerator (bản đồ nhánh đơn giản, 1 zone trước)
[ ] Implement FloorManager (Quiz Room cơ bản — dùng lại Gameplay hiện tại)
[ ] Tạo MapLayout.uxml (UI bản đồ text-based)
[ ] Mở rộng PlayerData.cs (thêm meta fields)
[ ] Thêm nút "Leo Tháp" vào HomeLayout.uxml
[ ] Test: Chạy được 1 run cơ bản (5 floors, không có Boss)
```

### Phase 3.2: Boss System (Sprint 2 — ~2 tuần)

```
[ ] Implement BossManager + BossConfig (ScriptableObject)
[ ] Implement IBossGimmick interface
[ ] Implement 2 Gimmick đầu tiên:
    [ ] Bot Học Việt (Boss Zone 1, không gimmick)
    [ ] Giáo Sư Ảo Ảnh (Boss Zone 2, che đáp án)
[ ] Tạo BossLayout.uxml (Boss HP bar, Gimmick indicator)
[ ] Thêm Combo System vào ScoreManager
[ ] Test: Chạy run hoàn chỉnh Zone 1 + 2 với Boss battle
```

### Phase 3.3: Relic + Event + Shop trong Run (Sprint 3 — ~2 tuần)

```
[ ] Implement RelicManager + RelicConfig (ScriptableObject)
[ ] Implement 4 Relic đầu tiên (R01–R04)
[ ] Implement EventManager + 3 Event đầu tiên
[ ] Implement InRunShopUI (mua power-up bằng Run Gold)
[ ] Implement Rest Room (hồi HP)
[ ] Tạo RelicChoiceUI
[ ] Test: Run hoàn chỉnh 3 Zone với đầy đủ loại phòng
```

### Phase 3.4: Meta-Progression (Sprint 4 — ~1 tuần)

```
[ ] Implement MetaUpgradeManager + MetaUpgradeConfig
[ ] Implement MetaCrystalManager
[ ] Tạo MetaUpgradeLayout.uxml
[ ] Tích hợp Meta bonus vào RunManager (HP, Shop discount, v.v.)
[ ] Implement RunResultUI (hiển thị phần thưởng cuối run)
[ ] Firebase sync cho Meta Crystals + Upgrades
[ ] Test: Nâng cấp meta → ảnh hưởng run tiếp theo
```

### Phase 3.5: Ghost Record + PvP Rework (Sprint 5 — ~1 tuần)

```
[ ] Implement GhostRecorder (ghi hành vi PvP)
[ ] Implement GhostReplayer (phát lại Ghost)
[ ] Sửa Matchmaking: bỏ thông báo "chuyển sang bot"
[ ] Tích hợp Ghost khi ghép trận timeout (>10s)
[ ] Firebase schema cho ghostRecords
[ ] Test: Ghép trận timeout → chơi với Ghost → trải nghiệm tự nhiên
```

### Phase 3.6: Cải tiến câu hỏi + Boss còn lại (Sprint 6 — ~2 tuần)

```
[ ] Mở rộng QuestionData (thêm QuestionType, Element)
[ ] Implement UI cho câu hỏi True/False
[ ] Implement UI cho câu hỏi Ordering
[ ] Implement hệ thống Nguyên tố (Element bonus damage)
[ ] Implement Gimmick còn lại:
    [ ] Vua Thời Gian (Zone 2 alt)
    [ ] Giáo Sư Hỗn Loạn (Zone 3)
    [ ] Vua Nghịch Lý (Zone 3 alt)
[ ] Implement Boss Scaling (khó hơn sau mỗi lần clear)
[ ] Test toàn bộ + Playtest + Balance
```

---

## 6. Rủi ro kỹ thuật & Giải pháp

| # | Rủi ro | Mức độ | Giải pháp |
|---|---|---|---|
| R1 | Bản đồ nhánh khó implement UI trên mobile | Cao | Dùng ScrollView vertical + node/line rendering đơn giản. Fallback: bản đồ dạng list thay vì graph |
| R2 | Boss Gimmick "Xáo trộn" gây khó chịu UX | Trung bình | Playtest sớm, điều chỉnh tần suất shuffle. Có thể cho thời gian "đứng yên" 1s sau mỗi lần shuffle |
| R3 | Ghost Record thiếu khi server mới launch | Trung bình | Tạo sẵn 20–30 ghost records "hand-crafted" từ quá trình playtest. Dùng làm seed data |
| R4 | Câu hỏi Ordering/Matching khó input trên mobile | Trung bình | Dùng Drag & Drop (Unity UI Toolkit drag events). Fallback: chọn thứ tự bằng numbered buttons |
| R5 | Meta Crystals inflation (tích quá nhiều) | Thấp | Giới hạn Meta Crystals/run. Thêm upgrade tốn nhiều crystal ở late-game |
| R6 | Cheat: Client-side HP manipulation | Trung bình | Validate kết quả run server-side (Cloud Functions) khi có thời gian. Phase 3 chấp nhận trust client |

---

## 7. Metrics cần tracking (Analytics)

| Metric | Mô tả | Dùng để |
|---|---|---|
| `run_started` | Số run bắt đầu / ngày | Đo engagement |
| `run_completed` | Số run hoàn thành Floor 15 | Đo difficulty balance |
| `run_death_floor` | Floor mà người chơi chết nhiều nhất | Điều chỉnh độ khó |
| `boss_win_rate` | Tỉ lệ thắng mỗi Boss | Balance Boss difficulty |
| `power_up_usage` | Power-up nào được dùng nhiều/ít nhất | Balance power-up |
| `relic_pick_rate` | Relic nào được chọn nhiều nhất | Balance relic |
| `meta_upgrade_priority` | Upgrade nào được nâng đầu tiên | Hiểu player preference |
| `ghost_detection_rate` | % người chơi báo cáo "bot" trong PvP | Đánh giá chất lượng Ghost |
| `pvp_to_roguelike_ratio` | Tỉ lệ chơi PvP vs Rogue-like | Đo sức hút mỗi chế độ |
| `avg_run_duration` | Thời gian trung bình 1 run | Optimize session length |

---

*Tài liệu này là bản thiết kế sống (living document). Cập nhật khi có thay đổi kỹ thuật.*
