# Hướng dẫn: Thiết lập & Tùy chỉnh UI Toolkit (Unity 6)
**Ngày lập bài:** 19/04/2026
**Dành cho:** PvP Quiz Game - Giai đoạn Chuyển đổi UI

---

## 1. Kết nối Script với Scene (Bắt buộc)

Sau khi tôi đã remapping GUID, Unity sẽ tự nhận diện các script mới. Để game chạy được, bạn hãy làm theo các bước sau trong từng Scene tương ứng:

### A. InitScene
1. Tìm GameObject tên là **UI** hoặc **InitUIController**.
2. Nếu script cũ báo lỗi *Missing*, hãy Remove nó đi.
3. Kéo script **InitSceneController_UXML.cs** vào GameObject đó.
4. Kéo Component **UI Document** ở gần đó vào ô trống `Ui Document` trong script.

### B. HomeScene
1. Tìm GameObject quản lý chính (thường là **MainMenu**).
2. Gắn script **MainMenuUIController_UXML.cs**.
3. Kéo **UI Document** (đã chọn file `HomeLayout.uxml`) vào ô tham chiếu.

### C. GameplayScene
1. **InputManager**: Gắn script **InputController_UXML.cs**.
2. **GameplayUI**: Gắn script **GameplayUIController_UXML.cs**.
3. **Quan trọng**: Trong `GameplayUIController_UXML`, hãy kéo file `ResultPopup.uxml` vào ô **Result Popup Template**.

---

## 2. Tinh chỉnh giao diện bằng UI Builder

Vì chúng ta dùng UI Toolkit, bạn có thể chỉnh sửa giao diện mà không cần động vào một dòng code nào:

1. Double-click vào file `.uxml` bất kỳ (ví dụ `GameplayLayout.uxml`).
2. Tab **Hierarchy** bên trái cho bạn thấy cấu trúc (Câu hỏi, Nút bấm).
3. Tab **Inspector** bên phải cho phép thay đổi:
    *   `Margin/Padding`: Khoảng cách.
    *   `Background Color`: Màu nền.
    *   `Border Radius`: Độ bo cong của các nút.
4. Nhấn **Ctrl+S** trong UI Builder để lưu lại - kết quả sẽ hiện ngay lập tức trong cửa sổ Game.

---

## 3. Thay đổi hình ảnh & Icons

Để thay thế các placeholder (như logo PVP hay hình người chơi):
1. Mở file UXML trong UI Builder.
2. Chọn phần tử muốn đổi ảnh (ví dụ `avatar`).
3. Tìm mục **Background -> Image**.
4. Chọn tấm ảnh Sprite bạn muốn (Unity 6 hỗ trợ Sprite cực tốt trong UI Toolkit).

---

## 4. Xử lý Background đẹp (MainBG)

Như đã thảo luận, để giữ tấm ảnh nền tuyệt đẹp của bạn:
1. Mở `InitLayout.uxml`, `HomeLayout.uxml`.
2. Chọn phần tử **root** (thành phần cha ngoài cùng).
3. Trong Inspector của UI Builder, tìm **Background -> Image** và chọn tệp `GameplayBackground_0`.
4. Chỉnh **Image Fit** thành `Scale to Fill` để ảnh tràn màn hình.

---

## 5. Dọn dẹp hệ thống cũ

Sau khi đã thấy UI mới hiển thị và script nhận diện được nút bấm:
1. Xóa các GameObject **Canvas**, **EventSystem** cũ.
2. Xóa các thư mục `NewUI` (Texture cũ) nếu bạn cảm thấy không cần dùng đến ảnh demo nữa.

---
> [!TIP]
> **Mẹo Unity 6**: Bạn có thể mở đồng thời cửa sổ **UI Builder** và cửa sổ **Game**. Khi bạn sửa màu trong UI Builder, cửa sổ Game sẽ cập nhật Real-time, giúp bạn test màu sắc cực nhanh!

**Người hướng dẫn:** Antigravity AI
