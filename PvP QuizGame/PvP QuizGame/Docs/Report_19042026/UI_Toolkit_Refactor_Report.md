# Báo cáo: Chuyển đổi Hệ thống UI sang UI Toolkit (Unity 6)
**Ngày thực hiện:** 19/04/2026
**Dự án:** PvP Quiz Game

---

## 1. Tổng quan
Chúng ta đã thực hiện thay thế hoàn toàn hệ thống giao diện cũ (UGUI/Canvas) sang hệ thống **UI Toolkit** hiện đại nhất trên Unity 6. Phương pháp tiếp cận được sử dụng là **Manual Querying** để đảm bảo tính linh hoạt trong việc xử lý logic PvP thời gian thực.

## 2. Các tệp tin đã Sao lưu (Backup)
Để đảm bảo an toàn, các script cũ đã được đổi tên thành `*_Old.cs` tại thư mục cũ:
*   `Assets/Scripts/UI/InitSceneController_Old.cs`
*   `Assets/Scripts/UI/MainMenuUIController_Old.cs`
*   `Assets/Scripts/UI/GameplayUIController_Old.cs`
*   `Assets/Scripts/Controller/InputController_Old.cs`

## 3. Danh sách các tệp tin mới (Refactored)

### 3.1. Logic Khởi động (`InitSceneController_UXML.cs`)
*   **Chức năng**: Quản lý màn hình Loading (InitScene).
*   **Thay đổi**: Lớp được đặt tên lại thành `InitSceneController_UXML`. Sử dụng `root.Q<VisualElement>("loading-fill")` để thay đổi độ rộng của thanh tiến trình.

### 3.2. Logic Sảnh chính (`MainMenuUIController_UXML.cs`)
*   **Chức năng**: Điều hướng các màn hình sảnh (HomeScene).
*   **Thay đổi**: Lớp được đặt tên lại thành `MainMenuUIController_UXML`. Chuyển đổi giữa các Panel bằng `DisplayStyle.Flex`.

### 3.3. Logic Thi đấu (`GameplayUIController_UXML.cs`)
*   **Chức năng**: Hiển thị HUD điểm số, Câu hỏi, Thanh thời gian.
*   **Thay đổi**: Lớp được đặt tên lại thành `GameplayUIController_UXML`. Quản lý `ResultPopup` động.

### 3.4. Logic Input (`InputController_UXML.cs`)
*   **Chức năng**: Xử lý việc chọn đáp án (Player Gameplay).
*   **Thay đổi**: Lớp được đặt tên lại thành `InputController_UXML`. Tích hợp màu sắc phản hồi trực tiếp cho VisualElement.

---

## 4. Hướng dẫn thiết lập trong Unity Editor

> [!IMPORTANT]
> **Cập nhật Script Reference**: Vì tên lớp và tên tệp đã thay đổi, các Component gắn trên GameObject trong Unity có thể bị "Missing Script". Bạn cần:
> 1. Gỡ bỏ script cũ nếu bị lỗi Missing.
> 2. Gắn lại các file script mới (có hậu tố `_UXML`) vào đúng GameObject tương ứng.
> 3. Kéo tham chiếu **UI Document** vào ô trống trong Inspector.

1.  **Gán UIDocument**: Trong mỗi Scene, hãy gắn script Controller tương ứng vào một GameObject và kéo Component **UI Document** của Scene đó vào ô tham chiếu trong Script.
2.  **Gán Result Popup Template**: Trong `GameplayUIController`, bạn cần kéo file `ResultPopup.uxml` vào ô **Result Popup Template** để Game có thể hiển thị bảng thắng thua.
3.  **Kiểm tra Tên (Name)**: Hãy đảm bảo các phần tử trong UI Builder có tên (Name) khớp chính xác với các chuỗi Query trong code (ví dụ: `find-match-btn`, `p1-score`).

---
## 5. Kết luận
Hệ thống UI mới nhẹ hơn, sắc nét hơn và dễ dàng mở rộng thông qua tệp Style tập trung (`GlobalStyles.uss`). Việc tách biệt Styles (USS) và Layout (UXML) giúp bạn có thể thay đổi giao diện game mà không cần mở code C#.

**Người thực hiện:** Antigravity AI
