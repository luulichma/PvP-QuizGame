# Kế hoạch Nâng cấp Hệ thống Auth & Giao diện Popup

Yêu cầu: Thiết kế lại luồng khởi đầu game với các lựa chọn Đăng nhập, Đăng ký, hoặc Chơi khách. Phóng to giao diện để dễ thao tác hơn.

## 1. Cấu trúc lại giao diện Popup (`AuthPopup.uxml`)
Tôi sẽ chia Popup thành 4 "Container" chính, chuyển đổi qua lại bằng thuộc tính `display: none`:
- **MainChoiceContainer**: Chứa 3 nút lớn: [ĐĂNG NHẬP], [ĐĂNG KÝ], [CHƠI KHÁCH].
- **LoginContainer**: Form nhập Email + Password + Nút [QUAY LẠI] + Nút [Xác nhận].
- **RegisterContainer**: Form nhập Email + Password + Tên hiển thị + Nút [QUAY LẠI] + Nút [Xác nhận].
- **GuestContainer**: Form nhập Tên hiển thị (như hiện tại) + Nút [QUAY LẠI] .

**Thông số thẩm mỹ mới:**
- Popup Width: Tăng từ 500px lên 600px.
- Font-size: Title (48px), Label/Input (32px), Button (36px).
- Input height: Tăng lên 80px để dễ gõ trên mobile.

## 2. Bổ sung Logic Backend (`FirebaseManager.cs`)
Hiện tại `FirebaseManager` chỉ hỗ trợ ẩn danh. Tôi sẽ bổ sung:
- `SignUpWithEmail(email, password, displayName)`: Đăng ký tài khoản mới và lưu profile lên Cloud.
- `SignInWithEmail(email, password)`: Đăng nhập tài khoản cũ và tải profile về.
- Cập nhật logic `SignInAnonymousAndLoadProfile` để tương thích với luồng mới.

## 3. Điều phối luồng UI (`InitSceneController_UXML.cs`)
- Cập nhật hàm `ShowAuthPopupRoutine` để lắng nghe sự kiện từ cả 3 nút lựa chọn.
- Xây dựng máy trạng thái đơn giản (State Machine) để ẩn/hiện các Container tương ứng khi người chơi nhấn nút.
- Xử lý các thông báo lỗi (Email sai định dạng, mật khẩu yếu, v.v.) trực tiếp trên UI.

## Các tệp tin sẽ chỉnh sửa:
### [MODIFY] `FirebaseManager.cs`
- Thêm phương thức Đăng nhập/Đăng ký bằng Email.
### [MODIFY] `AuthPopup.uxml`
- Thiết kế lại layout đa tầng, phóng to mọi thành phần.
### [MODIFY] `InitSceneController_UXML.cs`
- Logic chuyển đổi các form và gọi API Firebase tương ứng.

## Câu hỏi & Lưu ý:
> [!IMPORTANT]
> - Việc sử dụng Email/Password yêu cầu bạn phải **Bật (Enable) phương thức Email/Password** trong Firebase Console -> Authentication -> Sign-in method.
> - Tôi sẽ giữ nguyên cơ chế lưu `PlayerPrefs` cho Tên hiển thị để đồng bộ với logic hiện tại của bạn.
> - Bạn có muốn tôi bổ sung nút "Quên mật khẩu" luôn không?
