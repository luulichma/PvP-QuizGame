# Hướng dẫn: Thiết lập Firebase cho Unity 6 (Step-by-Step)

Tài liệu này hướng dẫn bạn cách khởi tạo dự án Firebase và tích hợp vào Unity một cách chuẩn nhất để tránh các lỗi xung đột version.

---

## 🏗️ Bước 1: Tạo dự án trên Firebase Console

1.  Truy cập: [Firebase Console](https://console.firebase.google.com/).
2.  Nhấn **"Add Project"**, đặt tên cho dự án (ví dụ: `PVP-Quiz-Game`).
3.  **Google Analytics**: Bạn có thể chọn Enable (Khuyên dùng) hoặc Disable tùy ý.

---

## 📱 Bước 2: Đăng ký ứng dụng Unity của bạn

1.  Tại màn hình Dashboard dự án, nhấn vào biểu tượng **Unity (U)** ở giữa màn hình.
2.  **Register App**:
    *   **Android Package Name**: Điền đúng ID trong Unity (vào `Project Settings > Player > Android > Package Name`). Ví dụ: `com.yourname.pvpquiz`.
    *   **iOS Bundle ID**: Tương tự như Android (ví dụ: `com.yourname.pvpquiz`).
3.  Nhấn **"Register App"**.

---

## 📂 Bước 3: Tải tệp cấu hình (Quan trọng nhất)

1.  Tải tệp **`google-services.json`** (cho Android) và/hoặc **`GoogleService-Info.plist`** (cho iOS).
2.  **Vị trí đặt file**: Copy tệp vừa tải về vào thư mục **`Assets/`** bất kỳ trong dự án Unity của bạn. 
    > [!TIP]
    > Bạn nên tạo thư mục `Assets/FirebaseSettings/` để lưu trữ cho gọn gàng.

---

## 📦 Bước 4: Thêm Firebase SDK vào Unity (Cách hiện đại cho Unity 6)

Thay vì tải về các tệp phức tạp, chúng ta sẽ sử dụng **Package Manager** để Unity tự động quản lý:

1.  Trong Unity, vào **Edit > Project Settings > Package Manager**.
2.  Thêm một **Scoped Registry** mới:
    *   **Name**: `Game Package Registry by Google`
    *   **URL**: `https://unityproxy.googlesettings.com/`
    *   **Scopes**: 
        *   `com.google.firebase`
        *   `com.google.external-dependency-manager`
3.  Nhấn **Save**.
4.  Vào **Window > Package Manager**, đổi chế độ xem sang **"My Registries"**.
5.  Chọn và cài đặt (Install) các gói sau:
    *   **Firebase Auth** (Xác thực người dùng)
    *   **Firebase Realtime Database** (Đồng bộ trận đấu)

---

## 🚀 Bước 5: Cấu hình mã nguồn (Tôi sẽ làm giúp bạn)

Sau khi bạn đã hoàn thành **Bước 3**, hãy báo cho tôi. Tôi sẽ thực hiện:
*   Tự động cấu hình file `manifest.json` để Unity tải gói về.
*   Viết code khởi tạo thực tế cho `FirebaseManager.cs`.
