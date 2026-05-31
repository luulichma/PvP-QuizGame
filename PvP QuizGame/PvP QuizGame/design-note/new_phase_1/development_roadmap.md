# LỘ TRÌNH PHÁT TRIỂN (DEVELOPMENT ROADMAP) V2

Bản lộ trình này đã được cập nhật lại dựa trên thiết kế phân tách 3 loại tài nguyên mới: **Tiền (Money)**, **Điểm Xếp Hạng (Rank Points)**, và **Kinh Nghiệm/Cấp Độ (EXP/Level)**.

---

## 🟢 PHASE 1: CHUẨN BỊ RA MẮT (MVP - Minimum Viable Product)

*Mục tiêu: Đảm bảo game có đủ tính năng giữ chân người chơi cơ bản và bảo mật an toàn để có thể đưa lên Store/itch.io.*

**1. Cấu trúc lại Hệ thống Tài nguyên & Bảo mật (Core Updates)**

- Khóa chặt Firebase Security Rules (Chống thao túng database, chỉ cho phép user sửa profile của chính họ).
- Cập nhật Data Schema: Thêm trường `rankPoints` vào `PlayerData` và Firebase.
- Viết lại `ScoreManager` và `PlayerDataManager` để áp dụng công thức thưởng dựa trên hệ số (Level Multiplier).

**2. Bảng Xếp Hạng (Leaderboard)**

- Xây dựng UI Leaderboard bằng UI Toolkit.
- Hiển thị danh sách Top 100 người chơi dựa theo **Điểm Xếp Hạng (`rankPoints`)**.

**3. Hệ thống Thành tựu (Achievements)**

- Xây dựng hệ thống UI và Logic mở khóa thành tựu.
- Định nghĩa 3-5 thành tựu cơ bản (vd: Đạt Level 5, Đạt 1000 Điểm Xếp Hạng, Thắng 10 trận).
- Tích hợp thông báo (Popup notification) khi mở khóa thành tựu.

**4. Mở rộng Nội dung (Content Expansion)**

- Bổ sung 50-100 câu hỏi vào Google Sheets (ít nhất cho 3 chủ đề phổ biến).
- Đảm bảo cơ chế tải và cache đa ngôn ngữ hoạt động trơn tru với bộ dữ liệu lớn.

---

## 🟡 PHASE 2: TĂNG TƯƠNG TÁC & THƯƠNG MẠI HÓA (1-2 tháng sau ra mắt)

*Mục tiêu: Đa dạng hóa trải nghiệm và tạo mục đích sử dụng cho đơn vị "Tiền".*

**1. Cửa hàng vật phẩm (In-Game Shop)**

- Xây dựng giao diện Cửa hàng.
- Bán các Avatar mới, Khung viền (Border), hoặc Hiệu ứng đặc biệt.
- Giao dịch sử dụng **Tiền (Money)** kiếm được từ trận đấu.

**2. Chọn chủ đề câu hỏi (Topic Selection)**

- Cho phép người chơi chọn chủ đề (Toán học, Lịch sử, Đố vui...) trước khi tìm trận hoặc đấu máy.
- Hệ thống lấy random bộ câu hỏi dựa theo chủ đề đã chọn.

**3. Thử thách hàng ngày (Daily Challenge)**

- Bộ câu hỏi chung duy nhất trong ngày cho toàn Server.
- Cung cấp lượng lớn Điểm Xếp Hạng và Tiền nếu hoàn thành xuất sắc.

---

## 🔴 PHASE 3: KẾT NỐI CỘNG ĐỒNG (3-6 tháng sau ra mắt)

*Mục tiêu: Xây dựng cộng đồng người chơi vững mạnh.*

**1. Phòng riêng & Đấu với bạn (Private Room)**

- Tạo phòng với mã Code gồm 4-6 ký tự.
- Gửi mã cho bạn bè để đấu trực tiếp mà không cần Matchmaking.

**2. Hệ thống Kết bạn & Thống kê cá nhân**

- Tính năng gửi lời mời kết bạn (Friend Request).
- Xem hồ sơ chi tiết của bạn bè (Tỷ lệ thắng, Chủ đề mạnh nhất, Tổng điểm Xếp hạng).

---

## 📋 CÁC BƯỚC THỰC HIỆN NGAY BÂY GIỜ (Cho Phase 1)

1. Bắt tay vào sửa đổi file Security Rules của Firebase.
2. Thêm `rankPoints` vào source code (C#) và Firebase Schema.
3. Chỉnh sửa logic tính toán điểm thưởng khi kết thúc trận (Áp dụng Level Multiplier).
4. Thiết kế giao diện Bảng xếp hạng.

