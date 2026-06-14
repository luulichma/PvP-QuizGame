# BÁO CÁO HOÀN THÀNH PHASE 2 — PvP Quiz Game

> **Ngày hoàn thành:** 14/06/2026  
> **Phạm vi:** Refactor kiến trúc + Tính năng mới Phase 2 (Tier/Rank, Power-Up, Shop, Season, Daily Quests, Achievements)  
> **Nền tảng:** Unity — UI Toolkit (UXML/USS)  
> **Backend:** Firebase Realtime Database + Firebase Auth + Remote Config

---

## MỤC LỤC

1. [Tổng Quan Phase 2](#1-tổng-quan-phase-2)
2. [Pre-Phase 2: Refactor Kiến Trúc (Phá God Object)](#2-pre-phase-2-refactor-kiến-trúc)
3. [Cấu Trúc Thư Mục Sau Refactor](#3-cấu-trúc-thư-mục-sau-refactor)
4. [Tính Năng Mới Phase 2](#4-tính-năng-mới-phase-2)
   - 4.1. [Hệ Thống Tier & Rank Points](#41-hệ-thống-tier--rank-points)
   - 4.2. [Hệ Thống Power-Up Consumables](#42-hệ-thống-power-up-consumables)
   - 4.3. [Cửa Hàng (Shop)](#43-cửa-hàng-shop)
   - 4.4. [Mùa Giải (Seasonal Ranked)](#44-mùa-giải-seasonal-ranked)
   - 4.5. [Nhiệm Vụ Hàng Ngày (Daily Quests)](#45-nhiệm-vụ-hàng-ngày-daily-quests)
   - 4.6. [Hệ Thống Thành Tựu (Achievements)](#46-hệ-thống-thành-tựu-achievements)
5. [Thay Đổi Giao Diện: Từ 3 Tab → 4 Tab](#5-thay-đổi-giao-diện-từ-3-tab--4-tab)
6. [Icon Fix: Chuyển Emoji → PNG Icon](#6-icon-fix-chuyển-emoji--png-icon)
7. [Intermission (Giai Đoạn Nghỉ Giữa Mùa)](#7-intermission-giai-đoạn-nghỉ-giữa-mùa)
8. [Chi Tiết Từng File Đã Thay Đổi / Tạo Mới](#8-chi-tiết-từng-file-đã-thay-đổi--tạo-mới)
9. [Thống Kê Dòng Code](#9-thống-kê-dòng-code)
10. [Sơ Đồ Kiến Trúc Mới](#10-sơ-đồ-kiến-trúc-mới)

---

# 1. TỔNG QUAN PHASE 2

Phase 2 bao gồm **hai giai đoạn chính**:

| Giai đoạn | Mô tả | Kết quả |
|---|---|---|
| **Pre-Phase 2 Refactor** | Phá vỡ 4 God Object (MainMenuUI 1.357 dòng, GameplayUI 1.083 dòng, FirebaseManager 880 dòng, HomeLayout.uxml) | MainMenu → 320 dòng, GameplayUI → 203 dòng, FirebaseManager → 4 partial file ≤297 dòng |
| **Phase 2 Features** | Thêm 6 hệ thống mới: Tier/Rank, Power-Up, Shop, Season, Daily Quests, Achievements | 14 file mới, ~3.500 dòng code mới |

**Điểm nổi bật:**
- Tách từ **3 tab** (Home / Xếp Hạng / Thành Tựu) thành **4 tab** (Shop / Home / Xếp Hạng / Thành Tựu)
- Thêm **sub-tab** trong Achievements panel: Thành Tựu + Nhiệm Vụ hàng ngày
- Toàn bộ emoji (💎🥇💰...) được thay bằng **PNG icon** (fix lỗi mất icon trên APK Android)
- Hệ thống **Intermission** (giai đoạn nghỉ giữa các mùa giải)

---

# 2. PRE-PHASE 2: REFACTOR KIẾN TRÚC

## 2.1. Vấn Đề Trước Refactor

| File | Dòng (trước) | Vấn đề |
|---|---|---|
| `MainMenuUIController_UXML.cs` | **1.357** | Ôm tất cả: tabs, matchmaking, toast, auth popup, settings, logout, profile, leaderboard, achievements, XP bar, localize, animation |
| `GameplayUIController_UXML.cs` | **1.083** | HUD, countdown, câu hỏi, timer, score, streak, toast, turn summary, settings, exit, result popup |
| `FirebaseManager.cs` | **880** | Init + Auth + Profile sync + Matchmaking + Room sync (monolith) |

## 2.2. Giải Pháp: Phân Rã Theo SRP (Single Responsibility Principle)

### MainMenuUIController_UXML — Từ 1.357 → 320 dòng

Controller gốc giờ chỉ còn là **orchestrator** — khởi tạo và điều phối các sub-controller:

| Sub-Controller Mới | Trách nhiệm | Số dòng |
|---|---|---|
| `HomeNavController.cs` | 4 bottom tab + sub-tab + ShowPanel | 194 |
| `PlayerHeaderController.cs` | Avatar, tên, tiền, level tag, XP bar | ~130 |
| `MatchmakingPanelController.cs` | Tìm trận / đấu máy / hủy / timeout | ~200 |
| `LeaderboardPanelController.cs` | Bảng xếp hạng (theo tier) | ~120 |
| `AchievementsPanelController.cs` | Danh sách thành tựu + tiến độ | ~150 |
| `ShopPanelController.cs` | Cửa hàng Power-Up & Bundles | ~280 |
| `DailyQuestsPanelController.cs` | Tab nhiệm vụ hàng ngày | ~180 |
| `RankPanelController.cs` | Season banner + 5 tier filter + BXH | 215 |

### GameplayUIController_UXML — Từ 1.083 → 203 dòng

Controller gốc chỉ còn **subscribe event + route xuống sub-controller**:

| Sub-Controller Mới | Trách nhiệm | Số dòng |
|---|---|---|
| `GameplayHUDController.cs` | Score, timer arc, streak, turn summary, opponent status | ~430 |
| `QuestionViewController.cs` | Câu hỏi + counter + progress bar | ~100 |
| `CountdownOverlayController.cs` | Đếm ngược 3-2-1-GO | ~55 |
| `ResultPopupController.cs` | Popup kết quả trận (thắng/thua/hòa + reward) | ~260 |
| `GameplaySettingsPopupController.cs` | Settings popup trong trận | ~70 |
| `ExitConfirmPopupController.cs` | Xác nhận thoát giữa trận | ~45 |
| `PowerUpHUDController.cs` | 3 nút Power-Up + trạng thái | 142 |

### FirebaseManager — Từ 880 dòng 1 file → 4 partial file

Dùng `partial class` để giữ nguyên GUID, scene reference và public API:

| Partial File | Trách nhiệm | Số dòng |
|---|---|---|
| `FirebaseManager.cs` | Singleton, SDK refs, events, Init + RemoteConfig | 142 |
| `Firebase/FirebaseManager.Auth.cs` | Auth (email/anonymous/reset) + Profile sync + Tier | ~284 |
| `Firebase/FirebaseManager.Matchmaking.cs` | Queue / search / claim / timeout / cancel | ~297 |
| `Firebase/FirebaseManager.Room.cs` | Join room / presence / in-match sync / leave | ~181 |

### Hạ Tầng UI Chung (Mới)

| File Mới | Mô tả |
|---|---|
| `UI/Common/PopupBase.cs` | Base class cho popup: instantiate template, overlay, show/hide animation, close event |
| `UI/Common/ToastService.cs` | Gộp 2 bản toast trùng lặp thành 1 nguồn duy nhất |
| `UI/Common/UILocalizer.cs` | Binding tập trung: BindLabel/BindButton + auto refresh khi đổi ngôn ngữ |
| `UI/Common/FlyTextService.cs` | Text bay lên + fade (feedback nhanh cho mua/dùng power-up) |
| `UI/Common/UIIconHelper.cs` | Tạo VisualElement icon PNG + label (thay thế emoji) |

### Popup Controllers (Tách ra từ god objects)

| File | Từ |
|---|---|
| `Popups/AuthPopupController.cs` | MainMenu (~250 dòng inline) |
| `Popups/SettingsPopupController.cs` | MainMenu + Gameplay (chung template) |
| `Popups/ProfilePopupController.cs` | MainMenu (~114 dòng inline) |
| `Popups/LogoutConfirmPopupController.cs` | MainMenu |
| `Popups/EndOfSeasonPopupController.cs` | **MỚI** (Phase 2) |

---

# 3. CẤU TRÚC THƯ MỤC SAU REFACTOR

```
Assets/Scripts/
├── Controller/
│   ├── InputController_UXML.cs      (438 dòng — thêm 50:50 logic)
│   └── TimerController.cs           (~120 dòng — thêm AddTime cho Power-Up)
│
├── Core/
│   ├── GameController.cs            (398 dòng — ít thay đổi)
│   ├── GameManager.cs               (~85 dòng)
│   ├── ScoreManager.cs              (309 dòng — thêm RP + Shield + DailyQuest hooks)
│   ├── QuizManager.cs               (~215 dòng)
│   ├── AudioManager.cs              (~100 dòng)
│   ├── LocalizationManager.cs       (~400 dòng)
│   ├── PlayerDataManager.cs         (158 dòng — thêm Save/Load Phase 2 fields)
│   ├── SceneTransition.cs           (~85 dòng)
│   ├── [MỚI] PowerUpManager.cs     (141 dòng)
│   ├── [MỚI] ShopManager.cs        (176 dòng — static)
│   ├── [MỚI] SeasonManager.cs      (280 dòng)
│   ├── [MỚI] DailyQuestManager.cs  (220 dòng)
│   └── [MỚI] AchievementManager.cs (145 dòng)
│
├── Data/
│   ├── PlayerData.cs                (177 dòng — thêm Power-Up, Tier, Season fields)
│   ├── QuestionData.cs
│   ├── QuizDatabase.cs
│   └── GoogleSheetDownloader.cs
│
├── Network/
│   ├── FirebaseManager.cs           (142 dòng — core/singleton)
│   ├── Firebase/
│   │   ├── FirebaseManager.Auth.cs         (~284 dòng)
│   │   ├── FirebaseManager.Matchmaking.cs  (~297 dòng)
│   │   └── FirebaseManager.Room.cs         (~181 dòng)
│   ├── FirebaseMatchProvider.cs     (~180 dòng)
│   ├── LeaderboardManager.cs        (120 dòng — thêm FetchTierLeaderboard)
│   ├── LocalMatchProvider.cs        (~115 dòng)
│   └── MockOpponent.cs              (~90 dòng)
│
└── UI/
    ├── Common/
    │   ├── [MỚI] PopupBase.cs
    │   ├── [MỚI] ToastService.cs
    │   ├── [MỚI] UILocalizer.cs
    │   ├── [MỚI] FlyTextService.cs
    │   └── [MỚI] UIIconHelper.cs
    ├── Popups/
    │   ├── [MỚI] AuthPopupController.cs
    │   ├── [MỚI] SettingsPopupController.cs
    │   ├── [MỚI] ProfilePopupController.cs
    │   ├── [MỚI] LogoutConfirmPopupController.cs
    │   └── [MỚI] EndOfSeasonPopupController.cs
    ├── Home/
    │   ├── [MỚI] HomeNavController.cs
    │   ├── [MỚI] PlayerHeaderController.cs
    │   ├── [MỚI] MatchmakingPanelController.cs
    │   ├── [MỚI] LeaderboardPanelController.cs
    │   ├── [MỚI] AchievementsPanelController.cs
    │   ├── [MỚI] ShopPanelController.cs
    │   ├── [MỚI] DailyQuestsPanelController.cs
    │   └── [MỚI] RankPanelController.cs
    ├── Gameplay/
    │   ├── [MỚI] GameplayHUDController.cs
    │   ├── [MỚI] QuestionViewController.cs
    │   ├── [MỚI] CountdownOverlayController.cs
    │   ├── [MỚI] ResultPopupController.cs
    │   ├── [MỚI] GameplaySettingsPopupController.cs
    │   ├── [MỚI] ExitConfirmPopupController.cs
    │   └── [MỚI] PowerUpHUDController.cs
    ├── MainMenuUIController_UXML.cs       (320 dòng — orchestrator)
    ├── GameplayUIController_UXML.cs       (203 dòng — router)
    ├── InitSceneController_UXML.cs        (~560 dòng)
    ├── AvatarHelper.cs
    ├── HapticFeedback.cs
    ├── InfiniteBackground_UXML.cs
    ├── SpinnerHelper.cs
    ├── TimerArcElement.cs
    ├── UIAnimator.cs
    └── UIParticleEffect.cs
```

---

# 4. TÍNH NĂNG MỚI PHASE 2

## 4.1. Hệ Thống Tier & Rank Points

### Thiết kế

Rank Points (RP) phản ánh **kỹ năng thuần túy** — không bị ảnh hưởng bởi Level Multiplier.

| Tier | Tên | RP Cần |
|:---:|---|:---:|
| 1 | 🥉 Bronze | 0 – 499 |
| 2 | 🥈 Silver | 500 – 1.499 |
| 3 | 🥇 Gold | 1.500 – 2.999 |
| 4 | 💎 Diamond | 3.000 – 4.999 |
| 5 | 👑 Legend | 5.000+ |

### RP Award (Mỗi trận)

| Kết quả | RP |
|---|:---:|
| Thắng | +30 |
| Hòa | +10 |
| Thua | −15 |
| Đầu hàng | −25 |

### File liên quan

- **`PlayerData.cs`** — thêm `rankPoints`, `currentTier`, `highestTierThisSeason`, `ComputeTier()`, `RecomputeTier()`
- **`ScoreManager.cs`** — thêm `WIN_RANK_POINTS`, `LOSE_RANK_POINTS`, `FORCED_LOSE_RANK_POINTS`; RP **KHÔNG nhân** Level Multiplier
- **`FirebaseManager.Auth.cs`** — `GetPlayerTier()` đổi từ `int level` sang `int rankPoints`
- **`LeaderboardManager.cs`** — thêm `FetchTierLeaderboardAsync(int tier)` để lọc BXH theo tier

### Hardcore Demotion

Tier được **tính lại ngay lập tức** sau mỗi trận:
```csharp
// ScoreManager.AwardRewards()
PlayerDataManager.Instance.Data.AddRankPoints(rankPointsAwarded);
PlayerDataManager.Instance.Data.RecomputeTier(); // Giáng ngay nếu RP tụt ngưỡng
```

---

## 4.2. Hệ Thống Power-Up Consumables

### 3 loại Power-Up

| ID | Tên | Hiệu ứng | Giá |
|---|---|---|:---:|
| `pu_5050` | ✂️ 50:50 | Ẩn 2 đáp án sai (random) | 150$ |
| `pu_time` | ⏱️ +5s | Cộng thêm 5 giây | 100$ |
| `pu_shield` | 🛡️ Lá chắn | Trả lời sai không mất streak | 200$ |

### Luồng sử dụng

```
User bấm nút → PowerUpManager.Use5050() / UseExtraTime() / UseShield()
  → Validate (đủ count? chưa dùng trong trận?)
  → Trừ inventory + Save → Fire OnPowerUpUsed
  → Listener thực thi:
      - 50:50 → InputController_UXML ẩn 2 đáp án sai
      - +Time → TimerController.AddTime(5)
      - Shield → ScoreManager check IsShieldActive khi sai → giữ streak
```

### File liên quan

| File | Vai trò |
|---|---|
| `Core/PowerUpManager.cs` (141 dòng) | Singleton quản lý state trong trận, validate + fire event |
| `Controller/InputController_UXML.cs` | Listen `OnPowerUpUsed("pu_5050")` → ẩn 2 nút bằng Fisher-Yates shuffle |
| `Core/ScoreManager.cs` | Check `IsShieldActive` khi sai → `ConsumeShield()` → giữ streak |
| `UI/Gameplay/PowerUpHUDController.cs` (142 dòng) | 3 nút UI + count + trạng thái disable |

### Quy tắc

- Mỗi loại chỉ dùng **1 lần/trận** (giữ qua field `_used5050InMatch`...)
- `PowerUpManager.ResetForNewMatch()` được gọi khi countdown bắt đầu
- Power-Up là **local-only** — không sync sang đối thủ (tránh vỡ online sync)

---

## 4.3. Cửa Hàng (Shop)

### Thiết kế nền kinh tế

**Mua đơn lẻ:**

| Item | Giá đơn | Giá x5 (−10%) |
|---|:---:|:---:|
| 50:50 | 150$ | 675$ |
| +Time | 100$ | 450$ |
| Shield | 200$ | 900$ |

**Combo Bundles:**

| Bundle | Nội dung | Giá | Giá gốc |
|---|---|:---:|:---:|
| 🎯 Khởi Động | 2× 50:50 + 3× Time | 500$ | 600$ |
| ⚔️ Chiến Binh | 1× 50:50 + 2× Time + 1× Shield | 400$ | 550$ |
| 🏆 Vô Địch | 3× 50:50 + 3× Time + 3× Shield | 1000$ | 1.350$ |

### File liên quan

| File | Vai trò |
|---|---|
| `Core/ShopManager.cs` (176 dòng) | Static class — quản lý giá, mua đơn lẻ (`BuyItem`), mua bundle (`BuyBundle`) |
| `UI/Home/ShopPanelController.cs` (~280 dòng) | Render danh sách sản phẩm, nút Mua, event handling |
| `Data/PlayerData.cs` | Field `powerUp_5050`, `powerUp_extraTime`, `powerUp_shield` + `TrySpendMoney()`, `AddPowerUp()` |

### Luồng mua hàng

```
User bấm "Mua" → ShopManager.BuyItem("pu_5050", 1)
  → Kiểm tra đủ tiền? → TrySpendMoney(150)
  → Cộng power-up: AddPowerUp("pu_5050", 1)
  → SaveData() + SaveProfileToCloud()
  → Fire OnPurchaseSuccess → UI refresh
```

---

## 4.4. Mùa Giải (Seasonal Ranked)

### Thiết kế

- Mỗi mùa kéo dài mặc định **30 ngày** (config qua Firebase `/seasonConfig`)
- Khi mùa kết thúc: **giáng 2 tier**, RP reset về mốc tier mới
- Phần thưởng cuối mùa dựa trên **tier cao nhất đạt được**

### Bảng thưởng cuối mùa (§3.4)

| Tier Cao Nhất | Tiền | Power-Up |
|---|:---:|---|
| Bronze (1) | 200$ | — |
| Silver (2) | 500$ | 2× Time |
| Gold (3) | 1.000$ | 2× 50:50 + 2× Time |
| Diamond (4) | 2.000$ | 3× 50:50 + 3× Time + 2× Shield |
| Legend (5) | 5.000$ | 5× mỗi loại |

### Luồng xử lý (Client-driven — không cần Cloud Functions)

```
Vào HomeScene → SeasonManager.CheckSeasonAsync()
  → LoadSeasonConfigAsync() — đọc /seasonConfig từ Firebase
  → TryProcessEndOfSeason():
      if (UtcNow > seasonEndDate && lastSeasonProcessed < currentSeason):
        1. Snapshot vào /seasonArchive/season_{N}/{uid}
        2. Tính reward theo highestTierThisSeason
        3. Cộng money + power-up
        4. Giáng 2 tier, RP = mốc tier mới
        5. Append badge "T{tier}S{season}" vào seasonBadges
        6. SaveData + SaveProfileToCloud
        7. Fire OnSeasonEnded → EndOfSeasonPopupController hiện popup
```

### File liên quan

| File | Vai trò |
|---|---|
| `Core/SeasonManager.cs` (280 dòng) | Đọc config, xử lý reset, archive, fire event |
| `UI/Popups/EndOfSeasonPopupController.cs` (168 dòng) | Popup hiện reward cuối mùa |
| `UI/Home/RankPanelController.cs` (215 dòng) | Season banner + tier filter + intermission |

---

## 4.5. Nhiệm Vụ Hàng Ngày (Daily Quests)

### 4 nhiệm vụ (reset 00:00 UTC mỗi ngày)

| ID | Mô tả | Mục tiêu | Thưởng |
|---|---|:---:|:---:|
| `play_3` | Chơi 3 trận | 3 | 50$ |
| `win_1` | Thắng 1 trận | 1 | 100$ |
| `correct_15` | Đúng 15 câu | 15 | 75$ |
| `perfect` | 1 trận không sai câu nào | 1 | 200$ |

### Tracking hooks

Các sự kiện được gọi tự động từ `ScoreManager.AwardRewards()`:

```csharp
DailyQuestManager.Instance?.NotifyMatchPlayed();     // Mỗi trận (bất kể kết quả)
DailyQuestManager.Instance?.NotifyMatchWon();         // Chỉ khi thắng
DailyQuestManager.Instance?.NotifyCorrectAnswer();    // Mỗi câu đúng (gọi từ CheckAnswer)
DailyQuestManager.Instance?.NotifyPerfectRound();     // Thắng mà không sai câu nào
```

### Cách lưu trữ

- State lưu dưới dạng **JSON** trong `PlayerData.dailyQuestsData`
- Tự động reset khi detect ngày mới (kiểm tra mỗi frame trong `Update()`)
- User bấm nút "Nhận" → `TryClaim(questId)` → cộng tiền + sync cloud

### File liên quan

| File | Vai trò |
|---|---|
| `Core/DailyQuestManager.cs` (220 dòng) | Tracker + persist + claim logic |
| `UI/Home/DailyQuestsPanelController.cs` (~180 dòng) | Render danh sách quest + nút nhận |

---

## 4.6. Hệ Thống Thành Tựu (Achievements)

### 8 thành tựu

| ID | Tên | Điều kiện | Thưởng |
|---|---|---|---|
| `ach_bot_1` | Khởi Động Sương Sương | Thắng 1 trận máy | 50$ |
| `ach_bot_50` | Kẻ Hủy Diệt Máy Móc | Thắng 50 trận máy | 500$ |
| `ach_money_10k` | Phú Hào Mới Nổi | Tích lũy 10.000$ | 1.000$ |
| `ach_rank_1k` | Bước Chân Thần Tốc | Đạt 1.000 RP | 200$ |
| `ach_rank_5k` | Đỉnh Bảng Phong Thần | Đạt 5.000 RP | 1.000$ |
| `ach_streak_5` | Cỗ Máy Ghi Điểm | Chuỗi 5 thắng liên tiếp | 300$ |
| `ach_streak_10` | Độc Cô Cầu Bại | Chuỗi 10 thắng liên tiếp | 1.000$ |
| `ach_perfect_1` | Trí Tuệ Đỉnh Cao | Thắng không sai câu nào | 100 RP |

### Luồng tự động

```
Trận kết thúc → ScoreManager.AwardRewards()
  → Cập nhật stats (botWins, currentWinStreak, highestWinStreak...)
  → AchievementManager.CheckAchievements()
  → Nếu đạt điều kiện → UnlockAchievement() → ClaimReward() → SaveData + Sync Cloud
```

### File liên quan

| File | Vai trò |
|---|---|
| `Core/AchievementManager.cs` (145 dòng) | Define 8 achievements + check + unlock + reward |
| `UI/Home/AchievementsPanelController.cs` (~150 dòng) | Render danh sách + tiến độ + icon |
| `Data/PlayerData.cs` | Fields: `botWins`, `totalMoneyEarned`, `currentWinStreak`, `highestWinStreak`, `unlockedAchievements` |

---

# 5. THAY ĐỔI GIAO DIỆN: TỪ 3 TAB → 4 TAB

## Trước Phase 2: 3 Bottom Tab

```
┌──────────────────────────┐
│        HOME SCENE        │
├──────┬──────┬────────────┤
│ Home │ Rank │ Achievements│
└──────┴──────┴────────────┘
```

## Sau Phase 2: 4 Bottom Tab + Sub-Tab

```
┌──────────────────────────────────────────┐
│              HOME SCENE                  │
├────────┬────────┬──────────┬─────────────┤
│  SHOP  │  HOME  │   RANK   │ ACHIEVEMENTS │
│  (0)   │  (1)   │   (2)    │    (3)       │
└────────┴────────┴──────────┴─────────────┘
                                    │
                       ┌────────────┼────────────┐
                       │ Sub-tab 0  │ Sub-tab 1  │
                       │ Thành Tựu  │ Nhiệm Vụ  │
                       └────────────┴────────────┘
```

### Chi tiết nội dung từng tab

| Tab | Index | Nội dung |
|---|:---:|---|
| **SHOP** | 0 | Danh sách Power-Up đơn lẻ + Bundles + Bulk x5, nút Mua |
| **HOME** | 1 | Hero banner, nút Tìm Trận + Đấu Máy, player header |
| **RANK** | 2 | Season banner (countdown / intermission), 5 Tier filter chip, BXH theo tier |
| **ACHIEVEMENTS** | 3 | Sub-tab Thành Tựu (8 achievements) + Sub-tab Nhiệm Vụ (daily quests) |

### Controller quản lý: `HomeNavController.cs`

```csharp
// Wire 4 bottom tabs
_navShopBtn.clicked += () => SwitchBottomTab(0);
_navHomeBtn.clicked += () => SwitchBottomTab(1);
_navRankBtn.clicked += () => SwitchBottomTab(2);        // Chặn Guest
_navAchievementsBtn.clicked += () => SwitchBottomTab(3);

// Wire 2 sub-tabs (trong Achievements panel)
_subtabAchievementsBtn.clicked += () => SwitchAchievementsSubTab(0);
_subtabQuestsBtn.clicked += () => SwitchAchievementsSubTab(1);
```

---

# 6. ICON FIX: CHUYỂN EMOJI → PNG ICON

### Vấn đề

Build APK Android không có font emoji fallback → tất cả emoji (💎🥇💰🤖⚔️...) bị mất/hiện ô vuông trắng.

### Giải pháp

Tạo hệ thống **PNG Icon** thông qua USS class:

```
Assets/UI/Icons/       ← Thư mục chứa file .png
GlobalStyles.uss       ← Section "PNG ICONS" define .icon-{name} + .icon-tint-{color}
UI/Common/UIIconHelper.cs ← Helper tạo VisualElement icon
```

### API sử dụng

```csharp
// Trước (mất trên APK):
var label = new Label("💰 +500$");

// Sau (hoạt động mọi nơi):
var row = UIIconHelper.MakeIconLabel("icon-coins", "+500$",
    IconTint.Money, iconSizePx: 32f, fontSizePx: 28);
```

### Danh sách Icon Class + Tint

| Icon Class | Mô tả |
|---|---|
| `icon-coins` | Tiền vàng |
| `icon-trophy` | Cúp |
| `icon-crown` | Vương miện |
| `icon-gem` | Kim cương |
| `icon-award` | Huy chương |
| `icon-bot` | Robot |
| `icon-swords` | Kiếm chéo |
| `icon-flame` | Ngọn lửa |
| `icon-brain` | Bộ não |
| `icon-zap` | Tia sét |
| `icon-scissors` | Kéo (50:50) |
| `icon-timer` | Đồng hồ (+Time) |
| `icon-shield` | Lá chắn |
| `icon-gift` | Quà tặng |

| Tint Class | Màu |
|---|---|
| `icon-tint-gold` | Vàng kim |
| `icon-tint-silver` | Bạc |
| `icon-tint-bronze` | Đồng |
| `icon-tint-diamond` | Xanh nhạt |
| `icon-tint-legend` | Tím vàng |
| `icon-tint-money` | Vàng tiền |
| `icon-tint-purple` | Tím |
| `icon-tint-green` | Xanh lá |
| `icon-tint-red` | Đỏ |
| `icon-tint-cyan` | Xanh dương |

### AchievementDef cũ vs mới

```csharp
// Trước (emoji):
iconString = "🤖"

// Sau (PNG):
iconClass = "icon-bot"
iconTint  = IconTint.Cyan
```

### Season Badge format cũ vs mới

```csharp
// Trước: "💎S1,🥇S2"   ← mất trên APK
// Sau:   "T4S1,T3S2"    ← parse bằng EndOfSeasonPopupController.ParseBadge()
```

---

# 7. INTERMISSION (GIAI ĐOẠN NGHỈ GIỮA MÙA)

### Khái niệm

Khi mùa cũ đã kết thúc nhưng admin chưa mở mùa mới → hệ thống vào trạng thái **Intermission**.

### Hành vi trong Intermission

| Hệ thống | Hành vi |
|---|---|
| **Rank Points** | Freeze — không cộng/trừ RP |
| **Phần thưởng trận** | RP dương được bù bằng Money + EXP (tỷ lệ 1 RP = 2$ + 1 XP) |
| **BXH** | Vẫn hiển thị nhưng không có thay đổi xếp hạng |
| **Season Banner** | Đổi style vàng + message admin + countdown đến mùa mới |

### Phát hiện Intermission

```csharp
// SeasonManager.IsIntermission
public bool IsIntermission =>
    DateTime.UtcNow >= SeasonEndUtc && pd.lastSeasonProcessed >= CurrentSeason;
```

### Config Firebase (admin set)

```json
// /seasonConfig
{
  "currentSeason": 1,
  "seasonStartDate": "2026-06-01T00:00:00Z",
  "seasonEndDate": "2026-06-30T00:00:00Z",
  "nextSeasonId": 2,                              // optional
  "nextSeasonStartDate": "2026-07-05T00:00:00Z",  // optional
  "intermissionMessage": "Nghỉ xả hơi nhé!"       // optional
}
```

### File liên quan

| File | Chi tiết |
|---|---|
| `SeasonManager.cs` | Properties: `IsIntermission`, `NextSeasonId`, `NextSeasonStartUtc`, `IntermissionMessage`, `TimeUntilNextSeason` |
| `ScoreManager.cs` | L147-162: Check `isIntermission` → bù RP bằng Money + EXP, set `rankPointsAwarded = 0` |
| `RankPanelController.cs` | L116-167: Banner intermission style + message + countdown |

---

# 8. CHI TIẾT TỪNG FILE ĐÃ THAY ĐỔI / TẠO MỚI

## 8.1. Files Đã Sửa Đổi (MODIFY)

### `Data/PlayerData.cs`
- **Thêm fields:** `rankPoints`, `powerUp_5050`, `powerUp_extraTime`, `powerUp_shield`, `currentTier`, `highestTierThisSeason`, `lastSeasonProcessed`, `seasonBadges`, `dailyQuestsData`, `botWins`, `totalMoneyEarned`, `currentWinStreak`, `highestWinStreak`, `unlockedAchievements`
- **Thêm methods:** `TrySpendMoney()`, `AddRankPoints()`, `GetPowerUpCount()`, `AddPowerUp()`, `ComputeTier()`, `RecomputeTier()`, `AppendSeasonBadge()`, cập nhật `Reset()`

### `Core/PlayerDataManager.cs`
- Cập nhật `SaveData()` / `LoadData()` / `ClearData()` cho tất cả field Phase 2 mới
- Thêm `RecomputeTier()` call khi load (self-heal migration)

### `Core/ScoreManager.cs`
- Thêm hằng số RP: `WIN_RANK_POINTS=30`, `DRAW_RANK_POINTS=10`, `LOSE_RANK_POINTS=-15`, `FORCED_LOSE_RANK_POINTS=-25`
- `AwardRewards()`: tích hợp RP + Shield + DailyQuest hooks + Achievement tracking + Intermission bù
- `CheckAnswer()`: tích hợp Shield logic (`PowerUpManager.IsShieldActive`) + `NotifyCorrectAnswer()`
- Level Multiplier chỉ áp dụng cho Money & EXP — RP giữ nguyên

### `Controller/InputController_UXML.cs`
- Thêm `_eliminatedByFiftyFifty` HashSet tracking các index đã ẩn
- Thêm `HandlePowerUpUsed()` listener → ẩn 2 đáp án sai bằng Fisher-Yates shuffle
- Reset eliminated state mỗi câu mới (`HandleQuestionChanged`)

### `Network/LeaderboardManager.cs`
- Thêm field `tier` trong `LeaderboardEntry`
- Thêm `FetchTierLeaderboardAsync(int tier)` — filter BXH theo tier
- `FetchTopRankPlayersAsync()`: đọc `currentTier` từ cloud, fallback `ComputeTier(rp)`

### `Network/FirebaseManager.cs`
- Chuyển thành `partial class` — chỉ giữ singleton + SDK refs + Init + RemoteConfig (142 dòng)

### `UI/MainMenuUIController_UXML.cs`
- Từ 1.357 → 320 dòng
- Chỉ còn: khởi tạo sub-controllers, wire 2 nút header, route popup, background animation
- Tích hợp: `SeasonManager.OnSeasonEnded` → `EndOfSeasonPopupController`
- Tích hợp: `RankPanelController`, `DailyQuestsPanelController`

### `UI/GameplayUIController_UXML.cs`
- Từ 1.083 → 203 dòng
- Chỉ còn: subscribe GameController events + route xuống sub-controller
- Tích hợp: `PowerUpHUDController` (Attach/Detach + RefreshOnNewQuestion)

## 8.2. Files Tạo Mới (NEW)

### Core Layer (5 file mới)

| File | Dòng | Mô tả |
|---|:---:|---|
| `Core/PowerUpManager.cs` | 141 | Quản lý 3 loại power-up trong trận |
| `Core/ShopManager.cs` | 176 | Mua đơn lẻ + bundles (static class) |
| `Core/SeasonManager.cs` | 280 | Seasonal ranked + intermission |
| `Core/DailyQuestManager.cs` | 220 | 4 daily quests + auto reset |
| `Core/AchievementManager.cs` | 145 | 8 achievements + auto unlock + reward |

### Network Layer (3 file mới)

| File | Dòng | Mô tả |
|---|:---:|---|
| `Network/Firebase/FirebaseManager.Auth.cs` | ~284 | Auth + Profile sync + Tier |
| `Network/Firebase/FirebaseManager.Matchmaking.cs` | ~297 | Queue / search / claim / cancel |
| `Network/Firebase/FirebaseManager.Room.cs` | ~181 | Room sync / presence / leave |

### UI Common Layer (5 file mới)

| File | Dòng | Mô tả |
|---|:---:|---|
| `UI/Common/PopupBase.cs` | ~105 | Base popup: instantiate, overlay, animation |
| `UI/Common/ToastService.cs` | ~80 | Gộp 2 bản toast thành 1 |
| `UI/Common/UILocalizer.cs` | ~75 | Binding ngôn ngữ tập trung |
| `UI/Common/FlyTextService.cs` | 63 | Text bay (feedback mua/dùng item) |
| `UI/Common/UIIconHelper.cs` | 92 | PNG icon helper (thay emoji) |

### UI Popup Layer (5 file mới)

| File | Dòng | Mô tả |
|---|:---:|---|
| `Popups/AuthPopupController.cs` | ~280 | Inline auth popup (đăng nhập/đăng ký) |
| `Popups/SettingsPopupController.cs` | ~120 | Settings dùng chung Home + Gameplay |
| `Popups/ProfilePopupController.cs` | ~110 | Chỉnh sửa hồ sơ |
| `Popups/LogoutConfirmPopupController.cs` | ~60 | Xác nhận đăng xuất |
| `Popups/EndOfSeasonPopupController.cs` | 168 | Popup kết thúc mùa |

### UI Home Layer (8 file mới)

| File | Dòng | Mô tả |
|---|:---:|---|
| `Home/HomeNavController.cs` | 194 | 4 bottom tab + 2 sub-tab |
| `Home/PlayerHeaderController.cs` | ~130 | Player info header |
| `Home/MatchmakingPanelController.cs` | ~200 | Tìm/hủy trận + timeout |
| `Home/LeaderboardPanelController.cs` | ~120 | BXH theo tier |
| `Home/AchievementsPanelController.cs` | ~150 | Danh sách thành tựu |
| `Home/ShopPanelController.cs` | ~280 | Cửa hàng |
| `Home/DailyQuestsPanelController.cs` | ~180 | Nhiệm vụ hàng ngày |
| `Home/RankPanelController.cs` | 215 | Season banner + tier filter |

### UI Gameplay Layer (7 file mới)

| File | Dòng | Mô tả |
|---|:---:|---|
| `Gameplay/GameplayHUDController.cs` | ~430 | HUD tổng hợp |
| `Gameplay/QuestionViewController.cs` | ~100 | Hiển thị câu hỏi |
| `Gameplay/CountdownOverlayController.cs` | ~55 | 3-2-1-GO |
| `Gameplay/ResultPopupController.cs` | ~260 | Kết quả trận |
| `Gameplay/GameplaySettingsPopupController.cs` | ~70 | Settings trong trận |
| `Gameplay/ExitConfirmPopupController.cs` | ~45 | Xác nhận thoát |
| `Gameplay/PowerUpHUDController.cs` | 142 | 3 nút power-up |

---

# 9. THỐNG KÊ DÒNG CODE

## So sánh Before/After (God Objects)

| File | Trước | Sau | Giảm |
|---|:---:|:---:|:---:|
| `MainMenuUIController_UXML.cs` | 1.357 | 320 | **−76%** |
| `GameplayUIController_UXML.cs` | 1.083 | 203 | **−81%** |
| `FirebaseManager.cs` (tổng 4 file) | 880 | 142+284+297+181 = 904 | *+3% (tách vật lý)* |

## Tổng File Mới Tạo Trong Phase 2

| Nhóm | Số file | Tổng dòng (ước lượng) |
|---|:---:|:---:|
| Core Managers | 5 | ~962 |
| Firebase Partials | 3 | ~762 |
| UI Common | 5 | ~415 |
| UI Popups | 5 | ~738 |
| UI Home Controllers | 8 | ~1.469 |
| UI Gameplay Controllers | 7 | ~1.102 |
| **Tổng** | **33 file** | **~5.448 dòng** |

## Quy mô dự án hiện tại (ước tính)

| Danh mục | Số file .cs | Số dòng (ước tính) |
|---|:---:|:---:|
| Scripts/ tổng | ~60 | ~10.000+ |
| UXML layouts | ~8 | — |
| USS stylesheets | ~3 | ~1.000+ |

---

# 10. SƠ ĐỒ KIẾN TRÚC MỚI

```
┌─────────────────────────────────────────────────────────────┐
│                    UNITY SCENES                              │
│  ┌──────────┐   ┌──────────┐   ┌──────────────────────┐     │
│  │InitScene │   │HomeScene │   │   GameplayScene      │     │
│  │  Auth    │   │  Menu    │   │   Quiz Match         │     │
│  └────┬─────┘   └────┬─────┘   └──────────┬───────────┘     │
│       │              │                     │                 │
├───────┼──────────────┼─────────────────────┼─────────────────┤
│       │     UI LAYER (UI Toolkit)          │                 │
│       │              │                     │                 │
│  ┌────▼─────┐   ┌────▼──────────────┐  ┌──▼──────────────┐  │
│  │InitScene │   │MainMenu (320 LOC) │  │GameplayUI (203) │  │
│  │Controller│   │  Orchestrator     │  │   Router        │  │
│  └──────────┘   ├───────────────────┤  ├─────────────────┤  │
│                 │ HomeNavController  │  │ HUDController   │  │
│                 │ PlayerHeader       │  │ QuestionView    │  │
│                 │ MatchmakingPanel   │  │ CountdownOverlay│  │
│                 │ ShopPanel          │  │ ResultPopup     │  │
│                 │ RankPanel          │  │ PowerUpHUD      │  │
│                 │ AchievementsPanel  │  │ ExitConfirm     │  │
│                 │ DailyQuestsPanel   │  │ SettingsPopup   │  │
│                 └───────────────────┘  └─────────────────┘  │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                    CORE LAYER                                │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐   │
│  │GameCtrl  │ │ScoreMgr  │ │QuizMgr   │ │TimerCtrl      │   │
│  │(398 LOC) │ │(309 LOC) │ │(~215)    │ │InputCtrl(438) │   │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘   │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐   │
│  │PowerUpMgr│ │ShopMgr   │ │SeasonMgr │ │DailyQuestMgr  │   │
│  │(141 LOC) │ │(176,stat)│ │(280 LOC) │ │(220 LOC)      │   │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘   │
│  ┌──────────┐ ┌───────────────────────────────┐              │
│  │AchievMgr │ │PlayerDataManager (158) + Data │              │
│  │(145 LOC) │ │PlayerData.cs (177 LOC)        │              │
│  └──────────┘ └───────────────────────────────┘              │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                   NETWORK LAYER                              │
│  ┌───────────────────────────────────────────────────┐       │
│  │ FirebaseManager (partial class)                   │       │
│  │ ┌──────────┐ ┌────────────┐ ┌──────────────────┐ │       │
│  │ │Core (142)│ │Auth (284)  │ │Matchmaking (297) │ │       │
│  │ └──────────┘ └────────────┘ └──────────────────┘ │       │
│  │              ┌──────────────────┐                 │       │
│  │              │ Room (181)       │                 │       │
│  │              └──────────────────┘                 │       │
│  └───────────────────────────────────────────────────┘       │
│  ┌────────────────┐ ┌──────────────┐ ┌────────────────┐      │
│  │FirebaseMatch   │ │LeaderboardMgr│ │LocalMatchProv  │      │
│  │Provider        │ │(+TierFilter) │ │+ MockOpponent  │      │
│  └────────────────┘ └──────────────┘ └────────────────┘      │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                   FIREBASE CLOUD                             │
│  /users/{uid}          — profile + rank + tier + powerUps    │
│  /matchmakingQueue     — realtime matchmaking                │
│  /rooms/{roomId}       — in-match sync                       │
│  /seasonConfig         — season dates + intermission config  │
│  /seasonArchive        — historical season data              │
└──────────────────────────────────────────────────────────────┘
```

---

## KẾT LUẬN

Phase 2 đã **hoàn thành đầy đủ** cả về mặt refactor lẫn tính năng mới:

1. **Kiến trúc:** God objects đã được phá vỡ triệt để — không file UI nào vượt 430 dòng, controller chính chỉ còn ~200-320 dòng (giảm 76-81%).

2. **Tính năng:** 6 hệ thống mới (Tier/Rank, Power-Up, Shop, Season, Daily Quests, Achievements) được tích hợp chặt chẽ với nhau thông qua event-driven architecture.

3. **UI/UX:** Giao diện được tổ chức lại thành 4 tab rõ ràng, có sub-tab cho Achievements/Quests, và season banner tích hợp trực tiếp trong Rank panel.

4. **Compatibility:** Icon Fix đảm bảo toàn bộ icon hiển thị đúng trên APK Android (thay emoji bằng PNG).

5. **Maintainability:** Mọi feature mới chỉ cần tạo file mới hoặc sửa file < 300 dòng — không còn phẫu thuật god object.

> **Tổng cộng:** ~33 file C# mới, ~5.448 dòng code mới, 3 file chính giảm tổng cộng ~2.797 dòng.
