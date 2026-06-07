# 📋 Kế Hoạch Triển Khai (Phase 2) — Tier & Rank System Mới + Power-Up

**Tài liệu tham khảo chính:** `economy-design.md` (v2.0)
**Mục tiêu:** Cập nhật nền kinh tế, chuyển Level thành Converter, dùng Rank Points để phân Tier, thêm Seasonal Rank và hệ thống Power-Up Consumables.

---

## 📍 Bước 1: Nền Tảng Dữ Liệu (Data Layer)
- [ ] **Sửa `PlayerData.cs`**:
  - Thêm các biến lưu trữ Power-Up: `powerUp_5050`, `powerUp_extraTime`, `powerUp_shield`.
  - Thêm các biến Ranking: `currentTier`, `highestTierThisSeason`, `lastSeasonProcessed`, `seasonBadges`.
  - Thêm field cho Daily Quests: `dailyQuestsData`.
- [ ] **Sửa `PlayerDataManager.cs`**: 
  - Cập nhật `Save()` và `Load()` để lưu trữ an toàn các biến mới vào local (PlayerPrefs) / JSON.
- [ ] **Sửa `ScoreManager.cs`**: 
  - Đảm bảo **Level Multiplier** CHỈ áp dụng cho `Money` và `EXP`.
  - **Rank Points (RP)** phải được tính cố định (không bị nhân).
- [ ] **Sửa `FirebaseManager.cs`**: 
  - Đổi logic hàm `GetPlayerTier(int level)` thành `GetPlayerTier(int rankPoints)`.
  - Cập nhật schema khi push dữ liệu lên Firebase Database (gửi thêm `tierRankPoints`, `currentTier`, `powerUp`...).

## 📍 Bước 2: Hệ Thống Power-Up (Logic Trong Trận)
- [ ] **Tạo `PowerUpManager.cs`**: 
  - Singleton quản lý việc sử dụng 3 loại Power-Up trong trận đấu.
  - Expose các event: `OnPowerUpUsed`, `OnPowerUpFailed`.
- [ ] **Tạo `ShopManager.cs`**: 
  - Quản lý logic mua hàng: kiểm tra đủ tiền, trừ tiền `Money`, cộng thêm item vào `PlayerData`, sau đó lưu lại.
- [ ] **Cập nhật `GameplayLayout.uxml`**:
  - Thêm UI container chứa 3 nút Power-Up (50:50, Thêm giờ, Lá chắn).
- [ ] **Cập nhật `GameplayUIController_UXML.cs`**:
  - Bắt sự kiện click vào nút Power-Up.
  - Cập nhật UI thể hiện số lượng Power-Up còn lại.
  - Disable nút 50:50/Thêm giờ nếu đã sử dụng.
- [ ] **Cập nhật `ScoreManager.cs` (Shield Logic)**:
  - Tích hợp logic **Lá chắn**: nếu người chơi trả lời sai nhưng cờ Shield đang bật → giữ nguyên `currentStreak`, chỉ trừ shield count.

## 📍 Bước 3: Cửa Hàng UI (Shop & Main Menu)
- [ ] **Cập nhật `HomeLayout.uxml`**:
  - Thiết kế lại nội dung bên trong `shop-panel` (thay vì "Coming Soon").
  - Thêm các gói mua lẻ (50:50, Time, Shield) và các combo (Gói Chiến Binh, Gói Khởi Động...).
- [ ] **Cập nhật `MainMenuUIController_UXML.cs`**:
  - Load số dư tiền & số lượng item hiện có lên màn hình Shop.
  - Xử lý event click nút "Mua" → gọi `ShopManager.BuyItem()`.

## 📍 Bước 4: Chế Độ Rank Theo Mùa (Seasonal Ranked)
- [ ] **Tạo `SeasonManager.cs`**:
  - Đọc cấu hình Season từ Firebase (`seasonStartDate`, `seasonEndDate`).
  - Chạy check ngay khi vào game: nếu hết mùa → gọi luồng giáng Tier (trừ 2 rank), reset RP về mốc, và trao thưởng (Money + Item) cho người chơi.
  - Cập nhật `lastSeasonProcessed` để không nhận thưởng 2 lần.
- [ ] **Cập nhật `LeaderboardManager.cs`**:
  - Thay đổi Query Firebase để filter theo `currentTier` của mùa hiện tại (thay vì toàn server).
- [ ] **Cập nhật UI Main Menu**:
  - Thêm badge/label báo đếm ngược kết thúc mùa giải hiện tại.
  - Hiển thị danh hiệu mùa trên Profile của người chơi (VD: `[💎 S1]`).

## 📍 Bước 5: Nhiệm Vụ Hàng Ngày (Future Phase)
- [ ] **Tạo `DailyQuestManager.cs`**: Theo dõi hoạt động trong ngày và reset vào lúc 0h00 UTC.
- [ ] **Cập nhật UI**: Hiển thị tab Quests trên sảnh chờ.

---

### Nguyên Tắc Cốt Lõi Khi Triển Khai
1. **Thiết kế không lệ thuộc Art**: Dùng Text, Emoji và Glassmorphism (USS) có sẵn.
2. **Hardcore Demotion**: Giáng tier ngay lập tức khi RP tụt ngưỡng (không có grace period).
3. **Mọi Power-Up đều được dùng Online**: Xử lý cẩn thận đồng bộ đáp án (chỉ ảnh hưởng local flow, không làm vỡ online sync). 
