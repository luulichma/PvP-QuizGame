# TÀI LIỆU KỸ THUẬT: TRIỂN KHAI PHASE 1 (MVP RELEASE)

Tài liệu này mô tả chi tiết phương pháp kỹ thuật, luồng dữ liệu và các bước triển khai cho các tính năng thuộc Phase 1 của dự án PvP Quiz Game.

---

## 1. Vá Lỗ Hổng Bảo Mật (Firebase Security Rules) - Ưu tiên hàng đầu

Trước khi ra mắt, việc đầu tiên là thay thế bộ Rule lỏng lẻo (`.read: "auth != null"`) bằng bộ Rule nghiêm ngặt hơn để chống hack cơ bản.

### 1.1. Kiến trúc Database Rule mới
- **Node `/users/$uid`**: 
  - Đọc: Bất kỳ ai đăng nhập cũng có thể đọc (để phục vụ Leaderboard).
  - Ghi: Chỉ có `$uid` (chính chủ) mới được ghi.
  - Validation: Cần kiểm tra dữ liệu Exp và Money mới không được tăng đột biến so với dữ liệu cũ.
- **Node `/matchmakingQueue`**: 
  - Chỉ cho phép người dùng tự ghi vào Node của chính họ.
- **Node `/rooms`**: 
  - Đọc: Mọi người có thể đọc để kiểm tra phòng trống.
  - Ghi: Chỉ người có UID nằm trong danh sách `players` của phòng đó mới được phép ghi vào phòng (ví dụ: ghi điểm số, ghi đáp án).

### 1.2. Kế hoạch triển khai
- Sẽ tạo một file `firebase_rules_v2.json` trong dự án.
- Áp dụng các rules cơ bản lên Firebase Console trước khi build bản Release.

---

## 2. Bảng Xếp Hạng (Leaderboard)

### 2.1. Tiếp cận Kỹ thuật
Vì giới hạn của bản Free (không có Cloud Functions để tự động sort định kỳ), ta sẽ tận dụng chức năng Query trực tiếp của Firebase Realtime Database.

- **Query:** Truy vấn node `users/` và sắp xếp theo số tiền (`money`) tích lũy được.
- **Command:** `FirebaseDatabase.DefaultInstance.GetReference("users").OrderByChild("money").LimitToLast(100)` (Lấy top 100 người có nhiều tiền nhất).

### 2.2. Triển khai Code (C#)
Sẽ tạo thêm Script `LeaderboardManager.cs`:
1. `FetchGlobalLeaderboard()`: Gọi Firebase API như trên, nhận JSON.
2. Sắp xếp lại từ cao xuống thấp (vì `LimitToLast` trả về thứ tự tăng dần).
3. Đổ dữ liệu vào UI.

### 2.3. Triển khai Giao diện (UI Toolkit)
- Tạo file `LeaderboardPopup_UXML.uxml` và `LeaderboardStyles.uss`.
- Dùng thành phần `ListView` hoặc `ScrollView` để render danh sách người chơi, bao gồm:
  - Hạng (1, 2, 3...)
  - Avatar
  - Tên hiển thị
  - Số Tiền ($)

---

## 3. Hệ Thống Thành Tựu (Achievement System)

### 3.1. Cấu trúc Dữ liệu
**Local (`PlayerData`):**
Cập nhật ScriptableObject `PlayerData` thêm dictionary hoặc list lưu trạng thái thành tựu: `Dictionary<string, bool> unlockedAchievements`.

**Cloud (Firebase):**
Lưu trữ trên node: `users/{uid}/achievements/{achievementId} : true`.

### 3.2. Triển khai Code (C#)
1. **AchievementConfig (ScriptableObject):** Chứa ID thành tựu, Tiêu đề (dịch đa ngôn ngữ), Mô tả, Icon, và Mức thưởng (nếu có).
2. **AchievementManager.cs (Singleton):** 
   - Lắng nghe các sự kiện (Events) từ GameController và ScoreManager.
   - Ví dụ: `ScoreManager.OnMatchEnd += CheckWinAchievement;`
   - Kiểm tra điều kiện (vd: nếu trận thắng hiện tại là trận thứ 10 -> Mở khóa).
3. **Lưu trữ:** Cập nhật PlayerData và gọi `FirebaseManager.SaveAchievementsToCloud()`.

### 3.3. Hiển thị UI
- **In-game Notification:** Một popup nhỏ gọn (Toast) trượt từ trên xuống với dòng chữ "Thành tựu đã mở khóa: Cú Đêm!" khi người chơi vừa đạt được.
- **Trang Hồ sơ:** Thêm một Tab "Thành tựu" trong Profile Popup để xem các huy hiệu đã thu thập được.

---

## 4. Mở Rộng Ngân Hàng Câu Hỏi

### 4.1. Kỹ thuật
- Hệ thống `GoogleSheetDownloader` và `LocalizationManager` đã được thiết kế tối ưu, có thể xử lý file CSV với hàng ngàn dòng mà không gặp vấn đề về hiệu năng do đã có cơ chế cache ra JSON cục bộ.
- Kỹ thuật: Không cần sửa code, chỉ cần **thêm dữ liệu vào Google Sheet**.

### 4.2. Kế hoạch Nội dung
- Phân chia 4-5 chủ đề lớn (Toán học, Văn hóa, Khoa học, IT, Đố vui).
- Thu thập và thêm 50-100 câu mỗi chủ đề vào sheet (tuân thủ quy ước format: `q_category_id`, `a_category_id_1`, `a_category_id_2`...).
- Đáp án 1 (`_1`) luôn là đáp án đúng (hệ thống sẽ tự xáo trộn khi load).

---

## Các bước tiếp theo
1. Cập nhật Security Rules để chống hack (Thực hiện trực tiếp trên Firebase Console hoặc cung cấp file json cho Admin).
2. Code giao diện và logic của Leaderboard.
3. Code hệ thống Achievement cơ bản.
4. Cập nhật Database câu hỏi.
