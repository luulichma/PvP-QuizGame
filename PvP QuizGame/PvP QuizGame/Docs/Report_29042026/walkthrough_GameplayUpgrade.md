# Project Walkthrough - 29/04/2026
## Cập nhật Cơ chế Gameplay & Tối ưu hóa Giao diện

Bản cập nhật này tập trung vào việc chuyển đổi luồng chơi PvP sang dạng bất đồng bộ (Race Mode), tối ưu hóa hiển thị trên các thiết bị di động hiện đại và sửa các lỗi về kết nối Firebase.

---

### 1. Cơ chế PvP Đua Tốc độ (Fixed-Time Rhythm)
Thay vì bắt hai người chơi phải chờ nhau trả lời xong mới qua câu tiếp theo, hệ thống đã được chuyển sang cơ chế đếm ngược cố định cho từng câu hỏi.
- **Per-question Timer**: Mỗi câu hỏi có 15 giây (cấu hình qua Firebase Remote Config key `question_duration`).
- **Nhịp độ cố định**: Dù người chơi trả lời sớm, game vẫn sẽ đợi hết 15 giây mới hiển thị đáp án (Reveal) và tự động chuyển sang câu tiếp theo.
- **Tiến triển độc lập**: Hai người chơi tự chạy thời gian riêng, không còn phụ thuộc vào trạng thái của đối thủ, giúp game liền mạch và kịch tính hơn.

**Các file ảnh hưởng:**
- `FirebaseManager.cs`: Thêm Remote Config cho thời gian câu hỏi.
- `TimerController.cs`: Chuyển từ đếm ngược tổng sang đếm ngược từng câu.
- `GameController.cs`: Điều phối luồng chấm điểm và chuyển câu dựa trên sự kiện hết giờ.

---

### 2. Tối ưu hóa Giao diện (Responsive UI & Font Scaling)
Khắc phục vấn đề giao diện bị bé trên các màn hình điện thoại dài (iPhone 12 Pro Max, v.v.) và cải thiện khả năng đọc.
- **Panel Settings**: Điều chỉnh `Match Mode` từ 0 (Width) sang **0.5** để cân bằng tỉ lệ scale trên mọi loại màn hình.
- **Đơn vị linh hoạt (%)**: Chuyển đổi các layout từ Pixel cố định sang tỷ lệ phần trăm (Width: 50%, Padding: 5%, v.v.).
- **Tăng kích thước chữ**: Tăng Font-size trung bình từ **20-30%** trên toàn bộ hệ thống (Nút bấm, Tiêu đề, Ô nhập liệu).

**Các file ảnh hưởng:**
- `MainPanelSetting.asset`: Chỉnh lại tỉ lệ match.
- `GlobalStyles.uss`: Cập nhật font chữ chung.
- `InitLayout.uxml`, `AuthPopup.uxml`, `HomeLayout.uxml`: Cấu trúc lại layout theo dạng responsive.

---

### 3. Tách biệt Chế độ Chơi (Game Modes)
Loại bỏ việc người dùng phải tự tích chọn "isOfflineMode" trong Inspector. Hệ thống hiện đã tự động hóa hoàn toàn.
- **Nút bấm riêng biệt**: 
    - **Tìm trận đấu**: Tự động tắt Offline mode, gọi Firebase Matchmaking.
    - **Đấu với máy**: Tự động bật Offline mode, vào trận với Bot ngay lập tức.
- **Tự động hóa logic**: Các Provider (Firebase/Local) tự chuyển đổi dựa trên lựa chọn của người chơi.

**Các file ảnh hưởng:**
- `HomeLayout.uxml`: Thêm nút `Practice`.
- `MainMenuUIController_UXML.cs`: Xử lý logic bật/tắt chế độ chơi tự động.

---

### 4. Sửa lỗi kết nối Firebase (Bug Fixes)
Sửa lỗi "Firebase chưa sẵn sàng" khi người dùng chuyển từ chế độ Offline sang Online.
- **Khởi tạo On-demand**: Firebase hiện luôn được khởi tạo và đăng nhập ẩn danh ngay khi mở app (nếu có mạng), thay vì bị chặn bởi cờ Offline.
- **Auth Flow**: Màn hình Init luôn thực hiện luồng Auth để đảm bảo Token luôn sẵn sàng khi người chơi muốn tìm trận Online.

**Các file ảnh hưởng:**
- `FirebaseManager.cs`: Bỏ chặn khởi tạo trong hàm `Start`.
- `InitSceneController_UXML.cs`: Luôn chạy luồng Auth khi khởi động.

---

### Hướng dẫn kiểm tra (Verification Plan)
1. **Kiểm tra UI**: Mở `GameplayScene` hoặc `HomeScene` trong Unity, chuyển đổi giữa các Simulator (iPhone, iPad, Android). Giao diện phải tự phình to và cân đối.
2. **Kiểm tra Chế độ chơi**: 
    - Nhấn "Đấu với máy" -> Phải vào game ngay với Bot.
    - Nhấn "Tìm trận" -> Phải hiện màn hình tìm kiếm (yêu cầu có mạng).
3. **Kiểm tra Timer**: Trả lời thật nhanh một câu hỏi, đợi xem game có đứng im đợi hết 15 giây rồi mới qua câu mới hay không.

---
**Người thực hiện:** Antigravity AI
**Ngày cập nhật:** 29/04/2026
