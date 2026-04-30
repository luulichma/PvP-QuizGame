# Báo cáo Triển khai Hệ thống Đa ngôn ngữ (Localization)
**Ngày lập:** 28/04/2026
**Dự án:** PvP Quiz Game

---

## 1. Tổng quan Hệ thống (Hybrid Localization)
Thay vì sử dụng Unity Localization Package phức tạp, dự án đã triển khai hệ thống **Custom Localization Manager**.
*   **Ưu tiên Online (Live Update):** Tự động kéo dữ liệu từ Google Sheet (CSV) khi có mạng. Phân tích Header cột để tìm đúng ngôn ngữ (Ví dụ: `vi` ➔ Cột `Vietnamese`).
*   **Dự phòng Offline:** Nạp dữ liệu từ file JSON nội bộ (`StreamingAssets/Localization/`) nếu không có mạng.
*   **Tương thích:** Hoạt động hoàn toàn trơn tru với UI Toolkit mà không cần thiết lập Editor phức tạp.

---

## 2. Bảng đối chiếu Key hiện tại (Đã khớp với UI)
Dưới đây là các giao diện đã được lập trình để lấy Text từ Google Sheet theo đúng Key bạn cung cấp:

### Màn hình Sảnh chính (Home Scene)
| Thành phần UI | Key trong Sheet | Ý nghĩa |
| :--- | :--- | :--- |
| Nút "Chơi ngay" | `btn_main_play` | Bắt đầu tìm trận đấu. |
| Nút "Cài đặt" | `btn_main_setting` | Mở Popup Cài đặt. |
| Nhãn trạng thái tìm trận | `pvp_lobby_searching` | Hiển thị "Đang tìm đối thủ...". |

### Màn hình Chơi game & Kết quả (Gameplay Scene)
| Thành phần UI | Key trong Sheet | Ý nghĩa |
| :--- | :--- | :--- |
| Tiêu đề Thắng | `ui_res_victory` | Báo hiệu người chơi thắng. |
| Tiêu đề Thua | `ui_res_defeat` | Báo hiệu người chơi thua. |
| Nút "Chơi lại" | `btn_res_rematch` | Đấu lại trận mới. |

---

## 3. Đề xuất các Key CẦN BỔ SUNG vào Google Sheet
Trong quá trình code, tôi phát hiện một số thành phần UI chưa có Key tương ứng trong bảng Google Sheet của bạn. Bạn hãy bổ sung các dòng sau vào Sheet nhé:

### 3.1. Dành cho Sảnh chính (Lobby/Menu)
*   **`btn_lobby_cancel`**: (Ví dụ: Hủy / Cancel) Dùng cho nút Hủy tìm trận.

### 3.2. Dành cho Gameplay
*   **`ui_game_me`**: (Ví dụ: TÔI / ME) Nhãn chỉ định điểm số của bản thân.
*   **`ui_game_opp`**: (Ví dụ: ĐỐI THỦ / OPP) Nhãn chỉ định điểm số của địch.
*   **`ui_game_draw`**: (Ví dụ: HÒA! / DRAW!) Tiêu đề khi 2 bên bằng điểm.
*   **`btn_res_home`**: (Ví dụ: VỀ SẢNH / HOME) Nút thoát ra sảnh ở màn hình kết quả.
*   **`ui_question_count`**: (Ví dụ: CÂU {0} / {1} | QUESTION {0} / {1}) Nhãn đếm câu hỏi.

### 3.3. Dành cho màn hình Loading (Init Scene)
*   **`ui_init_loading`**: (Ví dụ: Đang tải dữ liệu... / Loading Data...) Dùng cho màn hình Splash ban đầu.

---

## 4. Hướng dẫn nạp thêm UI mới
Khi bạn thiết kế thêm giao diện mới, chỉ cần dùng đoạn code sau trong Controller để áp dụng đa ngôn ngữ:
```csharp
myLabel.text = LocalizationManager.Instance.GetText("key_tu_google_sheet");
```
Hệ thống sẽ lo phần còn lại.
