# Báo cáo: Hiển thị ID người chơi trong trận đấu

Tôi đã cập nhật giao diện Gameplay để hiển thị ID thực tế của người chơi thay vì các nhãn mặc định "TÔI" và "ĐỐI THỦ".

## Các thay đổi chính

### 1. Cập nhật `GameplayUIController_UXML.cs`
- Thay đổi logic trong hàm `LocalizeHUD()`.
- **Phía người chơi:** Ưu tiên hiển thị `LocalUserId` (Firebase UID). Nếu chưa đăng nhập, sẽ hiển thị `LocalDisplayName` (mặc định là "PLAYER").
- **Phía đối thủ:** Ưu tiên hiển thị `OpponentId`. Nếu không có ID đối thủ (chế độ Offline), sẽ hiển thị `OpponentName` (mặc định là "BOT").
- **Tối ưu hiển thị:** Vì UID của Firebase rất dài (28 ký tự), tôi đã thêm logic tự động cắt ngắn chuỗi xuống **10 ký tự đầu tiên** để đảm bảo tên không bị tràn ra khỏi khung UI của thẻ bài.

## Kết quả
Bây giờ, khi vào trận đấu, bạn sẽ thấy 10 ký tự đầu của ID mình và đối thủ hiện lên ở hai góc màn hình, tạo cảm giác chuyên nghiệp và chuẩn xác hơn cho hệ thống PvP.
