# MÔ TẢ SEQUENCE DIAGRAM — MỨC PHÂN TÍCH (PvP Quiz Game)

> Tài liệu mô tả **16 Sequence Diagram** ở mức phân tích nghiệp vụ.  
> Tập trung vào **luồng tương tác** giữa các đối tượng phân tích và logic nghiệp vụ.  
> Không đề cập chi tiết triển khai kỹ thuật (tên class, phương thức, sự kiện cụ thể).

---

## Quy ước tham gia

| Đối tượng | Loại | Vai trò |
|---|---|---|
| **Người chơi** | Actor | Người dùng thực tế đang thao tác |
| **Đối thủ** | Actor | Người chơi thứ hai trong trận PvP |
| **Màn hình Khởi động** | Boundary | Giao diện đăng nhập / đăng ký / chơi khách |
| **Màn hình Trang chủ** | Boundary | Giao diện sảnh chờ và điều hướng |
| **Màn hình Trận đấu** | Boundary | Giao diện trong trận đấu |
| **Hệ thống** | Control | Xử lý luồng nghiệp vụ chính |
| **Dữ liệu Người chơi** | Entity | Hồ sơ, điểm số, cấp độ, tiền |
| **Kho Câu hỏi** | Entity | Ngân hàng câu hỏi và đáp án |
| **Dịch vụ Đám mây** | External | Hệ thống xác thực và lưu trữ dữ liệu online |

---

## SD-01: Đăng ký tài khoản (UC01)

**Các đối tượng tham gia:** Người chơi · Màn hình Khởi động · Hệ thống · Dịch vụ Đám mây · Dữ liệu Người chơi

**Mô tả luồng chính:**

Người chơi mở ứng dụng và chọn chức năng "Đăng ký". Màn hình Khởi động hiển thị form yêu cầu nhập Tên hiển thị, Email và Mật khẩu. Người chơi điền đầy đủ thông tin và nhấn xác nhận. Hệ thống kiểm tra tính hợp lệ của dữ liệu ngay trên thiết bị (định dạng email đúng, mật khẩu đủ dài, tên không để trống). Sau khi dữ liệu hợp lệ, Hệ thống gửi yêu cầu tạo tài khoản lên Dịch vụ Đám mây. Dịch vụ Đám mây tạo tài khoản mới và phản hồi thành công. Hệ thống lưu hồ sơ mặc định của người chơi lên Đám mây (cấp độ 1, 0 điểm kinh nghiệm, 0 tiền ảo). Màn hình Khởi động tự động chuyển sang Màn hình Trang chủ.

**Luồng thay thế:**
- *[Dữ liệu nhập không hợp lệ]* Hệ thống hiển thị thông báo lỗi tương ứng ngay trên form; không gửi yêu cầu lên đám mây; người chơi chỉnh sửa và thử lại.
- *[Email đã được đăng ký]* Dịch vụ Đám mây từ chối; Màn hình thông báo "Email này đã tồn tại".
- *[Không có kết nối mạng]* Hệ thống không liên lạc được với Dịch vụ Đám mây; Màn hình thông báo lỗi kết nối và gợi ý chuyển sang chế độ Chơi khách.

---

## SD-02: Đăng nhập (UC02)

**Các đối tượng tham gia:** Người chơi · Màn hình Khởi động · Hệ thống · Dịch vụ Đám mây · Dữ liệu Người chơi

**Mô tả luồng chính:**

Người chơi chọn chức năng "Đăng nhập" trên Màn hình Khởi động. Màn hình hiển thị form nhập Email và Mật khẩu. Người chơi điền thông tin và nhấn xác nhận. Hệ thống gửi yêu cầu xác thực đến Dịch vụ Đám mây. Dịch vụ Đám mây kiểm tra thông tin, xác nhận danh tính thành công và trả về phiên đăng nhập. Hệ thống truy xuất hồ sơ của người chơi từ kho lưu trữ trên đám mây (tên, cấp độ, điểm kinh nghiệm, số tiền, avatar). Dữ liệu Người chơi được tải vào bộ nhớ thiết bị. Màn hình chuyển sang Trang chủ với thông tin hồ sơ đã được hiển thị đầy đủ.

**Luồng thay thế:**
- *[Sai mật khẩu hoặc email không tồn tại]* Dịch vụ Đám mây từ chối; Màn hình thông báo "Thông tin đăng nhập không đúng".
- *[Không có kết nối mạng]* Hệ thống không liên lạc được với đám mây; Màn hình thông báo lỗi và gợi ý Chơi khách.

---

## SD-03: Đăng xuất (UC03)

**Các đối tượng tham gia:** Người chơi · Màn hình Trang chủ · Hệ thống · Dữ liệu Người chơi

**Mô tả luồng chính (Tài khoản đã đăng nhập):**

Người chơi mở cửa sổ Cài đặt từ Màn hình Trang chủ và nhấn "Đăng xuất". Hệ thống xóa phiên đăng nhập đang lưu trên thiết bị và xóa toàn bộ dữ liệu người chơi đang tải trong bộ nhớ. Màn hình chuyển về Màn hình Khởi động.

**Luồng thay thế — Tài khoản Khách:**
- Khi người chơi đang dùng chế độ Khách và nhấn Đăng xuất, Hệ thống hiển thị cảnh báo: "Dữ liệu chơi của bạn sẽ bị xóa vĩnh viễn nếu đăng xuất". Nếu người chơi xác nhận, toàn bộ dữ liệu cục bộ bị xóa và chuyển về Màn hình Khởi động. Nếu người chơi hủy, quay lại Trang chủ bình thường.

---

## SD-04: Chơi khách (UC04)

**Các đối tượng tham gia:** Người chơi · Màn hình Khởi động · Hệ thống · Dữ liệu Người chơi

**Mô tả luồng chính:**

Người chơi chọn "Chơi khách" trên Màn hình Khởi động. Màn hình yêu cầu nhập Tên hiển thị. Người chơi nhập tên và xác nhận. Hệ thống tạo hồ sơ tạm thời ngay trên thiết bị (không tạo tài khoản trên đám mây). Dữ liệu Người chơi được khởi tạo với giá trị mặc định và lưu chỉ trong bộ nhớ cục bộ. Hệ thống đánh dấu trạng thái là "chế độ offline". Màn hình chuyển sang Trang chủ với đầy đủ chức năng ngoại trừ tính năng online.

**Điểm đặc biệt:** Toàn bộ luồng không có tương tác với Dịch vụ Đám mây. Dữ liệu chỉ tồn tại trong phiên hiện tại và sẽ mất khi đăng xuất.

---

## SD-05: Chỉnh sửa hồ sơ (UC05)

**Các đối tượng tham gia:** Người chơi · Màn hình Trang chủ · Hệ thống · Dữ liệu Người chơi · Dịch vụ Đám mây

**Mô tả luồng chính:**

Người chơi nhấn vào khu vực hiển thị hồ sơ (avatar + tên) trên Màn hình Trang chủ. Cửa sổ Chỉnh sửa hồ sơ xuất hiện, hiển thị tên hiện tại và lưới 8 avatar để lựa chọn. Người chơi thay đổi tên hiển thị và/hoặc chọn avatar mới, sau đó nhấn "Lưu thay đổi". Hệ thống cập nhật Dữ liệu Người chơi trong bộ nhớ và lưu vào bộ nhớ thiết bị. Màn hình Trang chủ tức thì làm mới khu vực hồ sơ để phản ánh thay đổi. Nếu người chơi đang đăng nhập online, Hệ thống đồng thời ghi thay đổi lên Dịch vụ Đám mây để đồng bộ.

**Luồng thay thế:**
- *[Tên quá ngắn]* Hệ thống hiển thị thông báo yêu cầu nhập lại, không lưu thay đổi.
- *[Tài khoản Khách]* Không có bước đồng bộ lên đám mây; thay đổi chỉ lưu trên thiết bị.

---

## SD-06: Tìm trận đấu (UC06)

**Các đối tượng tham gia:** Người chơi · Đối thủ · Màn hình Trang chủ · Hệ thống · Dịch vụ Đám mây

**Mô tả luồng chính:**

Người chơi nhấn "TÌM TRẬN" trên Màn hình Trang chủ. Màn hình chuyển sang trạng thái "Đang tìm đối thủ...". Hệ thống đăng ký thông tin người chơi vào hàng đợi ghép trận trên Dịch vụ Đám mây. Song song đó, một Đối thủ khác cũng đang tìm trận và được thêm vào hàng đợi. Khi Dịch vụ Đám mây phát hiện đủ hai người trong hàng đợi, hệ thống tự động tạo một Phòng đấu chung và thông báo cho cả hai người chơi. Cả hai nhận được thông báo "Tìm được đối thủ!" và Màn hình tự động chuyển sang cảnh Trận đấu.

**Điểm đặc biệt:** Cần thể hiện hai actor (Người chơi và Đối thủ) hoạt động song song và đồng thời nhận được thông báo từ Dịch vụ Đám mây.

---

## SD-07: Hủy tìm trận (UC07)

**Các đối tượng tham gia:** Người chơi · Màn hình Trang chủ · Hệ thống · Dịch vụ Đám mây

**Mô tả luồng chính:**

Trong khi đang ở màn hình chờ "Đang tìm đối thủ...", người chơi nhấn nút "Hủy". Hệ thống gửi yêu cầu rút tên người chơi khỏi hàng đợi ghép trận trên Dịch vụ Đám mây. Dịch vụ Đám mây xác nhận đã xóa. Màn hình ẩn trạng thái chờ và quay lại hiển thị giao diện Trang chủ bình thường. Trận đấu không được tạo.

---

## SD-08: Bắt đầu trận đấu (UC08)

**Các đối tượng tham gia:** Người chơi · Đối thủ · Màn hình Trận đấu · Hệ thống · Kho Câu hỏi · Dịch vụ Đám mây *(chỉ Online)*

**Mô tả luồng chính:**

Sau khi hai người chơi được dẫn vào cảnh Trận đấu, Màn hình hiển thị thông tin của cả hai người (avatar, tên, điểm ban đầu = 0). Hệ thống bắt đầu đếm ngược 3-2-1 để cả hai người chuẩn bị. Hết đếm ngược, Hệ thống xác định danh sách câu hỏi sẽ dùng trong trận (Online: lấy thông tin từ Phòng đấu trên đám mây để đảm bảo hai người cùng câu hỏi; Offline: tự tính toán cục bộ). Hệ thống tải câu hỏi từ Kho Câu hỏi, xáo trộn theo thứ tự đã được thống nhất, và khởi động đồng hồ đếm ngược. Câu hỏi đầu tiên xuất hiện trên Màn hình của cả hai người chơi đồng thời.

**Điểm đặc biệt:** Luồng `alt` giữa chế độ Online (đồng bộ thứ tự câu hỏi qua đám mây) và Offline (tính cục bộ, đối thủ là máy).

---

## SD-09: Trả lời câu hỏi (UC09)

**Các đối tượng tham gia:** Người chơi · Màn hình Trận đấu · Hệ thống · Kho Câu hỏi · Dịch vụ Đám mây *(chỉ Online)*

**Mô tả luồng chính:**

Câu hỏi đang được hiển thị, đồng hồ đếm ngược đang chạy. Người chơi nhấn chọn một trong bốn đáp án. Màn hình lập tức đổi màu nút vừa chọn (màu vàng — đang chờ kết quả). Hệ thống ghi nhận lựa chọn và gửi thông tin đáp án lên Dịch vụ Đám mây *(chỉ Online)*. Khi đồng hồ hết giờ, Hệ thống đối chiếu đáp án người chơi với đáp án đúng trong Kho Câu hỏi. Màn hình hiển thị phản hồi: nút đúng đổi xanh lá, nút sai đổi đỏ. Nếu trả lời đúng, Hệ thống tính điểm thưởng dựa trên tốc độ trả lời và cập nhật điểm lên Màn hình. Sau khoảng thời gian hiển thị kết quả ngắn, Hệ thống tải câu hỏi tiếp theo.

**Luồng thay thế:**
- *[Hết giờ mà chưa chọn]* Hệ thống coi như trả lời sai; chuyển sang câu tiếp theo, không cộng điểm.

---

## SD-10: Xem điểm đối thủ real-time (UC10)

**Các đối tượng tham gia:** Đối thủ · Dịch vụ Đám mây · Màn hình Trận đấu · Người chơi

**Mô tả luồng chính:**

Đây là luồng hoàn toàn tự động, xảy ra trong nền khi Đối thủ trả lời câu hỏi. Khi Đối thủ hoàn thành một câu trả lời, điểm số của Đối thủ được ghi lên Dịch vụ Đám mây trong Phòng đấu. Dịch vụ Đám mây tự động thông báo thay đổi cho thiết bị của Người chơi hiện tại. Hệ thống nhận thông báo và cập nhật điểm số của Đối thủ lên phần HUD bên phải của Màn hình Trận đấu. Người chơi thấy điểm đối thủ thay đổi ngay lập tức mà không cần làm gì.

**Điểm đặc biệt:** Luồng này chạy song song với luồng chính (SD-09) và hoàn toàn do Dịch vụ Đám mây chủ động kích hoạt (push-based). Chỉ có trong chế độ Online.

---

## SD-11: Đầu hàng / Thoát trận (UC11)

**Các đối tượng tham gia:** Người chơi · Màn hình Trận đấu · Hệ thống · Dịch vụ Đám mây *(chỉ Online)*

**Mô tả luồng chính:**

Trong khi trận đấu đang diễn ra, Người chơi nhấn nút thoát (✖). Màn hình tạm dừng đồng hồ đếm ngược và hiển thị hộp thoại xác nhận: "Bạn có chắc muốn rời đi? Bạn sẽ bị xử thua". Người chơi nhấn "Xác nhận". Hệ thống kết thúc trận đấu ngay lập tức, xác định Người chơi là bên thua. Nếu đang Online, Hệ thống thông báo lên Dịch vụ Đám mây rằng Đối thủ đã thắng do bỏ cuộc. Màn hình hiển thị kết quả trận đấu (thua) với phần thưởng thấp nhất.

**Luồng thay thế:**
- *[Người chơi nhấn Hủy trong hộp thoại]* Đồng hồ tiếp tục chạy, trận đấu tiếp diễn bình thường.

---

## SD-12: Xem kết quả trận đấu (UC12)

**Các đối tượng tham gia:** Hệ thống · Màn hình Trận đấu · Người chơi · Dữ liệu Người chơi

**Mô tả luồng chính:**

Khi câu hỏi cuối cùng trong trận được trả lời xong, Hệ thống so sánh điểm của hai người chơi và xác định kết quả (Thắng / Thua / Hòa). Màn hình hiển thị bảng kết quả với tiêu đề lớn ở giữa (THẮNG! / THUA! / HÒA!), điểm so sánh của hai người, và số tiền ảo được thưởng. Hệ thống tự động cộng phần thưởng vào Dữ liệu Người chơi (tiền ảo, điểm kinh nghiệm, nâng cấp độ nếu đủ điều kiện) và lưu lại. Người chơi có hai lựa chọn: nhấn "Chơi lại" để quay về Trang chủ và tìm trận mới, hoặc nhấn "Về sảnh" để về Trang chủ nghỉ ngơi.

**Điểm đặc biệt:** Việc lưu phần thưởng xảy ra tự động trước khi người chơi nhìn thấy bảng kết quả.

---

## SD-13: Chơi với máy / Luyện tập (UC13)

**Các đối tượng tham gia:** Người chơi · Màn hình Trang chủ · Màn hình Trận đấu · Hệ thống · Kho Câu hỏi · Đối thủ Ảo

**Mô tả luồng chính:**

Người chơi nhấn "ĐẤU VỚI MÁY" trên Màn hình Trang chủ. Hệ thống chuyển sang chế độ offline (không cần kết nối mạng) và lập tức bắt đầu chuẩn bị trận đấu mà không cần chờ ghép trận. Màn hình hiển thị thông báo "Đang chuẩn bị..." trong vài giây. Hệ thống tự động tạo một Đối thủ Ảo (máy). Trận đấu bắt đầu theo đúng luồng SD-08. Trong suốt trận đấu, mỗi khi có câu hỏi mới, Đối thủ Ảo tự động suy nghĩ trong một khoảng thời gian ngẫu nhiên rồi gửi câu trả lời — tạo cảm giác như đang chơi với người thật. Toàn bộ trận đấu diễn ra theo luồng giống Online nhưng hoàn toàn không có tương tác với Dịch vụ Đám mây.

---

## SD-14: Đổi ngôn ngữ (UC14)

**Các đối tượng tham gia:** Người chơi · Màn hình Trang chủ · Hệ thống · Kho Nội dung Ngôn ngữ

**Mô tả luồng chính:**

Người chơi mở cửa sổ Cài đặt và chọn ngôn ngữ mới trong danh sách (Tiếng Việt / English). Hệ thống lưu lựa chọn ngôn ngữ vào bộ nhớ thiết bị (để ghi nhớ cho lần sau). Hệ thống tải bộ nội dung tương ứng từ Kho Nội dung Ngôn ngữ. Sau khi tải xong, Hệ thống thông báo cho tất cả các thành phần giao diện đang hiển thị. Toàn bộ văn bản trên màn hình (nút bấm, nhãn, tiêu đề) đồng loạt cập nhật sang ngôn ngữ mới ngay lập tức mà không cần khởi động lại ứng dụng.

**Điểm đặc biệt:** Chỉ các ngôn ngữ có bộ nội dung đầy đủ mới xuất hiện trong danh sách chọn. Nếu bộ nội dung tải thất bại, Hệ thống tự động giữ nguyên ngôn ngữ cũ.

---

## SD-15: Cài đặt âm thanh (UC15)

**Các đối tượng tham gia:** Người chơi · Màn hình Trang chủ · Hệ thống

**Mô tả luồng chính:**

Người chơi mở cửa sổ Cài đặt. Cửa sổ hiển thị hai công tắc bật/tắt: "Nhạc nền" và "Âm thanh hiệu ứng", phản ánh trạng thái hiện tại. Người chơi bật hoặc tắt một trong hai công tắc. Hệ thống lập tức áp dụng thay đổi — nhạc nền dừng hoặc phát lại ngay, âm thanh hiệu ứng bật hoặc tắt. Hệ thống lưu trạng thái lựa chọn vào bộ nhớ thiết bị để giữ nguyên cho các lần mở ứng dụng sau.

**Điểm đặc biệt:** Thay đổi có hiệu lực ngay lập tức, không cần xác nhận thêm. Luồng giống nhau cho cả hai loại âm thanh (nhạc nền và hiệu ứng).

---

## SD-16: Tính điểm & Phần thưởng (UC16)

**Các đối tượng tham gia:** Hệ thống · Dữ liệu Người chơi · Dịch vụ Đám mây *(chỉ Online)* · Màn hình Trận đấu

**Mô tả luồng chính:**

Ngay khi trận đấu kết thúc (hết câu hỏi hoặc một bên đầu hàng), Hệ thống tính toán kết quả cuối cùng và xác định Thắng / Thua / Hòa. Dựa trên kết quả, Hệ thống tính phần thưởng theo bảng quy định: Thắng nhận nhiều nhất, Hòa nhận vừa, Thua nhận ít nhất. Hệ thống cộng điểm kinh nghiệm vào Dữ liệu Người chơi; nếu điểm kinh nghiệm đạt ngưỡng, người chơi lên cấp. Tiền ảo thưởng được cộng vào số dư. Toàn bộ thay đổi được lưu trên thiết bị. Nếu đang Online, Hệ thống đồng bộ dữ liệu mới nhất (cấp độ, tiền, điểm kinh nghiệm) lên Dịch vụ Đám mây. Cuối cùng Màn hình hiển thị bảng kết quả với đầy đủ thông tin phần thưởng cho người chơi xem.

**Điểm đặc biệt:** Luồng `opt` cho bước đồng bộ đám mây — chỉ thực hiện khi người chơi đang online. Toàn bộ tính toán và lưu trữ cục bộ xảy ra trước khi Màn hình hiển thị kết quả.
