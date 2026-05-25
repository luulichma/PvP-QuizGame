# BÁO CÁO TIẾN ĐỘ HÀNG TUẦN — TUẦN 6 ĐẾN TUẦN 11
## Dự án: PvP Quiz Game — Ứng dụng Game Đố Vui Trực Tuyến 1 vs 1

**Sinh viên:** Nguyễn Thế Chiến  
**Môn học:** Thiết kế & Triển khai Hệ thống Phần mềm  
**Nền tảng:** Android | Unity | Firebase  

---

# BÁO CÁO TUẦN 6
**Thời gian:** 23/03/2026 — 29/03/2026

## 1. Công việc thực hiện trong tuần

### GĐ3 — Hoàn thiện UI & Audio (Task 3.5)
- Hoàn thiện toàn bộ giao diện người dùng cho 3 màn hình chính: Init Scene (đăng nhập/đăng ký), Home Scene (sảnh chờ) và Gameplay Scene (trận đấu).
- Tích hợp hệ thống âm thanh: nhạc nền (BGM) riêng biệt cho màn hình trang chủ và trận đấu; hiệu ứng âm thanh (SFX) cho các sự kiện trả lời đúng, trả lời sai và kết thúc trận.
- Xây dựng `AudioManager` với chức năng bật/tắt nhạc nền và SFX độc lập, lưu trạng thái qua `PlayerPrefs`.
- Hoàn thiện các Popup: Settings, Profile, Exit Confirm, Result — đều có animation xuất hiện/ẩn mượt mà thông qua `UIAnimator` (DOTween).

### GĐ4 — Hệ thống Xác thực (Task 4.1 — bắt đầu)
- Tích hợp **Firebase Authentication** (Email/Password).
- Xây dựng `FirebaseManager` với các phương thức đăng ký, đăng nhập và đăng xuất.
- Màn hình Auth Popup hỗ trợ 3 luồng: Đăng nhập, Đăng ký, Chơi khách (offline).
- Sau xác thực thành công: tải dữ liệu hồ sơ người chơi từ Firebase Realtime Database về thiết bị.

### Thay đổi phạm vi dự án
- **Loại bỏ chức năng Admin Panel** khỏi phạm vi dự án. Lý do: chức năng này không phục vụ trực tiếp trải nghiệm người chơi, làm phức tạp không cần thiết và nằm ngoài trọng tâm của môn học. Cập nhật lại danh sách Use Case từ 18 UC xuống còn 16 UC (loại bỏ các UC liên quan đến quản trị viên).

## 2. Kết quả đạt được
- Giao diện 3 màn hình hoàn chỉnh, có animation, responsive với nhiều tỷ lệ màn hình Android.
- Hệ thống đăng nhập/đăng ký hoạt động ổn định với Firebase Authentication.
- Âm thanh tích hợp đầy đủ, có thể điều chỉnh trong Settings.

## 3. Vấn đề gặp phải & Hướng giải quyết

| Vấn đề | Hướng giải quyết |
|---|---|
| `UIDocument` chưa build xong khi `OnEnable` được gọi dẫn đến không query được các Button | Thêm cơ chế retry query trong 30 frame sau khi enable, đảm bảo luôn tìm được đủ nút |
| Animation DOTween xung đột với trạng thái UI Toolkit khi popup đóng quá nhanh | Dùng callback hoàn thành animation (`OnComplete`) trước khi `RemoveFromHierarchy` |

## 4. Kế hoạch tuần tiếp theo
- Hoàn thiện hệ thống Xác thực, xử lý các edge case (sai mật khẩu, không có mạng).
- Bắt đầu xây dựng hệ thống ghép trận (Matchmaking) sử dụng Firebase Realtime Database.

## 5. Mức độ hoàn thành tổng thể dự án: **~60%**

---

# BÁO CÁO TUẦN 7
**Thời gian:** 30/03/2026 — 05/04/2026

## 1. Công việc thực hiện trong tuần

### GĐ4 — Hệ thống Ghép trận Matchmaking (Task 4.2)
- Xây dựng toàn bộ luồng ghép trận tự động (Matchmaking) trên Firebase Realtime Database.
- Thuật toán: Khi người chơi nhấn "Tìm trận", hệ thống ghi UID vào node `matchmakingQueue`. Firebase listener phát hiện có ≥ 2 người trong hàng đợi → người vào trước (Host) tạo node `rooms/{roomId}`, ghi thông tin cả 2 người chơi vào room, xóa cả 2 UID khỏi queue.
- Cả 2 client lắng nghe sự kiện `OnMatchFound` → tự động chuyển sang màn hình trận đấu.
- Xây dựng chức năng **Hủy tìm trận**: xóa UID khỏi `matchmakingQueue` và quay về màn hình chờ.

### Thay đổi thiết kế luật chơi quan trọng
- **Thay đổi cơ chế trả lời câu hỏi:** Luật chơi ban đầu yêu cầu người chơi phải chờ đối thủ trả lời xong mới chuyển câu tiếp theo. Sau khi đánh giá lại, cơ chế này gây ra trải nghiệm bị động và thụ động.
- **Cơ chế mới:** Mỗi câu hỏi có một **đồng hồ đếm ngược chung** (mặc định 15 giây, cấu hình được qua Firebase Remote Config). Cả 2 người chơi cùng trả lời trong khoảng thời gian đó. Khi hết giờ, hệ thống chấm điểm đồng thời cho cả 2, sau đó chuyển câu tiếp theo. Ai trả lời sớm hơn sẽ được tính thêm điểm tốc độ.
- Cập nhật `TimerController`: quản lý vòng đời đếm ngược (Start, Pause, Resume), phát sự kiện `OnTimerTick` và `OnTimerEnd`.

## 2. Kết quả đạt được
- Hệ thống Matchmaking hoạt động thực tế, có thể ghép 2 thiết bị khác nhau qua Internet.
- Cơ chế timer mới được thiết kế và tích hợp, cải thiện đáng kể tính năng động của trận đấu.
- Hệ thống phòng đấu (Room) trên Firebase được thiết lập với cấu trúc rõ ràng.

## 3. Vấn đề gặp phải & Hướng giải quyết

| Vấn đề | Hướng giải quyết |
|---|---|
| Race condition khi 2 client cùng lúc cố tạo room (cả 2 đều thấy queue có 2 người) | Quy định chỉ người vào queue trước (Host) mới có quyền tạo room; client còn lại chờ lắng nghe node room xuất hiện |
| Người chơi thoát app giữa chừng khi đang trong hàng đợi khiến UID "ma" tồn tại mãi trong queue | Sử dụng Firebase `onDisconnect()` để tự động xóa UID khỏi queue khi mất kết nối |

## 4. Kế hoạch tuần tiếp theo
- Xây dựng hệ thống đồng bộ trạng thái real-time giữa 2 thiết bị trong trận đấu.
- Triển khai cơ chế đồng bộ seed câu hỏi để đảm bảo 2 người cùng thứ tự câu hỏi.

## 5. Mức độ hoàn thành tổng thể dự án: **~70%**

---

# BÁO CÁO TUẦN 8
**Thời gian:** 06/04/2026 — 12/04/2026

## 1. Công việc thực hiện trong tuần

### GĐ4 — Đồng bộ Trạng thái Real-time (Task 4.3)
- Hoàn thiện cơ chế đồng bộ trạng thái giữa 2 thiết bị trong trận đấu thông qua Firebase Realtime Database với độ trễ dưới 200ms.
- Triển khai **seed-based shuffle**: Host tạo một seed ngẫu nhiên, ghi vào `rooms/{id}/seed`. Cả 2 client đọc cùng seed và dùng thuật toán Fisher-Yates với seed đó → đảm bảo 2 thiết bị luôn hiển thị cùng thứ tự câu hỏi và đáp án.
- Xây dựng cơ chế ghi/đọc đáp án theo thời gian thực: mỗi khi người chơi chọn đáp án, kết quả được ghi lên `rooms/{id}/answers/{uid}`. Firebase listener phía bên kia nhận được ngay lập tức → cập nhật điểm đối thủ lên HUD.
- Xử lý trường hợp đối thủ mất kết nối giữa trận: Firebase `OnDisconnect` + listener `OnOpponentDisconnected` → thông báo cho người chơi còn lại và xử lý kết quả (người còn lại thắng).

### GĐ4 — Bảo mật Dữ liệu (Task 4.4)
- Thiết lập Firebase Security Rules để bảo vệ dữ liệu:
  - Chỉ người dùng đã xác thực mới được đọc/ghi dữ liệu của bản thân.
  - Dữ liệu phòng đấu chỉ được truy cập bởi 2 người chơi trong phòng đó.
  - Ngăn chặn người dùng tự ý chỉnh sửa điểm số của đối thủ.

## 2. Kết quả đạt được
- Trận đấu online đồng bộ hoàn toàn: điểm số, đáp án, trạng thái cập nhật real-time giữa 2 thiết bị.
- Hệ thống bảo mật Firebase Rules được thiết lập, ngăn chặn gian lận cơ bản.
- Xử lý ổn định các trường hợp đặc biệt: mất mạng, thoát game đột ngột.

## 3. Vấn đề gặp phải & Hướng giải quyết

| Vấn đề | Hướng giải quyết |
|---|---|
| Cả 2 client đều nhận `OnOpponentDisconnected` khi một bên mất mạng rồi kết nối lại, gây ra kết thúc trận sai | Thêm cờ `isMatchEnded` trên Firebase để không xử lý sự kiện disconnect hai lần |
| Điểm số cập nhật không đồng đều giữa 2 máy do Firebase listener có độ trễ nhỏ | Chỉ dùng Firebase để đồng bộ điểm đối thủ; điểm của bản thân luôn tính cục bộ ngay lập tức |

## 4. Kế hoạch tuần tiếp theo
- Bắt đầu giai đoạn kiểm thử toàn diện trên thiết bị Android thực tế.
- Build APK và cài đặt thử nghiệm.

## 5. Mức độ hoàn thành tổng thể dự án: **~83%**

---

# BÁO CÁO TUẦN 9
**Thời gian:** 13/04/2026 — 19/04/2026

## 1. Công việc thực hiện trong tuần

### GĐ5 — Kiểm thử & Sửa lỗi trên thiết bị thực (Task 5.1 — bắt đầu)
- Cài đặt APK thử nghiệm lên thiết bị Android thực tế (không phải giả lập Unity Editor).
- Phát hiện và ghi lại danh sách lỗi (fix-log) sau khi kiểm thử:
  - Một số Label trong UI Toolkit hiển thị chữ bị tràn ra ngoài khung trên màn hình nhỏ → Điều chỉnh USS `overflow: hidden` và `text-overflow: ellipsis`.
  - Font chữ tiếng Việt không hiển thị đúng trên một số thiết bị Android cũ → Nhúng font trực tiếp vào asset thay vì dùng font hệ thống.
  - Hiệu ứng scale animation của DOTween không chạy đúng trên Android IL2CPP → Điều chỉnh lại target scale value.

### GĐ5 — Build APK hoàn chỉnh (Task 5.2)
- Cấu hình Build Settings: IL2CPP, ARM64, Min SDK 23 (Android 6.0 trở lên).
- Bật **Minification** (Release mode) để giảm kích thước APK.
- Tích hợp các file Firebase config (`google-services.json`) vào build.
- Xuất APK thành công, kích thước sau tối ưu phù hợp cho phân phối.

## 2. Kết quả đạt được
- APK chạy ổn định trên thiết bị thực, không crash.
- Phát hiện và ghi nhận 8 lỗi trong quá trình kiểm thử, đã sửa được 6/8.
- File APK hoàn chỉnh, sẵn sàng cho vòng kiểm thử tiếp theo.

## 3. Vấn đề gặp phải & Hướng giải quyết

| Vấn đề | Hướng giải quyết |
|---|---|
| Trên Android build, `File.ReadAllText()` không đọc được file trong `StreamingAssets` (chỉ hoạt động trong Editor) | Chuyển sang dùng `UnityWebRequest` với URI `jar:file:///` cho tất cả I/O trong `StreamingAssets` trên Android |
| APK bị từ chối kết nối Firebase do thiếu SHA-1 fingerprint | Thêm SHA-1 của debug keystore vào cấu hình Firebase Console |

## 4. Kế hoạch tuần tiếp theo
- Tiếp tục kiểm thử chuyên sâu, đặc biệt tập trung vào các luồng lỗi và edge case.
- Sửa 2 lỗi còn lại trong fix-log.
- Bắt đầu viết báo cáo tổng kết dự án.

## 5. Mức độ hoàn thành tổng thể dự án: **~90%**

---

# BÁO CÁO TUẦN 10
**Thời gian:** 20/04/2026 — 26/04/2026

## 1. Công việc thực hiện trong tuần

### GĐ5 — Kiểm thử & Sửa lỗi (Task 5.1 — tiếp tục)
- Kiểm thử chuyên sâu hệ thống Localization (đa ngôn ngữ) trên bản build Android:
  - **Phát hiện lỗi nghiêm trọng:** Khi chuyển sang ngôn ngữ tiếng Anh trong bản build, dữ liệu không được nạp lại đúng cách. Phân tích nguyên nhân: hàm `SwitchLanguage()` gọi coroutine bất đồng bộ nhưng không có cơ chế hủy coroutine cũ, dẫn đến race condition khi người dùng đổi ngôn ngữ nhanh; ngoài ra Settings hiển thị 8 lựa chọn ngôn ngữ nhưng chỉ có 2 file JSON (`vi.json`, `en.json`) tồn tại thực tế.
  - **Đã sửa:** Thêm `_switchCoroutine` để hủy coroutine cũ trước khi khởi động mới; thêm cơ chế rollback `_previousLanguage` khi file ngôn ngữ không tồn tại; bổ sung đầy đủ 8 file JSON còn thiếu (`fr.json`, `it.json`, `de.json`, `es.json`, `ja.json`, `ko.json`) để hỗ trợ toàn bộ 8 ngôn ngữ trong dropdown; thay `Path.Combine()` bằng string concatenation trực tiếp để đảm bảo URI `jar:file://` hợp lệ trên Android.
- Kiểm thử toàn bộ luồng game end-to-end: Đăng ký → Đăng nhập → Tìm trận → Chơi → Xem kết quả → Chơi lại.
- Sửa lỗi hiển thị avatar đối thủ không cập nhật đúng khi vào Gameplay Scene.

### GĐ5 — Viết báo cáo (Task 5.3 — bắt đầu)
- Hoàn thiện tài liệu đặc tả yêu cầu: 16 Use Case Specification đầy đủ.
- Viết mô tả sơ đồ lớp phân tích và sơ đồ tuần tự (sequence diagram) ở mức phân tích nghiệp vụ.
- Soạn thảo phần mô tả hệ thống và khảo sát hệ thống tương tự.

## 2. Kết quả đạt được
- Lỗi Localization trên Android build đã được tìm ra nguyên nhân gốc rễ và sửa hoàn toàn.
- Toàn bộ fix-log đã được giải quyết (8/8 lỗi).
- Phần I và II của báo cáo hoàn thành bản nháp.

## 3. Vấn đề gặp phải & Hướng giải quyết

| Vấn đề | Hướng giải quyết |
|---|---|
| Lỗi Localization chỉ xuất hiện trên build Android, không tái hiện trong Editor do `File.IO` đồng bộ trong Editor nhưng `UnityWebRequest` bất đồng bộ trên Android | Phân tích luồng code kỹ, thêm coroutine cancellation và fallback rollback |
| Dropdown ngôn ngữ trong Settings hiển thị 8 tùy chọn nhưng 6 file JSON chưa có → crash khi chọn | Bổ sung đầy đủ 6 file JSON còn thiếu cho các ngôn ngữ `fr`, `it`, `de`, `es`, `ja`, `ko`; toàn bộ 8 ngôn ngữ hoạt động bình thường |

## 4. Kế hoạch tuần tiếp theo
- Hoàn thiện toàn bộ báo cáo (các phần còn lại: kiến trúc, thiết kế chi tiết, kết quả kiểm thử).
- Build APK phiên bản cuối cùng sau khi đã sửa hết lỗi.
- Chú thích source code và dọn dẹp code thừa.

## 5. Mức độ hoàn thành tổng thể dự án: **~95%**

---

# BÁO CÁO TUẦN 11
**Thời gian:** 27/04/2026 — 30/04/2026

## 1. Công việc thực hiện trong tuần

### GĐ5 — Hoàn thiện kiểm thử (Task 5.1 — hoàn thành)
- Thực hiện vòng kiểm thử cuối cùng sau tất cả các bản vá lỗi.
- Kiểm tra lại toàn bộ 16 Use Case trên thiết bị thực: tất cả đều hoạt động đúng theo đặc tả.
- Kiểm thử đặc biệt với 2 thiết bị Android khác nhau kết nối qua Firebase: độ trễ đồng bộ điểm số < 200ms, ghép trận ổn định.
- Chú thích toàn bộ source code theo chuẩn XML Documentation (summary, param, returns) cho các class cốt lõi: `LocalizationManager`, `GameController`, `FirebaseManager`, `QuizManager`, `ScoreManager`.

### GĐ5 — Hoàn thiện báo cáo (Task 5.3 — hoàn thành)
- Hoàn thành toàn bộ báo cáo theo cấu trúc 5 phần (I–V):
  - Phần I: Mô tả hệ thống & khảo sát tương tự
  - Phần II: Thu thập yêu cầu (16 UC, sơ đồ phân tích)
  - Phần III: Phân tích hệ thống (class diagram, sequence diagram)
  - Phần IV: Thiết kế chi tiết (kiến trúc, detailed class & sequence diagram)
  - Phần V: Triển khai & Kết quả (môi trường, hướng dẫn cài đặt, kết quả kiểm thử)
- Cập nhật lại tài liệu phản ánh đúng phạm vi dự án thực tế: 16 UC (không có Admin), luật chơi timer-based.
- Soạn danh sách các hướng phát triển tương lai: Bảng xếp hạng, Hệ thống thành tựu, Phòng riêng/Mời bạn, Chọn chủ đề câu hỏi, Cửa hàng vật phẩm, Thử thách hàng ngày...

## 2. Kết quả đạt được — Tổng kết dự án

| Hạng mục | Kết quả |
|---|---|
| Tổng số Use Case hoàn thành | 16/16 UC |
| Chế độ chơi | Online PvP (Firebase) + Offline (AI Bot) |
| Ngôn ngữ hỗ trợ | Tiếng Việt, Tiếng Anh |
| Số màn hình (Scene) | 3 (Init, Home, Gameplay) |
| Kết quả kiểm thử | 8/8 lỗi đã sửa, tất cả luồng UC hoạt động đúng |
| Nền tảng | Android (Min SDK 23, ARM64) |
| File APK | Hoàn chỉnh, đã kiểm thử trên thiết bị thực |

## 3. Thay đổi so với kế hoạch ban đầu

| Nội dung thay đổi | Lý do |
|---|---|
| Loại bỏ Admin Panel (6 UC) | Không phục vụ trực tiếp trải nghiệm người chơi; nằm ngoài trọng tâm môn học |
| Đổi cơ chế trả lời: chờ đối thủ → timer chung | Timer tạo áp lực thời gian, tăng tính cạnh tranh và tốc độ trận đấu |
| Giảm từ 18 UC xuống 16 UC | Phản ánh đúng phạm vi thực tế sau khi bỏ Admin |
| Mở rộng Localization lên đủ 8 ngôn ngữ | Ban đầu chỉ có vi/en; trong quá trình kiểm thử phát hiện lỗi và bổ sung đủ file JSON cho fr/it/de/es/ja/ko để hoàn thiện tính năng đa ngôn ngữ như thiết kế ban đầu |

## 4. Đánh giá tổng kết
Dự án đã hoàn thành đúng mục tiêu ban đầu: xây dựng một ứng dụng game đố vui PvP trên Android với đầy đủ các thành phần của một hệ thống phần mềm hoàn chỉnh (xác thực, cơ sở dữ liệu thời gian thực, giao diện người dùng, hệ thống phần thưởng và đa ngôn ngữ). Các thay đổi trong quá trình phát triển đều có lý do rõ ràng và không ảnh hưởng đến chất lượng tổng thể của sản phẩm.

## 5. Mức độ hoàn thành tổng thể dự án: **100%**
