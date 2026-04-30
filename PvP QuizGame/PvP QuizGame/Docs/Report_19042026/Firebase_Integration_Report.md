# Báo cáo: Tích hợp Firebase SDK (Phase 1)
**Ngày lập:** 19/04/2026
**Dự án:** PvP Quiz Game

---

## 🏗️ 1. Quá trình thiết lập Hệ thống

Trong phiên làm việc này, chúng ta đã thực hiện việc đưa Firebase vào dự án thông qua các bước sau:

1.  **Khởi tạo dự án**: Đã tạo dự án trên Firebase Console và đăng ký ứng dụng Android với thông tin chính xác.
2.  **Cấu hình tệp tin**: Đã thêm tệp `google-services.json` vào thư mục `Assets/` để Unity nhận diện cấu hình backend.
3.  **Xử lý lỗi Registry**: 
    *   Ban đầu chúng ta thử cài đặt qua Scoped Registry của Google (`unityproxy.googlesettings.com`).
    *   Do lỗi định dạng phản hồi (HTML thay vì JSON) của server Google, tôi đã thực hiện dọn dẹp sạch `manifest.json` để tránh lỗi biên dịch.
4.  **Cài đặt SDK chuẩn**: Chuyển sang phương pháp **Manual Import** sử dụng bộ SDK chính thức bản stable (v12.1.0/v13.x) qua `.unitypackage`.

---

## 💻 2. Hiện thực mã nguồn (`FirebaseManager.cs`)

Tôi đã nâng cấp `FirebaseManager` từ một bản nháp (stub) thành logic khởi tạo thực tế:

*   **Dependency Check**: Sử dụng `CheckAndFixDependenciesAsync` để đảm bảo dịch vụ Google Play trên thiết bị di động luôn khả dụng trước khi chạy các tính năng khác.
*   **Module đã sẵn sàng**:
    *   `FirebaseAuth`: Hỗ trợ đăng ký/đăng nhập.
    *   `FirebaseDatabase`: Sẵn sàng cho hệ thống Matchmaking.
*   **Tính năng hiện có**: Đã chuẩn bị sẵn hàm `SignInAnonymous()` để người chơi có thể vào game nhanh mà không cần tạo tài khoản.

---

## 🛠️ 4. Xử lý lỗi Build Android & EDM4U (Unity 6)

Trong quá trình cài đặt, chúng ta đã xử lý các lỗi đặc thù của Unity 6 để đảm bảo Firebase hoạt động trên Android:

1.  **Định danh ứng dụng (Package Name)**: Khắc phục lỗi trống Package Name trong Project Settings, đảm bảo khớp 100% với tệp `google-services.json`.
2.  **Lỗi DirectoryNotFound (Gradle Templates)**:
    *   Vấn đề: EDM4U không tìm thấy đường dẫn để nạp thư viện.
    *   Giải pháp: Kích hoạt **Custom Main Gradle Template** và **Custom Settings Gradle Template** trong Player Settings.
3.  **Lỗi Jetifier (Unity 6 Compatibility)**:
    *   Vấn đề: Unity 6 yêu cầu cấu hình Jetifier để tương thích với các thư viện Android hiện đại của Firebase.
    *   Giải pháp: Kích hoạt **Custom Gradle Properties Template** để EDM4U có thể ghi các thuộc tính cần thiết vào quá trình build.
4.  **Kết quả**: Đã thực hiện `Force Resolve` thành công. Các thư viện native đã được nạp đầy đủ vào thư mục `Assets/Plugins/Android`.

---

## ✅ 5. Trạng thái hiện tại
*   **Compilation**: Dự án đã hết lỗi đỏ và biên dịch thành công sau khi Import SDK.
*   **EDM4U**: Hệ thống External Dependency Manager đã được tích hợp để tự động quản lý các thư viện `.aar` trên Android.

---
> [!IMPORTANT]
> **Lưu ý tiếp theo**: Bước tiếp theo chúng ta sẽ tiến hành xây dựng màn hình **Login/Register** hoặc triển khai trực tiếp logic **Matchmaking** (ghép phòng) cho 2 người chơi.

**Người thực hiện:** Antigravity AI
