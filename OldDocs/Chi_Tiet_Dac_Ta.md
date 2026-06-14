# ĐẶC TẢ CHI TIẾT — PvP Quiz Game
> **Tài liệu bổ trợ** cho Bao_Cao_PvP_QuizGame.md  
> Bao gồm: 16 UC Specification đầy đủ · Mô tả sơ đồ lớp phân tích · Mô tả tất cả Sequence Diagram · Mô tả Detailed Class Diagram

---

## MỤC LỤC

1. [16 UC Specification](#1-16-uc-specification)
2. [Mô tả sơ đồ lớp phân tích (Analysis Class Diagram)](#2-mô-tả-sơ-đồ-lớp-phân-tích)
3. [Mô tả tất cả Sequence Diagram (Phân tích — Mục III.3)](#3-mô-tả-tất-cả-sequence-diagram-phân-tích)
4. [Mô tả Detailed Class Diagram (Mục IV.2.1)](#4-mô-tả-detailed-class-diagram)
5. [Mô tả Detailed Sequence Diagram (Mục IV.2.2)](#5-mô-tả-detailed-sequence-diagram)

---

# 1. 16 UC SPECIFICATION

---

## UC01 — Đăng ký tài khoản

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC01 |
| **Tên** | Đăng ký tài khoản |
| **Actor chính** | Người dùng chưa có tài khoản |
| **Actor phụ** | Firebase Authentication, Firebase Realtime Database |
| **Mô tả** | Người dùng tạo tài khoản mới bằng Tên hiển thị, Email và Mật khẩu. Sau khi đăng ký thành công, hệ thống khởi tạo bản ghi dữ liệu mặc định cho người chơi trên Firebase và chuyển vào HomeScene. |
| **Tiền điều kiện** | Người dùng đang ở màn hình Auth Popup (InitScene); chọn tab "Đăng ký" |
| **Hậu điều kiện — Thành công** | Tài khoản Firebase được tạo; bản ghi `users/{uid}` mặc định (Level 1, 0 EXP, 0 tiền) được ghi trên Realtime DB; người dùng vào HomeScene |
| **Hậu điều kiện — Thất bại** | Người dùng ở lại Auth Popup; thông báo lỗi hiển thị |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người dùng | Nhập Tên hiển thị, Email và Mật khẩu vào form Đăng ký |
| 2 | Hệ thống | Kiểm tra hợp lệ cục bộ: Email đúng định dạng, Mật khẩu ≥ 6 ký tự, Tên không rỗng |
| 3 | Hệ thống | Gọi `FirebaseManager.RegisterWithEmail(name, email, password)` |
| 4 | Firebase Auth | Tạo tài khoản mới, trả về `UserCredential` chứa `uid` |
| 5 | Hệ thống | Ghi bản ghi mặc định lên `users/{uid}`: `{displayName, level:1, currentExp:0, money:0, avatarIndex:0, lastSeen}` |
| 6 | Hệ thống | Fire `OnAuthSuccess`; tải dữ liệu vào `PlayerDataManager` |
| 7 | Hệ thống | `GameManager.LoadHomeScene()` |

**Luồng thay thế:**

- **2a — Dữ liệu nhập không hợp lệ:** Hiển thị thông báo lỗi tương ứng ngay trên form (Email sai định dạng / Mật khẩu quá ngắn / Tên trống); ở lại bước 1.
- **4a — Email đã tồn tại:** Firebase trả về lỗi `email-already-in-use`; hiển thị "Email này đã được đăng ký"; ở lại bước 1.
- **4b — Không có mạng:** Firebase trả về lỗi kết nối; hiển thị "Không thể kết nối. Vui lòng kiểm tra mạng"; ở lại bước 1.

---

## UC02 — Đăng nhập

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC02 |
| **Tên** | Đăng nhập |
| **Actor chính** | Người chơi đã có tài khoản |
| **Actor phụ** | Firebase Authentication, Firebase Realtime Database |
| **Mô tả** | Người chơi xác thực danh tính bằng Email và Mật khẩu. Sau khi thành công, hệ thống tải dữ liệu hồ sơ từ Firebase Cloud về thiết bị và chuyển vào HomeScene. |
| **Tiền điều kiện** | Người dùng đang ở Auth Popup; chọn tab "Đăng nhập" |
| **Hậu điều kiện — Thành công** | Session Firebase được lưu trên thiết bị; `PlayerData` được nạp từ Cloud; vào HomeScene |
| **Hậu điều kiện — Thất bại** | Ở lại Auth Popup; thông báo lỗi hiển thị |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người chơi | Nhập Email và Mật khẩu; nhấn "Đăng nhập" |
| 2 | Hệ thống | Gọi `FirebaseManager.LoginWithEmail(email, password)` |
| 3 | Firebase Auth | Xác thực thông tin; trả về `UserCredential` nếu thành công |
| 4 | Hệ thống | Đọc dữ liệu từ `users/{uid}` trên Realtime DB |
| 5 | Hệ thống | Nạp dữ liệu vào `PlayerDataManager` (name, level, exp, money, avatar) |
| 6 | Hệ thống | Fire `OnAuthSuccess`; `GameManager.LoadHomeScene()` |

**Luồng thay thế:**

- **3a — Sai mật khẩu:** Firebase trả về lỗi `wrong-password`; hiển thị "Email hoặc mật khẩu không đúng"; ở lại bước 1.
- **3b — Email không tồn tại:** Firebase trả về `user-not-found`; hiển thị thông báo lỗi tương ứng; ở lại bước 1.
- **2a — Không có mạng:** Hiển thị "Không thể kết nối"; gợi ý chuyển sang Chơi khách.
- **5a — Bản ghi `users/{uid}` không tồn tại:** Hệ thống tạo bản ghi mặc định (tương tự UC01 bước 5) rồi tiếp tục.

---

## UC03 — Đăng xuất

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC03 |
| **Tên** | Đăng xuất |
| **Actor chính** | Người chơi (đã đăng nhập hoặc Khách) |
| **Mô tả** | Người chơi kết thúc phiên làm việc, xóa session khỏi thiết bị và quay về màn hình khởi động. Nếu là tài khoản Khách, hệ thống cảnh báo sẽ mất dữ liệu cục bộ trước khi xác nhận. |
| **Tiền điều kiện** | Người chơi đang ở HomeScene; mở Settings Popup |
| **Hậu điều kiện — Thành công** | Session được xóa; dữ liệu cục bộ bị xóa (nếu là Khách); chuyển về InitScene |
| **Hậu điều kiện — Hủy** | Người chơi ở lại HomeScene |

**Luồng chính (Tài khoản đã đăng nhập):**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người chơi | Mở Settings (⚙️); nhấn nút "Đăng xuất" |
| 2 | Hệ thống | Gọi `FirebaseManager.Logout()` → Firebase Auth sign out |
| 3 | Hệ thống | Xóa session đã lưu trên thiết bị |
| 4 | Hệ thống | `GameManager.LoadInitScene()` |

**Luồng thay thế (Tài khoản Khách):**

- **1a:** Sau khi nhấn "Đăng xuất", hệ thống hiển thị Popup cảnh báo: *"Đăng xuất sẽ làm mất toàn bộ dữ liệu cục bộ (tiền, level, kinh nghiệm). Bạn có chắc không?"*
- **1a.1 — Xác nhận:** `PlayerDataManager.ClearData()` → Xóa tất cả `PlayerPrefs` → `LoadInitScene()`
- **1a.2 — Hủy:** Đóng popup; ở lại HomeScene.

---

## UC04 — Chơi khách (Guest Login)

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC04 |
| **Tên** | Chơi khách |
| **Actor chính** | Người dùng chưa muốn tạo tài khoản |
| **Mô tả** | Người dùng trải nghiệm game mà không cần Email/Mật khẩu. Chỉ cần nhập Tên hiển thị. Dữ liệu (Level, EXP, Tiền) chỉ lưu cục bộ trên thiết bị (`PlayerPrefs`) và không được đồng bộ Cloud. |
| **Tiền điều kiện** | Người dùng đang ở Auth Popup; chọn tab "Chơi khách" |
| **Hậu điều kiện — Thành công** | Người dùng vào HomeScene với dữ liệu mặc định (Level 1); không có tài khoản Firebase |
| **Hậu điều kiện — Ràng buộc** | Nút "TÌM TRẬN ĐẤU" bị vô hiệu hóa (chỉ có thể Đấu với máy) |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người dùng | Nhập Tên hiển thị (hoặc để trống); nhấn "Chơi khách" |
| 2 | Hệ thống | Nếu tên trống: tự động đặt tên mặc định `"Player_XXXX"` (XXXX = số ngẫu nhiên) |
| 3 | Hệ thống | Lưu tên vào `PlayerPrefs`; `PlayerDataManager.LoadData()` với giá trị mặc định |
| 4 | Hệ thống | Đặt `FirebaseManager.isOfflineMode = true` |
| 5 | Hệ thống | `GameManager.LoadHomeScene()` |

**Luồng thay thế:**

- Không có luồng thất bại — tên mặc định được dùng nếu người dùng không nhập.

---

## UC05 — Chỉnh sửa hồ sơ

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC05 |
| **Tên** | Chỉnh sửa hồ sơ |
| **Actor chính** | Người chơi đã đăng nhập |
| **Actor phụ** | Firebase Realtime Database |
| **Mô tả** | Người chơi thay đổi Tên hiển thị và/hoặc Avatar từ 8 lựa chọn có sẵn. Thay đổi được áp dụng ngay lập tức lên giao diện và đồng bộ lên Firebase. |
| **Tiền điều kiện** | Người chơi đang ở HomeScene; đã đăng nhập (không phải Khách) |
| **Hậu điều kiện — Thành công** | `PlayerData` được cập nhật; Firebase `users/{uid}` đồng bộ; Profile HUD hiển thị tên/avatar mới |
| **Hậu điều kiện — Thất bại** | Dữ liệu không thay đổi; thông báo lỗi (nếu có) |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người chơi | Nhấn vào vùng Avatar/Tên ở Profile HUD góc trên trái |
| 2 | Hệ thống | Mở Profile Popup hiển thị Tên hiện tại và lưới 8 Avatar |
| 3 | Người chơi | Sửa Tên và/hoặc chọn Avatar mới |
| 4 | Người chơi | Nhấn "Lưu thay đổi" |
| 5 | Hệ thống | Cập nhật `PlayerData.playerName` và `PlayerData.avatarIndex` |
| 6 | Hệ thống | `PlayerDataManager.SaveData()` → lưu vào `PlayerPrefs` |
| 7 | Hệ thống | Gọi `FirebaseManager` → cập nhật `users/{uid}/displayName` và `avatarIndex` |
| 8 | Hệ thống | Đóng popup; Profile HUD refresh hiển thị dữ liệu mới |

**Luồng thay thế:**

- **4a — Tên để trống:** Hệ thống không cho lưu; hiển thị "Tên không được bỏ trống".
- **7a — Mất kết nối:** Dữ liệu đã lưu cục bộ (bước 6) thành công; Firebase sync thất bại nhưng không block người dùng; thông báo "Sẽ đồng bộ lại khi có mạng".
- **5a — Người chơi Khách:** Bỏ qua bước 7; chỉ lưu `PlayerPrefs`.

---

## UC06 — Tìm trận đấu (Matchmaking)

*(Đã có trong báo cáo chính — giữ nguyên, bổ sung thêm chi tiết)*

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC06 |
| **Tên** | Tìm trận đấu (Matchmaking) |
| **Actor chính** | Người chơi đã đăng nhập |
| **Actor phụ** | Firebase Realtime Database |
| **Mô tả** | Người chơi kích hoạt chế độ tìm đối thủ. Hệ thống tự động ghép cặp qua Firebase Matchmaking Queue và chuyển cả 2 vào GameplayScene khi đủ người. |
| **Tiền điều kiện** | Người chơi ở HomeScene; đã xác thực Firebase; có kết nối mạng |
| **Hậu điều kiện — Thành công** | Room được tạo trên Firebase; cả 2 người vào GameplayScene với cùng roomId |
| **Hậu điều kiện — Thất bại** | Người chơi ở lại HomeScene; không có room nào tồn tại |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người chơi | Nhấn "TÌM TRẬN ĐẤU" |
| 2 | Hệ thống | Giao diện chuyển sang màn hình "Đang tìm đối thủ..."; hiển thị nút Hủy |
| 3 | Hệ thống | `FirebaseManager.StartMatchmaking()` → ghi `matchmakingQueue/{uid} = {name, joinedAt}` |
| 4 | Firebase | `OnValueChanged` lắng nghe node `matchmakingQueue` |
| 5 | Firebase | Phát hiện ≥ 2 UID trong queue → chọn cặp ghép |
| 6 | Hệ thống (Client đầu tiên vào = Host) | Tạo `rooms/{roomId}` với seed ngẫu nhiên, `state:"waiting"`, ghi 2 players; xóa cả 2 UID khỏi queue |
| 7 | Firebase | Cả 2 client nhận `OnValueChanged` của room → fire `FirebaseManager.OnMatchFound` |
| 8 | Hệ thống | Cả 2 `GameManager.LoadGameplayScene()` |

**Luồng thay thế:**

- **3a — Hủy tìm kiếm (UC07):** Người chơi nhấn "Hủy" → `CancelMatchmaking()` → xóa khỏi queue → về HomeScene.
- **4a — Không có ai trong queue:** Người chơi tự trở thành Host, tạo room rỗng và chờ người thứ 2.
- **7a — Đối thủ ngắt kết nối sau ghép:** `OnOpponentDisconnected` → xử thắng cho người ở lại.
- **3b — Lỗi kết nối Firebase:** `OnMatchmakingError` → thông báo lỗi → về HomeScene.

---

## UC07 — Hủy tìm trận

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC07 |
| **Tên** | Hủy tìm trận |
| **Actor chính** | Người chơi đã đăng nhập |
| **Actor phụ** | Firebase Realtime Database |
| **Mô tả** | Trong khi đang chờ Matchmaking, người chơi quyết định không muốn tìm trận nữa và quay về HomeScene. |
| **Tiền điều kiện** | Đang ở màn hình Matchmaking "Đang tìm đối thủ..."; UID đang có trong `matchmakingQueue` |
| **Hậu điều kiện — Thành công** | UID bị xóa khỏi `matchmakingQueue`; giao diện về trạng thái HomeScene bình thường |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người chơi | Nhấn nút "Hủy" trên màn hình Matchmaking |
| 2 | Hệ thống | `FirebaseManager.CancelMatchmaking()` |
| 3 | Firebase | `DELETE matchmakingQueue/{uid}` |
| 4 | Hệ thống | Dừng lắng nghe `matchmakingQueue` listener |
| 5 | Hệ thống | Giao diện chuyển về HomeScene bình thường (ẩn màn hình chờ) |

**Luồng thay thế:**

- **3a — Mất kết nối trước khi xóa:** Firebase sẽ tự xóa entry nhờ `onDisconnect()` handler (nếu được cấu hình); hoặc entry hết hạn sau timeout.
- **2a — Đã được ghép cặp trước khi hủy kịp:** `OnMatchFound` đã fire → điều hướng sang GameplayScene thay vì hủy.

---

## UC08 — Bắt đầu trận đấu

*(Đã có trong báo cáo chính)*

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC08 |
| **Tên** | Bắt đầu trận đấu |
| **Actor chính** | Hệ thống (tự động) |
| **Mô tả** | Sau khi vào GameplayScene (từ Matchmaking hoặc Đấu với máy), hệ thống khởi tạo: đồng bộ seed và số câu (Online) hoặc tính seed và số câu theo Level (Offline); đếm ngược 3-2-1 và bắt đầu vòng lặp câu hỏi. |
| **Tiền điều kiện** | Đã vào GameplayScene; Online: có `CurrentRoomId` hợp lệ; Offline: `isOfflineMode = true` |
| **Hậu điều kiện** | `GameState = Playing`; câu hỏi đầu tiên hiển thị; `TimerController` đang chạy |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Hệ thống | `GameController.Start()`: chờ `LocalizationManager.IsReady`; xác định Online/Offline |
| 2 | Hệ thống | `ChangeState(Countdown)`: reset điểm, chuẩn bị HUD |
| 3 | Hệ thống | Coroutine đếm ngược: fire `OnCountdownTick(3)` → `(2)` → `(1)` (mỗi giây 1 lần) |
| 4 | Hệ thống | `ChangeState(Playing)` |
| 5 | Hệ thống (Online) | `ReadSeedFromRoom()` và `ReadQuestionCountFromRoom()` — chờ kết quả async từ Firebase |
| 5 | Hệ thống (Offline) | `seed = DateTime.UtcNow.Ticks`; `questionCount` tra Remote Config theo Tier Level |
| 6 | Hệ thống | `QuizManager.StartQuiz(seed, questionCount)` → Fisher-Yates shuffle + shuffle đáp án |
| 7 | Hệ thống | `TimerController.StartTimer()` (lấy duration từ Remote Config) |
| 8 | Hệ thống | `QuizManager.NextQuestion()` → fire `OnQuestionChanged` → UI hiển thị câu 1 |

**Luồng thay thế:**

- **5a — Không đọc được seed từ Firebase:** Fallback sang seed random từ `DateTime.UtcNow.Ticks`.
- **6a — Không có câu hỏi nào trong database:** Log error; dừng trận.

---

## UC09 — Trả lời câu hỏi

*(Đã có trong báo cáo chính — bổ sung chi tiết Offline)*

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC09 |
| **Tên** | Trả lời câu hỏi |
| **Actor chính** | Người chơi |
| **Actor phụ** | Firebase Realtime Database (Online) / LocalMatchProvider (Offline) |
| **Mô tả** | Trong trận, người chơi chọn 1 trong 4 đáp án trước khi hết giờ. Hệ thống kiểm tra, hiển thị feedback màu sắc, cập nhật điểm số và chuyển câu tiếp. |
| **Tiền điều kiện** | `GameState = Playing`; `QuizManager.CurrentQuestion != null`; `TimerController` đang chạy |
| **Hậu điều kiện — Đúng** | `Player1Score += 10`; feedback xanh; điểm đẩy lên Firebase (Online) |
| **Hậu điều kiện — Sai hoặc hết giờ** | Điểm giữ nguyên; feedback đỏ; câu hỏi tiếp theo hoặc GameOver |

**Luồng chính (Online):**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Hệ thống | Hiển thị câu hỏi, 4 nút đáp án, bộ đếm ngược |
| 2 | Người chơi | Chạm vào 1 trong 4 nút đáp án |
| 3 | `InputController_UXML` | `GameController.SetLocalAnswer(index)` lưu lại; gửi lên Firebase: `rooms/{id}/answers/{uid} = index` |
| 4 | Hệ thống | Chờ `TimerController.OnTimerEnd` |
| 5 | `GameController` | `HandleTimerEnd()` → `RevealAndAdvance(p1Answer, -1)` |
| 6 | `ScoreManager` | `CheckAnswer(1, p1Answer)`: so với `correctAnswerIndex`; Đúng → `AddScore(1, 10)` → `UpdateMyScore()` |
| 7 | `InputController_UXML` | `ShowAnswerFeedback(correctIdx)`: đúng = xanh, sai = đỏ; chờ `revealDuration = 2.5s` |
| 8 | Hệ thống | `HasMoreQuestions()`? Có → `NextQuestion()` + `StartTimer()`; Không → `ChangeState(GameOver)` |

**Luồng chính (Offline):**

Giống Online, nhưng bước 3 gửi đáp án vào `LocalMatchProvider.SubmitAnswerP1()` thay vì Firebase. `MockOpponent` sẽ tự submit sau delay ngẫu nhiên. Khi cả 2 đã nộp hoặc hết giờ → `LocalMatchProvider.OnBothPlayersAnswered` → `RevealAndAdvance`.

**Luồng thay thế:**

- **2a — Không chọn, hết giờ:** `_currentLocalAnswer = -1`; `CheckAnswer(1, -1)` → Sai → +0 điểm.
- **4a — Đối thủ ngắt kết nối (Online):** `OnOpponentDisconnected` → `ForcedSurrender(Player2)` → `ChangeState(GameOver)` ngay.

---

## UC10 — Xem điểm đối thủ real-time

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC10 |
| **Tên** | Xem điểm đối thủ real-time |
| **Actor chính** | Người chơi |
| **Actor phụ** | Firebase Realtime Database |
| **Mô tả** | Trong suốt trận đấu Online, điểm số của đối thủ được cập nhật tự động lên HUD ngay khi đối thủ trả lời đúng. Người chơi không cần thực hiện thao tác nào — hệ thống tự động lắng nghe và hiển thị. |
| **Tiền điều kiện** | `GameState = Playing`; chế độ Online; `FirebaseMatchProvider` đã `AttachRoomListeners()` |
| **Hậu điều kiện** | Điểm P2 trên HUD phản ánh đúng điểm hiện tại của đối thủ |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Firebase | Đối thủ trả lời đúng → `UpdateMyScore(score)` → ghi lên `rooms/{id}/scores/{oppUid}` |
| 2 | `FirebaseMatchProvider` | Listener `OnValueChanged` tại `rooms/{id}/scores/{oppUid}` được trigger |
| 3 | `FirebaseMatchProvider` | Fire `OnOpponentScoreUpdated(newScore)` |
| 4 | `ScoreManager` | `SetOpponentScore(newScore)` → cập nhật `Player2Score` |
| 5 | `ScoreManager` | Fire `OnScoreChanged(p1Score, p2Score)` |
| 6 | `GameplayUIController` | Nhận event → Cập nhật label điểm P2 trên HUD |

**Luồng thay thế:**

- **1a — Đối thủ trả lời sai:** Không có gì ghi lên Firebase (điểm không tăng) → HUD không thay đổi.
- **2a — Mất kết nối tạm thời:** Điểm hiển thị cuối cùng được giữ lại cho đến khi kết nối phục hồi.

---

## UC11 — Đầu hàng / Thoát giữa trận

*(Đã có trong báo cáo chính)*

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC11 |
| **Tên** | Đầu hàng / Thoát giữa trận |
| **Actor chính** | Người chơi |
| **Mô tả** | Người chơi muốn rời khỏi trận đang diễn ra. Hệ thống cảnh báo hậu quả (thua) và thực hiện nếu xác nhận. |
| **Tiền điều kiện** | `GameState = Playing` |
| **Hậu điều kiện — Xác nhận** | P1 thua, P2 thắng; Online: `rooms/{id}/winner` cập nhật; chuyển sang `GameOver` |
| **Hậu điều kiện — Hủy** | Trận tiếp tục bình thường |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người chơi | Nhấn nút ✖ ở góc trên trái GameplayScene |
| 2 | Hệ thống | Tạm dừng `TimerController`; hiển thị Exit Confirm Popup |
| 3 | Người chơi | Nhấn "Xác nhận rời" |
| 4 | `GameController` | `ForcedSurrender()` → `ScoreManager.SetForcedWinner(WinResult.Player2Wins)` |
| 5 | `GameController` | `ChangeState(GameOver)` |
| 6 | Hệ thống (Online + Host) | `FirebaseManager.HostEndMatch(winner = OpponentId)` |
| 7 | Hệ thống | `AwardRewards()` với kết quả THUA; hiển thị Result Popup |

**Luồng thay thế:**

- **3a — Nhấn "Hủy":** Đóng popup; `TimerController.ResumeTimer()`; trận tiếp tục.
- **6a — Người chơi là non-Host:** Bỏ qua bước 6 (Host sẽ tự xử lý kết quả).

---

## UC12 — Xem kết quả trận đấu

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC12 |
| **Tên** | Xem kết quả trận đấu |
| **Actor chính** | Người chơi |
| **Mô tả** | Sau khi trận kết thúc, hệ thống hiển thị Result Popup với kết quả thắng/thua/hòa, điểm so sánh 2 bên, phần thưởng nhận được và 2 lựa chọn điều hướng. |
| **Tiền điều kiện** | `GameState = GameOver`; `AwardRewards()` đã được gọi |
| **Hậu điều kiện — Chơi lại** | Thoát phòng cũ → HomeScene → Auto-trigger tìm trận mới |
| **Hậu điều kiện — Về sảnh** | Về HomeScene bình thường |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Hệ thống | `GameController` fire `OnGameOver` event |
| 2 | `GameplayUIController` | Nhận event; hiển thị Result Popup |
| 3 | Hệ thống | Hiển thị tiêu đề THẮNG! / THUA! / HÒA! (to, màu nổi bật) |
| 4 | Hệ thống | Hiển thị điểm: "Bạn: X — Đối thủ: Y" |
| 5 | Hệ thống | Hiển thị phần thưởng: "+EXP | +Tiền$" |
| 6 | Người chơi | Chọn "Chơi Lại" hoặc "Về Sảnh" |
| 7a (Chơi lại) | Hệ thống | Online: `FirebaseManager` rời room; `GameManager.LoadHomeScene()` + trigger `StartMatchmaking()` |
| 7b (Về Sảnh) | Hệ thống | `GameManager.LoadHomeScene()` |

**Luồng thay thế:**

- **1a — Đối thủ ngắt kết nối (Online):** Kết quả được đánh dấu THẮNG cho người ở lại trước khi hiện Result Popup.

---

## UC13 — Chơi với máy (Practice)

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC13 |
| **Tên** | Chơi với máy (Practice) |
| **Actor chính** | Người chơi (đã đăng nhập hoặc Khách) |
| **Mô tả** | Người chơi chọn chế độ luyện tập đấu với AI bot, không cần kết nối mạng. Luồng game giống Online nhưng `FirebaseMatchProvider` được thay bằng `LocalMatchProvider + MockOpponent`. |
| **Tiền điều kiện** | Người chơi đang ở HomeScene |
| **Hậu điều kiện** | Vào GameplayScene chế độ Offline; AI bot sẽ tự trả lời sau delay ngẫu nhiên |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người chơi | Nhấn "ĐẤU VỚI MÁY" |
| 2 | Hệ thống | `FirebaseManager.isOfflineMode = true` |
| 3 | Hệ thống | `GameManager.LoadGameplayScene()` |
| 4 | `GameController.Start()` | Phát hiện `isOfflineMode = true` → subscribe `LocalMatchProvider.OnBothPlayersAnswered` thay vì `FirebaseMatchProvider` |
| 5 | `MockOpponent.Start()` | Subscribe `LocalMatchProvider.OnNewQuestionStarted` |
| 6 | Hệ thống | Bắt đầu trận (UC08 — Offline flow) |
| 7 | Mỗi câu hỏi | `MockOpponent` nhận `OnNewQuestionStarted` → suy nghĩ 1.5–4 giây → `LocalMatchProvider.SubmitAnswerP2(index)` |
| 8 | Mỗi câu hỏi | Sau khi P1 và P2 đều nộp (hoặc hết giờ) → `LocalMatchProvider.OnBothPlayersAnswered` → `GameController.RevealAndAdvance()` |

**Luồng thay thế:**

- **6a — Không có câu hỏi:** Giống UC08 luồng thay thế 6a.
- Bot có xác suất trả lời đúng cấu hình được (mặc định 50%).

---

## UC14 — Đổi ngôn ngữ

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC14 |
| **Tên** | Đổi ngôn ngữ giao diện |
| **Actor chính** | Người chơi (đã đăng nhập hoặc Khách) |
| **Mô tả** | Người dùng thay đổi ngôn ngữ hiển thị của toàn bộ giao diện và bộ câu hỏi. Toàn bộ text được cập nhật ngay lập tức không cần khởi động lại. |
| **Tiền điều kiện** | Người dùng đang ở bất kỳ scene nào có Settings Popup |
| **Hậu điều kiện** | `LocalizationManager._currentLanguage` thay đổi; tất cả UIController refresh text |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người dùng | Mở Settings Popup (⚙️); chọn ngôn ngữ mới (VD: "English") |
| 2 | Hệ thống | `LocalizationManager.SetLanguage("en")` |
| 3 | `LocalizationManager` | Lưu lựa chọn vào `PlayerPrefs["Language"]` |
| 4 | `LocalizationManager` | Load dữ liệu ngôn ngữ mới (từ dict đã có / reload từ file nếu cần) |
| 5 | `LocalizationManager` | Fire `OnLanguageChanged` |
| 6 | Tất cả UIController | Mỗi Controller nhận event → gọi lại `GetText(key)` cho từng element → cập nhật label/button text |
| 7 | Người dùng | Thấy toàn bộ giao diện đổi sang ngôn ngữ mới ngay lập tức |

**Luồng thay thế:**

- **4a — Ngôn ngữ chưa được tải:** `LocalizationManager` tự load từ cache hoặc JSON local trước khi fire event.

---

## UC15 — Cài đặt âm thanh

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC15 |
| **Tên** | Cài đặt âm thanh |
| **Actor chính** | Người chơi (đã đăng nhập hoặc Khách) |
| **Mô tả** | Người dùng bật hoặc tắt âm nhạc nền và/hoặc hiệu ứng âm thanh. Thay đổi có hiệu lực ngay lập tức và được lưu lại cho các lần mở app sau. |
| **Tiền điều kiện** | Người dùng đang ở Settings Popup |
| **Hậu điều kiện** | Trạng thái âm thanh mới được áp dụng; lưu vào `PlayerPrefs` |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | Người dùng | Mở Settings Popup (⚙️) |
| 2 | Người dùng | Toggle switch "Nhạc nền" (Music) sang On hoặc Off |
| 3 | Hệ thống | `AudioManager` (hoặc `GameManager`) cập nhật `AudioSource.mute` cho nhạc nền ngay lập tức |
| 4 | Người dùng | Toggle switch "Hiệu ứng âm thanh" (SFX) sang On hoặc Off |
| 5 | Hệ thống | Cập nhật `AudioSource.mute` cho group SFX ngay lập tức |
| 6 | Hệ thống | Lưu trạng thái vào `PlayerPrefs["MusicOn"]` và `PlayerPrefs["SFXOn"]` |

**Luồng thay thế:**

- Không có luồng thất bại (thao tác đơn giản, không phụ thuộc mạng).

---

## UC16 — Tính điểm & Trao phần thưởng

*(Đã có trong báo cáo chính)*

| Trường | Nội dung |
|---|---|
| **Mã UC** | UC16 |
| **Tên** | Tính điểm & Trao phần thưởng |
| **Actor chính** | Hệ thống (tự động) |
| **Actor phụ** | Firebase Realtime Database (Online) |
| **Mô tả** | Sau khi trận kết thúc (`GameState = GameOver`), hệ thống xác định kết quả thắng/thua/hòa, trao phần thưởng EXP và Tiền ảo, kiểm tra nâng Level, lưu dữ liệu cục bộ và đồng bộ lên Firebase (nếu Online). |
| **Tiền điều kiện** | `GameState` vừa chuyển sang `GameOver` |
| **Hậu điều kiện** | `PlayerData.level`, `.currentExp`, `.money` được cập nhật; Online: đồng bộ lên `users/{uid}` |

**Luồng chính:**

| Bước | Actor | Hành động |
|:---:|---|---|
| 1 | `GameController` | `EndMatchRoutine()` được trigger khi `ChangeState(GameOver)` |
| 2 | Hệ thống (Online + Host) | `FirebaseManager.HostEndMatch(winner)` → ghi `state:"ended"` và `winner` lên Firebase room |
| 3 | `ScoreManager` | `AwardRewards()`: gọi `GetWinner()` → xác định `WinResult` |
| 4 | `ScoreManager` | Tra bảng phần thưởng: Thắng: +50XP +100$; Hòa: +20XP +40$; Thua: +10XP +10$ |
| 5 | `PlayerData` | `AddExp(expAmount)`: cộng EXP; vòng lặp kiểm tra nâng Level (`while currentExp >= level * 100`) |
| 6 | `PlayerData` | `AddMoney(moneyAmount)` |
| 7 | `PlayerDataManager` | `SaveData()` → ghi tất cả vào `PlayerPrefs` |
| 8 | Hệ thống (Online) | `FirebaseManager.SaveProfileToCloud()` → `SET users/{uid}/{level, currentExp, money}` |
| 9 | `GameController` | Fire `OnGameOver` event |
| 10 | `GameplayUIController` | Nhận event → hiển thị Result Popup (UC12) |

**Luồng thay thế:**

- **3a — ForcedSurrender:** `_forcedWinnerResult` đã được set trước → `GetWinner()` trả về forced result thay vì so sánh điểm.
- **8a — Mất kết nối:** Cloud save thất bại; dữ liệu đã lưu cục bộ (bước 7) vẫn an toàn; retry khi có mạng.

---

---

# 2. MÔ TẢ SƠ ĐỒ LỚP PHÂN TÍCH

Sơ đồ lớp phân tích (Analysis Class Diagram) mô tả các **khái niệm nghiệp vụ** của hệ thống ở mức trừu tượng cao, chưa đề cập đến chi tiết kỹ thuật triển khai (không gắn với Unity/Firebase cụ thể). Gồm 6 lớp chính:

---

### Lớp `NguoiChoi` (Player)

**Vai trò nghiệp vụ:** Đại diện cho người dùng tham gia vào hệ thống. Đây là thực thể trung tâm — mọi hoạt động trong game đều xoay quanh người chơi.

**Thuộc tính:**
- `ten: String` — Tên hiển thị, dùng để nhận diện trong trận đấu và bảng xếp hạng.
- `email: String` — Địa chỉ email đăng ký, dùng cho xác thực; rỗng nếu là tài khoản Khách.
- `level: int` — Cấp độ hiện tại của người chơi, tăng lên khi đủ EXP.
- `exp: int` — Điểm kinh nghiệm tích lũy trong level hiện tại.
- `tien: int` — Đơn vị tiền ảo trong game, dùng làm phần thưởng.
- `avatarIndex: int` — Chỉ số hình đại diện đang dùng (0–7).

**Phương thức:**
- `dangNhap()` — Xác thực danh tính và tải dữ liệu hồ sơ.
- `dangXuat()` — Kết thúc phiên, xóa session.
- `chinhSuaHoSo()` — Cập nhật tên và avatar.

**Quan hệ:**
- Tham gia `TranDau` (2 `NguoiChoi` trên 1 `TranDau`)
- Nhận `KetQua` sau trận
- Sử dụng `Matchmaking` để tìm đối thủ
- Sở hữu `CaiDat` (1–1)

---

### Lớp `TranDau` (Match)

**Vai trò nghiệp vụ:** Đại diện cho một phiên đấu đang diễn ra, quản lý vòng đời từ lúc khởi tạo đến khi kết thúc và tạo ra kết quả.

**Thuộc tính:**
- `roomId: String` — Định danh duy nhất của phòng đấu trên hệ thống.
- `seed: int` — Giá trị khởi tạo bộ sinh ngẫu nhiên; đảm bảo 2 máy có cùng bộ câu hỏi.
- `soLuongCau: int` — Số câu hỏi trong trận (5, 10 hoặc 15 tùy Tier).
- `trangThai: String` — Trạng thái hiện tại: `"waiting"`, `"playing"`, `"ended"`.

**Phương thức:**
- `batDau()` — Khởi tạo trận, tải câu hỏi, bắt đầu đếm ngược.
- `ketThuc()` — Xác định kết quả, tính thưởng, lưu trữ.
- `thoat()` — Xử lý khi người chơi rời trận giữa chừng (ForcedSurrender).

**Quan hệ:**
- Chứa nhiều `CauHoi` (composition — câu hỏi không tồn tại ngoài trận đấu)
- Tạo ra 1 `KetQua`
- Có 2 `NguoiChoi` tham gia

---

### Lớp `CauHoi` (Question)

**Vai trò nghiệp vụ:** Đơn vị nội dung cơ bản của trò chơi. Mỗi câu hỏi chứa đề bài và các lựa chọn trả lời.

**Thuộc tính:**
- `noiDung: String` — Nội dung đề bài câu hỏi.
- `dapAn: String[4]` — Mảng 4 đáp án (A, B, C, D).
- `dapAnDung: int` — Chỉ số của đáp án đúng trong mảng `dapAn` (0–3).
- `danhMuc: String` — Danh mục câu hỏi (IT, khoa học, lịch sử...).

**Quan hệ:**
- Thuộc về `TranDau` (composition — một câu hỏi trong một trận đấu cụ thể)

---

### Lớp `KetQua` (Result)

**Vai trò nghiệp vụ:** Chứa toàn bộ thông tin về kết thúc của một trận đấu: ai thắng, điểm bao nhiêu, và phần thưởng nhận được.

**Thuộc tính:**
- `diemNguoiChoi: int` — Tổng điểm của người chơi hiện tại (P1).
- `diemDoiThu: int` — Tổng điểm của đối thủ (P2).
- `ketQua: WinResult` — Kết quả cuối: `Player1Wins`, `Player2Wins`, hoặc `Draw`.
- `expNhan: int` — Lượng EXP được trao sau trận.
- `tienNhan: int` — Lượng Tiền ảo được trao sau trận.

**Quan hệ:**
- Được tạo bởi `TranDau`
- Được nhận bởi `NguoiChoi`

---

### Lớp `Matchmaking`

**Vai trò nghiệp vụ:** Đại diện cho quá trình tìm kiếm và ghép cặp đối thủ. Không giữ trạng thái lâu dài — là một quy trình tạm thời.

**Phương thức:**
- `timTran()` — Ghi UID vào hàng chờ, lắng nghe sự kiện ghép cặp thành công.
- `huyTimTran()` — Xóa UID khỏi hàng chờ, hủy bỏ quá trình tìm kiếm.
- `ghepCap()` — Logic tạo phòng hoặc tham gia phòng có sẵn khi có ≥ 2 người chờ.

**Quan hệ:**
- Được `NguoiChoi` sử dụng
- Dẫn đến việc tạo `TranDau` khi thành công

---

### Lớp `CaiDat` (Settings)

**Vai trò nghiệp vụ:** Lưu trữ và quản lý tùy chọn cá nhân của người dùng về trải nghiệm giao diện và âm thanh.

**Thuộc tính:**
- `ngonNgu: String` — Mã ngôn ngữ hiện tại (`"vi"` hoặc `"en"`).
- `nhacNen: bool` — Trạng thái bật/tắt âm nhạc nền.
- `amThanhHieuUng: bool` — Trạng thái bật/tắt hiệu ứng âm thanh (SFX).

**Phương thức:**
- `doiNgonNgu(langCode)` — Thay đổi ngôn ngữ và refresh toàn bộ giao diện.
- `batTatAm(loai, trangThai)` — Cập nhật trạng thái bật/tắt cho nhạc hoặc SFX.

**Quan hệ:**
- Thuộc về `NguoiChoi` (quan hệ 1–1)

---

---

# 3. MÔ TẢ TẤT CẢ SEQUENCE DIAGRAM (Phân tích)

Phần này mô tả bằng văn xuôi nội dung cần thể hiện trong từng sequence diagram ở cấp độ phân tích (mục III.3 báo cáo). Mỗi diagram thể hiện tương tác giữa Actor và các đối tượng hệ thống theo một luồng UC cụ thể.

---

### SD-01: Đăng ký tài khoản (UC01)

**Các đối tượng tham gia:** Người dùng → `InitSceneController` → `FirebaseManager` → Firebase Auth → Firebase Realtime DB

**Mô tả luồng:** Người dùng điền form và nhấn Đăng ký. `InitSceneController` thu thập dữ liệu và gọi `FirebaseManager.RegisterWithEmail()`. `FirebaseManager` chuyển yêu cầu đến Firebase Auth — nếu thành công, nhận về `uid`. Tiếp theo, `FirebaseManager` ghi bản ghi mặc định lên `users/{uid}` trên Realtime DB. Sau khi ghi xong, fire `OnAuthSuccess` → `InitSceneController` gọi `GameManager.LoadHomeScene()`.

**Điểm đặc biệt cần thể hiện:** Luồng thay thế khi email đã tồn tại (Firebase trả về lỗi) → `InitSceneController` hiển thị thông báo lỗi thay vì chuyển scene.

---

### SD-02: Đăng nhập (UC02)

**Các đối tượng tham gia:** Người dùng → `InitSceneController` → `FirebaseManager` → Firebase Auth → Firebase Realtime DB → `PlayerDataManager`

**Mô tả luồng:** Người dùng nhập Email + Mật khẩu. `FirebaseManager.LoginWithEmail()` gọi Firebase Auth. Nếu thành công, đọc `users/{uid}` từ Realtime DB để lấy profile. Dữ liệu được nạp vào `PlayerDataManager`. Fire `OnAuthSuccess` → LoadHomeScene.

**Điểm đặc biệt:** Luồng `alt` khi sai mật khẩu hoặc không có mạng — thể hiện hai nhánh lỗi khác nhau.

---

### SD-03: Đăng xuất (UC03)

**Các đối tượng tham gia:** Người dùng → `MainMenuUIController` → `FirebaseManager` → `PlayerDataManager` → `GameManager`

**Mô tả luồng:** Người dùng nhấn Đăng xuất trong Settings. Nếu là tài khoản Khách, `MainMenuUIController` hiển thị Confirmation Popup trước. Sau khi xác nhận: `FirebaseManager.Logout()` xóa session Firebase; `PlayerDataManager.ClearData()` xóa `PlayerPrefs` (với Khách); `GameManager.LoadInitScene()`.

**Điểm đặc biệt:** Luồng `alt` giữa tài khoản đăng nhập thường (không có popup cảnh báo) và tài khoản Khách (có popup cảnh báo mất dữ liệu).

---

### SD-04: Chơi khách (UC04)

**Các đối tượng tham gia:** Người dùng → `InitSceneController` → `PlayerDataManager` → `FirebaseManager` → `GameManager`

**Mô tả luồng:** Người dùng nhập Tên và nhấn "Chơi khách". `InitSceneController` lưu tên vào `PlayerPrefs`. `PlayerDataManager.LoadData()` khởi tạo dữ liệu mặc định. `FirebaseManager.isOfflineMode = true`. `GameManager.LoadHomeScene()` — không có bước xác thực Firebase.

**Điểm đặc biệt:** Không có tương tác với Firebase Auth/DB; dữ liệu chỉ tồn tại trong bộ nhớ cục bộ.

---

### SD-05: Chỉnh sửa hồ sơ (UC05)

**Các đối tượng tham gia:** Người dùng → `MainMenuUIController` → `PlayerDataManager` → `FirebaseManager`

**Mô tả luồng:** Người dùng nhấn vùng Profile HUD → Profile Popup mở. Người dùng chỉnh tên và chọn avatar. Nhấn Lưu → `PlayerDataManager` cập nhật `PlayerData` và `SaveData()` vào `PlayerPrefs`. Đồng thời, nếu đang Online: `FirebaseManager` ghi `displayName` và `avatarIndex` lên `users/{uid}`. `MainMenuUIController` refresh Profile HUD ngay.

**Điểm đặc biệt:** Luồng `opt` (optional) cho bước đồng bộ Firebase — chỉ thực hiện nếu đã đăng nhập.

---

### SD-06: Tìm trận đấu (UC06)

**Các đối tượng tham gia:** Người dùng → `MainMenuUIController` → `FirebaseManager` → Firebase Realtime DB → `FirebaseManager` (Người dùng 2) → `GameManager`

**Mô tả luồng:** Người dùng nhấn "TÌM TRẬN". `FirebaseManager.StartMatchmaking()` ghi vào `matchmakingQueue`. `MainMenuUIController` hiển thị màn hình chờ. Firebase listener `OnValueChanged` phát hiện ≥ 2 người trong queue → Host tạo Room, xóa 2 UID khỏi queue. Cả 2 client nhận `OnMatchFound` → `LoadGameplayScene()`.

**Điểm đặc biệt:** Cần thể hiện 2 actor người dùng song song (P1 và P2) và sự đồng bộ qua Firebase.

---

### SD-07: Hủy tìm trận (UC07)

**Các đối tượng tham gia:** Người dùng → `MainMenuUIController` → `FirebaseManager` → Firebase Realtime DB

**Mô tả luồng:** Người dùng nhấn "Hủy". `FirebaseManager.CancelMatchmaking()` gửi lệnh xóa `matchmakingQueue/{uid}` lên Firebase. Sau khi xóa thành công, `MainMenuUIController` ẩn màn hình chờ, hiển thị lại màn hình HomeScene bình thường.

---

### SD-08: Bắt đầu trận đấu (UC08)

**Các đối tượng tham gia:** `GameController` → `LocalizationManager` → `FirebaseManager` (Online) / tính toán local (Offline) → `QuizManager` → `TimerController` → `GameplayUIController`

**Mô tả luồng:** `GameController.Start()` chờ `LocalizationManager.IsReady`. Sau đó gọi `ChangeState(Countdown)` → countdown 3-2-1 fire `OnCountdownTick`. Chuyển sang `Playing`: Online thì await `ReadSeedFromRoom()` và `ReadQuestionCountFromRoom()`; Offline thì tính local. Gọi `QuizManager.StartQuiz(seed, count)` → `TimerController.StartTimer()` → câu đầu tiên hiển thị.

---

### SD-09: Trả lời câu hỏi (UC09)

**Các đối tượng tham gia:** Người dùng → `InputController_UXML` → `GameController` → `FirebaseMatchProvider` (Online) / `LocalMatchProvider` (Offline) → `TimerController` → `ScoreManager` → `GameplayUIController`

**Mô tả luồng:** Câu hỏi đang hiển thị, timer chạy. Người dùng chọn đáp án → `InputController` ghi nhận và gửi lên Firebase (hoặc `LocalMatchProvider`). Chờ hết giờ. `HandleTimerEnd()` → `RevealAndAdvance()`: chấm điểm, hiện feedback, kiểm tra còn câu hay GameOver.

---

### SD-10: Xem điểm đối thủ real-time (UC10)

**Các đối tượng tham gia:** Firebase Realtime DB → `FirebaseMatchProvider` → `ScoreManager` → `GameplayUIController`

**Mô tả luồng:** Đây là luồng hoàn toàn tự động, người dùng không chủ động trigger. Khi đối thủ trả lời đúng và điểm được ghi lên `rooms/{id}/scores/{oppUid}`, Firebase trigger `OnValueChanged` tại listener của `FirebaseMatchProvider`. Listener fire `OnOpponentScoreUpdated(score)` → `ScoreManager.SetOpponentScore()` → `OnScoreChanged` → `GameplayUIController` cập nhật HUD P2.

---

### SD-11: Đầu hàng / Thoát trận (UC11)

**Các đối tượng tham gia:** Người dùng → `GameplayUIController` → `GameController` → `ScoreManager` → `FirebaseManager` (Online)

**Mô tả luồng:** Nhấn ✖ → `GameplayUIController` tạm dừng timer và hiển thị Exit Confirm Popup. Nếu xác nhận: `GameController.ForcedSurrender()` → `ScoreManager.SetForcedWinner(Player2Wins)` → `ChangeState(GameOver)` → (Online) `FirebaseManager.HostEndMatch()` → `EndMatchRoutine()`.

---

### SD-12: Xem kết quả trận đấu (UC12)

**Các đối tượng tham gia:** `GameController` → `ScoreManager` → `GameplayUIController` → Người dùng

**Mô tả luồng:** `GameController` fire `OnGameOver`. `GameplayUIController` nhận event, hiển thị Result Popup với dữ liệu từ `ScoreManager` (điểm P1, P2, WinResult, LastRewardExp, LastRewardMoney). Người dùng chọn "Chơi Lại" → `GameManager.LoadHomeScene()` rồi auto-matchmaking; hoặc "Về Sảnh" → `LoadHomeScene()` bình thường.

---

### SD-13: Chơi với máy (UC13)

**Các đối tượng tham gia:** Người dùng → `MainMenuUIController` → `GameManager` → `GameController` → `LocalMatchProvider` ↔ `MockOpponent`

**Mô tả luồng:** Nhấn "ĐẤU VỚI MÁY" → `isOfflineMode = true` → `LoadGameplayScene()`. `GameController.Start()` subscribe `LocalMatchProvider`. `MockOpponent.Start()` subscribe `OnNewQuestionStarted`. Mỗi câu hỏi mới: `MockOpponent` nhận event → delay ngẫu nhiên → `LocalMatchProvider.SubmitAnswerP2()`. Luồng từ bước này giống Online nhưng không có Firebase.

---

### SD-14: Đổi ngôn ngữ (UC14)

**Các đối tượng tham gia:** Người dùng → `SettingsPopup (UI)` → `LocalizationManager` → (tất cả UIController)

**Mô tả luồng:** Người dùng toggle ngôn ngữ trong Settings. `LocalizationManager.SetLanguage()` lưu `PlayerPrefs`, load dữ liệu ngôn ngữ mới, fire `OnLanguageChanged`. Tất cả UIController đang subscribe sự kiện này (`InitSceneController`, `MainMenuUIController`, `GameplayUIController`) đồng loạt gọi `GetText(key)` và cập nhật text. Toàn bộ chữ trên màn hình đổi ngay lập tức.

---

### SD-15: Cài đặt âm thanh (UC15)

**Các đối tượng tham gia:** Người dùng → `SettingsPopup (UI)` → `AudioManager`

**Mô tả luồng:** Người dùng nhấn toggle Music → `AudioManager.SetMusicEnabled(bool)` → `AudioSource.mute` đổi ngay → `PlayerPrefs` lưu trạng thái. Tương tự cho SFX toggle.

---

### SD-16: Tính điểm & Phần thưởng (UC16)

**Các đối tượng tham gia:** `GameController` → `FirebaseManager` (Online) → `ScoreManager` → `PlayerData` → `PlayerDataManager` → `FirebaseManager` (Cloud Save) → `GameplayUIController`

**Mô tả luồng:** `ChangeState(GameOver)` kích hoạt `EndMatchRoutine()`. Host ghi kết quả lên Firebase room. `ScoreManager.AwardRewards()` tính phần thưởng, gọi `PlayerData.AddExp()` (bao gồm kiểm tra level-up), `AddMoney()`. `PlayerDataManager.SaveData()` ghi `PlayerPrefs`. Online: `FirebaseManager.SaveProfileToCloud()`. Cuối cùng fire `OnGameOver` để UI hiển thị kết quả.

---

---

# 4. MÔ TẢ DETAILED CLASS DIAGRAM

Phần này mô tả chi tiết từng lớp trong Detailed Class Diagram (Mục IV.2.1), bao gồm Pattern áp dụng, ý nghĩa từng thuộc tính, phương thức, và quan hệ với các lớp khác.

---

### Lớp `GameManager`

**Pattern:** Singleton + `DontDestroyOnLoad`  
**Vai trò:** Quản lý lifecycle toàn bộ ứng dụng. Là lớp duy nhất chịu trách nhiệm điều hướng giữa các Scene và cấu hình các thông số chung của app (framerate, sleep timeout). Tồn tại từ đầu đến cuối ứng dụng.

**Thuộc tính:**
- `Instance: GameManager` (static) — Điểm truy cập Singleton toàn cục.

**Phương thức:**
- `LoadInitScene()` — Chuyển về màn hình khởi động (sau đăng xuất).
- `LoadHomeScene()` — Chuyển về Sảnh chờ (sau đăng nhập, sau trận).
- `LoadGameplayScene()` — Chuyển vào màn hình trận đấu.
- `LoadSceneAsync(name, onProgress, onLoaded)` — Tải scene bất đồng bộ với callback tiến trình (dùng cho loading screen).

**Quan hệ:** Được gọi bởi hầu hết các UIController và `GameController` khi cần chuyển scene.

---

### Lớp `GameController`

**Pattern:** Singleton + State Machine  
**Vai trò:** Bộ não điều phối của một trận đấu. Quản lý vòng đời trận từ Idle đến GameOver, điều phối giữa `QuizManager`, `ScoreManager`, `TimerController` và provider (Online/Offline). Quyết định khi nào chuyển câu, khi nào kết thúc.

**Thuộc tính:**
- `Instance: GameController` (static) — Singleton trong GameplayScene.
- `CurrentState: GameState` — Trạng thái hiện tại của State Machine.
- `OnGameStateChanged: Action<GameState>` (static event) — Thông báo khi state thay đổi; UIController lắng nghe để ẩn/hiện các panel phù hợp.
- `OnCountdownTick: Action<int>` (static event) — Fire mỗi giây đếm ngược (3, 2, 1); UIController hiển thị số đếm.
- `OnGameOver: Action` (static event) — Fire khi trận kết thúc; `GameplayUIController` lắng nghe để hiện Result Popup.
- `OnOpponentLeft: Action` (static event) — Fire khi đối thủ ngắt kết nối; dùng để hiện thông báo riêng.
- `_isOnline: bool` — Cờ xác định mode; được set trong `Start()` dựa trên trạng thái `FirebaseManager`.
- `_currentLocalAnswer: int` — Lưu đáp án người chơi đã chọn trong câu hiện tại (-1 = chưa chọn).
- `revealDuration: float` — Thời gian hiển thị feedback đúng/sai (2.5 giây, cấu hình qua Inspector).

**Phương thức:**
- `ChangeState(newState)` — Chuyển state và thực hiện logic tương ứng (reset, start countdown, start timer...).
- `StartGame()` — Công khai: kích hoạt `ChangeState(Countdown)`.
- `ForcedSurrender()` — Xử lý khi người chơi thoát giữa trận: set forced winner, chuyển GameOver.
- `SetLocalAnswer(index)` — Được `InputController` gọi khi người chơi chọn đáp án.
- `StartQuizWithSeed()` (coroutine) — Async: đọc seed từ Firebase (Online) hoặc tính local (Offline), rồi gọi `QuizManager.StartQuiz()`.
- `RevealAndAdvance(p1, p2)` (coroutine) — Cốt lõi logic câu hỏi: chấm điểm, feedback, chuyển câu hoặc GameOver.
- `EndMatchRoutine()` (coroutine) — Kết thúc trận: (Host) ghi Firebase → `AwardRewards()` → (Online) cloud save → fire `OnGameOver`.

**Quan hệ:** Sử dụng `QuizManager`, `ScoreManager`, `TimerController`; subscribe sự kiện từ `FirebaseMatchProvider` hoặc `LocalMatchProvider` tùy mode.

---

### Lớp `QuizManager`

**Pattern:** Singleton  
**Vai trò:** Quản lý toàn bộ ngân hàng câu hỏi trong một trận đấu. Chịu trách nhiệm shuffle theo seed, giới hạn số câu và cung cấp từng câu hỏi theo yêu cầu của `GameController`.

**Thuộc tính:**
- `Instance: QuizManager` (static)
- `CurrentQuestion: QuestionData` — Câu hỏi đang được hiển thị (null nếu chưa bắt đầu).
- `OnQuestionChanged: Action<QuestionData>` (static event) — Fire khi có câu mới; UIController và `InputController` lắng nghe để cập nhật nội dung hiển thị.
- `OnQuestionsExhausted: Action` (static event) — Fire khi hết câu hỏi.
- `_shuffledQuestions: List<QuestionData>` — Danh sách câu hỏi đã được shuffle theo seed.
- `_currentIndex: int` — Con trỏ câu hỏi hiện tại (-1 = chưa bắt đầu).

**Phương thức:**
- `StartQuiz(seed, limit)` — Khởi động quiz: gọi `AutoGenerateDatabase()`, áp dụng Fisher-Yates shuffle theo seed, giới hạn số câu, gọi `NextQuestion()`.
- `NextQuestion()` — Tăng `_currentIndex`; nếu hết → fire `OnQuestionsExhausted`; nếu còn → fire `OnQuestionChanged`.
- `HasMoreQuestions()` — Kiểm tra xem còn câu tiếp theo không.
- `AutoGenerateDatabase()` — Tự động tạo danh sách `QuestionData` từ keys trong `LocalizationManager` (pattern: `q_<cat>_<idx>` cho câu, `a_<cat>_<idx>_<n>` cho đáp án).

**Quan hệ:** Được `GameController` điều khiển; cung cấp dữ liệu cho `InputController_UXML` và `GameplayUIController` qua events.

---

### Lớp `ScoreManager`

**Pattern:** Singleton  
**Vai trò:** Theo dõi điểm số của cả 2 người chơi trong trận. Cung cấp logic chấm điểm, xác định kết quả thắng/thua/hòa, và phân bổ phần thưởng.

**Thuộc tính:**
- `Player1Score: int` — Điểm tích lũy của người chơi hiện tại.
- `Player2Score: int` — Điểm tích lũy của đối thủ.
- `LastRewardMoney: int` — Số tiền ảo vừa được trao (để UI hiển thị).
- `LastRewardExp: int` — Số EXP vừa được trao (để UI hiển thị).
- `OnScoreChanged: Action<int,int>` (static event) — Fire sau mỗi lần điểm thay đổi; `GameplayUIController` lắng nghe để cập nhật HUD.
- `_forcedWinnerResult: WinResult?` — Khi đặt giá trị này, `GetWinner()` bỏ qua so sánh điểm và trả về giá trị cưỡng bức (dùng cho ForcedSurrender và đối thủ mất mạng).
- Hằng số: `CORRECT_POINTS = 10`, `WRONG_POINTS = 0`, `WIN_XP = 50`, `DRAW_XP = 20`, `LOSE_XP = 10`, `WIN_MONEY = 100`, `DRAW_MONEY = 40`, `LOSE_MONEY = 10`.

**Phương thức:**
- `CheckAnswer(playerId, answerIndex)` — So sánh với `correctAnswerIndex`; nếu đúng → `AddScore()`; trả về bool.
- `AddScore(playerId, points)` — Cộng điểm; nếu `playerId == 1` và Online → `FirebaseManager.UpdateMyScore()`.
- `SetOpponentScore(score)` — Cập nhật `Player2Score` từ Firebase listener (không push ngược lại).
- `SetForcedWinner(result)` — Đặt kết quả cưỡng bức (Surrender/Disconnect).
- `GetWinner()` — Trả về `WinResult` dựa trên so sánh điểm hoặc forced result.
- `AwardRewards()` — Tính và trao phần thưởng; gọi `PlayerData.AddExp()` và `AddMoney()`.
- `ResetScores()` — Xóa điểm và forced result (khi bắt đầu trận mới).

**Quan hệ:** Subscribe `FirebaseMatchProvider.OnOpponentScoreUpdated`; gọi `FirebaseManager.UpdateMyScore()`; gọi `PlayerDataManager.Data.AddExp/AddMoney`.

---

### Lớp `TimerController`

**Pattern:** Singleton  
**Vai trò:** Quản lý bộ đếm ngược thời gian cho mỗi câu hỏi. Sử dụng Coroutine để không block main thread. Thời gian mỗi câu lấy từ `FirebaseManager.QuestionDuration` (Remote Config).

**Thuộc tính:**
- `Instance: TimerController` (static)
- `RemainingTime: float` — Thời gian còn lại của câu hiện tại (giây).
- `IsRunning: bool` — Cờ cho biết timer có đang chạy không.
- `TotalTime: float` — Tổng thời gian mỗi câu (mặc định 180s trong Inspector, override bởi Remote Config).
- `OnTimerTick: Action<float>` (static event) — Fire mỗi giây; UIController dùng để cập nhật hiển thị đồng hồ.
- `OnTimerEnd: Action` (static event) — Fire khi hết giờ; `GameController` lắng nghe để kích hoạt logic chấm điểm.

**Phương thức:**
- `StartTimer()` — Dừng coroutine cũ (nếu có), lấy `QuestionDuration` từ Remote Config, reset `RemainingTime`, khởi động coroutine.
- `StopTimer()` — Dừng hoàn toàn coroutine, `IsRunning = false`.
- `PauseTimer()` / `ResumeTimer()` — Tạm dừng/tiếp tục mà không reset (dùng cho Exit Confirm Popup).
- `GetFormattedTime()` — Trả về chuỗi `"mm:ss"` từ `RemainingTime`.

**Quan hệ:** Được `GameController` điều khiển (Start/Stop); cung cấp events cho `GameController` và UIController.

---

### Lớp `FirebaseManager`

**Pattern:** Singleton + `DontDestroyOnLoad`  
**Vai trò:** Cổng giao tiếp duy nhất với toàn bộ hạ tầng Firebase (Auth, Realtime DB, Remote Config). Xử lý: xác thực người dùng, Matchmaking queue, quản lý room, cloud save và đọc Remote Config.

**Thuộc tính quan trọng:**
- `IsConnected: bool` — Firebase SDK sẵn sàng.
- `IsAuthenticated: bool` — Có user đang đăng nhập.
- `LocalUserId: string` — UID của người dùng hiện tại.
- `LocalDisplayName: string` — Tên hiển thị người dùng hiện tại.
- `CurrentRoomId: string` — ID phòng đang tham gia (null nếu không trong phòng).
- `OpponentId: string` — UID đối thủ trong phòng hiện tại.
- `IsHost: bool` — `true` nếu `LocalUserId < OpponentId` (so sánh chuỗi); Host có quyền ghi kết quả.
- `isOfflineMode: bool` — Cờ cho phép bypass Firebase (chế độ Khách/Đấu với máy).
- `QuestionDuration: float` — Lấy từ Remote Config, mặc định 15 giây.
- `OnFirebaseReady, OnAuthSuccess, OnMatchFound, OnOpponentDisconnected` — Các static event thông báo trạng thái.

**Phương thức:**
- `InitializeFirebase()` — Kiểm tra và cài đặt Firebase dependencies.
- `LoginWithEmail(email, pass)` / `RegisterWithEmail(name, email, pass)` / `Logout()` — Quản lý xác thực.
- `StartMatchmaking()` / `CancelMatchmaking()` — Ghi/xóa UID trong `matchmakingQueue`.
- `UpdateMyScore(score)` — Ghi điểm lên `rooms/{id}/scores/{uid}`.
- `HostEndMatch(winner)` — Ghi `state:"ended"` và `winner` lên room (chỉ Host gọi).
- `SaveProfileToCloud()` — Đẩy `level`, `currentExp`, `money` lên `users/{uid}`.
- `ReadSeedFromRoom()` / `ReadQuestionCountFromRoom()` — Đọc async seed và số câu từ room.
- `GetPlayerTier(level)` / `GetQuestionCountForTier(tier)` — Tính số câu theo Level (dùng Offline).

**Quan hệ:** Phụ thuộc vào Firebase SDK; được hầu hết Manager khác sử dụng; cung cấp events cho `GameController`, `FirebaseMatchProvider`, `PlayerDataManager`.

---

### Lớp `FirebaseMatchProvider`

**Pattern:** Singleton  
**Vai trò:** Lớp chuyên trách đồng bộ hóa trận đấu Online qua Firebase Realtime Database. Lắng nghe ba node: `answers` (cả 2 đã trả lời chưa), `scores` (điểm đối thủ thay đổi), `state` (trạng thái room). Chỉ hoạt động khi Online.

**Thuộc tính:**
- `OnBothPlayersAnswered: Action<int,int>` (static event) — Fire khi cả 2 UID đều có giá trị trong `rooms/{id}/answers`. Tham số: (p1Answer, p2Answer) theo quy ước local player = P1.
- `OnOpponentScoreUpdated: Action<int>` (static event) — Fire khi điểm đối thủ thay đổi trên Firebase.
- `OnMatchEndedByRoom: Action<string>` (static event) — Fire khi `rooms/{id}/state = "ended"`; tham số là `winner` uid hoặc `"draw"`.

**Phương thức:**
- `AttachRoomListeners()` — Đăng ký `EventHandler<ValueChangedEventArgs>` cho 3 node trên Firebase. Được gọi khi `FirebaseManager.OnMatchFound` fire.
- `DetachRoomListeners()` — Gỡ bỏ tất cả listener (gọi trong `OnDestroy` và khi re-attach).

**Quan hệ:** Subscribe `FirebaseManager.OnMatchFound`; subscribe `QuizManager.OnQuestionChanged` (để reset trạng thái chờ đáp án giữa các câu); cung cấp events cho `GameController` và `ScoreManager`.

---

### Lớp `LocalMatchProvider`

**Pattern:** Singleton  
**Vai trò:** "Server cục bộ thu nhỏ" cho chế độ Offline. Thay thế `FirebaseMatchProvider` khi không có mạng. Nhận đáp án từ `InputController` (P1) và `MockOpponent` (P2), chờ cả 2 đã nộp rồi mới fire event để `GameController` xử lý.

**Thuộc tính:**
- `_p1Answer: int` — Đáp án P1 đã nộp (-1 = chưa nộp).
- `_p2Answer: int` — Đáp án P2 (bot) đã nộp (-1 = chưa nộp).
- `_isWaiting: bool` — Cờ đang chờ đáp án cho câu hiện tại.
- `OnBothPlayersAnswered: Action<int,int>` (static event) — Tương đương sự kiện của `FirebaseMatchProvider`; `GameController` subscribe sự kiện này khi Offline.
- `OnNewQuestionStarted: Action` (static event) — Fire khi câu mới bắt đầu; `MockOpponent` lắng nghe để bắt đầu "suy nghĩ".

**Phương thức:**
- `SubmitAnswerP1(index)` — Được `InputController` gọi khi người thật chọn đáp án.
- `SubmitAnswerP2(index)` — Được `MockOpponent` gọi sau delay.
- `CheckIfBothAnswered()` — Kiểm tra nếu cả 2 đã nộp → fire `OnBothPlayersAnswered`.

**Quan hệ:** Subscribe `QuizManager.OnQuestionChanged` để reset state; được gọi bởi `InputController_UXML` (P1) và `MockOpponent` (P2).

---

### Lớp `MockOpponent`

**Pattern:** MonoBehaviour thường (không phải Singleton)  
**Vai trò:** Giả lập người chơi thứ 2 trong chế độ Offline. Khi nhận `OnNewQuestionStarted`, chờ một khoảng thời gian ngẫu nhiên (1.5–4 giây) rồi tự động nộp đáp án. Xác suất trả lời đúng cấu hình được qua Inspector (mặc định 50%).

**Thuộc tính:**
- `minThinkTime: float` — Thời gian suy nghĩ tối thiểu (1.5 giây).
- `maxThinkTime: float` — Thời gian suy nghĩ tối đa (4.0 giây).
- `correctAnswerChance: float` — Xác suất chọn đúng (0.0–1.0).

**Phương thức:**
- `HandleNewQuestion()` — Xử lý event `OnNewQuestionStarted`; khởi động coroutine suy nghĩ.
- `ThinkAndAnswer()` (coroutine) — Delay ngẫu nhiên → quyết định đáp án theo xác suất → `LocalMatchProvider.SubmitAnswerP2()`.

**Quan hệ:** Subscribe `LocalMatchProvider.OnNewQuestionStarted`; gọi `LocalMatchProvider.SubmitAnswerP2()`.

---

### Lớp `LocalizationManager`

**Pattern:** Singleton + `DontDestroyOnLoad`  
**Vai trò:** Quản lý toàn bộ hệ thống đa ngôn ngữ. Tải nội dung ngôn ngữ theo thứ tự ưu tiên (Google Sheet → Cache → JSON local), cung cấp API tra cứu chuỗi theo key, và thông báo khi ngôn ngữ thay đổi.

**Thuộc tính:**
- `IsReady: bool` — `true` khi đã load xong ít nhất một nguồn ngôn ngữ.
- `OnLanguageChanged: Action` (static event) — Tất cả UIController subscribe để refresh text.
- `_localizedText: Dictionary<string, string>` — Map từ key sang chuỗi ngôn ngữ hiện tại.
- `_currentLanguage: string` — Mã ngôn ngữ đang dùng (`"vi"` hoặc `"en"`).
- `sheetUrl: string` — URL Google Sheets CSV (có thể để trống để skip).
- `sheetTimeoutSeconds: int` — Timeout tải Sheet (mặc định 6 giây).

**Phương thức:**
- `GetText(key)` — Tra cứu chuỗi theo key; trả về `"[key]"` nếu không tìm thấy.
- `SetLanguage(langCode)` — Đổi ngôn ngữ, lưu `PlayerPrefs`, reload dữ liệu, fire event.
- `GetAllKeys()` — Trả về toàn bộ key trong dictionary (dùng bởi `QuizManager.AutoGenerateDatabase()`).
- `InitLocalization()` (coroutine) — Thử lần lượt 3 nguồn: Sheet → Cache → JSON local.
- `DownloadFromSheet(lang)` (coroutine) — HTTP GET URL Sheet với timeout.
- `TryLoadFromCache(lang)` — Đọc CSV từ `Application.persistentDataPath`.
- `LoadLocalLanguage(lang)` — Load JSON từ `StreamingAssets`.

**Quan hệ:** Được `GameController` và tất cả UIController phụ thuộc; cung cấp data cho `QuizManager.AutoGenerateDatabase()`.

---

### Lớp `PlayerDataManager`

**Pattern:** Singleton + `DontDestroyOnLoad`  
**Vai trò:** Cầu nối giữa dữ liệu hồ sơ người chơi (`PlayerData` ScriptableObject) và cơ chế lưu trữ. Chịu trách nhiệm đọc/ghi `PlayerPrefs` và cung cấp điểm truy cập `Data` cho các lớp khác.

**Thuộc tính:**
- `Instance: PlayerDataManager` (static)
- `Data: PlayerData` — Tham chiếu đến ScriptableObject; các lớp khác truy cập dữ liệu qua thuộc tính này.

**Phương thức:**
- `SaveData()` — Ghi `level`, `currentExp`, `money`, `avatarIndex`, `playerName` vào `PlayerPrefs`.
- `LoadData()` — Đọc từ `PlayerPrefs`; nếu không có → dùng giá trị mặc định.
- `ClearData()` — Xóa tất cả `PlayerPrefs`; reset `PlayerData` về mặc định (dùng khi Khách đăng xuất).

**Quan hệ:** Giữ tham chiếu `PlayerData`; được `ScoreManager` gọi sau trận; được `FirebaseManager` đọc để push Cloud Save.

---

### Lớp `PlayerData`

**Pattern:** ScriptableObject (Unity)  
**Vai trò:** Đối tượng dữ liệu thuần túy (Plain Data Object) lưu trữ thông tin hồ sơ người chơi. Được `PlayerDataManager` quản lý và tồn tại như một asset trong Project.

**Thuộc tính:**
- `playerName: string` — Tên hiển thị.
- `level: int` — Cấp độ hiện tại (min: 1).
- `currentExp: int` — EXP tích lũy trong level hiện tại.
- `money: int` — Tiền ảo.
- `avatarIndex: int` — Chỉ số avatar (0–7).

**Phương thức:**
- `GetExpToNextLevel()` — Trả về `level * 100` (ngưỡng EXP để lên cấp).
- `AddExp(amount)` — Cộng EXP; vòng lặp `while (currentExp >= GetExpToNextLevel())` để xử lý nhiều lần lên cấp cùng lúc.
- `AddMoney(amount)` — Cộng tiền.
- `Reset()` — Đặt tất cả về mặc định (chủ yếu dùng cho test).

**Quan hệ:** Được `PlayerDataManager` quản lý; được `ScoreManager.AwardRewards()` gọi; được `FirebaseManager.SaveProfileToCloud()` đọc.

---

### Lớp `QuestionData`

**Pattern:** ScriptableObject (Unity)  
**Vai trò:** Đối tượng dữ liệu của một câu hỏi quiz. Mỗi câu hỏi được tạo như một asset trong Project, hoặc được sinh động từ `LocalizationManager` trong `QuizManager.AutoGenerateDatabase()`.

**Thuộc tính:**
- `questionText: string` — Nội dung câu hỏi (hoặc localization key).
- `answers: string[4]` — Mảng 4 đáp án; thứ tự có thể bị shuffle trong `QuizManager`.
- `correctAnswerIndex: int` — Chỉ số đáp án đúng SAU KHI shuffle (0–3).

**Quan hệ:** Được `QuizManager` quản lý dưới dạng List; được `ScoreManager.CheckAnswer()` đọc; được `InputController_UXML` và `GameplayUIController` hiển thị.

---

### Lớp `InputController_UXML`

**Pattern:** Singleton (trong GameplayScene)  
**Vai trò:** Xử lý input của người dùng trong màn hình trận đấu. Nhận tương tác từ 4 nút đáp án (UXML buttons), thông báo cho `GameController`, và thực hiện animation feedback màu sắc.

**Thuộc tính:**
- `Instance: InputController_UXML` (static)

**Phương thức:**
- `OnAnswerButtonClicked(index)` — Callback khi người dùng bấm nút đáp án; gọi `GameController.SetLocalAnswer(index)`; Offline: `LocalMatchProvider.SubmitAnswerP1(index)`.
- `ShowAnswerFeedback(correctIdx)` (coroutine) — Nhận chỉ số đáp án đúng; tô xanh nút đúng, đỏ nút sai; chờ `revealDuration` rồi reset màu.

**Quan hệ:** Subscribe `QuizManager.OnQuestionChanged` để cập nhật text 4 nút đáp án; gọi `GameController.SetLocalAnswer()`; gọi `LocalMatchProvider.SubmitAnswerP1()` (Offline).

---

### Enum `GameState`

**Các giá trị:**
- `Idle` — Trạng thái chờ khởi tạo; điểm được reset, không có câu hỏi nào đang chạy.
- `Countdown` — Đang đếm ngược 3-2-1 trước khi bắt đầu; người chơi không thể tương tác.
- `Playing` — Trận đang diễn ra; người chơi trả lời câu hỏi, timer chạy.
- `GameOver` — Trận kết thúc; hiển thị kết quả, chờ người chơi điều hướng.

---

### Enum `WinResult`

**Các giá trị:**
- `Player1Wins` — Người chơi hiện tại (local) thắng (P1Score > P2Score hoặc đối thủ đầu hàng/mất mạng).
- `Player2Wins` — Đối thủ thắng (P2Score > P1Score hoặc người chơi đầu hàng).
- `Draw` — Hòa (P1Score == P2Score khi GameOver bình thường).

---

---

# 5. MÔ TẢ DETAILED SEQUENCE DIAGRAM

Phần này mô tả nội dung cần thể hiện trong từng Detailed Sequence Diagram (Mục IV.2.2 báo cáo). Khác với Analysis Sequence Diagram, các diagram này đi đến mức lớp và phương thức cụ thể trong source code.

---

### DSD-01: Luồng khởi động & Đăng nhập (Detailed)

**Các đối tượng:** `Android OS` → `Unity App (Awake/Start)` → `LocalizationManager` → `Google Sheets API` → `FirebaseManager` → `Firebase Auth/DB` → `InitSceneController_UXML` → `PlayerDataManager` → `GameManager`

**Mô tả chi tiết:** Ứng dụng khởi động theo thứ tự `Awake` → `Start` trên các Singleton. `LocalizationManager.Start()` kích hoạt coroutine `InitLocalization()`: thử `DownloadFromSheet()` với `UnityWebRequest` (timeout 6s) → nếu thành công, parse CSV và lưu vào `Application.persistentDataPath`; nếu thất bại, `TryLoadFromCache()` đọc file CSV đã lưu; nếu không có cache, `LoadLocalLanguage()` parse `vi.json` từ `StreamingAssets`. Song song, `FirebaseManager.Start()` gọi `FirebaseApp.CheckAndFixDependenciesAsync()` để kiểm tra SDK. Khi cả 2 sẵn sàng, `InitSceneController_UXML` kiểm tra Firebase current user. Nếu đã có session → auto login → `GET users/{uid}` → nạp `PlayerData` → `LoadHomeScene()`. Nếu chưa → hiển thị Auth Popup.

---

### DSD-02: Luồng Matchmaking đầy đủ (Detailed)

**Các đối tượng:** `Player1: MainMenuUIController` → `FirebaseManager` → `Firebase DB (matchmakingQueue)` → `Firebase DB (rooms)` → `Player2: MainMenuUIController` → `GameManager (cả 2 client)`

**Mô tả chi tiết:** P1 nhấn Tìm trận → `FirebaseManager.StartMatchmaking()` ghi `matchmakingQueue/uid1`. P2 cũng nhấn Tìm trận → ghi `matchmakingQueue/uid2`. Firebase trigger `OnValueChanged` cho cả 2 client khi queue thay đổi. Client phát hiện ≥ 2 người → client nào là "Host" (uid nhỏ hơn) sẽ: `SET rooms/{roomId} = {seed:random, questionCount, state:"waiting", players:{uid1,uid2}}` → `DELETE matchmakingQueue/uid1` → `DELETE matchmakingQueue/uid2`. Cả 2 client nhận `OnValueChanged` trên node room → `FirebaseManager` parse `OpponentId`, `IsHost`, `CurrentRoomId` → fire `OnMatchFound` → `MainMenuUIController` gọi `GameManager.LoadGameplayScene()`. Cần thể hiện hai "lifeline" song song cho P1 và P2.

---

### DSD-03: Luồng trả lời câu hỏi Online đầy đủ (Detailed)

**Các đối tượng:** `Player (input)` → `InputController_UXML` → `GameController` → `FirebaseMatchProvider` → `Firebase DB (rooms/{id}/answers)` → `TimerController` → `ScoreManager` → `FirebaseManager (UpdateMyScore)` → `Firebase DB (rooms/{id}/scores)` → `FirebaseMatchProvider (listener P2 score)` → `ScoreManager (SetOpponentScore)` → `GameplayUIController`

**Mô tả chi tiết:** Câu hỏi đang hiển thị, `TimerController` đang đếm. P1 bấm đáp án [idx] → `InputController.OnAnswerButtonClicked(idx)` → `GameController.SetLocalAnswer(idx)` → `FirebaseMatchProvider` ghi `rooms/{id}/answers/{uid_p1} = idx`. Song song, P2 cũng bấm (trên thiết bị khác) → ghi `rooms/{id}/answers/{uid_p2}`. Khi cả 2 đã ghi vào `answers`, `FirebaseMatchProvider` listener phát hiện → fire `OnBothPlayersAnswered(p1Ans, p2Ans)`. `TimerController` cũng `OnTimerEnd` → `HandleTimerEnd()`. `RevealAndAdvance()`: `ScoreManager.CheckAnswer(1, p1Ans)` → nếu đúng `AddScore(1,10)` → `UpdateMyScore(score)` → ghi `scores/{uid_p1}`. `FirebaseMatchProvider` listener `scores/{uid_p2}` nhận điểm mới → `OnOpponentScoreUpdated` → `SetOpponentScore()` → `OnScoreChanged` → HUD cập nhật. `ShowAnswerFeedback(correctIdx)` → đợi 2.5s → `NextQuestion()` hoặc `ChangeState(GameOver)`.

---

### DSD-04: Luồng kết thúc trận & lưu kết quả (Detailed)

**Các đối tượng:** `GameController` → `FirebaseManager (HostEndMatch)` → `Firebase DB (rooms)` → `ScoreManager` → `PlayerData` → `PlayerDataManager (SaveData)` → `FirebaseManager (SaveProfileToCloud)` → `Firebase DB (users/{uid})` → `GameplayUIController`

**Mô tả chi tiết:** `ChangeState(GameOver)` kích hoạt `EndMatchRoutine()` coroutine. Nếu Online + Host: `FirebaseManager.HostEndMatch(winner)` → `SET rooms/{id}/state = "ended"` và `SET rooms/{id}/winner = uid` (hoặc `"draw"`). Await task. `ScoreManager.AwardRewards()`: `GetWinner()` (kiểm tra `_forcedWinnerResult` trước, rồi so sánh điểm) → xác định EXP và Money theo bảng thưởng → `PlayerData.AddExp(exp)` (vòng lặp level-up nếu cần) → `PlayerData.AddMoney(money)`. `PlayerDataManager.SaveData()` → ghi 5 key vào `PlayerPrefs`. Nếu Online: `FirebaseManager.SaveProfileToCloud()` → `SET users/{uid}/level`, `.currentExp`, `.money` → await. Fire `GameController.OnGameOver` → `GameplayUIController` nhận → animate và hiển thị Result Popup với dữ liệu từ `ScoreManager.LastRewardExp`, `.LastRewardMoney`, `.GetWinner()`, `.Player1Score`, `.Player2Score`.

---

### DSD-05: Luồng Offline với MockOpponent (Detailed)

**Các đối tượng:** `MainMenuUIController` → `GameManager` → `GameController` → `LocalMatchProvider` ↔ `MockOpponent` ↔ `QuizManager` → `InputController_UXML` → `ScoreManager` → `GameplayUIController`

**Mô tả chi tiết:** Nhấn "ĐẤU VỚI MÁY" → `isOfflineMode = true` → `LoadGameplayScene()`. `GameController.Start()` phát hiện Offline → subscribe `LocalMatchProvider.OnBothPlayersAnswered`. `MockOpponent.Start()` subscribe `LocalMatchProvider.OnNewQuestionStarted`. Câu hỏi đầu tiên: `QuizManager.OnQuestionChanged` → `LocalMatchProvider.HandleNewQuestion()` reset state, set `_isWaiting = true`, fire `OnNewQuestionStarted` → `MockOpponent.HandleNewQuestion()` stop coroutine cũ, start `ThinkAndAnswer()`. Người chơi bấm đáp án → `LocalMatchProvider.SubmitAnswerP1(idx)` → `CheckIfBothAnswered()`. Bot sau delay → `LocalMatchProvider.SubmitAnswerP2(botIdx)` → `CheckIfBothAnswered()`. Khi cả 2 đã nộp → `OnBothPlayersAnswered(p1Ans, p2Ans)` → `GameController.HandleBothPlayersAnswered()` → `RevealAndAdvance(p1Ans, p2Ans)`: chấm điểm cho cả P1 lẫn P2 (Offline chấm cả 2 local).

---

*Tài liệu này được tổng hợp từ source code thực tế. Tất cả tên lớp, phương thức, thuộc tính và hằng số đã được xác minh từ file .cs trong dự án.*
