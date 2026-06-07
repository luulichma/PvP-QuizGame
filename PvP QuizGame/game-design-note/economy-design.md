# 📋 Thiết kế Nền Kinh Tế — PvP QuizGame

> **Phiên bản:** 2.0  
> **Ngày cập nhật:** 2026-06-07  
> **Tác giả:** Solo Dev  
> **Trọng tâm:** Power-Up Consumables + Hệ thống Rank theo Mùa (Seasonal Ranked)

---

## 1. Tổng quan vấn đề

### 1.1. Hiện trạng nền kinh tế

Game hiện tại có **3 loại tài nguyên**:

| Tài nguyên | Vai trò | Source (Nguồn tạo) |
|---|---|---|
| **Money ($)** | Tiền tệ chính | Thắng: +100$, Hòa: +40$, Thua: +10$, Achievement |
| **EXP** | Kinh nghiệm → Level Up | Thắng: +50, Hòa: +20, Thua: +10 |
| **Rank Points (RP)** | Điểm xếp hạng → Leaderboard | Thắng: +30, Hòa: +10, Thua: -10 |

**Level Multiplier hiện có:** Phần thưởng dương × `(1.0 + level × 0.1)`.

### 1.2. Vấn đề cốt lõi

```
❌ KHÔNG CÓ SINK — Tiền ($) kiếm được nhưng KHÔNG CÓ CHỖ ĐỂ TIÊU
❌ Leaderboard cố định — Người chơi mới không bao giờ đuổi kịp Top → mất động lực
❌ BXH trống — Server chưa nhiều người, BXH nhìn lèo tèo thiếu hấp dẫn
❌ Không có vòng lặp tiêu dùng — Tích lũy tiền vô nghĩa → mất hứng thú
```

### 1.3. Mục tiêu thiết kế

```
✅ Tạo vòng lặp: Chơi → Kiếm tiền → Mua Power-Up → Đua Top → Cần thêm tiền → Chơi tiếp
✅ Rank theo Mùa: Reset định kỳ → mọi người đều có cơ hội → BXH sôi động
✅ Level = Converter (khuếch đại), Rank Points = Công cụ phân Tier
✅ Không cần asset art: Dùng emoji/text-based UI (phù hợp solo dev)
```

---

## 2. Vai trò của Level và Rank Points (THAY ĐỔI LỚN)

### 2.1. Level = Converter (Máy khuếch đại phần thưởng)

**Level KHÔNG dùng để phân tier nữa.** Level chỉ đóng vai trò duy nhất: **khuếch đại phần thưởng**.

```
Công thức hiện có (giữ nguyên):
  Phần thưởng thực tế = Phần thưởng cơ bản × (1.0 + level × 0.1)

Ví dụ:
  Level 1  → ×1.1 (tăng 10%)
  Level 5  → ×1.5 (tăng 50%)
  Level 10 → ×2.0 (gấp đôi)
  Level 20 → ×3.0 (gấp ba)
```

**Ý nghĩa:** Level cao = chơi lâu = được thưởng nhiều hơn mỗi trận. Đây chính là **Converter** — biến thời gian đã bỏ ra thành lợi thế kinh tế, KHÔNG phải lợi thế xếp hạng.

> Level tăng vĩnh viễn, không bao giờ bị reset qua mùa.

### 2.2. Rank Points = Công cụ phân Tier

**Rank Points (RP)** bây giờ là chỉ số quyết định bạn ở **Tier nào** và **vị trí trên BXH**.

| Tier | Tên | RP yêu cầu | Số câu/trận | Matchmaking Queue |
|---|---|---|---|---|
| 🥉 I | **Đồng** (Bronze) | 0 – 499 RP | 10 câu | `matchmakingQueue/tier_1` |
| 🥈 II | **Bạc** (Silver) | 500 – 1,499 RP | 15 câu | `matchmakingQueue/tier_2` |
| 🥇 III | **Vàng** (Gold) | 1,500 – 2,999 RP | 20 câu | `matchmakingQueue/tier_3` |
| 💎 IV | **Kim Cương** (Diamond) | 3,000 – 4,999 RP | 25 câu | `matchmakingQueue/tier_4` |
| 👑 V | **Huyền Thoại** (Legend) | 5,000+ RP | 30 câu | `matchmakingQueue/tier_5` |

**Cơ chế thăng/giáng tier:**
- Khi RP vượt ngưỡng trên → **tự động thăng Tier** (ví dụ: 500 RP → lên Silver).
- Khi RP rơi dưới ngưỡng → **tự động giáng Tier** (ví dụ: 499 RP → về Bronze).
- Có thể thêm "lưới an toàn": sau khi mới thăng tier, cần thua 3 trận liên tiếp mới bị giáng (tránh dao động liên tục ở ranh giới).

**Thay đổi code cần thiết:**

```
Hiện tại (FirebaseManager.cs L405-410):
  GetPlayerTier(int level) → dựa vào Level

Đổi thành:
  GetPlayerTier(int rankPoints) → dựa vào RP
  
  if (rankPoints < 500)  return 1;  // Bronze
  if (rankPoints < 1500) return 2;  // Silver
  if (rankPoints < 3000) return 3;  // Gold
  if (rankPoints < 5000) return 4;  // Diamond
  return 5;                         // Legend
```

---

## 3. Hệ thống Rank theo Mùa (Seasonal Ranked)

### 3.1. Tại sao cần Rank theo Mùa?

| Vấn đề (Rank cố định) | Giải pháp (Rank theo Mùa) |
|---|---|
| Người chơi mới không bao giờ đuổi kịp Top → bỏ cuộc | Reset mỗi mùa → ai cũng bắt đầu từ gần nhau |
| BXH trống vì ít người chơi | Reset kéo nhiều người về cùng tier → BXH sôi động hơn |
| Không có lý do quay lại sau khi đạt rank cao | Mỗi mùa mới = cuộc đua mới + phần thưởng mùa |
| Top đứng yên vĩnh viễn → nhàm chán | Phải bảo vệ vị trí mỗi mùa → luôn cạnh tranh |

### 3.2. Cấu trúc Mùa (Season)

| Thông số | Giá trị | Lý do |
|---|---|---|
| **Thời lượng 1 mùa** | 30 ngày (1 tháng) | Đủ dài để leo rank, đủ ngắn để không nhàm |
| **Reset RP** | Về 0 RP (tất cả) | Công bằng tuyệt đối |
| **Reset Tier** | Giảm 2 bậc (tối thiểu = Bronze) | Giữ lại một phần thành quả |
| **Countdown** | Hiển thị "Mùa X – Còn N ngày" trên UI | Tạo urgency |

### 3.3. Luồng kết thúc mùa (End of Season)

```
Mùa 1 kết thúc (Ngày thứ 30, 23:59 UTC)
    │
    ▼
┌─────────────────────────────────┐
│ 1. CHỤP SNAPSHOT                │
│    Lưu lại: tier + RP cuối mùa │
│    của toàn bộ người chơi       │
└─────────────┬───────────────────┘
              │
              ▼
┌─────────────────────────────────┐
│ 2. TRAO PHẦN THƯỞNG MÙA        │
│    Dựa trên Tier cao nhất đạt   │
│    được trong mùa               │
└─────────────┬───────────────────┘
              │
              ▼
┌─────────────────────────────────┐
│ 3. GIÁNG TIER                   │
│    Mỗi người giảm 2 bậc:       │
│    Legend → Gold                │
│    Diamond → Silver             │
│    Gold → Bronze                │
│    Silver → Bronze (tối thiểu)  │
│    Bronze → Bronze              │
└─────────────┬───────────────────┘
              │
              ▼
┌─────────────────────────────────┐
│ 4. RESET RANK POINTS            │
│    RP = Điểm khởi đầu của      │
│    Tier mới sau giáng           │
│    (VD: Gold→Bronze = 0 RP)     │
│    (VD: Legend→Gold = 1500 RP)  │
└─────────────┬───────────────────┘
              │
              ▼
┌─────────────────────────────────┐
│ 5. MÙA MỚI BẮT ĐẦU            │
│    Hiển thị popup "Mùa 2"      │
│    Hiển thị phần thưởng đã nhận │
│    Hiển thị tier hiện tại mới   │
└─────────────────────────────────┘
```

### 3.4. Phần thưởng cuối mùa

Phần thưởng dựa trên **Tier cao nhất đạt được** (không phải tier kết thúc):

| Tier đạt được | Phần thưởng Money | Phần thưởng Power-Up | Danh hiệu |
|---|---|---|---|
| 🥉 Bronze | 200$ | — | — |
| 🥈 Silver | 500$ | 2× Thêm giờ | Bạc Mùa X |
| 🥇 Gold | 1,000$ | 2× 50:50 + 2× Thêm giờ | Vàng Mùa X |
| 💎 Diamond | 2,000$ | 3× 50:50 + 3× Thêm giờ + 2× Lá chắn | Kim Cương Mùa X |
| 👑 Legend | 5,000$ | 5× 50:50 + 5× Thêm giờ + 5× Lá chắn | Huyền Thoại Mùa X |

> **Danh hiệu mùa** hiển thị bên cạnh tên người chơi (text badge, không cần art). Ví dụ: `[💎 S1] PlayerName`.

### 3.5. Vì sao giảm 2 bậc thay vì reset hoàn toàn?

| Reset hoàn toàn (tất cả về Bronze) | Giảm 2 bậc |
|---|---|
| ✅ Tuyệt đối công bằng | ✅ Công bằng tương đối |
| ❌ Pro player phá nát Bronze → Newbie bị vùi dập | ✅ Pro player bắt đầu ở Gold → không gặp Newbie |
| ❌ Mất hết thành quả → nản | ✅ Giữ lại 1 phần → vẫn có động lực |
| ❌ Top 1 mùa trước = Level 1 mùa sau → vô lý | ✅ Top 1 mùa trước vẫn ở tier cao hơn → hợp lý |

**RP sau giáng 2 bậc:** RP được set về **mốc khởi đầu** của tier mới.

| Tier cuối mùa | Tier mùa mới | RP mùa mới |
|---|---|---|
| 👑 Legend (5000+) | 🥇 Gold | 1,500 RP |
| 💎 Diamond (3000-4999) | 🥈 Silver | 500 RP |
| 🥇 Gold (1500-2999) | 🥉 Bronze | 0 RP |
| 🥈 Silver (500-1499) | 🥉 Bronze | 0 RP |
| 🥉 Bronze (0-499) | 🥉 Bronze | 0 RP |

### 3.6. Leaderboard theo Mùa

**BXH hiển thị RP trong mùa hiện tại**, được phân theo Tier:

```
┌──────────────────────────────────────────┐
│  🏆 BXH — Mùa 1 (Còn 12 ngày)          │
│  ──────────────────────────────────────  │
│  [TAB: 🥉 Đồng | 🥈 Bạc | 🥇 Vàng...] │
│  ──────────────────────────────────────  │
│  Đang xem: 🥈 Bạc (500 – 1,499 RP)     │
│                                          │
│  🥇 1. PlayerA ............. 1,380 RP    │
│  🥈 2. PlayerB ............. 1,250 RP    │
│  🥉 3. PlayerC ............. 1,100 RP    │
│     4. BẠN ▶ ............... 890 RP ◀    │
│     5. PlayerD ............. 820 RP      │
│     ...                                  │
│                                          │
│  📊 Vị trí của bạn: #4 / 28 người       │
└──────────────────────────────────────────┘
```

**Giải quyết bài toán "BXH trống":**
- Reset mùa nén tất cả người chơi lại → mỗi tier có nhiều người hơn.
- Tier thấp (Bronze) luôn đông nhất vì tất cả newbie + người bị giáng đều ở đây.
- Hiển thị "Vị trí của bạn: #4 / 28 người" → dù BXH ít người vẫn thấy có ý nghĩa.

### 3.7. Firebase Schema cho Seasonal Rank

```
/seasonConfig:
  currentSeason: 1
  seasonStartDate: "2026-06-01T00:00:00Z"
  seasonEndDate: "2026-07-01T00:00:00Z"
  seasonDurationDays: 30

/users/{uid}:
  rankPoints: 890           // RP mùa hiện tại (dùng cho BXH + phân tier)
  currentTier: 2            // Tier hiện tại (tính từ RP)
  highestTierThisSeason: 3  // Tier cao nhất đạt được trong mùa (để tính thưởng)
  lastSeasonProcessed: 0    // Season ID cuối cùng đã xử lý reset (tránh reset 2 lần)
  seasonBadges: "💎S1,🥇S2" // Danh hiệu các mùa trước

/seasonArchive/season_1/{uid}:  // Lưu trữ kết quả mùa cũ (optional, cho lịch sử)
  finalTier: 4
  finalRP: 4200
  highestTier: 5
  rewardsClaimed: true
```

### 3.8. Xử lý kỹ thuật: Khi nào reset mùa?

**Phương án đề xuất: Client-driven reset (đơn giản, phù hợp solo dev)**

```
Khi người chơi mở game (hoặc vào HomeScene):
  1. Đọc /seasonConfig/currentSeason và seasonEndDate
  2. Nếu DateTime.UtcNow > seasonEndDate:
     a. Kiểm tra lastSeasonProcessed < currentSeason
     b. Nếu chưa xử lý → thực hiện reset cho user này:
        - Lưu snapshot vào seasonArchive
        - Tính thưởng, cộng money + power-up
        - Giáng 2 tier, set RP về mốc tier mới
        - Cập nhật lastSeasonProcessed = currentSeason
     c. Server admin (bạn) cập nhật seasonConfig khi mùa mới bắt đầu
  3. Nếu chưa hết mùa → chơi bình thường
```

> Ưu điểm: Không cần Cloud Functions / Server-side cron. Mỗi client tự xử lý khi mở app.
> Nhược điểm: Cần bạn (admin) cập nhật seasonConfig trên Firebase Console khi mùa mới bắt đầu. Có thể tự động hóa sau bằng Cloud Scheduler nếu muốn.

---

## 4. Phần thưởng trận đấu (Điều chỉnh)

### 4.1. Phần thưởng cơ bản (chung cho mọi tier)

| Kết quả | Money | EXP | RP |
|---|---|---|---|
| **Thắng** | 100$ | 50 XP | +30 RP |
| **Hòa** | 40$ | 20 XP | +10 RP |
| **Thua** | 10$ | 10 XP | -15 RP |
| **Đầu hàng** | 0$ | 0 XP | -25 RP |

### 4.2. Level Multiplier (áp dụng lên Money và EXP, KHÔNG áp dụng lên RP)

```
Money thực tế = Money cơ bản × (1.0 + level × 0.1)
EXP thực tế   = EXP cơ bản   × (1.0 + level × 0.1)
RP thực tế    = RP cơ bản     (KHÔNG nhân, giữ nguyên)
```

> **Quan trọng:** RP KHÔNG được nhân bởi Level Multiplier. Nếu nhân, người chơi Level cao sẽ leo rank nhanh hơn một cách bất công. RP phải phản ánh **kỹ năng thuần túy**, không phải thời gian đã chơi.

### 4.3. Ví dụ cụ thể

| Tình huống | Level 1 (×1.1) | Level 10 (×2.0) | Level 20 (×3.0) |
|---|---|---|---|
| Thắng → Money | 110$ | 200$ | 300$ |
| Thắng → EXP | 55 XP | 100 XP | 150 XP |
| Thắng → RP | +30 RP | +30 RP | +30 RP |
| Thua → RP | -15 RP | -15 RP | -15 RP |

→ Level cao = giàu hơn, nhưng **không leo rank nhanh hơn**.

---

## 5. Power-Up Consumables

### 5.1. Tại sao Power-Up là Sink phù hợp nhất?

1. **Không cần art:** Dùng emoji + text cho UI (✂️ 50:50, ⏱ +Time, 🛡 Shield).
2. **Tiêu hao liên tục:** Mua → dùng hết → phải mua lại → vòng lặp tiền tệ hoạt động.
3. **Gắn chặt với đua top:** Muốn leo Rank → cần Power-Up → cần tiền → cần chơi nhiều.

### 5.2. Danh sách Power-Up

| ID | Tên | Emoji | Giá | Hiệu ứng | Giới hạn/trận |
|---|---|---|---|---|---|
| `pu_5050` | **50:50** | ✂️ | 150$ | Loại bỏ 2 đáp án sai, chỉ còn 2 lựa chọn | 1 lần |
| `pu_time` | **Thêm giờ** | ⏱️ | 100$ | +5 giây cho câu hỏi hiện tại | 1 lần |
| `pu_shield` | **Lá chắn** | 🛡️ | 200$ | Trả lời sai vẫn giữ nguyên streak (không bị reset) | 1 lần |

### 5.3. Vòng lặp kinh tế hoàn chỉnh

```
┌──────────────────────────────────────────────────────────────────┐
│                     VÒNG LẶP TRONG MÙA                          │
│                                                                  │
│  CHƠI TRẬN ──→ Kiếm Money + RP ──→ MUA POWER-UP (Sink)         │
│      ▲                 │                    │                    │
│      │                 │                    ▼                    │
│      │                 │           DÙNG POWER-UP TRONG TRẬN     │
│      │                 │                    │                    │
│      │                 │                    ▼                    │
│      │                 │           THẮNG DỄ HƠN → +30 RP       │
│      │                 │                    │                    │
│      │                 │                    ▼                    │
│      │                 │           LEO RANK / THĂNG TIER         │
│      │                 │                    │                    │
│      │                 │                    ▼                    │
│      │                 │           MUỐN GIỮ TOP → CẦN POWER-UP │
│      │                 │                    │                    │
│      │                 ▼                    │                    │
│      │           HẾT TIỀN ←────────────────┘                    │
│      │                 │                                         │
│      └─────────────────┘                                         │
│                                                                  │
├──────────────────────────────────────────────────────────────────┤
│                     KẾT THÚC MÙA                                │
│                                                                  │
│  SNAPSHOT TIER ──→ PHẦN THƯỞNG MÙA (Money + Power-Up)           │
│       │                                                          │
│       ▼                                                          │
│  GIÁNG 2 TIER ──→ RP RESET về mốc tier mới                     │
│       │                                                          │
│       ▼                                                          │
│  MÙA MỚI ──→ LẠI ĐUA TOP TỪ ĐẦU ──→ (vòng lặp trên)         │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 5.4. Tại sao phải tiêu Power-Up để đua top?

Không dùng Power-Up thì vẫn chơi được bình thường. Nhưng:

- **Đối thủ có thể dùng** → Bạn bất lợi nếu không dùng.
- **50:50** tăng xác suất đúng từ 25% → 50% ở câu khó.
- **Thêm giờ** cứu mạng khi gặp câu dài/khó cần đọc kỹ.
- **Lá chắn** bảo vệ streak → ảnh hưởng UX (streak fire effect).

→ Ai chơi ranked cạnh tranh sẽ **tự nguyện** mua Power-Up. Không ép buộc nhưng khuyến khích mạnh.

### 5.5. Logic trong trận đấu (chi tiết kỹ thuật)

#### Luồng sử dụng 50:50:

```
1. Người chơi bấm nút [✂️ 50:50] trên Gameplay UI
2. PowerUpManager kiểm tra: playerData.powerUp_5050 > 0?
   ├── KHÔNG → Hiện toast "Hết power-up! Mua thêm tại Shop."
   └── CÓ → Tiếp tục:
3. powerUp_5050 -= 1
4. Lấy correctAnswerIndex từ QuizManager.CurrentQuestion
5. Chọn random 2 đáp án SAI trong 3 đáp án sai
6. Ẩn (disable) 2 nút đáp án đó trên UI
7. Đánh dấu: đã dùng 50:50 cho câu này (không cho dùng lại)
8. Fire event: OnPowerUpUsed("pu_5050")
```

#### Luồng sử dụng Thêm giờ:

```
1. Người chơi bấm nút [⏱️ +5s]
2. Kiểm tra powerUp_extraTime > 0
3. powerUp_extraTime -= 1
4. TimerController.Instance.RemainingTime += 5f
5. Fire event: OnPowerUpUsed("pu_time")
```

#### Luồng sử dụng Lá chắn:

```
1. Người chơi bấm nút [🛡️ Shield]
2. Kiểm tra powerUp_shield > 0
3. powerUp_shield -= 1
4. Đặt flag: _shieldActive = true
5. Khi ScoreManager.CheckAnswer() phát hiện trả lời SAI:
   ├── _shieldActive == true → KHÔNG reset streak, hủy flag
   └── _shieldActive == false → Reset streak như bình thường
6. Fire event: OnPowerUpUsed("pu_shield")
```

---

## 6. Shop UI (Tab "CỬA HÀNG" hiện có)

### 6.1. Hiện trạng

Trong `HomeLayout.uxml` (line 64-68), tab Shop đã có sẵn nhưng hiển thị "Coming Soon".

### 6.2. Thiết kế Shop mới (Text-based, không cần art)

```
┌─────────────────────────────────────────────┐
│  🛒 CỬA HÀNG                               │
│─────────────────────────────────────────────│
│                                             │
│  💰 Số dư: $1,250                           │
│                                             │
│  ┌─────────────────────────────────────┐    │
│  │  ✂️ 50:50                    150$   │    │
│  │  Loại bỏ 2 đáp án sai              │    │
│  │  Bạn có: 2 cái                     │    │
│  │              [MUA ×1] [MUA ×5 -10%] │    │
│  └─────────────────────────────────────┘    │
│                                             │
│  ┌─────────────────────────────────────┐    │
│  │  ⏱️ Thêm giờ (+5s)          100$   │    │
│  │  +5 giây cho câu hỏi hiện tại      │    │
│  │  Bạn có: 5 cái                     │    │
│  │              [MUA ×1] [MUA ×5 -10%] │    │
│  └─────────────────────────────────────┘    │
│                                             │
│  ┌─────────────────────────────────────┐    │
│  │  🛡️ Lá chắn                 200$   │    │
│  │  Giữ streak khi trả lời sai        │    │
│  │  Bạn có: 0 cái                     │    │
│  │              [MUA ×1] [MUA ×5 -10%] │    │
│  └─────────────────────────────────────┘    │
│                                             │
│  ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─  │
│  📦 GÓI ƯU ĐÃI                            │
│  ┌─────────────────────────────────────┐    │
│  │  🎁 Gói Chiến Binh           400$  │    │
│  │  1× 50:50 + 2× Thêm giờ + 1× Lá   │    │
│  │  chắn (Tiết kiệm 50$)              │    │
│  │                          [MUA NGAY] │    │
│  └─────────────────────────────────────┘    │
│                                             │
└─────────────────────────────────────────────┘
```

### 6.3. Gói combo (Bundle)

| Gói | Nội dung | Giá gốc | Giá gói | Tiết kiệm |
|---|---|---|---|---|
| **Gói Khởi Động** | 2× 50:50, 3× Thêm giờ | 600$ | 500$ | 17% |
| **Gói Chiến Binh** | 1× 50:50, 2× Thêm giờ, 1× Lá chắn | 550$ | 400$ | 27% |
| **Gói Vô Địch** | 3× 50:50, 3× Thêm giờ, 3× Lá chắn | 1,350$ | 1,000$ | 26% |

---

## 7. Daily Quests (Nhiệm vụ hàng ngày) — Source bổ sung

### 7.1. Danh sách Quests

| Quest | Mục tiêu | Phần thưởng |
|---|---|---|
| 🎮 Chơi 3 trận | Hoàn thành 3 trận (bất kỳ kết quả) | 50$ |
| 🏆 Thắng 1 trận | Thắng ít nhất 1 trận | 100$ |
| ✅ Đúng 15 câu | Trả lời đúng tổng cộng 15 câu | 75$ |
| ⭐ Perfect Round | 1 trận không sai câu nào | 200$ |

**Reset:** Mỗi ngày lúc 00:00 UTC.

---

## 8. Cân bằng kinh tế tổng quan

### 8.1. Thu nhập trung bình / ngày (10 trận, win rate 50%, Level 10)

| Nguồn | Tính toán | Tổng |
|---|---|---|
| 5 trận thắng × 100$ × 2.0 | | 1,000$ |
| 3 trận hòa × 40$ × 2.0 | | 240$ |
| 2 trận thua × 10$ × 2.0 | | 40$ |
| Daily Quests (3/4 hoàn thành) | | ~225$ |
| **Tổng thu nhập/ngày** | | **~1,505$** |

### 8.2. Chi tiêu (người chơi competitive)

| Hạng mục | Tính toán | Tổng |
|---|---|---|
| 10 trận × 1 power-up/trận (trung bình) | ~150$/lần | 1,500$ |

→ **Thu ≈ Chi** cho người competitive. Người casual tích lũy dần.

### 8.3. RP Progression (thời gian leo rank ước tính)

Giả sử win rate 55%, chơi 10 trận/ngày:

```
Mỗi trận thắng: +30 RP
Mỗi trận thua:  -15 RP

10 trận (5.5 thắng, 4.5 thua):
  Net RP = (5.5 × 30) - (4.5 × 15) = 165 - 67.5 = +97.5 RP/ngày
```

| Mốc | RP cần | Thời gian (từ 0) |
|---|---|---|
| 🥈 Silver (500 RP) | 500 | ~5 ngày |
| 🥇 Gold (1500 RP) | 1500 | ~15 ngày |
| 💎 Diamond (3000 RP) | 3000 | ~31 ngày (khó đạt trong 1 mùa với win rate 55%) |
| 👑 Legend (5000 RP) | 5000 | ~51 ngày (cần win rate > 60% hoặc > 10 trận/ngày) |

→ **Mùa 30 ngày:** Đa số người chơi đạt Gold – Diamond. Legend chỉ dành cho top players → hiếm và có giá trị.

---

## 9. Danh sách thay đổi Code

### 9.1. Files cần THÊM MỚI

| File | Mô tả |
|---|---|
| `Scripts/Core/PowerUpManager.cs` | Singleton quản lý mua/dùng power-up, expose events |
| `Scripts/Core/ShopManager.cs` | Logic mua hàng (kiểm tra đủ tiền, trừ tiền, thêm item) |
| `Scripts/Core/SeasonManager.cs` | Quản lý mùa: kiểm tra kết thúc mùa, reset, trao thưởng |
| `Scripts/Core/DailyQuestManager.cs` | Quản lý quest hàng ngày, reset, tracking |

### 9.2. Files cần SỬA ĐỔI

| File | Thay đổi |
|---|---|
| `Scripts/Data/PlayerData.cs` | Thêm: `powerUp_5050`, `powerUp_extraTime`, `powerUp_shield`, `currentTier`, `highestTierThisSeason`, `lastSeasonProcessed`, `seasonBadges`, daily quest fields |
| `Scripts/Core/PlayerDataManager.cs` | Thêm Save/Load cho các fields mới |
| `Scripts/Core/ScoreManager.cs` | RP không nhân Level Multiplier, shield logic, phần thưởng RP cố định |
| `Scripts/Network/FirebaseManager.cs` | `GetPlayerTier(rankPoints)` thay vì `GetPlayerTier(level)`, thêm seasonConfig, cloud schema mới |
| `Scripts/Network/LeaderboardManager.cs` | Query BXH theo tier trong mùa hiện tại, hiển thị countdown mùa |
| `Scripts/UI/GameplayUIController_UXML.cs` | Thêm 3 nút power-up vào gameplay |
| `Scripts/UI/MainMenuUIController_UXML.cs` | Shop functional + Season info trên UI + BXH theo tier |
| `UI/Layouts/HomeLayout.uxml` | Shop panel + Season countdown + Tier badge |
| `UI/Layouts/GameplayLayout.uxml` | Container cho 3 nút power-up |

### 9.3. Firebase Schema (hoàn chỉnh)

```
/seasonConfig:
  currentSeason: 1                          // Season ID hiện tại
  seasonStartDate: "2026-06-01T00:00:00Z"   // Ngày bắt đầu mùa
  seasonEndDate: "2026-07-01T00:00:00Z"     // Ngày kết thúc mùa
  seasonDurationDays: 30                    // Thời lượng mùa (ngày)

/users/{uid}:
  ... (fields hiện có giữ nguyên)
  rankPoints: 890                           // RP mùa hiện tại
  currentTier: 2                            // Tier hiện tại (tính từ RP)
  highestTierThisSeason: 3                  // Tier cao nhất mùa này (để tính thưởng)
  lastSeasonProcessed: 0                    // Season ID cuối cùng đã xử lý reset
  seasonBadges: "💎S1,🥇S2"                // Danh hiệu các mùa trước
  powerUp_5050: int                         // Số lượng 50:50 còn lại
  powerUp_extraTime: int                    // Số lượng +5s còn lại
  powerUp_shield: int                       // Số lượng lá chắn còn lại
  dailyQuestsData: "{...}"                  // JSON quest tracking

/seasonArchive/season_{N}/{uid}:            // Lưu trữ kết quả mùa cũ
  finalTier: 4
  finalRP: 4200
  highestTier: 5
  rewardsClaimed: true
```

---

## 10. Quyết định thiết kế (ĐÃ CHỐT ✅)

| # | Câu hỏi | Quyết định | Lý do |
|---|---|---|---|
| Q1 | Reset RP cuối mùa? | ✅ **CÓ reset** (Hardcore) | Giáng 2 tier + reset RP về mốc tier mới. Tạo cuộc đua mới mỗi mùa. |
| Q2 | Power-Up trong PvP Online? | ✅ **Cho phép** (cả Online + Offline) | Giới hạn 1 lần/loại/trận. Tiền kiếm từ skill → không phải pay-to-win. |
| Q3 | Giá Power-Up theo Tier? | ✅ **Giá cố định** | 50:50 = 150$, Thêm giờ = 100$, Lá chắn = 200$. Tier cao thưởng nhiều hơn → tự nhiên thoải mái hơn. |
| Q4 | Mua Power-Up ở đâu? | ✅ **Chỉ ở Shop** (MainMenu) | Mua trước, dùng trong trận. UX đơn giản, buộc chuẩn bị trước khi vào trận. |

### Giáng Tier trong mùa (in-season)

Khi RP rơi xuống dưới ngưỡng tier (ví dụ Silver 500 RP → 499 RP) → **Giáng ngay**, không có grace period. Phong cách **hardcore** — phù hợp với định hướng competitive của game.

---

## 11. Thứ tự triển khai đề xuất

### Phase 1: Nền tảng dữ liệu
1. Mở rộng `PlayerData.cs` — thêm fields power-up + tier + season
2. Cập nhật `PlayerDataManager.cs` — save/load fields mới
3. Sửa `FirebaseManager.cs` — `GetPlayerTier(rankPoints)`, cloud schema mới
4. Sửa `ScoreManager.cs` — RP không nhân multiplier

### Phase 2: Power-Up System
5. Tạo `PowerUpManager.cs` — logic sử dụng 3 loại
6. Tạo `ShopManager.cs` — logic mua hàng
7. Cập nhật `GameplayLayout.uxml` + `GameplayUIController_UXML.cs` — UI power-up
8. Cập nhật `ScoreManager.cs` — Shield logic

### Phase 3: Shop UI
9. Cập nhật `HomeLayout.uxml` — shop-panel functional
10. Cập nhật `MainMenuUIController_UXML.cs` — shop logic + mua hàng

### Phase 4: Seasonal Rank
11. Tạo `SeasonManager.cs` — logic mùa, reset, thưởng
12. Cập nhật `LeaderboardManager.cs` — BXH theo tier + mùa
13. Cập nhật UI — countdown mùa, tier badge, popup kết thúc mùa

### Phase 5: Daily Quests (bổ sung sau)
14. Tạo `DailyQuestManager.cs`
15. Thêm UI quest vào HomeScene

---

*Tài liệu này là bản thiết kế sống (living document). Cập nhật khi có thay đổi thiết kế.*
