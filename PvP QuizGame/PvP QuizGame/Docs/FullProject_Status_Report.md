# PvP Quiz Game - Full Project Status Report
**Ngày lập báo cáo:** 28/04/2026
**Trạng thái dự án:** Phase 2 - GameState Synchronization Completed

---

## 1. Tổng quan Công nghệ (Tech Stack)
*   **Engine:** Unity 6 (6000.x)
*   **UI System:** UI Toolkit (thay thế hoàn toàn cho UGUI cũ).
*   **Backend:** Firebase Realtime Database & Firebase Authentication (Anonymous).
*   **Logic Model:** MVC (Model-View-Controller) tách biệt logic game và hiển thị.

---

## 2. Các Module Chức năng Đã Hoàn Thành

### 2.1. Hệ thống Logic Core (`Assets/Scripts/Core/`)
*   **GameController:** Singleton điều phối trạng thái trận đấu (Idle -> Countdown -> Playing -> GameOver).
*   **QuizManager:** Quản lý ngân hàng câu hỏi, hỗ trợ xáo trộn (Shuffle) theo Seed để đồng bộ PvP.
*   **ScoreManager:** Tính toán điểm số (Đúng +10đ), trao thưởng XP/Tiền sau trận đấu.
*   **TimerController:** Quản lý thời gian trận đấu, tích hợp thanh tiến trình (Progress Bar) trên UI.

### 2.2. Hệ thống Giao diện (`Assets/Scripts/UI/` & `Assets/UI/`)
*   **Cấu trúc:** Sử dụng UXML cho Layout và USS cho Styling (tập trung tại `GlobalStyles.uss`).
*   **Màn hình đã chuyển đổi:**
    *   `InitScene`: Màn hình khởi động và kiểm tra kết nối Firebase.
    *   `HomeScene`: Sảnh chờ, hiển thị thông tin người chơi.
    *   `GameplayScene`: HUD điểm số, nội dung câu hỏi và bảng kết quả (Result Popup).
*   **Tính năng đặc biệt:** Tự động ẩn/hiện nút đáp án dựa trên số lượng dữ liệu thực tế trong Database.

### 2.3. Hệ thống PvP & Đồng bộ (`Assets/Scripts/Network/`)
*   **FirebaseManager:** Quản lý kết nối, đăng nhập ẩn danh và là cầu nối trung gian với Database.
*   **FirebaseMatchProvider:** Đồng bộ hóa đáp án giữa 2 người chơi thời gian thực.
*   **Cơ chế đồng bộ:** 
    *   Sử dụng chung **Random Seed** để đảm bảo 2 bên thấy cùng bộ câu hỏi.
    *   Chỉ chuyển câu mới khi nhận được tín hiệu đáp án từ cả hai phía.

### 2.4. Chế độ Phát triển (Debug/Offline)
*   **Offline Mode:** Cho phép tắt kết nối Firebase để test nhanh trong Editor.
*   **MockOpponent:** Bot tự động "suy nghĩ" và đưa ra đáp án ngẫu nhiên để đấu tập.
*   **Toggles:** Có thể bật tắt dễ dàng trong Inspector của `FirebaseManager`.

---

## 3. Cấu trúc Thư mục Dự án (Project Structure)
```text
Assets/
├── Firebase/               # Firebase SDK
├── Plugins/Android/        # Cấu hình Native Android (EDM4U, Jetifier)
├── Scripts/
│   ├── Controller/         # Input và các bộ điều khiển logic
│   ├── Core/               # Các Manager chính của Game
│   ├── Data/               # ScriptableObjects (QuestionData, QuizDatabase)
│   ├── Network/            # Logic Firebase và Matchmaking
│   └── UI/                 # Logic điều khiển UI Toolkit (_UXML scripts)
├── UI/
│   ├── Layouts/            # Các tệp thiết kế (.uxml)
│   └── Styles/             # Các tệp định dạng (.uss)
└── google-services.json    # Tệp cấu hình Firebase
```

---

## 4. Kế hoạch Phát triển Tiếp theo (Next Steps)
1.  **Hệ thống Matchmaking Hoàn chỉnh:** Thay thế ID phòng giả lập bằng logic tìm phòng trống thực tế.
2.  **Leaderboard:** Xây dựng bảng xếp hạng toàn cầu sử dụng Firebase Database.
3.  **Localization:** Tích hợp đa ngôn ngữ cho câu hỏi.

---
**Người báo cáo:** Antigravity AI
