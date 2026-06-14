# Hướng dẫn Setup & Test Online PvP trên 2 máy
**Ngày:** 29/04/2026
**Tiền đề:** Đã chạy được phiên bản offline (bot) thành công.

---

## PHẦN A — SETUP FIREBASE (làm 1 LẦN)

### A.1. Tạo Firebase Project
1. Mở https://console.firebase.google.com → **Add project** (đặt tên: `PvP-QuizGame` hoặc gì tuỳ bạn).
2. Bỏ tích Google Analytics nếu chỉ test (không cần). → Create.

### A.2. Bật Authentication
1. Vào project → Sidebar **Authentication** → **Get started**.
2. Tab **Sign-in method** → chọn **Anonymous** → **Enable** → Save.

### A.3. Tạo Realtime Database
1. Sidebar **Build → Realtime Database** → **Create database**.
2. Chọn **Singapore (asia-southeast1)** (gần VN nhất, latency tốt).
3. Chọn **Start in test mode** (tạm thời).
4. Sau khi tạo xong → Tab **Rules** → dán JSON dưới rồi **Publish**:
```json
{
  "rules": {
    ".read": "auth != null",
    ".write": "auth != null",
    "matchmakingQueue": {
      "$uid": {
        ".validate": "$uid === auth.uid"
      }
    },
    "users": {
      "$uid": {
        ".write": "$uid === auth.uid"
      }
    },
    "rooms": {
      "$roomId": {
        ".read": "auth != null",
        ".write": "auth != null"
      }
    }
  }
}
```

### A.4. Tải config files cho Unity
1. Project Overview (icon ⚙️) → **Project Settings**.
2. Tab **General** → kéo xuống "Your apps" → click icon **Unity** (chữ "Unity" hình logo).
3. Đăng ký 2 platform:
   - **iOS+** : đặt Bundle ID (vd: `com.yourname.pvpquiz`).
   - **Android**: đặt Package Name (giống vậy).
4. Tải về:
   - `google-services.json` → đặt vào `Assets/` (Unity sẽ tự convert).
   - `GoogleService-Info.plist` → đặt vào `Assets/`.
   - **Desktop:** click "**Configure Desktop**" hoặc nút trong Firebase plugin Unity → tải `google-services-desktop.json` → vào `Assets/StreamingAssets/`.

> **Lưu ý:** Project hiện đã có sẵn `Assets/StreamingAssets/google-services-desktop.json` — kiểm tra xem có đúng project bạn vừa tạo không. Nếu không, **xoá đi và tải lại** từ project mới.

### A.5. Verify trong Unity
1. Mở project Unity.
2. Menu **Window → Asset Store** (hoặc Package Manager) → kiểm tra Firebase SDK đã import:
   - Firebase **App** ✓
   - Firebase **Auth** ✓
   - Firebase **Database** ✓
3. Nếu chưa, tải Firebase Unity SDK 12.x từ [firebase.google.com/download/unity](https://firebase.google.com/download/unity), import 3 module trên.

---

## PHẦN B — CẤU HÌNH SCENE TRONG UNITY

### B.1. InitScene
Mở `Assets/Scenes/InitScene.unity`:
1. Trên GameObject **InitSceneController** (có script `InitSceneController_UXML`):
   - Inspector: kéo `Assets/UI/Layouts/AuthPopup.uxml` vào field **Auth Popup Template**.
2. Đảm bảo trong scene có các GameObject:
   - **GameManager** (script `GameManager.cs`) — singleton DontDestroyOnLoad.
   - **LocalizationManager** (script `LocalizationManager.cs`).
     - Field `Sheet Url`: để TRỐNG hoặc dán link CSV Google Sheet (tuỳ ý).
   - **PlayerDataManager** (script `PlayerDataManager.cs`) + kéo PlayerData asset vào.
   - **FirebaseManager** (script `FirebaseManager.cs`).
     - Field `Is Offline Mode`: **TẮT** (uncheck) khi test online.

### B.2. HomeScene
Mở `Assets/Scenes/HomeScene.unity`:
1. GameObject có UIDocument (gắn `HomeLayout.uxml`):
   - Cùng GameObject này gắn script `MainMenuUIController_UXML`.
   - Field **Settings Popup Template** → kéo `Assets/UI/Layouts/SettingsPopup.uxml` vào.

### B.3. GameplayScene
Mở `Assets/Scenes/GameplayScene.unity`:
1. GameObject có UIDocument (gắn `GameplayLayout.uxml`):
   - Cùng GameObject gắn cả 2 script: `GameplayUIController_UXML` + **`InputController_UXML`** (CHÍNH GIỮA TRÊN UIDOCUMENT — quan trọng!).
   - Field **Result Popup Template** trong GameplayUIController → kéo `Assets/UI/Layouts/ResultPopup.uxml`.
2. Tạo riêng GameObject **GameController** trong scene:
   - Gắn 4 script: `GameController.cs`, `QuizManager.cs`, `ScoreManager.cs`, `TimerController.cs`.
   - Trong GameController inspector: kéo chính nó vào 3 field `Quiz Manager`, `Score Manager`, `Timer Controller`.
3. Tạo GameObject **MatchProviders**:
   - Gắn cả `LocalMatchProvider.cs` và `FirebaseMatchProvider.cs`.
   - Tạo thêm `MockOpponent.cs` (chỉ chạy khi `isOfflineMode = true`).

> **Mẹo:** mở Console khi Play. Nếu thấy log `[InputController_UXML] QueryButtons OK — tìm thấy 4/4 nút.` là OK.

---

## PHẦN C — BUILD STANDALONE WINDOWS

### C.1. Cài đặt Build Settings
1. **File → Build Settings**.
2. Platform: **Windows, Mac, Linux** → Switch Platform nếu chưa.
3. **Add Open Scenes** theo thứ tự: InitScene (index 0), HomeScene (1), GameplayScene (2).
4. **Player Settings** → **Other Settings**:
   - **Active Input Handling** = **Both** (để keyboard fallback hoạt động).
5. Build → chọn folder `Builds/Win64/` → đặt tên `PvPQuiz.exe`.

### C.2. Copy build cho máy 2
- Sau khi build xong, có folder `PvPQuiz_Data/` + `PvPQuiz.exe`.
- Zip cả folder lại → copy sang máy 2 qua USB / Google Drive / Discord / WeTransfer.
- Máy 2 giải nén ra → chạy `PvPQuiz.exe`.

> **Cảnh báo bảo mật Windows:** Lần đầu chạy Windows Defender SmartScreen có thể block. Click "More info" → "Run anyway".

---

## PHẦN D — TESTING TRÊN 2 MÁY

### D.1. Happy path — 2 người chơi xong 1 trận
**Máy A (vd: laptop của bạn):**
1. Mở `PvPQuiz.exe`.
2. InitScene chạy progress → AuthPopup hiện ra → nhập tên `ChienA` → Confirm.
3. Vào HomeScene → bấm **TÌM TRẬN**.
4. Hiển thị "ĐANG TÌM ĐỐI THỦ..." → ĐỢI ở đây.

**Máy B (vd: máy bạn của bạn):**
1. Trong vòng 30s sau khi máy A bấm Tìm Trận, mở `PvPQuiz.exe`.
2. Nhập tên `ChienB` → Confirm.
3. HomeScene → bấm **TÌM TRẬN**.

**Kết quả mong đợi:**
- Cả 2 máy đồng thời chuyển từ matchmaking panel → GameplayScene.
- Máy A thấy "OPP" hiển thị tên `ChienB`. Máy B thấy "OPP" = `ChienA`.
- Countdown 3-2-1 → cả 2 thấy CÙNG câu hỏi đầu tiên (vd: "CPU là viết tắt của từ gì?").
- Đáp án 4 nút có cùng nội dung và cùng vị trí trên 2 máy.
- Khi máy A chọn → nút đổi vàng, hiện "ĐANG ĐỢI...". Đến khi máy B cũng chọn xong → cả 2 đồng thời thấy đáp án đúng (xanh) / sai (đỏ) → chuyển câu kế.
- Hết 10 câu hoặc hết 3 phút → ResultPopup hiện ở cả 2 máy với thắng/thua/hoà tương ứng.
- Bấm "Về Sảnh" → quay lại HomeScene, money/level đã cộng (kiểm tra góc trên HomeScene).

### D.2. Disconnect test
**Cách 1 — đóng đột ngột:**
- Máy A đang chơi → tắt Wi-Fi hoặc đóng app bằng Task Manager.
- Máy B sau ~5-10s sẽ nhận log `[FirebaseManager] Đối thủ ngắt kết nối!` → ResultPopup hiện THẮNG (do đối thủ rời).

**Cách 2 — bấm "Về Sảnh" giữa trận:**
- Máy A bấm "Về Sảnh" — chưa làm UI cho phép việc này, có thể skip. Hoặc dùng cách 1.

### D.3. Same machine test (debug nhanh không cần máy 2)
Để test online flow mà chỉ có 1 máy:
1. Build standalone như trên.
2. Mở Unity Editor → Play (instance 1).
3. Đồng thời chạy `PvPQuiz.exe` (instance 2).
4. Cả 2 sẽ thấy nhau trong queue.

> **Lưu ý:** mỗi instance phải có UID khác nhau. Vì SignInAnonymously sinh UID mới mỗi lần, OK.

---

## PHẦN E — DEBUGGING

### E.1. Console logs để track
| Log mong đợi | Phase |
|---|---|
| `[Localization] Đã nạp JSON local: NN key (lang=vi)` | Localization OK |
| `[FirebaseManager] Firebase đã sẵn sàng!` | Firebase init OK |
| `[FirebaseManager] SignIn Anonymous OK. UID = ...` | Auth OK |
| `[FirebaseManager] Tạo room ... (Host=True)` hoặc `Đã join room ... (Host=False)` | Match found |
| `[GameController] Online seed (từ room): NNNNNN` | Seed sync OK |
| `[InputController_UXML] QueryButtons OK — tìm thấy 4/4 nút.` | UI OK |
| `[FirebaseMatchProvider] Cả 2 đã trả lời. Me=N, Opp=N` | Sync answers OK |

### E.2. Lỗi thường gặp

**1. "Firebase chưa sẵn sàng" / không SignIn được**
- Kiểm tra `google-services-desktop.json` đã đúng project chưa.
- Mở Firebase Console → Authentication → tab **Users** → xem có UID xuất hiện không.

**2. 2 máy không thấy nhau trong queue**
- Mở Firebase Console → Realtime Database → xem node `matchmakingQueue` có data của 2 máy không.
- Nếu node trống → có thể rule chặn write. Kiểm tra Rules trong A.3.

**3. Câu hỏi 2 máy khác nhau**
- Seed không sync. Mở Realtime Database → `rooms/<roomId>/seed` xem có giá trị giống nhau ở 2 client không (1 number).

**4. Nút answer không update text**
- Xem log `[InputController_UXML] QueryButtons` — nếu thấy `0/4 nút` thì InputController không cùng GameObject với UIDocument.
- Sửa: trong GameplayScene, kéo script InputController_UXML lên CHÍNH GameObject có UIDocument.

**5. Điểm P2 không cập nhật**
- Mở Realtime Database → `rooms/<roomId>/scores` xem có ghi gì không.
- Nếu có, kiểm tra OpponentId trong FirebaseManager đã đúng UID máy bạn không.

---

## PHẦN F — MONITORING qua Firebase Console

Khi đang test, mở 2 tab Firebase Console:
- Tab 1: **Realtime Database → Data** — xem live structure các node `users`, `matchmakingQueue`, `rooms` thay đổi real-time.
- Tab 2: **Authentication → Users** — xem danh sách UID đã sign in.

Đây là cách verify mạnh nhất xem matchmaking, room sync, score sync có hoạt động đúng không.

---

## PHẦN G — CLEANUP DỮ LIỆU TEST

Sau mỗi buổi test, có thể clean Firebase để demo lại sạch:
- Realtime Database → click 3 chấm cạnh node → **Delete**.
- Hoặc paste vào Rules tạm thời:
  ```json
  { "rules": { ".read": false, ".write": false } }
  ```
  → reload → mọi data bị "ẩn" (không xoá thật).

---

## PHẦN H — ROADMAP TIẾP THEO (sau khi PvP chạy ngon)

1. **Leaderboard:** node `users/{uid}/level` đã có → query top 10 theo level.
2. **Friend Match:** thay vì queue ngẫu nhiên, cho phép tạo room với mã 6 chữ số.
3. **Voice / Emoji react** trong trận: ghi node `rooms/{id}/reactions/{uid}` với TTL.
4. **Rank/Elo** sau mỗi trận PvP.
5. **Daily Quest** ghi xuống `users/{uid}/quests`.

---

**Chúc test thành công! 🎮**

Có lỗi gì lạ trong Console hoặc UI thì copy log và báo, tôi sẽ debug tiếp.
