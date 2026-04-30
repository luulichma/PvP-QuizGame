# Báo cáo: Hoàn thiện GameState & Đồng bộ hóa PvP (Phase 2)
**Ngày thực hiện:** 28/04/2026
**Dự án:** PvP Quiz Game

---

## 1. Tổng quan công việc
Trong phiên làm việc này, chúng ta đã hoàn thiện logic cốt lõi của trò chơi PvP trực tuyến bằng cách kết nối các thành phần UI Toolkit với Firebase Realtime Database. Mục tiêu là đảm bảo cả hai người chơi thấy cùng một bộ câu hỏi và cập nhật được tiến trình của đối thủ.

## 2. Các thay đổi chính

### 2.1. Đồng bộ hóa Câu hỏi (Quiz Synchronization)
*   **Vấn đề**: Trước đây mỗi máy khách (client) tự xáo trộn câu hỏi dẫn đến việc hai người chơi thấy nội dung khác nhau.
*   **Giải pháp**: Nâng cấp `QuizManager.cs` hỗ trợ **Seeded Shuffle**.
*   **Hiện thực**: `GameController` hiện tại truyền một `seed` cố định (ví dụ: `12345`) vào `quizManager.StartQuiz()`, đảm bảo thứ tự câu hỏi giống hệt nhau trên mọi thiết bị.

### 2.2. Đồng bộ hóa Câu trả lời (Answer Sync)
*   **FirebaseMatchProvider [MỚI]**: Đã tạo mới script này để thay thế/bổ sung cho `LocalMatchProvider`.
*   **Logic**: 
    *   Khi người chơi chọn đáp án, script sẽ ghi dữ liệu lên đường dẫn `rooms/{roomId}/answers/{userId}`.
    *   Đồng thời lắng nghe thay đổi tại `rooms/{roomId}/answers/{opponentId}`.
    *   Khi nhận đủ 2 đáp án, nó sẽ phát sự kiện `OnBothPlayersAnswered` để `GameController` thực hiện chấm điểm và chuyển câu.

### 2.3. Đồng bộ hóa Điểm số (Score Sync)
*   **ScoreManager.cs**: 
    *   Đã kết nối với `FirebaseManager`. Mỗi khi người chơi thực hiện trả lời đúng, điểm số sẽ được `Push` lên Firebase.
    *   Lắng nghe sự kiện `OnOpponentScoreUpdated` từ Firebase để cập nhật điểm đối thủ lên UI HUD.
*   **FirebaseManager.cs**: Thêm các phương thức `UpdateMyScore`, `SubmitMyAnswer` và logic `ValueChanged` listener để theo dõi dữ liệu Realtime.

### 2.4. Hỗ trợ Chế độ Offline (Debug Mode)
*   **Vấn đề**: Khi phát triển, người dùng cần test nhanh với Bot (máy) trong Unity Editor mà không cần mở 2 bản build.
*   **Giải pháp**: Thêm biến `isOfflineMode` vào `FirebaseManager`.
*   **Hiện thực**:
    *   `InputController_UXML` sẽ tự động chuyển hướng đáp án sang `LocalMatchProvider` (Bot) hoặc `FirebaseMatchProvider` (Online) dựa trên biến này.
    *   `MockOpponent` (Bot) chỉ kích hoạt logic "suy nghĩ" khi ở chế độ Offline.

---

## 3. Cấu trúc dữ liệu trên Firebase (Đề xuất/Hiện tại)
Dữ liệu được tổ chức theo cấu trúc phẳng để tối ưu tốc độ Realtime:
```json
{
  "rooms": {
    "room_test_123": {
      "answers": {
        "player_local_id": 2,
        "player_opponent_id": 0
      },
      "scores": {
        "player_local_id": 20,
        "player_opponent_id": 10
      }
    }
  }
}
```

---

## 4. Hướng dẫn Kiểm thử (Testing)
Để kiểm tra tính năng đồng bộ, bạn có thể:
1.  **Mở đồng thời 2 bản Build** (hoặc 1 Editor + 1 Build).
2.  Đảm bảo cả hai cùng kết nối vào `room_test_123` (Hiện tại đang được giả lập cứng trong `FirebaseManager.JoinOrCreateRoom`).
3.  Khi Người chơi 1 chọn đáp án, HUD của Người chơi 2 sẽ chưa thay đổi cho đến khi cả hai cùng chọn xong (theo đúng luật PvP).
4.  Điểm số của đối thủ sẽ được cập nhật ngay lập tức sau khi câu hỏi kết thúc.

## 5. Kết luận
Hệ thống GameState đã hoàn thành việc "Gửi/Nhận" dữ liệu cần thiết. Bước tiếp theo có thể là hoàn thiện hệ thống **Matchmaking thực tế** (thay vì dùng ID phòng cố định) và thêm hiệu ứng chờ đợi (Loading/Waiting for opponent) sinh động hơn.

**Người thực hiện:** Antigravity AI
