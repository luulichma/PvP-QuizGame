# Plan Hoàn Thiện PvP Quiz Game (Online Mode)
**Ngày lập:** 29/04/2026
**Mục tiêu:** Game chơi được trọn vẹn từ Login → Matchmaking → Trận đấu → Result, hỗ trợ 2 client thật trên 2 máy khác nhau qua Firebase. Offline mode chỉ giữ lại để dev test.

---

## 0. Tổng quan luồng hoàn chỉnh

```
┌──────────────┐   Anonymous Sign-In  ┌──────────────┐    Find Match    ┌──────────────┐
│  InitScene   │ ───────────────────▶ │  HomeScene   │ ────────────────▶│  Matchmaking │
│  + Auth      │                      │  (Lobby)     │                   │  (Queue)     │
└──────────────┘                      └──────────────┘                   └──────┬───────┘
                                                                                 │ Both joined
                                                                                 ▼
┌──────────────┐  Game Over + Reward  ┌──────────────┐    Sync Q&A      ┌──────────────┐
│ Result Popup │ ◀──────────────────  │ GameplayScene│ ◀────────────────│  Room Ready  │
│ + Cloud Save │                      │  (PvP)       │                   │  (Same Seed) │
└──────────────┘                      └──────────────┘                   └──────────────┘
```

---

## 1. Schema Firebase Realtime Database

```
{
  "users": {
    "<uid>": {
      "displayName": "ChienK20",
      "level": 5,
      "currentExp": 120,
      "money": 1250,
      "lastSeen": 1746012345
    }
  },

  "matchmakingQueue": {
    "<uid>": {
      "displayName": "ChienK20",
      "joinedAt": 1746012345
    }
  },

  "rooms": {
    "<roomId>": {
      "createdAt": 1746012345,
      "seed": 481923572,
      "state": "waiting" | "playing" | "ended",
      "players": {
        "<uidP1>": { "name": "ChienK20", "ready": true },
        "<uidP2>": { "name": "Mai99",    "ready": true }
      },
      "currentQ": 0,
      "answers": {
        "<uidP1>": 2,
        "<uidP2>": 0
      },
      "scores": {
        "<uidP1>": 30,
        "<uidP2>": 20
      },
      "winner": "<uidP1>" | "draw" | null
    }
  }
}
```

---

## 2. Phase Roadmap

### **Phase A — Firebase Project Setup (NGOÀI CODE)**
1. Tạo Firebase project tại [console.firebase.google.com](https://console.firebase.google.com).
2. **Authentication** → Sign-in method → bật **Anonymous**.
3. **Realtime Database** → Create database → chọn **us-central1** (hoặc gần VN nhất là **asia-southeast1**).
4. Database **Rules** (test mode, đủ cho khoá luận):
   ```json
   {
     "rules": {
       ".read": "auth != null",
       ".write": "auth != null",
       "matchmakingQueue": {
         "$uid": { ".validate": "$uid === auth.uid" }
       },
       "users": {
         "$uid": { ".write": "$uid === auth.uid" }
       }
     }
   }
   ```
5. Project Settings → tải về:
   - `google-services.json` (Android) → `Assets/`
   - `GoogleService-Info.plist` (iOS) → `Assets/`
   - **Desktop:** `google-services-desktop.json` (đã có, kiểm tra đúng project chưa).

### **Phase B — Auth Layer (Code)**
- `InitScene` sau khi LocalizationManager ready → gọi `FirebaseManager.SignInAnonymous()`.
- Lưu `displayName` (PlayerPrefs lần đầu, popup cho user nhập).
- Sau khi Auth thành công → load `users/{uid}` (cloud save), nếu chưa có thì tạo mới.

### **Phase C — Matchmaking thật (Code)**
**Algorithm queue-based với Firebase Transaction:**

1. Player A bấm "Tìm trận":
   - Run transaction trên `matchmakingQueue`:
     - Nếu queue **rỗng** → ghi `matchmakingQueue/<uidA>: { name, joinedAt }` rồi đợi.
     - Nếu queue **có 1 người chờ** (B) → xoá B khỏi queue, tạo room mới `rooms/<roomId>`.
2. Player A đang đợi → listen `users/<uidA>/currentRoom` (hoặc `matchmakingQueue/<uidA>` bị xoá là dấu hiệu được ghép).
3. Cả 2 client đọc room → load GameplayScene với `roomId`.

### **Phase D — Room Sync trong trận**
- Khi vào GameplayScene, cả 2 client:
  - Đọc `seed` từ room → `QuizManager.StartQuiz(seed)` → câu hỏi giống nhau, đáp án shuffle giống nhau.
  - Listen `rooms/<roomId>/answers` → khi cả 2 có giá trị → trigger `OnBothPlayersAnswered`.
  - Sau mỗi câu, một client (host = uidP1) ghi `currentQ++` và **clear answers**.
- **Score:** mỗi client tự chấm điểm cho mình rồi `set scores/<myUid>`. Listen `scores/<opponentUid>` để hiển thị điểm đối thủ.

### **Phase E — End Match + Cloud Save**
- Khi `OnGameOver` (timer end hoặc câu hỏi hết):
  - Host ghi `rooms/<roomId>/state = "ended"` và `winner = <uid|"draw">`.
  - Cả 2 client đọc và hiển thị Result Popup.
  - Mỗi client ghi reward về `users/<myUid>` (level, exp, money).
  - Sau 10s hoặc khi user bấm "Về Sảnh" → xoá room.

### **Phase F — Disconnect Handling**
- Dùng Firebase `OnDisconnect()` API:
  - Khi vào queue: `OnDisconnect().RemoveValue()` để tự xoá nếu rớt mạng.
  - Khi vào room: ghi `rooms/<roomId>/players/<myUid>/online = true` + `OnDisconnect().setValue(false)`.
- Client còn lại phát hiện `online = false` → tự động thắng cuộc + về sảnh.

---

## 3. Files cần thay đổi

| File | Thay đổi |
|---|---|
| `FirebaseManager.cs` | **REWRITE**: SignIn, queue, matchmaking, cloud save, onDisconnect |
| `FirebaseMatchProvider.cs` | **REWRITE**: room listener, sync answers/scores/state |
| `GameController.cs` | Sửa `GenerateMatchSeed` để dùng seed từ Firebase room |
| `ScoreManager.cs` | Online: ghi điểm lên Firebase + listen điểm đối thủ |
| `MainMenuUIController_UXML.cs` | Bỏ FakeMatchmakingRoutine, gọi `FirebaseManager.JoinOrCreateRoom` |
| `GameplayUIController_UXML.cs` | Hiển thị tên người chơi (ME → tên mình, OPP → tên đối thủ) |
| `InitSceneController_UXML.cs` | Sau Localization → SignIn → load profile → next scene |
| `PlayerDataManager.cs` | Thêm `LoadFromCloud` + `SaveToCloud` |
| `AuthPopupController.cs` | **NEW**: hiển thị popup nhập tên lần đầu |
| `AuthPopup.uxml` | **NEW**: UXML cho popup nhập tên |

---

## 4. Quy ước Schema chi tiết

### 4.1 Room ID
- Format: `room_<6 ký tự ngẫu nhiên>` (ví dụ `room_a3f7b2`).
- Tạo bằng `Guid.NewGuid().ToString("N").Substring(0,6)`.

### 4.2 Seed deterministic
- Host (uidP1) sinh seed bằng `(int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF)`.
- Ghi vào `rooms/<roomId>/seed`.
- Cả 2 client `WaitUntil` đọc được seed → mới `StartQuiz(seed)`.

### 4.3 Phân vai Host vs Guest
- Trong room có 2 player; **uid nhỏ hơn (so sánh string)** = Host.
- Host chịu trách nhiệm:
  - Sinh `currentQ` increment.
  - Sinh trạng thái `state = "ended"` + `winner` cuối trận.
  - Xoá room sau khi cả 2 đã rời.

### 4.4 Sync câu hỏi
```
P1 chọn → ghi answers/{p1Uid} = 2
P2 chọn → ghi answers/{p2Uid} = 0
ValueChanged(answers) trên cả 2 client
  Nếu cả 2 có giá trị → tính điểm local
  Host ghi currentQ++ và RemoveValue(answers)
  Cả 2 nhận snapshot mới → load câu kế tiếp
```

### 4.5 Sync điểm
- Client tự chấm rồi `set scores/{myUid} = newScore`.
- Listen `scores/{opponentUid}` → update UI khi đối thủ ghi.

---

## 5. Edge Cases cần handle

| Tình huống | Xử lý |
|---|---|
| User bấm Cancel trong matchmaking | `RemoveValue(matchmakingQueue/{myUid})` + về Home |
| Đối thủ rời giữa trận | Listen `players/{oppUid}/online == false` → declare winner + về Home |
| Mất mạng tạm thời | onDisconnect tự xoá → đối thủ thấy → declare winner |
| Bot trận tay đôi (cả 2 cùng vào queue) | Transaction đảm bảo chỉ 1 ghép thành công |
| Sheet/JSON load fail | LocalizationManager fallback (đã làm) |
| Auth fail | Show error popup + retry button |

---

## 6. Testing Strategy 2 máy

1. Build Standalone Windows × 2.
2. Máy A mở game → InitScene → SignIn → HomeScene → "Tìm Trận".
3. Trong vòng 5s, máy B làm tương tự.
4. Cả 2 thấy "Đã tìm thấy trận!" → cùng vào GameplayScene.
5. Test các kịch bản:
   - **Happy path:** 2 người chơi xong, ai cao điểm hơn → Win, ai thấp → Lose, hoà → Draw.
   - **Disconnect:** giữa trận, máy A tắt game đột ngột → máy B nhận Win.
   - **Reward:** sau khi quay lại Home, kiểm tra money/exp/level đã cộng.
   - **Cloud Save:** xoá `LocalLow/.../PvPQuizGame/PlayerPrefs` trên máy → mở lại → vẫn còn data (vì đã sync cloud).

---

**Tiếp theo:** Tôi sẽ implement code theo thứ tự A→F. Sau đó là setup guide step-by-step.
