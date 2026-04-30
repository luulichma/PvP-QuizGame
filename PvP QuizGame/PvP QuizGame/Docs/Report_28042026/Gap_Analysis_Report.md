# Báo cáo Phân tích Lỗ hổng (Gap Analysis) - PvP Quiz Game
**Ngày lập:** 28/04/2026
**Tình trạng:** Cần bổ sung các tính năng cốt lõi để hoàn thiện MVP.

---

## 1. Hệ thống Giao diện & Trải nghiệm (UI/UX)

### 1.1. Hệ thống Đa ngôn ngữ (Localization)
*   **Trạng thái:** Đã làm.
*   **Vấn đề:** Toàn bộ text trong UXML (Labels, Buttons) đang viết cứng. Chưa có hệ thống chuyển đổi ngôn ngữ.
*   **Yêu cầu:** 
    *   Tích hợp Unity Localization Package hoặc hệ thống Dictionary custom.
    *   Tách biệt dữ liệu ngôn ngữ ra file JSON/CSV.

### 1.2. Lỗi Nút Settings & Popup
*   **Trạng thái:** 🔴 Lỗi logic.
*   **Vấn đề:** Nút Settings trong `MainMenuUIController_UXML` chỉ có Debug Log, chưa nạp `SettingsPopup.uxml`.
*   **Yêu cầu:** Hiện thực logic nạp và hiển thị Popup sử dụng UI Toolkit `TemplateContainer`.

---

## 2. Hệ thống Dữ liệu (Data Management)

### 2.1. Bộ nạp Dữ liệu Từ xa (Google Sheet)
*   **Trạng thái:** 🟡 Sơ khai (Placeholder).
*   **Vấn đề:** Link CSV đang trống. Logic parse CSV hiện tại dễ lỗi nếu nội dung câu hỏi chứa dấu phẩy. Dữ liệu chưa được lưu cache xuống ổ cứng.
*   **Yêu cầu:** 
    *   Cấu hình link Google Sheet thực tế.
    *   Cải thiện Regex để parse CSV an toàn hơn.
    *   Thêm cơ chế lưu cache Local (PersistentDataPath) để chơi Offline.

## 3. Hệ thống PvP & Online (Networking)

### 3.1. Hệ thống Xác thực (Authentication)
*   **Trạng thái:** 🔴 Thiếu.
*   **Vấn đề:** Game hiện tại chưa có màn hình Login. Người chơi chưa có lựa chọn Đăng nhập/Đăng ký hoặc chơi dưới tư cách khách (Guest).
*   **Yêu cầu:** 
    *   Xây dựng màn hình lựa chọn: Đăng nhập (Email/Pass), Đăng ký và Guest Mode.
    *   **Guest Mode:** Lưu dữ liệu ID duy nhất xuống máy (Local) và cho phép đồng bộ lên Account sau này.
    *   Tích hợp Firebase Authentication để quản lý tài khoản người dùng thực.

### 3.2. Hệ thống Ghép trận (Matchmaking)
*   **Trạng thái:** 🟡 Giả lập (Fake).
*   **Vấn đề:** Sử dụng ID phòng cố định và thời gian chờ giả (Fake Timer). Chưa có sự tương tác giữa 2 Client thật để tạo phòng.
*   **Yêu cầu:** Hiện thực logic Queue/Room sử dụng Firebase Realtime Database Transactions.

### 3.2. Lưu trữ Đám mây (Cloud Save)
*   **Trạng thái:** 🔴 Thiếu.
*   **Vấn đề:** Player Profile (Level, Money) mới chỉ lưu local qua `PlayerPrefs`.
*   **Yêu cầu:** Đồng bộ hóa dữ liệu người chơi lên Firebase sau mỗi trận đấu.

---

## 4. Các mục đã loại bỏ (Excluded)
*   **Bảng xếp hạng (Leaderboard):** Không thực hiện theo yêu cầu của người dùng để tập trung vào GameState.

---
**Người báo cáo:** Antigravity AI
