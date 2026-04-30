# Implementation Plan - Profile Setup System
## Tính năng: Thay đổi Tên & Ảnh đại diện (Avatar)

Tài liệu này mô tả chi tiết cách triển khai hệ thống Hồ sơ người chơi, cho phép tùy chỉnh thông tin cá nhân và đồng bộ hóa lên Firebase.

---

### 1. Phân tích Yêu cầu
- **Đổi tên**: Cho phép người chơi nhập tên mới (tối thiểu 3 ký tự, tối đa 15 ký tự).
- **Đổi ảnh đại diện**: Người chơi chọn từ một danh sách các Avatar có sẵn (Predefined Gallery) thay vì upload ảnh từ máy (để đảm bảo hiệu năng và bảo mật).
- **Đồng bộ hóa**: 
    - Lưu vào `PlayerPrefs` (Local).
    - Đồng bộ lên `users/{uid}/` trên Firebase Realtime Database.
    - Hiển thị Avatar của đối thủ trong trận đấu PvP.

---

### 2. Thay đổi Cấu trúc Dữ liệu
#### PlayerData (Local SO)
- Thêm trường `int avatarIndex`: Lưu chỉ số của ảnh đại diện đã chọn.

#### Firebase Schema
Cập nhật node người chơi:
```json
"users": {
    "uid_123": {
        "displayName": "New Name",
        "avatarIndex": 2,
        "level": 10,
        ...
    }
}
```

---

### 3. Giao diện (UI Toolkit)
#### [NEW] ProfilePopup.uxml
- Một Popup xuất hiện khi người chơi nhấn vào Avatar hoặc Tên ở màn hình chính.
- **Header**: Tiêu đề "HỒ SƠ CÁ NHÂN".
- **Avatar Section**: Hiển thị ảnh hiện tại và một Grid (danh sách) các ảnh đại diện khác để chọn.
- **Name Section**: Một `TextField` để nhập tên mới.
- **Footer**: Nút "LƯU" (Save) và "HỦY" (Cancel).

#### [MODIFY] HomeLayout.uxml
- Thêm một `Button` bao quanh khu vực Avatar/Tên để kích hoạt mở ProfilePopup.

---

### 4. Logic Triển khai (C#)
#### [NEW] ProfileUIController_UXML.cs
- Quản lý logic chọn Avatar (hiển thị khung viền chọn - selection highlight).
- Kiểm tra tính hợp lệ của tên mới.
- Gọi `FirebaseManager` và `PlayerDataManager` để lưu dữ liệu.

#### [MODIFY] FirebaseManager.cs
- Thêm hàm `UpdateProfile(string newName, int avatarIndex)` để đẩy dữ liệu lên Database.
- Cập nhật `HandleAuthResult` để tải thêm `avatarIndex` từ Cloud về máy.

#### [MODIFY] GameplayUIController_UXML.cs
- Cập nhật hiển thị Avatar của cả người chơi và đối thủ dựa trên `avatarIndex` nhận được từ Room Data.

---

### 5. Tài nguyên (Assets)
- Chuẩn bị bộ 10-20 ảnh đại diện dạng Cartoon/Icon đặt trong `Assets/Sprites/Avatars/`.
- Tạo một `AvatarSO` hoặc `List<Sprite>` để dễ dàng quản lý theo chỉ số (Index).

---

### 6. Kế hoạch xác minh (Verification)
1. **Kiểm tra Local**: Đổi tên/ảnh, tắt game mở lại xem có giữ nguyên không.
2. **Kiểm tra Firebase**: Kiểm tra trên Console xem node `avatarIndex` có được cập nhật đúng không.
3. **Kiểm tra PvP**: Hai máy cùng vào trận, kiểm tra xem có nhìn thấy ảnh đại diện của nhau không.

---
**Người lập kế hoạch:** Antigravity AI
**Ngày:** 29/04/2026
