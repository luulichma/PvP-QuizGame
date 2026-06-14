# PLAN HOÀN THIỆN DỰ ÁN PVP QUIZGAME

> **Ngày tạo:** 25/05/2026  
> **Cập nhật lần cuối:** 25/05/2026 — Bổ sung UI Enhancement Plan (G-01 → G-28)  
> **Phân tích bởi:** Claude AI  
> **Phiên bản hiện tại:** v1.0 (pre-release)  
> **Kiến trúc:** Unity (C#) + Firebase (Auth, Realtime DB, Remote Config) + UI Toolkit (UXML/USS)

---

## MỤC LỤC

1. [Tổng quan kiến trúc hiện tại](#1-tổng-quan-kiến-trúc-hiện-tại)
2. [PHẦN A — Bugs cần sửa](#2-phần-a--bugs-cần-sửa-14-bugs)
3. [PHẦN B — Nâng cấp trải nghiệm chơi game](#3-phần-b--nâng-cấp-trải-nghiệm-chơi-game-8-items)
4. [PHẦN C — Tính năng mở rộng tương lai](#4-phần-c--tính-năng-mở-rộng-tương-lai)
5. [Thứ tự triển khai đề xuất](#5-thứ-tự-triển-khai-đề-xuất)
6. [PHẦN D — UI Enhancement Plan](#6-phần-d--ui-enhancement-plan-28-items)
7. [Hướng dẫn thay thế Avatar](#7-hướng-dẫn-thay-thế-avatar)

---

## 1. TỔNG QUAN KIẾN TRÚC HIỆN TẠI

### 1.1 Scene Flow

```
InitScene → HomeScene → GameplayScene
   │            │              │
   │            │              ├── GameController (trạng thái: Idle→Countdown→Playing→GameOver)
   │            │              ├── QuizManager (sinh câu hỏi từ localization keys)
   │            │              ├── ScoreManager (chấm điểm, thưởng XP/Money)
   │            │              ├── TimerController (đếm ngược mỗi câu)
   │            │              ├── InputController_UXML (xử lý bấm đáp án)
   │            │              └── GameplayUIController_UXML (UI trận đấu + popup kết quả)
   │            │
   │            ├── MainMenuUIController_UXML (menu chính, settings, profile)
   │            └── Matchmaking (Find Match online / Practice offline)
   │
   ├── InitSceneController_UXML (splash, auth popup: Guest/Email)
   └── LocalizationManager (tải CSV từ Google Sheet, fallback JSON)
```

### 1.2 Mô hình PvP

- **Online:** `FirebaseMatchProvider` — đồng bộ answers/scores qua Firebase Realtime DB, seed chung để shuffle câu hỏi giống nhau.
- **Offline:** `LocalMatchProvider` + `MockOpponent` (bot) — mô phỏng server local, bot tự trả lời sau delay ngẫu nhiên.

### 1.3 Localization

- 8 ngôn ngữ được khai báo (vi, en, fr, it, de, es, ja, ko)
- Chỉ có 2 file JSON fallback hoàn chỉnh: `vi.json`, `en.json`
- Câu hỏi được sinh tự động từ localization keys `q_*` / `a_*`
- Hiện tại: ~10 câu hỏi × 5 chủ đề (IT, Geography, Math, History, Science)

---

## 2. PHẦN A — BUGS CẦN SỬA (14 bugs)

### 2.1 CRITICAL (5 bugs) — Ảnh hưởng trực tiếp đến gameplay

#### BUG-01: Race condition giữa Timer và ShowAnswerFeedback
- **File:** `GameController.cs` dòng 316-325, `InputController_UXML.cs` dòng 145-184
- **Mô tả:** Khi cả hai người chơi trả lời trước khi timer hết, `HandleBothPlayersAnswered` gọi `RevealAndAdvance()` → hiển thị feedback 1.5 giây (`ShowAnswerFeedback`). Trong thời gian 1.5s này, nếu timer cũ hết hạn, `HandleTimerEnd()` sẽ gọi `RevealAndAdvance()` lần thứ hai, khiến **câu hỏi mới bị auto-skip** với `_currentLocalAnswer = -1`.
- **Nguyên nhân gốc:** `timerController.StopTimer()` không được gọi khi bắt đầu `RevealAndAdvance`. Timer chỉ được restart SAU khi feedback kết thúc (dòng 218), để lại khoảng trống 1.5s.
- **Cách sửa:** Gọi `timerController.StopTimer()` ngay đầu `RevealAndAdvance()`, hoặc thêm flag `_isRevealing` để `HandleTimerEnd` bỏ qua khi đang trong quá trình reveal.

```csharp
// GameController.cs - Đầu hàm RevealAndAdvance
private IEnumerator RevealAndAdvance(int p1Answer, int p2Answer)
{
    // FIX BUG-01: Dừng timer ngay khi bắt đầu reveal
    timerController.StopTimer();
    
    var question = quizManager.CurrentQuestion;
    if (question == null) yield break;
    // ... phần còn lại giữ nguyên
}
```

---

#### BUG-02: Offline mode — Bot luôn bị tính sai khi timer hết
- **File:** `GameController.cs` dòng 325
- **Mô tả:** `HandleTimerEnd()` gọi `RevealAndAdvance(_currentLocalAnswer, -1)`, truyền `-1` cho P2 (bot). Nhưng trong offline mode, bot (`MockOpponent`) có thể đã trả lời đúng thông qua `LocalMatchProvider.SubmitAnswerP2()`. Answer của bot bị mất vì `HandleTimerEnd` hardcode `-1`.
- **Hậu quả:** Bot luôn bị chấm SAI khi timer hết — dù bot đã chọn đáp án đúng trước đó.
- **Cách sửa:** Lưu answer của P2 từ `LocalMatchProvider` vào biến `_currentP2Answer` tương tự `_currentLocalAnswer`, hoặc lấy từ `LocalMatchProvider` instance.

```csharp
// Thêm biến lưu P2 answer
private int _currentP2Answer = -1;

// Trong HandleBothPlayersAnswered — lưu lại cả p2
private void HandleBothPlayersAnswered(int p1Answer, int p2Answer)
{
    if (CurrentState != GameState.Playing) return;
    _currentP2Answer = p2Answer;  // Lưu lại
    StartCoroutine(RevealAndAdvance(p1Answer, p2Answer));
}

// HandleTimerEnd dùng _currentP2Answer thay vì -1
private void HandleTimerEnd()
{
    if (CurrentState != GameState.Playing) return;
    StartCoroutine(RevealAndAdvance(_currentLocalAnswer, _currentP2Answer));
}
```

---

#### BUG-03: Online mode — Đối thủ AFK không disconnect gây treo game
- **File:** `FirebaseMatchProvider.cs`, `GameController.cs`
- **Mô tả:** Nếu đối thủ không trả lời (AFK) nhưng vẫn connected (không disconnect), game sẽ chờ vĩnh viễn cho sự kiện `OnBothPlayersAnswered`. Timer local hết → `HandleTimerEnd` gọi `RevealAndAdvance` cho P1, nhưng Firebase listener vẫn chờ P2 → game desync.
- **Cách sửa:** Thêm timeout cho mỗi câu hỏi ở online mode. Nếu P2 không trả lời sau N giây (ví dụ: `QuestionDuration + 5s`), tự động submit answer mặc định (-1) cho P2.

---

#### BUG-04: Button handler tích lũy lambda — click gọi nhiều lần
- **File:** `GameplayUIController_UXML.cs` dòng 343, 350, 365
- **Mô tả:** Mỗi lần `HandleGameOver()` chạy, nó đăng ký thêm lambda mới vào `playAgainBtn.clicked` và `backHomeBtn.clicked`. Nếu result popup xuất hiện nhiều lần (restart offline), click một nút sẽ trigger TẤT CẢ handlers đã đăng ký → multiple scene loads, crash.
- **Cách sửa:** Unregister handler cũ trước khi đăng ký mới, hoặc dùng named method thay vì anonymous lambda.

```csharp
// Lưu reference để unsubscribe
private Action _playAgainHandler;
private Action _backHomeHandler;

// Trong HandleGameOver, trước khi đăng ký mới:
if (_playAgainHandler != null) playAgainBtn.clicked -= _playAgainHandler;
_playAgainHandler = () => { /* logic */ };
playAgainBtn.clicked += _playAgainHandler;
```

---

#### BUG-05: SignOut đặt IsConnected=false → không thể re-authenticate
- **File:** `FirebaseManager.cs` dòng 333
- **Mô tả:** `SignOut()` set `IsConnected = false`. Nhưng `IsConnected` đại diện cho trạng thái kết nối SDK Firebase (đã init thành công), không phải trạng thái authentication. Sau khi quay lại InitScene, code kiểm tra `IsConnected` trước khi cho phép auth → luôn false → auth bị reject.
- **Cách sửa:** Không set `IsConnected = false` trong `SignOut()`. Chỉ reset auth-related state.

```csharp
public void SignOut()
{
    if (_auth != null)
    {
        _auth.SignOut();
    }
    // KHÔNG set IsConnected = false — Firebase SDK vẫn connected
    LocalDisplayName = "Player";
    CurrentRoomId = null;
    OpponentId = null;
    OpponentName = null;
}
```

---

### 2.2 MEDIUM (5 bugs) — UX/Localization issues

#### BUG-06: Timer UI hiển thị sai format
- **File:** `TimerController.cs` dòng 24, 96-101
- **Mô tả:** `totalTime` mặc định là `180f` (3 phút) trong Inspector, nhưng thực tế Remote Config trả về 15s/câu. `GetFormattedTime()` trả về format `mm:ss` — khi timer là 15 giây, nó hiện `00:15` rồi đếm xuống `00:00`, nhưng UXML có default text `03:00` gây nhầm lẫn ban đầu.
- **Cách sửa:** Đổi `totalTime` mặc định thành `15f`, và cân nhắc dùng format `ss` thay vì `mm:ss` nếu timer luôn < 60s.

---

#### BUG-07: Thiếu localization keys cho Profile Popup
- **File:** `MainMenuUIController_UXML.cs` dòng 482-486
- **Mô tả:** Code gọi `L.GetText("profile_title")`, `L.GetText("profile_select_avatar")`, `L.GetText("profile_display_name")`, `L.GetText("profile_save")` — nhưng các key này KHÔNG TỒN TẠI trong `vi.json` và `en.json`. Kết quả: hiển thị fallback text tiếng Việt hardcode.
- **Cách sửa:** Thêm các key vào cả `vi.json`, `en.json` và Google Sheet.

---

#### BUG-08: UXML hard-code tiếng Việt trong ResultPopup
- **File:** `ResultPopup.uxml`
- **Mô tả:** Các stat label ("Diem cua ban", "Diem doi thu", "Tien thuong") được hard-code trong UXML. `HandleGameOver()` chỉ localize title, scores, và buttons — KHÔNG localize stat labels.
- **Cách sửa:** Thêm localization cho stat labels trong `HandleGameOver()` hoặc đổi UXML thành placeholder text.

---

#### BUG-09: ClearData xóa sạch tất cả PlayerPrefs
- **File:** `PlayerDataManager.cs` dòng 63
- **Mô tả:** `PlayerPrefs.DeleteAll()` xóa TẤT CẢ dữ liệu, bao gồm cả `MusicEnabled`, `SFXEnabled` (AudioManager), `SelectedLanguage` (LocalizationManager). Chỉ nên xóa player data keys.
- **Cách sửa:** Xóa từng key thay vì `DeleteAll()`.

```csharp
public void ClearData()
{
    PlayerPrefs.DeleteKey("PlayerLevel");
    PlayerPrefs.DeleteKey("PlayerExp");
    PlayerPrefs.DeleteKey("PlayerMoney");
    PlayerPrefs.DeleteKey("PlayerAvatar");
    PlayerPrefs.DeleteKey("PlayerName");
    PlayerPrefs.Save();
    // ... reset local SO values
}
```

---

#### BUG-10: Guest detection dùng logic sai
- **File:** `MainMenuUIController_UXML.cs` dòng 414
- **Mô tả:** `bool isGuest = ... || fm.LocalDisplayName.Contains("Player_")` — bất kỳ user nào đặt tên có chứa "Player_" sẽ bị coi là guest. Sai logic.
- **Cách sửa:** Kiểm tra `FirebaseAuth.CurrentUser.IsAnonymous` thay vì dựa vào tên.

```csharp
bool isGuest = _auth.CurrentUser != null && _auth.CurrentUser.IsAnonymous;
// Hoặc expose property IsAnonymous từ FirebaseManager
```

---

### 2.3 LOW (4 bugs)

#### BUG-11: Memory leak — ScriptableObject không được destroy
- **File:** `QuizManager.cs` dòng 188
- **Mô tả:** `ScriptableObject.CreateInstance<QuestionData>()` tạo SO mới mỗi lần `StartQuiz()`. Sau nhiều lần restart, SO cũ tồn tại trong memory mà không bị GC.
- **Cách sửa:** Destroy SO cũ trước khi tạo mới, hoặc dùng plain C# class thay vì ScriptableObject.

---

#### BUG-12: Answer so sánh bằng string — fragile khi 2 đáp án trùng text
- **File:** `QuizManager.cs` dòng 156-163
- **Mô tả:** Sau khi shuffle, code tìm lại correct index bằng `newAnswers[i] == correctKey`. Nếu 2 đáp án có cùng localization text, `correctAnswerIndex` có thể trỏ sai.
- **Cách sửa:** Lưu correct index trước shuffle, track bằng index swap thay vì string compare.

---

#### BUG-13: exitBtn handler không unsubscribe
- **File:** `GameplayUIController_UXML.cs` dòng 63
- **Mô tả:** `exitBtn.clicked += ShowExitConfirmation` đăng ký trong `OnEnable` nhưng không unregister trong `OnDisable`. Scene reload sẽ tích lũy handlers.
- **Cách sửa:** Thêm `exitBtn.clicked -= ShowExitConfirmation` vào `OnDisable()`.

---

#### BUG-14: CSV parser naive — không xử lý quoted fields
- **File:** `GoogleSheetDownloader.cs` dòng ~71
- **Mô tả:** Dùng `Split(',')` để parse CSV. Bất kỳ câu hỏi/đáp án nào chứa dấu phẩy sẽ parse sai. Hiện đây là dead code (dữ liệu đến từ localization), nhưng sẽ hỏng nếu re-enable.
- **Cách sửa:** Dùng regex-based CSV parser hoặc thư viện CsvHelper.

---

## 3. PHẦN B — NÂNG CẤP TRẢI NGHIỆM CHƠI GAME (8 items)

### UX-01: Hiệu ứng đúng/sai nâng cao
- **Hiện trạng:** Có đổi màu xanh/đỏ + sound effect + shake khi sai. Đã đủ cơ bản.
- **Nâng cấp đề xuất:**
  - Thêm **confetti particles** khi đúng (dùng USS animation hoặc VisualElement spawn)
  - **Streak counter** — hiển thị "2x Streak!", "3x Streak!" khi đúng liên tiếp, kèm animation scale-up
  - **Haptic feedback** trên Android (`Handheld.Vibrate()`) khi sai
  - **Score popup (+10)** bay từ nút lên thanh score với animation fade-out
- **Độ phức tạp:** Trung bình
- **File cần sửa:** `InputController_UXML.cs`, `GameplayUIController_UXML.cs`, `ScoreManager.cs`

### UX-02: Hiển thị trạng thái đối thủ real-time
- **Hiện trạng:** Chỉ hiện emoji ✅/❌ SAU khi cả 2 trả lời. Không biết đối thủ đang làm gì.
- **Nâng cấp đề xuất:**
  - Hiển thị **"Đang suy nghĩ..."** khi đối thủ chưa trả lời (Online: listen Firebase, Offline: dùng bot state)
  - Hiển thị **"Đã trả lời!"** khi đối thủ chọn xong (không tiết lộ đáp án)
  - Animation **typing dots (...)** trên card P2
- **Độ phức tạp:** Trung bình (cần thêm Firebase listener cho "hasAnswered" status)
- **File cần sửa:** `FirebaseMatchProvider.cs`, `GameplayUIController_UXML.cs`

### UX-03: Màn hình tóm tắt giữa các turn
- **Hiện trạng:** Sau 1.5s feedback, câu mới xuất hiện ngay — không có tóm tắt.
- **Nâng cấp đề xuất:**
  - Sau mỗi câu, hiện overlay 2-3 giây:
    - "Bạn: ✅ Đúng (+10)" / "Bạn: ❌ Sai"
    - "Đối thủ: ✅ Đúng" / "Đối thủ: ❌ Sai"
    - Score hiện tại: "50 — 40"
    - Transition slide sang câu tiếp
- **Độ phức tạp:** Trung bình
- **File cần sửa:** `GameplayUIController_UXML.cs`, thêm UXML template `TurnSummary.uxml`

### UX-04: Countdown visual 3-2-1 trước khi bắt đầu match
- **Hiện trạng:** Event `OnCountdownTick` được fire (3→2→1) nhưng KHÔNG CÓ UI element nào hiển thị. Countdown chạy ngầm, game đột ngột bắt đầu.
- **Nâng cấp đề xuất:**
  - Full-screen overlay với số "3", "2", "1", "GO!" — scale animation lớn → nhỏ, opacity fade
  - Sound effect tick cho mỗi số
- **Độ phức tạp:** Thấp
- **File cần sửa:** `GameplayUIController_UXML.cs` (subscribe `OnCountdownTick`)

### UX-05: Loading/transition animation giữa scenes
- **Hiện trạng:** `SceneManager.LoadSceneAsync()` chuyển scene đột ngột — không có transition.
- **Nâng cấp đề xuất:**
  - Fade-to-black hoặc slide transition khi chuyển HomeScene → GameplayScene
  - Loading indicator (spinner hoặc progress bar) khi async loading
  - Có thể dùng `DontDestroyOnLoad` canvas overlay
- **Độ phức tạp:** Trung bình
- **File cần sửa:** `GameManager.cs`, tạo `SceneTransition.cs` mới

### UX-06: Matchmaking timeout + Cancel UI
- **Hiện trạng:** Khi bấm "Find Match", nếu không có đối thủ, game chờ VĨnh VIỄN. Không có nút hủy, không có timeout.
- **Nâng cấp đề xuất:**
  - Timeout 30-60 giây, hiển thị "Đang tìm đối thủ... (15s)"
  - Nút "Hủy" rõ ràng
  - Sau timeout: "Không tìm thấy đối thủ. Thử lại?" hoặc tự động chuyển Practice mode
- **Độ phức tạp:** Trung bình
- **File cần sửa:** `MainMenuUIController_UXML.cs`, `FirebaseManager.cs`

### UX-07: Sound effects bổ sung
- **Hiện trạng:** Có BGM, correct/wrong SFX, win/lose SFX. Thiếu countdown ticks.
- **Nâng cấp đề xuất:**
  - Sound **tick** cho countdown 3-2-1
  - Sound **"GO!"** khi match bắt đầu
  - Sound **tick-tock** khi timer còn ≤ 5 giây (tăng urgency)
  - Sound **swoosh** khi câu mới slide vào
- **Độ phức tạp:** Thấp
- **File cần sửa:** `AudioManager.cs`, `GameplayUIController_UXML.cs`

### UX-08: Xử lý nút Back Android
- **Hiện trạng:** Bấm nút Back trên Android không có phản hồi — game không thoát, không quay lại.
- **Nâng cấp đề xuất:**
  - Trong GameplayScene: hiện Exit confirmation popup
  - Trong HomeScene: hiện "Bạn muốn thoát game?"
  - Trong popup: đóng popup thay vì thoát game
- **Độ phức tạp:** Thấp
- **File cần sửa:** Thêm `Update()` check `Input.GetKeyDown(KeyCode.Escape)` vào controllers

---

## 4. PHẦN C — TÍNH NĂNG MỞ RỘNG TƯƠNG LAI

### Ưu tiên P1 — Quan trọng nhất

| # | Tính năng | Mô tả | Độ phức tạp |
|---|-----------|-------|-------------|
| F-01 | **Mở rộng ngân hàng câu hỏi** | Hiện chỉ có ~10 câu/ngôn ngữ × 5 chủ đề. Cần ít nhất 100+ câu. Thêm chọn chủ đề trước match. | Cao |
| F-02 | **Reconnection logic** | Brief disconnect (~5-10s) không nên kết thúc match ngay. Thêm grace period + reconnect UI | Cao |

### Ưu tiên P2 — Nên có

| # | Tính năng | Mô tả | Độ phức tạp |
|---|-----------|-------|-------------|
| F-03 | **Leaderboard** | Bảng xếp hạng toàn cầu/theo tuần. UI button đã có sẵn trong UXML (đang `display:none`). Cần Firebase Cloud Functions hoặc query trực tiếp. | Trung bình |
| F-04 | **Lịch sử trận đấu** | Lưu match history (đối thủ, kết quả, score, thời gian). Hiển thị trong Profile. | Trung bình |
| F-05 | **Experience bar** | Thanh XP progress visual trên Home screen. Hiện chỉ hiện level text, không có progress bar. | Thấp |

### Ưu tiên P3 — Tốt nếu có

| # | Tính năng | Mô tả | Độ phức tạp |
|---|-----------|-------|-------------|
| F-06 | **Hoàn thiện 6 ngôn ngữ** | fr, it, de, es, ja, ko — hiện chỉ có vi, en. Cần dịch ~60 UI keys + 100+ câu hỏi/ngôn ngữ. | Cao (nội dung) |
| F-07 | **Room cleanup** | Firebase rooms bị orphan khi host crash. Cần Cloud Function scheduled để dọn rooms cũ > 1h. | Thấp |

### Ưu tiên P4 — Nice-to-have

| # | Tính năng | Mô tả | Độ phức tạp |
|---|-----------|-------|-------------|
| F-08 | **Invite friend** | Chia sẻ room code cho bạn bè, không cần matchmaking random. | Trung bình |
| F-09 | **Spectator mode** | Xem trận đấu của người khác. | Cao |
| F-10 | **Power-ups** | 50/50 (loại 2 đáp án sai), Thêm thời gian (+5s), Đóng băng đối thủ (pause timer đối thủ 3s). | Cao |

---

## 5. THỨ TỰ TRIỂN KHAI ĐỀ XUẤT

### Sprint 1: Sửa Critical Bugs (ước tính: 2-3 ngày)
1. BUG-01: Race condition timer/feedback
2. BUG-02: Bot answer bị mất khi timer hết
3. BUG-04: Button handler tích lũy
4. BUG-05: SignOut → IsConnected
5. BUG-03: Online AFK timeout

### Sprint 2: Sửa Medium Bugs + Quick UX wins (ước tính: 2-3 ngày)
1. BUG-06 → BUG-10: Các medium bugs
2. UX-04: Countdown visual (dễ, impact cao)
3. UX-08: Android back button
4. UX-07: Sound effects bổ sung

### Sprint 3: UX Nâng cao (ước tính: 3-5 ngày)
1. UX-01: Hiệu ứng đúng/sai nâng cao
2. UX-02: Trạng thái đối thủ real-time
3. UX-03: Màn hình tóm tắt giữa turns
4. UX-05: Scene transition
5. UX-06: Matchmaking timeout

### Sprint 4: Tính năng mở rộng (ước tính: ongoing)
1. F-01: Mở rộng câu hỏi (P1)
2. F-02: Reconnection (P1)
3. F-03: Leaderboard (P2)
4. F-05: Experience bar (P2)

### Sprint 5+: Polish & Scale
- F-04, F-06, F-07, F-08, F-09, F-10

---

> **Ghi chú:** Plan này dựa trên phân tích source code tại thời điểm 25/05/2026. Các line number có thể thay đổi sau khi sửa code. Mỗi bug/feature nên được test trên cả online mode và offline mode.

---

## 6. PHẦN D — UI ENHANCEMENT PLAN (28 items)

> Các đề xuất cải thiện giao diện người dùng, phân loại theo độ phức tạp.  
> **Tham khảo chi tiết tại:** [Chat Cursor ngày 25/05/2026]

### Sprint UI-1: Dễ — Impact cao (7 items)

| # | Thay đổi | File | Mô tả |
|---|----------|------|-------|
| G-01 | **Gradient nền động** | `GlobalStyles.uss` — `.bg-gradient` | Thay tím đơn sắc bằng gradient 3 tông (tím đậm → tím → xanh đậm) tạo chiều sâu |
| G-02 | **Glassmorphism cards** | `GlobalStyles.uss` — `.card` | `backdrop-filter: blur(12px)`, nền trong suốt `rgba(255,255,255,0.12)` + border mờ |
| G-03 | **Box shadow nhất quán** | `GlobalStyles.uss` — `.card`, `.btn` | Shadow tạo lớp lang, popup nổi trên nền game |
| G-04 | **Pulse animation loading** | `GlobalStyles.uss` — `.progress-fill` | `@keyframes pulse` cho thanh loading, breathing glow |
| G-05 | **Hover scale card** | `GlobalStyles.uss` — `.card:hover` | `scale: 1.02` — phản hồi tinh tế khi rê chuột |
| G-19 | **Backdrop blur popup** | `GlobalStyles.uss` — `.overlay` | `backdrop-filter: blur(5px)` thay vì overlay đen đặc |
| G-22 | **Custom font** | `GlobalStyles.uss` + tải font | Nhúng Google Font (Poppins/Inter) — cá tính, đẹp hơn font mặc định |

### Sprint UI-2: Trung bình (10 items)

| # | Thay đổi | File | Mô tả |
|---|----------|------|-------|
| G-06 | **Answer button flip animation** | `GameplayLayout.uxml` + `InputController_UXML.cs` | Hiệu ứng "lật bài" hoặc glow cho 4 nút đáp án |
| G-07 | **Logo gradient + glow** | `InitLayout.uxml` | Logo PVP phát sáng viền, font độc đáo |
| G-08 | **Loading tips** | `InitSceneController_UXML.cs` | Hiển thị tips học thuật ngẫu nhiên khi load game |
| G-09 | **Progress bar gradient** | `InitLayout.uxml` | Thanh loading đổi màu từ tím → xanh dương |
| G-10 | **Hero banner** | `HomeLayout.uxml` | Banner tên game có glow, phân cấp thị giác rõ |
| G-12 | **XP progress bar** | `HomeLayout.uxml` + `MainMenuUIController_UXML.cs` | Thanh XP dưới level tag, % hoặc fraction |
| G-14 | **Matchmaking spinner** | `HomeLayout.uxml` | Vòng quay/spinner thay progress bar khi tìm trận |
| G-16 | **Timer ring** | `GameplayLayout.uxml` + `GameplayUIController_UXML.cs` | Vòng tròn countdown (ring) thay timer bar ngang |
| G-18 | **Score fly text** | `InputController_UXML.cs` | "+10" bay lên từ nút đáp án, biến mất sau 1s |
| G-23 | **Letter-spacing buttons** | `GlobalStyles.uss` — `.btn` | `letter-spacing: 2px` cho button text |

### Sprint UI-3: Nâng cao (11 items)

| # | Thay đổi | Mô tả |
|---|----------|-------|
| G-11 | **UX hierarchy: Find Match nổi bật nhất** | Nút Find Match to hơn, gradient riêng, Practice nhỏ hơn |
| G-13 | **Daily reward indicator** | Icon quà nhấp nháy góc dưới phải (placeholder) |
| G-15 | **Player card redesign** | Avatar lớn hơn, thanh "HP" animation theo score |
| G-17 | **Question card glowing border** | Border glow pastel khi câu hỏi mới xuất hiện |
| G-20 | **Tab-style auth popup** | Chuyển 5 container ẩn/hiện → tab navigation |
| G-21 | **Result popup trophy + confetti** | Icon 🏆/🌟 + confetti particle animation |
| G-24 | **Typography hierarchy nhất quán** | Title 80px → Button 42px → Body 30px → Caption 20px |
| G-25 | **Particle background** | Particle system nền (sao bay/bong bóng) |
| G-26 | **Dark/Light mode toggle** | Theme switch, đổi USS variables |
| G-27 | **Parallax home screen** | Background layer di chuyển chậm |
| G-28 | **Avatar idle animation** | Avatar xoay nhẹ/pulse nhẹ |

---

## 7. HƯỚNG DẪN THAY THẾ AVATAR

### Hiện trạng
- `avatarSprites` là `Sprite[]` gán trong Inspector (`MainMenuUIController_UXML` và `GameplayUIController_UXML`)
- 8 avatar được lấy tạm trên mạng, index lưu trong `PlayerData.avatarIndex`
- Hiển thị qua `style.backgroundImage = new StyleBackground(sprite)`

### Các giải pháp thay thế

| Phương án | Độ khó | Chi phí | Mô tả |
|-----------|--------|---------|-------|
| **A. Asset Store Free** | Thấp | $0 | Tải "Simple 2D Avatar Icons" từ Unity Asset Store, thay sprite array |
| **B. DiceBear API** | Trung bình | $0 | `UnityWebRequest` gọi `api.dicebear.com` → avatar SVG độc đáo từ tên |
| **C. Initial Avatar (chữ cái)** | Thấp | $0 | Tự sinh vòng tròn màu + chữ cái đầu từ tên — không cần ảnh |
| **D. Emoji/Icon font** | Thấp | $0 | Dùng emoji 😎 hoặc icon từ Material Icons font |
| **E. Custom draw** | Cao | Tùy | Vẽ sprite riêng bằng Photoshop/Aseprite |

**Khuyến nghị:** Kết hợp **B + C** — dùng DiceBear cho online (avatar unique từ UID), fallback initial avatar khi không có mạng. Hoặc đơn giản nhất là **A + C**.
