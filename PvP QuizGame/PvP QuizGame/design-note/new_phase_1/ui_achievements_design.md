# THIẾT KẾ UI HOMESCENE VÀ HỆ THỐNG THÀNH TỰU (ACHIEVEMENTS)

## 1. Kiến trúc UI mới cho HomeScene
Để mang lại trải nghiệm giống các ứng dụng mobile hiện đại và thuận tiện cho việc mở rộng tính năng, UI của sảnh chờ (HomeScene) được chuyển đổi sang cấu trúc **Bottom Navigation Tabs**.

### Cấu trúc Layout
- **Content Area**: Vùng hiển thị nội dung chính của Tab hiện tại (chiếm 85% diện tích phía trên).
- **Bottom Navigation Bar**: Thanh menu cố định ở đáy màn hình với 3 icon:
  1. `[Shop Icon]` **Cửa Hàng** (Trái): Hiện tại hiển thị "Coming Soon".
  2. `[Home Icon]` **Trang Chủ** (Giữa - Default): Chứa nút Tìm Trận, Đấu Máy, Avatar, Thông tin tiền và level.
  3. `[Trophy Icon]` **Xếp Hạng** (Phải): Hiển thị khu vực vinh danh và thành tích cá nhân.

### Khu vực Leaderboard Tab
Khi người dùng chuyển sang Tab Xếp Hạng, Content Area sẽ hiển thị một Panel có 2 Sub-Tabs nằm ở đầu:
- **Xếp Hạng (Leaderboard)**: Top 100 người chơi có Điểm Xếp Hạng cao nhất.
- **Thành Tựu (Achievements)**: Danh sách các cột mốc cá nhân.

*Lưu ý: Toàn bộ kích thước chữ (font-size) trong game sẽ được tăng lên ~20% qua `GlobalStyles.uss` để dễ đọc hơn trên màn hình di động.*

---

## 2. Hệ Thống Thành Tựu (Đề xuất Sáng tạo)

Để tăng tính thú vị, các thành tựu không chỉ đo đếm số lần thắng mà còn đánh giá cách người chơi vượt qua thử thách. Mỗi thành tựu sẽ có thưởng Tiền (Money) hoặc Điểm (Rank Points).

### A. Nhóm "Cày Cuốc" (Tiến trình)
1. **Khởi Động Sương Sương**: Thắng trận đấu máy (Bot) đầu tiên. *(Thưởng: 50$)*
2. **Kẻ Hủy Diệt Máy Móc**: Đánh bại Bot 50 lần. *(Thưởng: 500$)*
3. **Phú Hào Mới Nổi**: Tổng tiền tích lũy chạm mốc 10,000$. *(Thưởng: Khung Avatar Đặc biệt - Phase 2)*
4. **Bước Chân Thần Tốc**: Đạt 1,000 Điểm Xếp Hạng (Vượt qua mức Tân binh). *(Thưởng: 200$)*
5. **Đỉnh Bảng Phong Thần**: Đạt 5,000 Điểm Xếp Hạng. *(Thưởng: 1,000$)*

### B. Nhóm "Kỹ Năng" (Thành tích trong trận)
6. **Cỗ Máy Ghi Điểm**: Thắng 5 trận Đấu Thường (Online) liên tiếp. *(Thưởng: 300$)*
7. **Độc Cô Cầu Bại**: Thắng 10 trận Đấu Thường (Online) liên tiếp. *(Thưởng: 1,000$)*
8. **Trí Tuệ Đỉnh Cao**: Chiến thắng một trận Đấu Thường mà **không trả lời sai câu nào**. *(Thưởng: 100 Rank Points)*
9. **Não To Hơn Máy**: Trả lời đúng một câu hỏi trong vòng chưa tới 2 giây. *(Thưởng: 20$)*
10. **Lật Kèo Thế Kỷ**: Giành chiến thắng chung cuộc dù đang bị đối thủ dẫn trước ở câu hỏi áp chót. *(Thưởng: 50 Rank Points)*

*(Lưu ý: Ở Phase 1 MVP, chúng ta sẽ ưu tiên code Logic Tracking cho 5 thành tựu cơ bản nhất để đảm bảo tiến độ: Khởi Động Sương Sương, Kẻ Hủy Diệt Máy Móc, Bước Chân Thần Tốc, Cỗ Máy Ghi Điểm, Trí Tuệ Đỉnh Cao. Các thành tựu còn lại sẽ mở rộng dần).*
