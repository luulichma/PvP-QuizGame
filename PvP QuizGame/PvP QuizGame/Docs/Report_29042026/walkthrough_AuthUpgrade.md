# Walkthrough: Nâng cấp Hệ thống Auth & Giao diện Popup

Dưới đây là tóm tắt các thay đổi đã thực hiện để nâng cấp hệ thống Authentication từ chỉ hỗ trợ "Ẩn danh" sang hỗ trợ đầy đủ "Email/Password", đồng thời cải thiện trải nghiệm người dùng với giao diện Popup mới.

## 1. Backend: Nâng cấp FirebaseManager
Tệp tin: [FirebaseManager.cs](file:///f:/TaiLieu_Nam%203%20Ky%202/TTCS/PvP-QuizGame/PvP%20QuizGame/PvP%20QuizGame/Assets/Scripts/Network/FirebaseManager.cs)

- **Bổ sung API mới**: 
    - `SignUpWithEmail(email, password, displayName)`: Tạo tài khoản mới.
    - `SignInWithEmail(email, password)`: Đăng nhập tài khoản hiện có.
    - `SyncProfile()`: Đồng bộ dữ liệu từ Cloud nếu người dùng đã có phiên đăng nhập (Persisted Session).
- **Refactoring**: Di chuyển logic tải/tạo hồ sơ người dùng vào hàm dùng chung `HandleAuthResult` để đảm bảo tính nhất quán giữa các phương thức đăng nhập.

## 2. UI: Thiết kế lại AuthPopup
Tệp tin: [AuthPopup.uxml](file:///f:/TaiLieu_Nam%203%20Ky%202/TTCS/PvP-QuizGame/PvP%20QuizGame/PvP%20QuizGame/Assets/UI/Layouts/AuthPopup.uxml)

- **Cấu trúc đa tầng (Multi-container)**: Sử dụng các VisualElement làm container để chuyển đổi giữa các màn hình mà không cần đổi Scene:
    - `main-choice-container`: Màn hình chọn (Đăng nhập / Đăng ký / Khách).
    - `login-container`: Form Email/Mật khẩu.
    - `register-container`: Form Email/Mật khẩu/Tên.
    - `guest-container`: Nhập tên chơi nhanh.
- **Cải thiện thẩm mỹ**:
    - Phóng to chiều rộng Popup lên **600px**.
    - Tăng kích thước Font chữ và chiều cao Input (**80px**) để tối ưu cho thao tác chạm trên thiết bị di động.

## 3. Logic: Điều phối luồng InitScene
Tệp tin: [InitSceneController_UXML.cs](file:///f:/TaiLieu_Nam%203%20Ky%202/TTCS/PvP-QuizGame/PvP%20QuizGame/PvP%20QuizGame/Assets/Scripts/UI/InitSceneController_UXML.cs)

- **State Machine UI**: Cập nhật hàm `ShowAuthPopupRoutine` để điều khiển việc ẩn/hiện các container dựa trên thao tác của người dùng.
- **Tích hợp Firebase API**: Kết nối các nút bấm "Xác nhận" với các phương thức tương ứng trong `FirebaseManager`.
- **Hỗ trợ Auto-login**: Nếu người dùng đã đăng nhập từ trước, hệ thống sẽ tự động gọi `SyncProfile` để vào thẳng HomeScene, mang lại trải nghiệm mượt mà hơn.

## Kết quả thử nghiệm
- [x] Chuyển đổi linh hoạt giữa các màn hình Login/Register/Guest.
- [x] Hiển thị thông báo lỗi trực quan (Email sai, mật khẩu ngắn...).
- [x] Lưu tên hiển thị vào `PlayerPrefs` và đồng bộ lên Firebase Database thành công.

> [!NOTE]
> Các thay đổi này đã sẵn sàng để tích hợp vào bản build tiếp theo. Đừng quên kiểm tra cấu hình Firebase Auth trong Console trước khi test trên thiết bị thật.

## 4. Mới: Điều khiển từ xa (Firebase Remote Config)
Tệp tin: [FirebaseManager.cs](file:///f:/TaiLieu_Nam%203%20Ky%202/TTCS/PvP-QuizGame/PvP%20QuizGame/PvP%20QuizGame/Assets/Scripts/Network/FirebaseManager.cs), [TimerController.cs](file:///f:/TaiLieu_Nam%203%20Ky%202/TTCS/PvP-QuizGame/PvP%20QuizGame/PvP%20QuizGame/Assets/Scripts/Controller/TimerController.cs)

- **Biến Remote**: Thêm key `match_duration` (mặc định 180 giây).
- **Cơ chế**:
    - `FirebaseManager` tự động fetch dữ liệu khi khởi động (cache 1 giờ).
    - `TimerController` sẽ ưu tiên lấy giá trị này thay vì giá trị cứng trong Inspector khi bắt đầu trận đấu online.
- **Lợi ích**: Bạn có thể thay đổi thời lượng trận đấu ngay trên Firebase Console mà không cần cập nhật lại app.
