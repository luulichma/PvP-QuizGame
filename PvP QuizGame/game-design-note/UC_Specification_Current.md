# Đặc tả Use Case chi tiết (Hệ thống mới)

Tài liệu này bao gồm danh sách đầy đủ các Use Case của dự án PvP QuizGame, đã được cập nhật và bổ sung các tính năng mới so với phiên bản cũ (bao gồm Cửa hàng, Vật phẩm, Bảng xếp hạng, Quên mật khẩu, v.v.).

---

## MỤC LỤC
**Nhóm 1: Xác thực & Hồ sơ**
- UC01: Đăng ký tài khoản
- UC02: Đăng nhập
- UC03: Chơi khách (Guest)
- UC04: Đăng xuất
- UC05: Chỉnh sửa hồ sơ
- UC06: Quên mật khẩu

**Nhóm 2: Sảnh chính & Tính năng (Home)**
- UC07: Tìm trận đấu PvP
- UC08: Hủy tìm trận
- UC09: Chơi với máy (Practice)
- UC10: Xem Bảng xếp hạng
- UC11: Xem Thành tựu
- UC12: Mua đồ trong Cửa hàng (Shop)
- UC13: Cài đặt trò chơi (Âm thanh, Ngôn ngữ)

**Nhóm 3: Trong trận & Kết quả (Gameplay)**
- UC14: Bắt đầu trận đấu
- UC15: Trả lời câu hỏi
- UC16: Sử dụng vật phẩm hỗ trợ (Power-up)
- UC17: Xem điểm đối thủ real-time
- UC18: Đầu hàng / Thoát giữa trận
- UC19: Xem kết quả trận đấu
- UC20: Tính điểm & Trao thưởng

---

### NHÓM 1: XÁC THỰC & HỒ SƠ

#### UC01 — Đăng ký tài khoản
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC01 |
| **Tên** | Đăng ký tài khoản |
| **Actor chính** | Người dùng chưa có tài khoản |
| **Actor phụ** | Firebase Auth, Firebase Database |
| **Mô tả** | Người dùng tạo tài khoản mới bằng Tên hiển thị, Email và Mật khẩu. |
| **Tiền điều kiện** | Đang ở Auth Popup, chọn tab "Đăng ký" (Register). |
| **Hậu điều kiện — Thành công** | Tài khoản được tạo, hồ sơ mặc định được ghi lên Firebase, người dùng vào Trang chủ. |
| **Luồng chính** | 1. Nhập Tên, Email, Mật khẩu. <br> 2. Bấm "Đăng ký". <br> 3. Hệ thống kiểm tra hợp lệ cục bộ (email đúng chuẩn, mk > 6 ký tự). <br> 4. Gửi request tạo tài khoản lên Firebase. <br> 5. Khởi tạo dữ liệu người dùng (Level 1, 0 EXP, 0 Tiền) trên database. <br> 6. Đóng popup, vào Sảnh chính. |
| **Luồng thay thế** | - Nếu email đã tồn tại hoặc mật khẩu yếu, Firebase trả lỗi, hệ thống hiển thị thông báo lỗi màu đỏ trên popup. |

#### UC02 — Đăng nhập
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC02 |
| **Tên** | Đăng nhập |
| **Actor chính** | Người dùng đã có tài khoản |
| **Actor phụ** | Firebase Auth, Firebase Database |
| **Mô tả** | Người dùng xác thực bằng Email và Mật khẩu để tải lại dữ liệu tiến trình. |
| **Tiền điều kiện** | Đang ở Auth Popup, chọn tab "Đăng nhập" (Login). |
| **Hậu điều kiện — Thành công** | Session được lưu, dữ liệu được tải về, người dùng vào Trang chủ. |
| **Luồng chính** | 1. Nhập Email, Mật khẩu và bấm "Đăng nhập". <br> 2. Hệ thống xác thực qua Firebase Auth. <br> 3. Tải hồ sơ người dùng từ Database. <br> 4. Làm mới giao diện Trang chủ với dữ liệu vừa tải. |
| **Luồng thay thế** | - Sai mật khẩu hoặc email không tồn tại: Báo lỗi trực tiếp trên giao diện. |

#### UC03 — Chơi khách (Guest)
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC03 |
| **Tên** | Chơi khách |
| **Actor chính** | Người dùng |
| **Actor phụ** | Firebase Auth (SignInAnonymously) |
| **Mô tả** | Trải nghiệm game nhanh không cần tạo tài khoản. |
| **Tiền điều kiện** | Đang ở Auth Popup, chọn "Chơi khách". |
| **Hậu điều kiện — Thành công** | Tài khoản ẩn danh được tạo, vào thẳng sảnh chính. Một số tính năng Rank có thể bị khóa. |
| **Luồng chính** | 1. Hệ thống tự tạo Tên ngẫu nhiên (VD: Player_123). <br> 2. Người dùng sửa tên và bấm "Chơi thử". <br> 3. Hệ thống gọi SignInAnonymously lên Firebase. <br> 4. Khởi tạo profile tạm thời và vào Sảnh chính. |

#### UC04 — Đăng xuất
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC04 |
| **Tên** | Đăng xuất |
| **Actor chính** | Người chơi |
| **Actor phụ** | Không |
| **Mô tả** | Thoát tài khoản hiện tại và trở về màn hình chọn Đăng nhập. |
| **Tiền điều kiện** | Đang ở Trang chủ, đã mở Popup Hồ sơ (Profile Popup) hoặc Cài đặt. |
| **Hậu điều kiện — Thành công** | Xóa session Firebase, xóa dữ liệu cục bộ (nếu là Khách), quay lại màn hình Auth. |
| **Luồng chính** | 1. Nhấn nút "Đăng xuất". <br> 2. Hệ thống hiển thị popup xác nhận (cảnh báo mất dữ liệu nếu là tài khoản Khách). <br> 3. Nếu đồng ý, gọi `FirebaseManager.SignOut()`. <br> 4. Tải lại màn hình Auth Popup. |

#### UC05 — Chỉnh sửa hồ sơ
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC05 |
| **Tên** | Chỉnh sửa hồ sơ |
| **Actor chính** | Người chơi |
| **Actor phụ** | Firebase Database |
| **Mô tả** | Thay đổi Tên hiển thị hoặc chọn Avatar mới. |
| **Tiền điều kiện** | Đang ở Trang chủ, nhấp vào Avatar góc trên màn hình. |
| **Hậu điều kiện — Thành công** | Avatar và Tên mới được hiển thị trên HUD, đồng bộ lên Database. |
| **Luồng chính** | 1. Mở Profile Popup. <br> 2. Thay đổi Tên hoặc chọn 1 trong 8 Avatar. <br> 3. Nhấn "Lưu thay đổi". <br> 4. Hệ thống lưu cục bộ vào PlayerPrefs. <br> 5. Đồng bộ `displayName` và `avatarIndex` lên Firebase. <br> 6. Refresh thanh Header HUD. |

#### UC06 — Quên mật khẩu
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC06 |
| **Tên** | Quên mật khẩu |
| **Actor chính** | Người dùng |
| **Actor phụ** | Firebase Auth |
| **Mô tả** | Gửi email chứa liên kết để người dùng tự đặt lại mật khẩu. |
| **Tiền điều kiện** | Chọn "Quên mật khẩu?" trên màn hình Đăng nhập. |
| **Hậu điều kiện — Thành công** | Email phục hồi được gửi đi. |
| **Luồng chính** | 1. Nhập Email cần khôi phục. <br> 2. Bấm "Gửi yêu cầu". <br> 3. Hệ thống gửi yêu cầu Password Reset qua Firebase. <br> 4. Firebase gửi email tự động. <br> 5. Hiển thị thông báo thành công và quay lại màn hình Đăng nhập. |

---

### NHÓM 2: SẢNH CHÍNH & TÍNH NĂNG (HOME)

#### UC07 — Tìm trận đấu PvP
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC07 |
| **Tên** | Tìm trận đấu PvP |
| **Actor chính** | Người chơi |
| **Actor phụ** | Firebase Matchmaking |
| **Mô tả** | Ghi tên vào hàng chờ (Queue) để hệ thống tự động ghép cặp người chơi khác. |
| **Tiền điều kiện** | Ở Trang chủ, bấm "Tìm trận". |
| **Hậu điều kiện — Thành công** | Kết nối với 1 người chơi khác, tạo phòng (Room) và tải Gameplay Scene. |
| **Luồng chính** | 1. Bấm Tìm trận. <br> 2. Giao diện hiện "Đang tìm đối thủ...". <br> 3. Ghi thông tin người chơi vào node `matchmakingQueue`. <br> 4. Firebase Cloud Function (hoặc Logic tự động) phát hiện 2 người trong Queue, ghép lại và tạo RoomID. <br> 5. Cả 2 Client nhận sự kiện tìm thấy phòng và load Gameplay. |

#### UC08 — Hủy tìm trận
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC08 |
| **Tên** | Hủy tìm trận |
| **Actor chính** | Người chơi |
| **Actor phụ** | Firebase Database |
| **Mô tả** | Thoát khỏi hàng chờ ghép cặp khi chưa tìm thấy đối thủ. |
| **Tiền điều kiện** | Đang ở trạng thái "Đang tìm đối thủ...". |
| **Hậu điều kiện — Thành công** | Xóa khỏi queue, quay lại giao diện Trang chủ bình thường. |
| **Luồng chính** | 1. Nhấn nút "Hủy". <br> 2. Xóa node UID khỏi `matchmakingQueue`. <br> 3. Ẩn Matchmaking Panel. |

#### UC09 — Chơi với máy (Practice)
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC09 |
| **Tên** | Chơi với máy |
| **Actor chính** | Người chơi |
| **Actor phụ** | AI / LocalMatchProvider |
| **Mô tả** | Đấu tập với AI tự động trả lời, không cần kết nối mạng. |
| **Tiền điều kiện** | Bấm "Đấu với máy" ở Sảnh. |
| **Hậu điều kiện — Thành công** | Tải Gameplay Scene ở chế độ Offline. |
| **Luồng chính** | 1. Bấm nút Đấu máy. <br> 2. Hệ thống set `isOfflineMode = true`. <br> 3. Tải Gameplay. Bot (MockOpponent) sẽ tự động nộp đáp án ngẫu nhiên sau vài giây. |

#### UC10 — Xem Bảng xếp hạng
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC10 |
| **Tên** | Xem Bảng xếp hạng |
| **Actor chính** | Người chơi |
| **Actor phụ** | Firebase Database |
| **Mô tả** | Xem top những người chơi có điểm hoặc rank cao nhất. |
| **Tiền điều kiện** | Chọn Tab "Xếp hạng" ở menu dưới cùng. |
| **Hậu điều kiện — Thành công** | Danh sách Top người chơi được hiển thị đầy đủ. |
| **Luồng chính** | 1. Nhấn vào tab Xếp hạng. <br> 2. Truy vấn Firebase lấy danh sách Top User sắp xếp theo số Trophies/Điểm. <br> 3. Parse dữ liệu và load vào giao diện UI (Avatar, Tên, Cấp độ, Điểm). |

#### UC11 — Xem Thành tựu
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC11 |
| **Tên** | Xem Thành tựu |
| **Actor chính** | Người chơi |
| **Actor phụ** | Không |
| **Mô tả** | Xem các cột mốc đã hoàn thành (VD: Trả lời 100 câu, Đạt chuỗi 10). |
| **Tiền điều kiện** | Chọn Tab "Thành tựu". |
| **Hậu điều kiện — Thành công** | Danh sách huy hiệu hiển thị trạng thái hoàn thành. |
| **Luồng chính** | 1. Mở tab Thành tựu. <br> 2. Hệ thống đối chiếu dữ liệu tiến trình (PlayerPrefs/Firebase) với danh sách quy tắc Thành tựu. <br> 3. Hiển thị UI: Sáng (Đã mở khóa) / Mờ (Chưa mở khóa). |

#### UC12 — Mua đồ trong Cửa hàng (Shop)
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC12 |
| **Tên** | Mua đồ trong Cửa hàng |
| **Actor chính** | Người chơi |
| **Actor phụ** | Firebase Database |
| **Mô tả** | Sử dụng Tiền ảo (Coins) kiếm được để mua Vật phẩm hỗ trợ (Power-up). |
| **Tiền điều kiện** | Đang ở tab "Cửa hàng" và có đủ Tiền ảo. |
| **Hậu điều kiện — Thành công** | Trừ tiền ảo, cộng số lượng vật phẩm vào túi đồ và đồng bộ Cloud. |
| **Luồng chính** | 1. Bấm "Mua" ở 1 vật phẩm. <br> 2. Kiểm tra số dư Tiền ảo. <br> 3. Nếu đủ, trừ tiền ảo và tăng số lượng vật phẩm. <br> 4. Lưu cục bộ và đồng bộ lên Firebase. <br> 5. Hiển thị thông báo "Mua thành công". |
| **Luồng thay thế** | - Không đủ tiền: Nút Mua bị vô hiệu hóa hoặc báo lỗi trực tiếp. |

#### UC13 — Cài đặt trò chơi
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC13 |
| **Tên** | Cài đặt trò chơi |
| **Actor chính** | Người chơi |
| **Actor phụ** | Không |
| **Mô tả** | Tùy chỉnh âm lượng (Nhạc nền, SFX) và Đổi ngôn ngữ (VI/EN). |
| **Tiền điều kiện** | Mở Popup Cài đặt. |
| **Hậu điều kiện — Thành công** | Các thay đổi có hiệu lực tức thì và được lưu lại. |
| **Luồng chính** | 1. Kéo thanh trượt âm lượng hoặc chuyển đổi Ngôn ngữ. <br> 2. Hệ thống thay đổi thuộc tính `AudioSource.volume` và `LocalizationManager`. <br> 3. Lưu vào PlayerPrefs. <br> 4. Các UI Controller tự động cập nhật text theo ngôn ngữ mới (nếu có). |

---

### NHÓM 3: TRONG TRẬN & KẾT QUẢ

#### UC14 — Bắt đầu trận đấu
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC14 |
| **Tên** | Bắt đầu trận đấu |
| **Actor chính** | Hệ thống |
| **Actor phụ** | Firebase Database (Online) |
| **Mô tả** | Đồng bộ câu hỏi giữa 2 người chơi và bắt đầu tính giờ. |
| **Tiền điều kiện** | Cảnh Gameplay tải xong. |
| **Hậu điều kiện — Thành công** | Trận đấu ở trạng thái "Playing", câu số 1 hiện lên. |
| **Luồng chính** | 1. Lấy Seed và số lượng câu hỏi từ Room trên Firebase. <br> 2. Khởi tạo danh sách câu hỏi random bằng thuật toán Fisher-Yates (đảm bảo 2 máy y hệt nhau). <br> 3. Hiển thị đếm ngược 3-2-1. <br> 4. Hiển thị câu số 1 và kích hoạt Timer. |

#### UC15 — Trả lời câu hỏi
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC15 |
| **Tên** | Trả lời câu hỏi |
| **Actor chính** | Người chơi |
| **Actor phụ** | Firebase Database |
| **Mô tả** | Bấm chọn 1 trong 4 đáp án trước khi hết thời gian. |
| **Tiền điều kiện** | Câu hỏi đang hiện, thời gian chưa hết. |
| **Hậu điều kiện — Thành công** | Đáp án được gửi đi, chờ kết quả. |
| **Luồng chính** | 1. Người chơi nhấp vào 1 đáp án. <br> 2. Giao diện khóa các nút còn lại. <br> 3. Gửi đáp án index lên Firebase node `answers`. <br> 4. Đợi hết giờ, chấm điểm, hiển thị nút xanh (đúng)/ đỏ (sai). <br> 5. Chuyển sang câu tiếp theo. |

#### UC16 — Sử dụng vật phẩm hỗ trợ (Power-up)
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC16 |
| **Tên** | Sử dụng vật phẩm hỗ trợ |
| **Actor chính** | Người chơi |
| **Actor phụ** | Không |
| **Mô tả** | Dùng Power-up (ví dụ 50/50, Thêm thời gian) để giải quyết câu khó. |
| **Tiền điều kiện** | Đang trả lời câu hỏi, sở hữu > 0 vật phẩm tương ứng. |
| **Hậu điều kiện — Thành công** | Số lượng vật phẩm giảm 1, hiệu ứng kích hoạt. |
| **Luồng chính** | 1. Bấm vào icon Vật phẩm trên HUD. <br> 2. Kiểm tra kho đồ (Inventory). <br> 3. Trừ 1 số lượng (lưu tạm, đồng bộ sau trận). <br> 4. Kích hoạt logic: Ví dụ 50/50 sẽ ẩn đi 2 nút đáp án sai ngẫu nhiên. <br> 5. Khóa nút vật phẩm đó cho đến câu tiếp theo. |

#### UC17 — Xem điểm đối thủ real-time
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC17 |
| **Tên** | Xem điểm đối thủ real-time |
| **Actor chính** | Người chơi |
| **Actor phụ** | Firebase Realtime DB |
| **Mô tả** | Thanh điểm của đối thủ trên HUD nhảy điểm tự động khi họ trả lời đúng. |
| **Tiền điều kiện** | Chế độ Online, đang ở trạng thái Playing. |
| **Hậu điều kiện — Thành công** | HUD cập nhật điểm chính xác. |
| **Luồng chính** | 1. Đối thủ trả lời đúng và cộng điểm của họ lên Firebase. <br> 2. Client của người chơi nhận tín hiệu `OnValueChanged`. <br> 3. Hệ thống gọi `UpdateOpponentScore`. <br> 4. UI Toolkit cập nhật nhãn điểm đối thủ. |

#### UC18 — Đầu hàng / Thoát giữa trận
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC18 |
| **Tên** | Đầu hàng / Thoát giữa trận |
| **Actor chính** | Người chơi |
| **Actor phụ** | Firebase Database |
| **Mô tả** | Thoát game khi trận đấu chưa kết thúc (bị xử thua). |
| **Tiền điều kiện** | Đang ở Gameplay Scene. |
| **Hậu điều kiện — Thành công** | Người thoát bị xử Thua, người ở lại Thắng. Chuyển sang Popup Kết quả. |
| **Luồng chính** | 1. Nhấn nút ✖ hoặc Nút Back trên Android. <br> 2. Hiển thị Popup xác nhận (cảnh báo xử thua). <br> 3. Bấm "Thoát". <br> 4. Hệ thống gọi `ForcedSurrender()`, set người chiến thắng là đối thủ trên Firebase. <br> 5. Mở Popup Kết quả. |

#### UC19 — Xem kết quả trận đấu
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC19 |
| **Tên** | Xem kết quả trận đấu |
| **Actor chính** | Người chơi |
| **Actor phụ** | Không |
| **Mô tả** | Xem bảng tổng kết Điểm, Thắng/Thua, Tiền và EXP nhận được. |
| **Tiền điều kiện** | Trận đấu kết thúc (hết câu hỏi hoặc có người đầu hàng). |
| **Hậu điều kiện — Thành công** | Hiển thị Popup. Có tùy chọn "Chơi lại" hoặc "Về sảnh". |
| **Luồng chính** | 1. Khi `GameState = GameOver`, gọi `AwardRewards()`. <br> 2. Tính toán xong, mở Result Popup. <br> 3. Hiện điểm tổng 2 bên, hiệu ứng Thắng/Thua và phần thưởng. <br> 4. Người dùng bấm "Về sảnh" để trở lại HomeScene. |

#### UC20 — Tính điểm & Trao thưởng
| Trường | Nội dung |
|---|---|
| **Mã UC** | UC20 |
| **Tên** | Tính điểm & Trao thưởng |
| **Actor chính** | Hệ thống |
| **Actor phụ** | Firebase Database |
| **Mô tả** | Xác định kết quả và phân phát phần thưởng dựa trên thắng/thua. |
| **Tiền điều kiện** | Trận đấu kết thúc. |
| **Hậu điều kiện — Thành công** | Hồ sơ được cập nhật lên Firebase với Tiền và EXP mới. |
| **Luồng chính** | 1. Hệ thống so sánh tổng điểm P1 và P2. <br> 2. Xác định Win/Lose/Draw. <br> 3. Tra bảng thưởng (Ví dụ: Thắng +50XP, +100 Tiền). <br> 4. Cộng điểm vào `PlayerData`, tính logic Tăng Level. <br> 5. Lưu `PlayerPrefs` và đồng bộ `users/{uid}` lên Firebase. |
