# Báo cáo kết quả sửa lỗi ngày 09/05/2026

Hôm nay đã thực hiện xử lý 2 vấn đề quan trọng liên quan đến trải nghiệm chơi game (Offline) và hệ thống ghép trận (Online).

---

## 1. Sửa lỗi không hiển thị Popup Win/Lose ở cuối game (Offline Mode)

### Vấn đề:
Khi người chơi trả lời xong câu hỏi cuối cùng trong chế độ đấu với máy, game không hiển thị popup thông báo thắng/thua dù logic tính điểm vẫn chạy.

### Nguyên nhân:
Trong file `GameplayUIController_UXML.cs`, tại hàm `HandleGameOver` và `ShowExitConfirmation`, các popup được khởi tạo bằng lệnh `Instantiate()` nhưng **chưa được add vào Visual Tree** (`uiDocument.rootVisualElement.Add(...)`). Do đó chúng chỉ nằm trong bộ nhớ mà không hiển thị lên màn hình.

### Đã xử lý:
- Đã thêm lệnh `uiDocument.rootVisualElement.Add(_resultPopupInstance);` trong hàm `HandleGameOver`.
- Đã thêm lệnh `uiDocument.rootVisualElement.Add(_exitPopupInstance);` trong hàm `ShowExitConfirmation`.
*(Tôi đã cập nhật trực tiếp vào file code của dự án).*

---

## 2. Sửa lỗi Race Condition và Deadlock trong Matchmaking (Online Mode)

### Vấn đề:
- **Deadlock:** Khi 2 người cùng vào hàng chờ lúc trống, cả 2 đều thấy trống và tự thêm mình vào rồi đứng đợi vô hạn.
- **Race Condition:** Khi 2 người thấy nhau, cả 2 cùng cố xoá người kia và tạo 2 phòng riêng biệt.

### Giải pháp (Đã thực hiện trong code):
Tôi đã viết lại logic Matchmaking trong `FirebaseManager.cs` theo chiến lược **"Add-First & Oldest Waits"**:
1. **Luôn thêm mình vào queue trước** ngay khi bấm tìm trận.
2. **Đọc queue** có sắp xếp theo thời gian `joinedAt`.
3. **Nếu mình là người cũ nhất**: Đứng yên và chờ (lắng nghe `currentRoom`).
4. **Nếu có người khác cũ hơn**: Thử dùng Transaction để "chiếm" họ. Nếu thành công thì xoá mình khỏi queue và tạo phòng.

### Kết quả:
- Loại bỏ hoàn toàn khả năng kẹt (Deadlock).
- Loại bỏ hoàn toàn khả năng trùng phòng (Race Condition) nhờ Transaction trên node của đối thủ.
- Tự động dọn dẹp node trên Firebase khi rớt mạng bằng `OnDisconnect`.

---
*Báo cáo được tạo tự động bởi Antigravity.*
