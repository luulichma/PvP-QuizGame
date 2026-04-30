# TÀI LIỆU MÔ TẢ LUỒNG TRÒ CHƠI & CHỨC NĂNG (PVP QUIZ GAME)

Tài liệu này tổng hợp toàn bộ luồng hoạt động (Game Flow) và các chức năng hệ thống từ lúc mở ứng dụng cho đến khi kết thúc một trận đấu.

---

## 1. Giai đoạn Khởi động & Xác thực (Init Scene)

### 1.1. Tải hệ thống (Loading)
- **Đồng bộ Đa ngôn ngữ (Localization):** Game bắt đầu bằng việc tải file CSV từ Google Sheet. Nếu có mạng, tải bản mới nhất; nếu không, sử dụng bản Cache hoặc JSON cục bộ.
- **Kiểm tra Firebase:** Kết nối với hệ thống Server Firebase để chuẩn bị xác thực.

### 1.2. Màn hình Đăng nhập (Auth Popup)
Nếu chưa có dữ liệu lưu trữ hợp lệ trên thiết bị, hệ thống sẽ mở Auth Popup với 3 lựa chọn chính:
*   **Đăng nhập (Login):** Nhập Email và Mật khẩu. Xác thực thông qua Firebase Auth. Hệ thống sẽ tải dữ liệu Profile từ Realtime Database về máy (Tên, Level, Avatar, Tiền).
*   **Đăng ký (Register):** Yêu cầu cung cấp Tên hiển thị, Email và Mật khẩu. Sau khi đăng ký thành công trên Firebase Auth, Game khởi tạo một bản ghi Database mặc định cho người chơi.
*   **Chơi khách (Guest):** Dành cho chơi Offline. Người chơi nhập "Tên hiển thị". Dữ liệu chỉ được lưu tại bộ nhớ tạm nội bộ của thiết bị (`PlayerPrefs`) và bỏ qua bước đồng bộ lên Cloud.

---

## 2. Giai đoạn Sảnh chờ (Home Scene)

Đây là trung tâm điều hướng của toàn bộ game, sử dụng giao diện UI Toolkit mượt mà.

### 2.1. Cụm Thông tin Người chơi (Profile HUD)
- **Hiển thị:** Ở góc trên cùng bên trái. Gồm có: Hình ảnh Avatar, Tên người chơi, Số tiền ($) và Cấp độ (Level).
- **Profile Popup (Sửa hồ sơ):** Khi bấm trực tiếp vào cụm Avatar/Tên hiển thị, một bảng Profile sẽ bật lên.
  - Cho phép thay đổi Tên và lựa chọn 1 trong 8 Avatar có sẵn.
  - Nhấn "Lưu thay đổi" sẽ cập nhật giao diện ngay lập tức và đồng bộ tự động lên Firebase Database.

### 2.2. Các Chức năng Điều hướng
- **TÌM TRẬN ĐẤU (Find Match):** Kích hoạt chế độ Online. 
  - Giao diện chuyển sang màn hình "Đang tìm đối thủ...". 
  - Thuật toán Matchmaking (Firebase) bắt đầu chạy: Tạo một phòng (Room) trên Database hoặc tham gia vào một phòng có sẵn. Khi đủ 2 người, chuyển sang cảnh Game.
- **ĐẤU VỚI MÁY (Practice):** Chơi Offline. Tắt đồng bộ Firebase và ném thẳng người chơi vào trận đấu mô phỏng với AI.
- **BẢNG XẾP HẠNG (Leaderboard):** Xem xếp hạng điểm số (nếu được kích hoạt).

### 2.3. Cài đặt (Settings Popup) & Đăng xuất
- Bấm biểu tượng ⚙️ góc phải trên cùng để mở bảng Cài đặt.
- **Âm thanh:** Tắt/bật Music và SFX.
- **Đa ngôn ngữ:** Tùy chọn ngôn ngữ (Tiếng Việt, English...). Đổi xong hệ thống tự động cập nhật mọi chữ trên màn hình.
- **Đăng xuất (Logout):** Có nút Đăng xuất dưới cùng. 
  - Nếu là tài khoản Khách, sẽ hiện Popup cảnh báo: *"Đăng xuất sẽ làm mất toàn bộ dữ liệu cục bộ"*. 
  - Sau khi xác nhận, đăng xuất tài khoản và văng ra màn hình `Init Scene`.

---

## 3. Giai đoạn Trong trận (Gameplay Scene)

### 3.1. Chuẩn bị trận đấu
- HUD phía trên chia làm 2 bên: **Bạn (P1)** bên trái và **Đối thủ (P2)** bên phải. Cập nhật đầy đủ Avatar, Tên và Điểm số (= 0).
- Hệ thống Load ngẫu nhiên X câu hỏi từ `QuizManager` (dựa trên gói ngôn ngữ hiện tại).

### 3.2. Tiến trình Câu hỏi (Quiz Flow)
- **Giao diện:** Hiển thị thẻ bộ đếm câu (vd: Câu 1 / 10), phần nội dung câu hỏi lớn ở giữa, và 4 nút Đáp án ở dưới (A, B, C, D).
- **Đếm ngược:** Bộ hẹn giờ chạy ở giữa màn hình. Khi hết giờ mà không chọn, coi như sai và chuyển câu tiếp theo.
- **Xử lý lựa chọn:** Người chơi nhấn vào 1 đáp án. Nút đổi màu lập tức (xanh = đúng, đỏ = sai). Điểm số tăng lên dựa vào thời gian trả lời (càng nhanh càng nhiều điểm).
- **Đồng bộ thời gian thực:** (Khi chơi Online) Đáp án của đối thủ cũng được gởi qua Firebase và làm mới HUD bên phải.

### 3.3. Tương tác Thoát game
- Nút ✖ mờ ở góc trái trên cùng.
- Khi bấm vào, mở **Exit Confirm Popup**: *"Bạn có chắc muốn rời đi? Bạn sẽ bị xử thua"*.
- Nếu Thoát, trận đấu lập tức dừng. Hệ thống thông báo với Firebase rằng người chơi này đã đầu hàng, đối thủ tự động thắng.

### 3.4. Kết thúc & Tổng kết (Result Popup)
Sau khi trả lời hết các câu hỏi, bảng Báo cáo Kết quả (Result Popup) sẽ hiện lên:
- **Tiêu đề:** Tuyên bố **THẮNG!**, **THUA!**, hoặc **HÒA!** to rõ ở giữa màn hình.
- **Chỉ số:** So sánh Điểm của Bạn vs Điểm Đối thủ.
- **Phần thưởng:** Cộng Tiền ảo dựa trên mức độ thắng thua.
- **Lưu hệ thống:** Tự động lưu Tiền thưởng, điểm Kinh nghiệm, nâng Level và cập nhật lên Firebase.
- **Điều hướng:**
  - *Chơi Lại (Play Again):* Chức năng này sẽ thoát phòng cũ, về lại sảnh và kích hoạt chế độ Tìm trận đấu mới.
  - *Về Sảnh (Back Home):* Về lại trang chủ để nghỉ ngơi.

---

## 4. Công nghệ Cốt lõi (Core Architecture)
- **UI Toolkit:** Giao diện dựng hoàn toàn bằng UXML/USS (chuẩn W3C như CSS/HTML) nhẹ, linh hoạt, hỗ trợ Flexbox cho màn hình ngang/dọc dễ dàng.
- **LocalizationManager:** Tải CSV dịch thuật tự động từ Google Sheets. Code tách riêng Logic với Nội dung.
- **Firebase Services:** Firebase Authentication (Mã hóa đăng nhập), Realtime Database (Đồng bộ phòng đấu, cập nhật tiến trình 2 máy dưới <200ms).
- **Event-Driven:** Dùng C# Action, Delegate để truyền Event giữa UI và Game Logic (ví dụ: `FirebaseManager.OnMatchFound`). Giúp code dễ bảo trì và không bị phụ thuộc vòng (Circular Dependency).
