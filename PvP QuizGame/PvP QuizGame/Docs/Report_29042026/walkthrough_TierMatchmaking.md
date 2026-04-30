# Tổng kết: Matchmaking theo Bậc & Số lượng Câu hỏi Động

Tôi đã hoàn thành việc triển khai hệ thống phân chia người chơi vào các nhánh chờ (Tier) dựa trên Level hiện tại của họ, đồng thời tự động hóa việc tính toán số lượng câu hỏi trong mỗi trận. Không có bất kỳ thay đổi cấu hình thủ công nào cần thiết trên Firebase Database, vì Firebase tự động xử lý các key đường dẫn khi chúng được khởi tạo.

## Các thay đổi chính

### 1. Phân quyền và Tìm kiếm trận (Matchmaking Queue)
- Trong `FirebaseManager.cs`, giờ đây hàm `GetPlayerTier(level)` sẽ đánh giá người chơi đang ở bậc nào (Level 1-10 -> Tier 1, v.v.).
- Khi bắt đầu tìm trận (Matchmaking), thay vì ném tất cả người chơi vào `matchmakingQueue`, hệ thống sẽ cho người chơi vào `matchmakingQueue/tier_1` (nếu họ ở Tier 1). Điều này đảm bảo những người ở Tier 1 không bao giờ bị ghép cặp với Tier 2 hay Tier 3.

### 2. Số lượng câu hỏi động (Dynamic Limits)
- Trong `FirebaseManager.cs`, hàm `GetQuestionCountForTier(tier)` sẽ quyết định số câu hỏi của trận (10, 20 hoặc 30).
- Khi Host tạo Room thành công trên Firebase, số lượng câu hỏi (ví dụ: `questionCount: 20`) sẽ được lưu kèm vào dữ liệu Room.

### 3. Đồng bộ hóa trong Game
- Cả hai máy khách (client) khi tham gia chung một Room sẽ cùng đọc thông số `questionCount` (thông qua `GameController.cs`).
- Con số này sau đó được truyền xuống `QuizManager.cs`. Thay vì load hết toàn bộ danh sách câu hỏi trong Database, `QuizManager` sẽ **tự động cắt ngắn (truncate)** danh sách các câu đã xáo trộn để đảm bảo game kết thúc chính xác sau 10, 20, hoặc 30 câu tuỳ thuộc vào bậc Level của room.
- Tính năng này cũng hoạt động liền mạch cho chế độ chơi Offline (Offline Mode sẽ tự kiểm tra level hiện tại của Local Player).

## Kết quả
Bạn có thể tiếp tục chơi thử! Người chơi có Level < 11 sẽ chỉ đối đầu với người chơi cùng hạng, và mỗi trận đấu sẽ chỉ gồm 10 câu. Càng lên cấp cao, các trận đấu sẽ càng dài hơi và kịch tính hơn.
