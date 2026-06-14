# HƯỚNG PHÁT TRIỂN TƯƠNG LAI — PvP Quiz Game

> Tài liệu tổng hợp các tính năng có thể mở rộng trong các giai đoạn phát triển tiếp theo,
> nhằm nâng cao trải nghiệm người dùng và tăng giá trị lâu dài của sản phẩm.

---

## 1. Bảng xếp hạng (Leaderboard)

**Mô tả:** Xây dựng bảng xếp hạng toàn cầu và theo khu vực, hiển thị top người chơi dựa trên tổng điểm tích lũy, tỷ lệ thắng, hoặc số trận đã chơi. Người chơi có thể xem thứ hạng hiện tại của bản thân và so sánh với những người chơi khác.

**Giá trị mang lại:** Tạo động lực cạnh tranh, khuyến khích người chơi quay lại thường xuyên để giữ vững hoặc cải thiện thứ hạng. Đây là yếu tố cốt lõi tạo nên tính gắn kết lâu dài (long-term retention) trong các game có yếu tố xã hội.

**Hướng triển khai:**
- Sử dụng Firebase Realtime Database hoặc Firestore để lưu điểm số tổng hợp theo thời gian thực.
- Tạo node `leaderboard/global` và `leaderboard/weekly` được cập nhật sau mỗi trận.
- Phân loại bảng xếp hạng theo chu kỳ: Tuần / Tháng / Mọi thời đại — reset định kỳ để tạo cơ hội cho người chơi mới.
- Giao diện hiển thị top 100, có nổi bật vị trí của người chơi hiện tại dù ở bất kỳ hạng nào.

---

## 2. Hệ thống thành tựu (Achievement System)

**Mô tả:** Xây dựng bộ huy hiệu / danh hiệu (badge) mà người chơi có thể mở khóa dựa trên các cột mốc hoạt động: số trận thắng, số câu đúng liên tiếp, số giờ chơi, v.v. Thành tựu được lưu vĩnh viễn vào hồ sơ và hiển thị trên trang cá nhân.

**Giá trị mang lại:** Tạo ra các mục tiêu ngắn và dài hạn song song với trận đấu chính, giúp người chơi luôn có cảm giác tiến bộ ngay cả khi thua trận. Thành tựu còn là yếu tố thể hiện cá tính và danh tiếng trong cộng đồng.

**Hướng triển khai:**
- Thiết kế danh sách thành tựu theo nhóm: **Chiến đấu** (thắng 10/50/100 trận), **Tri thức** (trả lời đúng 200 câu), **Kiên trì** (đăng nhập 7 ngày liên tiếp), **Tốc độ** (trả lời đúng dưới 3 giây), **Chuyên gia chủ đề** (trả lời đúng 20 câu Lịch sử)...
- Tạo bảng `achievements/{uid}` trên Firebase lưu trạng thái từng thành tựu.
- Sau mỗi trận, hệ thống chạy bộ kiểm tra thành tựu và hiển thị animation thông báo mở khóa.
- Một số thành tựu có thể trao phần thưởng thực tế (tiền ảo, avatar độc quyền).

---

## 3. Phòng riêng & Mời bạn bè (Private Room)

**Mô tả:** Cho phép người chơi tạo một phòng đấu riêng với mã phòng (room code), sau đó chia sẻ mã cho bạn bè để đấu trực tiếp mà không qua hệ thống ghép cặp tự động. Có thể tùy chỉnh số lượng câu hỏi, thời gian mỗi câu và chủ đề.

**Giá trị mang lại:** Mở rộng tính xã hội của trò chơi, cho phép sử dụng trong các bối cảnh cụ thể như lớp học, nhóm bạn, hoặc sự kiện nội bộ. Đây là tính năng khác biệt quan trọng so với chế độ matchmaking ngẫu nhiên hiện có.

**Hướng triển khai:**
- Thêm nút "Tạo phòng riêng" và "Nhập mã phòng" trên Màn hình Trang chủ.
- Tạo node `privateRooms/{roomCode}` trên Firebase với trạng thái chờ người tham gia.
- Host phòng có quyền cấu hình trận đấu (số câu, thời gian, chủ đề) trước khi bắt đầu.

---

## 4. Chọn chủ đề câu hỏi (Topic Selection)

**Mô tả:** Trước khi vào trận, người chơi (hoặc cả hai bên đồng thuận) chọn một hoặc nhiều chủ đề câu hỏi từ danh sách có sẵn: Công nghệ, Địa lý, Toán học, Lịch sử, Khoa học, Văn hóa đại chúng... Trận đấu sẽ chỉ lấy câu hỏi trong các chủ đề đã chọn.

**Giá trị mang lại:** Cho phép người chơi thi đấu trên lĩnh vực thế mạnh hoặc luyện tập chủ đề yếu. Tăng tính đa dạng và chiều sâu chiến thuật cho trò chơi.

**Hướng triển khai:**
- Mở rộng cấu trúc câu hỏi trong `LocalizationManager` để hỗ trợ filter theo category tag.
- Giao diện chọn chủ đề xuất hiện sau khi ghép trận thành công, trước khi đếm ngược.
- Trong chế độ Matchmaking, hỗ trợ tìm trận theo chủ đề phổ biến nhất hoặc chủ đề ngẫu nhiên.

---

## 5. Cửa hàng vật phẩm (In-Game Shop)

**Mô tả:** Xây dựng cửa hàng cho phép người chơi dùng tiền ảo (kiếm được từ các trận đấu) để mua các vật phẩm trang trí: avatar mới, khung avatar, hiệu ứng chiến thắng đặc biệt, hoặc biểu tượng cảm xúc phản ứng (emoji reactions) dùng trong trận.

**Giá trị mang lại:** Tạo vòng lặp kinh tế nội tại (in-game economy loop) — người chơi có lý do để tiếp tục chơi nhằm kiếm tiền ảo, và tiền ảo có mục đích sử dụng rõ ràng. Cá nhân hóa hình ảnh cũng giúp người chơi gắn bó hơn với nhân vật của mình.

**Hướng triển khai:**
- Thiết kế bảng giá cho từng vật phẩm; lưu danh sách vật phẩm đã mua trong `users/{uid}/inventory`.
- Tích hợp kiểm tra số dư tiền ảo trước khi xác nhận mua.
- Cho phép preview vật phẩm trước khi mua.

---

## 6. Thử thách hàng ngày (Daily Challenge)

**Mô tả:** Mỗi ngày hệ thống tự động tạo ra một bộ câu hỏi đặc biệt (Daily Quiz) gồm 5–10 câu với chủ đề thay đổi theo ngày. Người chơi chỉ được thực hiện thử thách một lần mỗi ngày; kết quả được so sánh với tất cả người chơi khác trên toàn cầu trong cùng ngày đó.

**Giá trị mang lại:** Tạo lý do để người chơi mở ứng dụng mỗi ngày (daily engagement), bổ sung trải nghiệm ngoài trận PvP trực tuyến. Đặc biệt hữu ích cho người chơi không muốn chờ ghép trận nhưng vẫn muốn rèn luyện.

**Hướng triển khai:**
- Sử dụng Firebase Remote Config hoặc Cloud Functions để tạo và phân phối bộ câu hỏi ngày mới mỗi 24 giờ.
- Lưu trạng thái "đã hoàn thành hôm nay" theo ngày trong `PlayerPrefs` hoặc Firebase.
- Hiển thị bảng xếp hạng riêng cho thử thách ngày, tự động reset lúc 0h.

---

## 7. Hệ thống bạn bè (Social / Friend List)

**Mô tả:** Người chơi có thể tìm kiếm và kết bạn với người dùng khác thông qua tên hiển thị hoặc mã ID. Danh sách bạn bè hiển thị trạng thái online/offline, số trận thắng, và cho phép thách đấu trực tiếp (thay thế hoặc bổ sung tính năng Phòng riêng).

**Giá trị mang lại:** Nâng cao yếu tố cộng đồng và xã hội, tạo thêm lý do để người dùng quay lại — đấu với bạn bè có sức hút cạnh tranh cao hơn so với đối thủ ngẫu nhiên.

**Hướng triển khai:**
- Tạo node `friends/{uid}/list` trên Firebase lưu danh sách UID bạn bè.
- Xây dựng chức năng gửi và chấp nhận lời mời kết bạn (friend request).
- Hiển thị trạng thái online thông qua Firebase Presence (`.info/connected`).

---

## 8. Thống kê cá nhân & Phân tích điểm yếu (Personal Analytics)

**Mô tả:** Sau một số lượng nhất định các trận đấu, hệ thống phân tích lịch sử trả lời của người chơi và hiển thị biểu đồ thống kê: tỷ lệ đúng theo từng chủ đề, tốc độ trả lời trung bình, số trận thắng/thua theo thời gian. Từ đó đề xuất chủ đề người chơi nên luyện tập thêm.

**Giá trị mang lại:** Biến trò chơi từ giải trí đơn thuần thành công cụ học tập có định hướng. Người chơi nhận được giá trị thực từ việc chơi và có mục tiêu cụ thể để cải thiện.

**Hướng triển khai:**
- Lưu lịch sử trả lời theo chủ đề vào `users/{uid}/stats` sau mỗi trận.
- Tính toán tỷ lệ đúng theo category và hiển thị dưới dạng biểu đồ tròn hoặc radar chart.
- Gợi ý "Chủ đề cần cải thiện" dựa trên tỷ lệ đúng thấp nhất.

---

## 9. Chế độ giải đấu (Tournament Mode)

**Mô tả:** Tổ chức các giải đấu theo thể thức loại trực tiếp (single elimination) hoặc vòng tròn tính điểm (round-robin) với số lượng người tham gia cố định (8 hoặc 16 người). Người chiến thắng sau mỗi vòng tiến lên vòng tiếp theo, người thua bị loại. Người vô địch nhận phần thưởng đặc biệt.

**Giá trị mang lại:** Cung cấp trải nghiệm cạnh tranh cao hơn so với matchmaking thông thường, tạo ra các sự kiện đặc biệt thu hút cộng đồng người chơi.

**Hướng triển khai:**
- Xây dựng hệ thống bracket tự động ghép cặp và quản lý kết quả từng vòng.
- Sử dụng Firebase Cloud Functions để điều phối lịch trình giải đấu tự động.
- Có thể chạy giải đấu theo định kỳ (hàng tuần/hàng tháng) hoặc theo sự kiện.

---

## 10. Mở rộng ngân hàng câu hỏi & Hỗ trợ đa ngôn ngữ

**Mô tả:** Tăng đáng kể số lượng câu hỏi trong từng chủ đề (từ 2–3 câu hiện tại lên 50–100+ câu mỗi chủ đề) và bổ sung các chủ đề mới (Thể thao, Nghệ thuật, Điện ảnh, Ẩm thực...). Song song, hoàn thiện bộ nội dung dịch thuật cho tất cả 8 ngôn ngữ đã thiết kế (Français, Italiano, Deutsch, Español, 日本語, 한국어).

**Giá trị mang lại:** Giảm đáng kể tình trạng người chơi gặp lại câu hỏi đã biết, tăng tính tươi mới và độ khó có thể điều chỉnh. Hỗ trợ đa ngôn ngữ đầy đủ mở rộng thị trường mục tiêu ra quốc tế.

**Hướng triển khai:**
- Tận dụng hệ thống Google Sheet CSV đã có sẵn: bổ sung câu hỏi vào Sheet mà không cần build lại ứng dụng.
- Tạo đủ file JSON local (`fr.json`, `de.json`, `ja.json`...) cho tất cả 8 ngôn ngữ.
- Phân loại câu hỏi theo độ khó (Dễ / Trung bình / Khó) để phục vụ tính năng chọn chủ đề và matchmaking theo trình độ.

---

## Tổng hợp độ ưu tiên

| # | Tính năng | Độ ưu tiên | Lý do |
|:---:|---|:---:|---|
| 1 | Bảng xếp hạng | ★★★★★ | Tăng retention mạnh nhất, kỹ thuật đơn giản |
| 2 | Hệ thống thành tựu | ★★★★★ | Bổ sung vòng lặp tiến trình song song, quan trọng cho engagement |
| 3 | Mở rộng ngân hàng câu hỏi | ★★★★☆ | Nền tảng cần thiết trước khi thêm tính năng khác |
| 4 | Chọn chủ đề câu hỏi | ★★★★☆ | Tăng chiều sâu chiến thuật, kỹ thuật không phức tạp |
| 5 | Thử thách hàng ngày | ★★★★☆ | Tạo daily active user, kỹ thuật vừa phải |
| 6 | Phòng riêng & Mời bạn | ★★★☆☆ | Tăng tính xã hội, cần thiết cho bối cảnh học đường |
| 7 | Cửa hàng vật phẩm | ★★★☆☆ | Khai thác vòng kinh tế tiền ảo đã có sẵn |
| 8 | Hệ thống bạn bè | ★★★☆☆ | Nền tảng cho nhiều tính năng xã hội khác |
| 9 | Thống kê cá nhân | ★★☆☆☆ | Giá trị học tập cao nhưng cần nhiều dữ liệu tích lũy |
| 10 | Chế độ giải đấu | ★★☆☆☆ | Trải nghiệm cao cấp nhưng phức tạp về kỹ thuật và vận hành |
