# 📋 Game Design Document — Phase 3: Rogue-like Tower

> **Phiên bản:** 1.0  
> **Ngày tạo:** 2026-06-18  
> **Tác giả:** Solo Dev  
> **Trọng tâm:** Chuyển đổi Core Gameplay sang Rogue-like PvE + Tái định vị PvP Ranked

---

## 1. Tổng quan & Lý do chuyển đổi

### 1.1. Vấn đề của mô hình hiện tại

| Vấn đề | Chi tiết |
|---|---|
| **Tỉ lệ chuyển hóa thấp** | Chế độ PvP Online là core gameplay nhưng trải nghiệm "chơi xong 1 trận → thoát" không đủ hấp dẫn để giữ chân người mới |
| **Phụ thuộc CCU** | PvP yêu cầu lượng người chơi cùng lúc lớn để ghép trận nhanh. Khi CCU thấp → chờ lâu → người chơi bỏ đi |
| **Thiếu chiều sâu** | Gameplay "trả lời 10–30 câu hỏi multiple choice" lặp đi lặp lại, không có yếu tố chiến lược hay tiến trình rõ ràng |
| **Không có long-term goal** | Sau khi đạt rank cao, người chơi không có lý do quay lại (ngoài reset mùa) |

### 1.2. Giải pháp: Chuyển dịch Core Gameplay

```
TRƯỚC (Phase 1–2):
  Core = PvP Online (đấu với người thật)
  Phụ  = Đấu với máy (Practice, không có phần thưởng đáng kể)

SAU (Phase 3+):
  Core = Rogue-like Tower (PvE, leo tháp, đánh Boss, chết là mất hết)
  Phụ  = PvP Ranked Online (đua rank leaderboard, nơi duy nhất tính Rank Points)
```

**Tại sao Rogue-like phù hợp?**
- ✅ Không cần CCU cao — Chơi offline/PvE 100%
- ✅ Tạo cảm giác "thêm 1 lượt nữa" (addictive loop) — Sấp ngửa giữa tiến xa và chết mất hết
- ✅ Mỗi lượt chơi (run) đều khác nhau — Random power-up, random nhánh, random boss
- ✅ Có meta-progression — Dù thua vẫn tiến bộ ở bên ngoài → không bao giờ "chơi phí"
- ✅ Phù hợp mobile — Session ngắn (5–15 phút/run), dễ pick up & play

---

## 2. Core Gameplay: Rogue-like Tower

### 2.1. Khái niệm

Người chơi **leo một tòa tháp** gồm nhiều tầng (floor). Mỗi tầng là một thử thách câu hỏi với độ khó tăng dần. Xen kẽ là các **trận đánh Boss**, **phòng nghỉ**, **shop**, và **sự kiện ngẫu nhiên**. 

**Quy tắc vàng:** _Chết = Mất hết tiến trình trong lượt chơi đó. Quay lại tầng 1._

### 2.2. Cấu trúc một lượt chơi (Run)

Một run bao gồm nhiều **Tầng (Floor)**, chia thành các **Khu vực (Zone)**:

```
ZONE 1 — "Chân Tháp" (Floor 1–5)       ⭐ Dễ
  │
  ├── Floor 1: Câu hỏi thường (Dễ)
  ├── Floor 2: Phòng Sự kiện ĐẶC BIỆT (Random)
  ├── Floor 3: Câu hỏi thường (Dễ–Trung bình)
  ├── Floor 4: Phòng Shop / Nghỉ ngơi
  └── Floor 5: ⚔️ MINI-BOSS
  │
ZONE 2 — "Thân Tháp" (Floor 6–10)      ⭐⭐ Trung bình
  │
  ├── Floor 6–9: (Tương tự cấu trúc Zone 1, câu hỏi khó hơn)
  └── Floor 10: ⚔️ BOSS
  │
ZONE 3 — "Đỉnh Tháp" (Floor 11–15)     ⭐⭐⭐ Khó
  │
  ├── Floor 11–14: (Câu hỏi cực khó, sự kiện hiếm)
  └── Floor 15: ⚔️ FINAL BOSS
  │
  🏆 CHINH PHỤC THÁP — Nhận phần thưởng lớn!
```

### 2.3. Bản đồ phân nhánh (Slay the Spire style)

Thay vì đi thẳng từng tầng, người chơi **chọn đường đi** trên một bản đồ nhánh:

```
     [START]
      / | \
    🎯  💰  ❓
    /  \  |   \
  🎯  ⚔️ 💰  🎯
   \  /   |  /
    🎯   🏪
     \   /
     [BOSS]

Chú thích:
  🎯 = Phòng Câu hỏi (Quiz Room) — Trả lời đúng để tiến tiếp
  💰 = Phòng Rương (Treasure) — Nhận Gold hoặc Power-up ngẫu nhiên
  ❓ = Phòng Sự kiện (Event) — Sự kiện ngẫu nhiên (rủi ro + phần thưởng)
  🏪 = Shop — Mua power-up bằng Gold kiếm được trong run
  ⚔️ = Phòng Elite (Mini-boss mạnh, thưởng lớn)
  🛏 = Phòng Nghỉ (Rest) — Hồi HP
```

**Chiến lược nhánh:**
- **Đường an toàn:** Quiz → Rest → Shop → Boss (ít rủi ro, ít thưởng)
- **Đường tham lam:** Treasure → Elite → Event → Boss (nhiều thưởng, nhiều rủi ro)
- Người chơi tự quyết định đường đi dựa trên HP và power-up hiện có

### 2.4. Hệ thống HP (Health Points)

Người chơi bắt đầu mỗi run với một lượng HP cố định:

| Thông số | Giá trị mặc định | Ghi chú |
|---|---|---|
| **HP khởi điểm** | 3 ❤️ | Có thể nâng bằng Meta-Progression |
| **HP tối đa** | 5 ❤️ | Giới hạn cứng |
| **Mất HP khi** | Trả lời sai 1 câu | -1 ❤️ |
| **Hồi HP** | Phòng Nghỉ, Rương hiếm, Sự kiện đặc biệt | Không tự hồi |
| **Chết (0 HP)** | Kết thúc run, mất hết tiến trình trong lượt | Giữ lại Meta-Exp |

**Cách mất HP trong phòng Quiz:**
- Mỗi phòng Quiz có 3–5 câu hỏi (tùy Zone)
- Trả lời **sai** → -1 HP
- Trả lời **hết giờ** → -1 HP (coi như sai)
- Trả lời **đúng** → Không mất HP, nhận Gold + Combo

### 2.5. Tiền tệ trong Run (Run Gold)

| Tiền tệ | Phạm vi | Mô tả |
|---|---|---|
| **🪙 Run Gold** | Chỉ trong 1 run | Kiếm từ trả lời đúng, rương, sự kiện. Dùng để mua power-up tại Shop trong run. **Mất khi chết.** |
| **💎 Meta Crystals** | Vĩnh viễn | Kiếm sau mỗi run (dù thắng hay thua). Dùng để nâng cấp Meta-Progression bên ngoài. **Không mất khi chết.** |

---

## 3. Hệ thống Boss

### 3.1. Triết lý thiết kế Boss

Boss **KHÔNG** chỉ đơn giản là "trả lời nhanh hơn máy" hay "đạt mốc điểm". Mỗi Boss phải có **Gimmick riêng** — một cơ chế đặc biệt thay đổi cách người chơi tương tác với câu hỏi.

### 3.2. Cơ chế đánh Boss

```
┌─────────────────────────────────────────────┐
│  ⚔️ BOSS BATTLE — "Giáo Sư Hỗn Loạn"       │
│─────────────────────────────────────────────│
│                                             │
│  Boss HP: ████████████░░░░░░  65/100        │
│  Player HP: ❤️❤️❤️                           │
│                                             │
│  [Câu hỏi hiện ra + Gimmick của Boss]       │
│                                             │
│  Trả lời ĐÚNG  → Boss mất HP               │
│  Trả lời SAI   → Player mất HP             │
│  Combo streak   → Boss mất x2 HP           │
│                                             │
│  Mục tiêu: Hạ Boss HP về 0 trước khi       │
│            Player HP về 0                   │
└─────────────────────────────────────────────┘
```

**Công thức sát thương lên Boss:**
- Trả lời đúng: Boss -10 HP (base)
- Combo 3+: Boss -15 HP
- Combo 5+: Boss -20 HP
- Combo 10+: Boss -30 HP
- Trả lời sai: Player -1 ❤️, reset Combo

### 3.3. Danh sách Boss & Gimmick

#### 🔹 Zone 1 — Mini-Boss (Floor 5)

| Boss | Tên | HP | Gimmick |
|---|---|---|---|
| 🤖 | **Bot Học Việt** | 50 HP | **Không có gimmick đặc biệt.** Boss đầu tiên, giới thiệu cơ chế đánh Boss cho người chơi. Câu hỏi cấp độ Dễ. |

#### 🔹 Zone 2 — Boss (Floor 10)

| Boss | Tên | HP | Gimmick |
|---|---|---|---|
| 👻 | **Giáo Sư Ảo Ảnh** | 80 HP | **Che đáp án:** 1 trong 4 đáp án bị làm mờ/ẩn một phần text. Người chơi phải suy luận từ các ký tự còn hiện. Mỗi 3 câu, Boss "mở khóa" 1 đáp án bị che → phần thưởng cho việc cầm cự. |
| ⏱️ | **Vua Thời Gian** | 80 HP | **Đếm ngược siêu nhanh:** Thay vì 10–15 giây/câu bình thường, Boss đặt timer chỉ **5 giây/câu**. Đòi hỏi phản xạ cực nhanh. Power-up "Thêm giờ" có giá trị rất lớn ở trận này. |

#### 🔹 Zone 3 — Final Boss (Floor 15)

| Boss | Tên | HP | Gimmick |
|---|---|---|---|
| 🌀 | **Giáo Sư Hỗn Loạn** | 120 HP | **Xáo trộn đáp án:** Vị trí của 4 nút đáp án (A, B, C, D) tự động **hoán đổi mỗi 2 giây**. Người chơi phải tập trung đọc text chứ không quen vị trí nút. Cực kỳ stress. |
| 🔄 | **Vua Nghịch Lý** | 100 HP | **Luật đảo ngược:** Người chơi phải chọn đáp án **SAI** để gây sát thương cho Boss. Chọn đáp án Đúng sẽ bị mất HP. Đòi hỏi tư duy ngược hoàn toàn. Sau mỗi 5 câu, Boss "switch" lại luật bình thường 2 câu rồi lại đảo → buộc người chơi chú ý indicator. |

### 3.4. Boss Scaling (Khó hơn theo số lần chinh phục)

Mỗi lần người chơi **chinh phục thành công tháp** (hoàn thành Floor 15), lần chơi tiếp theo tháp sẽ khó hơn:

| Lần chinh phục | Modifier |
|---|---|
| Lần 1 (lần đầu) | Base difficulty |
| Lần 2 | Boss HP +20%, câu hỏi khó hơn 1 bậc |
| Lần 3 | Boss HP +40%, thêm Gimmick phụ cho mỗi Boss |
| Lần 4+ | Boss HP +60%, câu hỏi cực khó, timer giảm 2s toàn run |

→ Tạo **Endless Replayability** — Không bao giờ "phá đảo hết game".

---

## 4. Hệ thống Power-up (Rework cho Rogue-like)

### 4.1. Thay đổi so với Phase 2

| Phase 2 (Hiện tại) | Phase 3 (Rogue-like) |
|---|---|
| Power-up mua ở Shop ngoài, dùng hết là hết | Power-up kiếm được **trong run** (Shop tầng, Rương, Sự kiện) |
| Giới hạn 1 lần/loại/trận (1 trận = 10–30 câu) | Sử dụng **không giới hạn** trong run, nhưng mỗi câu hỏi chỉ áp dụng **1 power-up duy nhất mỗi loại** |
| Mua bằng Money ($) | Trong run: mua bằng Run Gold 🪙. Ngoài run: vẫn mua bằng Money ($) để mang vào run |

### 4.2. Quy tắc Power-up trong Run

```
QUY TẮC CỐT LÕI:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. Power-up được mang vào từ inventory BÊN NGOÀI 
   + kiếm thêm TRONG RUN (Shop, Rương, Sự kiện)

2. Số lượng power-up dùng không giới hạn trong suốt run
   (cho đến khi hết)

3. Mỗi CÂU HỎI chỉ được áp dụng TỐI ĐA 1 POWER-UP 
   MỖI LOẠI:
   ✅ Câu 1: dùng 50:50 
   ✅ Câu 1: dùng 50:50 + Thêm giờ (khác loại → OK)
   ❌ Câu 1: dùng 50:50 hai lần (cùng loại → KHÔNG)

4. Power-up chưa dùng hết khi run kết thúc (thắng hoặc
   thua) → GIỮ LẠI trong inventory
   
5. Power-up kiếm được TRONG RUN mà chưa dùng → 
   ĐƯỢC GIỮ vào inventory sau run
```

### 4.3. Danh sách Power-up

| ID | Tên | Emoji | Giá (Run Gold) | Giá (Money — Shop ngoài) | Hiệu ứng |
|---|---|---|---|---|---|
| `pu_5050` | **50:50** | ✂️ | 30🪙 | 150$ | Loại bỏ 2 đáp án sai, còn 2 lựa chọn |
| `pu_time` | **Thêm giờ** | ⏱️ | 20🪙 | 100$ | +5 giây cho câu hỏi hiện tại |
| `pu_shield` | **Lá chắn** | 🛡️ | 40🪙 | 200$ | Trả lời sai không mất HP (1 lần) |
| `pu_heal` | **Hồi máu** | 💊 | 60🪙 | — (chỉ kiếm trong run) | Hồi 1 ❤️ HP |
| `pu_double` | **x2 Damage** | ⚔️ | 50🪙 | — (chỉ kiếm trong run) | Câu hỏi này gây x2 sát thương lên Boss |

> **Power-up mới (chỉ có trong Rogue-like):** `pu_heal` và `pu_double` KHÔNG bán ở Shop ngoài. Chỉ kiếm được trong run → tạo thêm giá trị cho việc khám phá và chọn nhánh.

---

## 5. Hệ thống Relic (Thánh Tích — Buff bị động)

### 5.1. Khái niệm

Relic là những vật phẩm **bị động** (passive) mà người chơi nhận được trong run. Khác với Power-up (dùng chủ động, tiêu hao), Relic **hoạt động suốt run** một khi đã nhặt.

Mỗi run, người chơi có thể sở hữu **tối đa 5 Relic**. Khi run kết thúc (thắng hoặc thua), tất cả Relic **bị mất**.

### 5.2. Nguồn nhận Relic

- Đánh thắng **Elite / Mini-boss** (chắc chắn drop)
- Phòng **Sự kiện đặc biệt** (% ngẫu nhiên)
- Phòng **Rương hiếm** (% thấp)

### 5.3. Danh sách Relic (MVP — Bộ đầu tiên)

| Relic | Tên | Emoji | Rarity | Hiệu ứng |
|---|---|---|---|---|
| R01 | **Cúp Học Giả** | 🏆 | Common | Mỗi **5 câu đúng liên tiếp** → Hồi 1 ❤️ HP |
| R02 | **Kính Viễn Vọng** | 🔭 | Common | Các phòng Sự kiện trên bản đồ hiện **tỷ lệ rủi ro/thưởng** trước khi chọn |
| R03 | **Đồng Hồ Cát** | ⏳ | Uncommon | +3 giây timer cho TẤT CẢ câu hỏi trong run |
| R04 | **Sách Cổ** | 📖 | Uncommon | Sau mỗi trận Boss, nhận thêm **1 power-up ngẫu nhiên** |
| R05 | **Lưỡi Hái Kiến Thức** | ⚔️ | Rare | Combo damage tăng **gấp đôi** (Combo 3 = -30 HP thay vì -15) |
| R06 | **Giáp Phản Đòn** | 🪞 | Rare | Khi trả lời sai, có **30% cơ hội** không mất HP |
| R07 | **Ngọc Tham Lam** | 💎 | Rare | Run Gold kiếm được tăng **+50%**, nhưng HP tối đa giảm 1 ❤️ |

### 5.4. Chọn Relic

Khi nhận Relic, người chơi được **chọn 1 trong 3** Relic ngẫu nhiên (giống Slay the Spire):

```
┌─────────────────────────────────────────────┐
│  🎁 CHỌN THÁNH TÍCH (1/3)                   │
│─────────────────────────────────────────────│
│                                             │
│  ┌───────────┐ ┌───────────┐ ┌───────────┐ │
│  │   🏆      │ │   ⏳      │ │   💎      │ │
│  │ Cúp Học   │ │ Đồng Hồ  │ │ Ngọc Tham │ │
│  │ Giả       │ │ Cát       │ │ Lam       │ │
│  │           │ │           │ │           │ │
│  │ 5 combo   │ │ +3s mỗi  │ │ +50% Gold │ │
│  │ = +1 HP   │ │ câu hỏi  │ │ -1 HP max │ │
│  │           │ │           │ │           │ │
│  │  [CHỌN]   │ │  [CHỌN]  │ │  [CHỌN]  │ │
│  └───────────┘ └───────────┘ └───────────┘ │
│                                             │
│              [BỎ QUA]                       │
└─────────────────────────────────────────────┘
```

---

## 6. Meta-Progression (Nâng cấp vĩnh viễn)

### 6.1. Khái niệm

Đây là lớp tiến trình **bên ngoài run**. Dù người chơi chết ở Floor 2 hay chinh phục Floor 15, họ luôn nhận được **💎 Meta Crystals** để nâng cấp các chỉ số vĩnh viễn.

→ Mỗi run đều có ý nghĩa. Không bao giờ "chơi phí".

### 6.2. Nguồn Meta Crystals

| Nguồn | Số lượng |
|---|---|
| Mỗi Floor vượt qua | +5 💎 |
| Đánh thắng Mini-boss | +20 💎 |
| Đánh thắng Boss | +50 💎 |
| Đánh thắng Final Boss | +100 💎 |
| Bonus hoàn thành run (không chết) | +50 💎 |
| Streak câu đúng (mỗi 10 câu liên tiếp) | +10 💎 |

### 6.3. Cây nâng cấp Meta

| ID | Nâng cấp | Max Level | Chi phí mỗi cấp | Hiệu ứng |
|---|---|---|---|---|
| M01 | **Sức khỏe khởi đầu** | 2 | 100 / 250 💎 | +1 ❤️ HP lúc bắt đầu run (3 → 4 → 5) |
| M02 | **Giảm giá Shop** | 3 | 80 / 150 / 300 💎 | -10% / -20% / -30% giá mua tại Shop trong run |
| M03 | **Rương hào phóng** | 3 | 60 / 120 / 200 💎 | Phòng Rương cho thêm +1 / +2 / +3 Run Gold |
| M04 | **Tỉ lệ Relic xịn** | 2 | 150 / 350 💎 | Tăng tỉ lệ Relic Rare xuất hiện khi chọn (+15% / +30%) |
| M05 | **Túi đồ mở rộng** | 2 | 120 / 280 💎 | Mang thêm +1 / +2 power-up vào đầu run (từ inventory ngoài) |
| M06 | **Combo Master** | 2 | 100 / 250 💎 | Combo bắt đầu tính từ câu đúng thứ 2 (thay vì 3) / thứ 1 |
| M07 | **Mắt Thần** | 1 | 200 💎 | Hiện rõ loại phòng trên bản đồ trước khi chọn nhánh |

### 6.4. Tiến trình Meta ước tính

```
Run trung bình (chết ở Floor 8–10):
  8 Floor × 5 + 1 Mini-boss × 20 + 1 Boss × 50 = 110 💎

Run hoàn hảo (chinh phục Floor 15, không chết):
  15 Floor × 5 + 1 Mini-boss × 20 + 2 Boss × 50 + 1 Final × 100 + Bonus 50 = 345 💎

→ Để max hết cây Meta (tổng ~3,230 💎):
  - Người chơi trung bình: ~30 run
  - Người chơi giỏi: ~15 run
```

---

## 7. Cải tiến Hệ thống Câu hỏi

### 7.1. Đa dạng hình thức câu hỏi

Để gameplay không bị lặp lại nhàm chán với dạng Multiple Choice truyền thống:

| Loại | Mô tả | Ví dụ | Zone xuất hiện |
|---|---|---|---|
| **Multiple Choice** (Trắc nghiệm) | Chọn 1 đáp án đúng trong 4 | Thủ đô Việt Nam? A. HCM B. Hà Nội C. Đà Nẵng D. Huế | Tất cả |
| **True/False** (Đúng/Sai) | Xác định phát biểu đúng hay sai | "Mặt trời quay quanh Trái đất" → Đúng / Sai | Tất cả |
| **Sắp xếp** (Ordering) | Sắp xếp 4 mục theo thứ tự | Sắp xếp: Sinh → Lão → Bệnh → Tử | Zone 2+ |
| **Nối cặp** (Matching) | Nối 3–4 cặp tương ứng | Nối: Việt Nam–Hà Nội, Thái Lan–Bangkok, Lào–Viêng Chăn | Zone 2+ |
| **Điền từ** (Fill-in) | Gõ đáp án ngắn (1–2 từ) | "Nguyên tố hóa học ký hiệu Fe là ___?" → Sắt | Zone 3 (Boss) |

### 7.2. Hệ thống Combo

```
COMBO SYSTEM:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  Trả lời đúng liên tiếp → Combo tăng dần
  Trả lời sai hoặc hết giờ → Combo reset về 0

  Combo 0–2:   ×1 (Base damage / Gold)
  Combo 3–4:   ×1.5
  Combo 5–9:   ×2.0
  Combo 10+:   ×3.0

  Hiệu ứng visual: 
    Combo 3+  → Viền câu hỏi phát sáng vàng
    Combo 5+  → Hiệu ứng lửa xung quanh avatar
    Combo 10+ → Nền chuyển sang tone vàng rực + particle
```

### 7.3. Hệ thống Nguyên tố Câu hỏi (Question Elements)

Phân loại câu hỏi thành các **nguyên tố** tạo thêm chiều sâu chiến thuật:

| Nguyên tố | Emoji | Chủ đề |
|---|---|---|
| 🔥 **Hỏa** (Fire) | 🔥 | Lịch sử, Chiến tranh |
| 🌊 **Thủy** (Water) | 🌊 | Địa lý, Thiên nhiên |
| ⚡ **Lôi** (Thunder) | ⚡ | Khoa học, Công nghệ |
| 🌿 **Mộc** (Wood) | 🌿 | Văn học, Nghệ thuật |
| 🪨 **Thổ** (Earth) | 🪨 | Đời sống, Thể thao |

**Tương tác với Boss:**
- Mỗi Boss có **điểm yếu** với 1 nguyên tố cụ thể
- Trả lời đúng câu hỏi thuộc nguyên tố khắc chế Boss → **x2 sát thương**
- Trên bản đồ nhánh, mỗi phòng Quiz hiển thị nguyên tố chủ đạo → Người chơi biết Boss yếu Thủy sẽ cố chọn nhánh có phòng Quiz Thủy

| Boss | Yếu tố bị khắc chế |
|---|---|
| 🤖 Bot Học Việt | Không có (Boss tutorial) |
| 👻 Giáo Sư Ảo Ảnh | 🌿 Mộc (Văn học, Nghệ thuật) |
| ⏱️ Vua Thời Gian | 🪨 Thổ (Đời sống — câu hỏi quen thuộc trả lời nhanh) |
| 🌀 Giáo Sư Hỗn Loạn | ⚡ Lôi (Khoa học — câu hỏi logic, ít bị lừa bởi vị trí) |
| 🔄 Vua Nghịch Lý | 🔥 Hỏa (Lịch sử — dễ nhận biết đáp án sai) |

---

## 8. Phòng Sự kiện (Event Room)

### 8.1. Danh sách sự kiện ngẫu nhiên

Khi vào phòng Sự kiện (❓), người chơi gặp một tình huống ngẫu nhiên và phải đưa ra lựa chọn:

| Sự kiện | Lựa chọn A | Lựa chọn B |
|---|---|---|
| **📚 Thư viện bí ẩn** | Đọc sách: Nhận 1 Relic ngẫu nhiên | Bỏ qua: Không xảy ra gì |
| **🎰 Bánh xe vận mệnh** | Quay: 50% nhận 50🪙, 50% mất 30🪙 | Bỏ đi: An toàn |
| **🧙 Nhà Hiền Triết** | Trả lời 1 câu cực khó: Đúng → +2 HP, Sai → -1 HP | Từ chối: Không xảy ra gì |
| **⚗️ Phòng thí nghiệm** | Uống thuốc: 50% tăng HP max +1, 50% giảm HP max -1 | Bỏ qua |
| **🗿 Bàn thờ cổ** | Hiến tế 1 power-up bất kỳ → Nhận 1 Relic Rare | Giữ đồ |
| **💀 Thách đấu ma** | Đấu mini-game 3 câu: Thắng → 80🪙, Thua → -1 HP | Bỏ đi |

---

## 9. Tái định vị PvP Ranked Online

### 9.1. Vị trí mới của PvP Ranked

```
TRƯỚC:
  PvP Online = Core Gameplay (tính năng chính)
  
SAU:
  PvP Online = Tính năng PHỤ (End-game content)
  → Nơi DUY NHẤT được tính Rank Points (RP)
  → Leo leaderboard mùa
  → Dành cho người chơi muốn thử thách thêm
```

### 9.2. Thay đổi Matchmaking

**Bỏ thông báo "Chuyển sang đấu với Bot":**

| Trước | Sau |
|---|---|
| Ghép trận > 15s → Hiện popup "Không tìm thấy đối thủ. Chuyển sang đấu với Bot?" | Ghép trận > 10s → Âm thầm ghép với **Ghost Record** (xem mục 9.3). Người chơi KHÔNG biết là đang đánh bot. |

### 9.3. Ghost Record System (Fake Multiplayer)

Thay vì sử dụng bot random, hệ thống sẽ **ghi lại hành vi của người chơi thật** và dùng lại cho các trận giả:

```
GHI LẠI (RECORD):
━━━━━━━━━━━━━━━━━━━━
Mỗi trận PvP Online, hệ thống âm thầm ghi lại:
- Thời gian suy nghĩ mỗi câu (think_time_ms)
- Đáp án đã chọn (answer_index)
- Kết quả đúng/sai (is_correct)
- Power-up đã dùng (power_up_used)

Lưu vào Firebase:
  /ghostRecords/{tier}/{recordId}:
    playerName: "PlayerA"
    avatarIndex: 3
    tier: 2
    answers: [
      { think_time: 3200, answer: 2, correct: true, powerUp: null },
      { think_time: 8100, answer: 0, correct: false, powerUp: "pu_5050" },
      ...
    ]

PHÁT LẠI (REPLAY):
━━━━━━━━━━━━━━━━━━━━
Khi ghép trận thất bại (timeout > 10s):
1. Lấy 1 Ghost Record ngẫu nhiên cùng Tier
2. Hiển thị tên + avatar của người chơi gốc
3. Phát lại hành vi: đợi đúng think_time rồi submit answer
4. Người chơi thấy: "Đối thủ" suy nghĩ giống người thật, 
   đôi khi đúng đôi khi sai, đôi khi dùng power-up

→ Trải nghiệm 100% GIỐNG đấu với người thật.
```

### 9.4. Tích hợp với Rogue-like

| Chế độ | Kiếm được | Không kiếm được |
|---|---|---|
| **Rogue-like Tower** | Meta Crystals 💎, Run Gold 🪙, Money ($), EXP, Power-up, Relic | Rank Points (RP) |
| **PvP Ranked** | Money ($), EXP, **Rank Points (RP)** | Meta Crystals, Relic |

→ Hai chế độ bổ trợ nhau: Rogue-like để mạnh hơn (meta-progression), PvP để đua top.

---

## 10. Vòng lặp tổng thể (Game Loop)

```
┌──────────────────────────────────────────────────────────────────────┐
│                          GAME LOOP TỔNG THỂ                         │
│                                                                      │
│   ┌─────────────┐                                                    │
│   │  HOME SCENE │                                                    │
│   │             │                                                    │
│   │ [LEO THÁP] [PVP RANKED] [SHOP] [LEADERBOARD] [META UPGRADE]    │
│   └─────┬───────────┬──────────────────────────────────┬────────────┘
│         │           │                                  │             │
│         ▼           ▼                                  ▼             │
│   ┌───────────┐ ┌───────────┐                    ┌───────────┐      │
│   │ ROGUE-LIKE│ │ PVP MATCH │                    │   META    │      │
│   │  TOWER    │ │           │                    │  UPGRADE  │      │
│   │           │ │ Rank Points│                    │           │      │
│   │ 💎 Meta   │ │ Money ($) │                    │ 💎 → Buff │      │
│   │ 🪙 Gold  │ │ EXP       │                    │ vĩnh viễn │      │
│   │ $ Money   │ │           │                    │           │      │
│   │ Power-up  │ │           │                    │           │      │
│   │ Relic     │ │           │                    │           │      │
│   └─────┬─────┘ └─────┬─────┘                    └───────────┘      │
│         │             │                                              │
│         ▼             ▼                                              │
│   ┌───────────────────────────────┐                                  │
│   │        RESULT SCREEN          │                                  │
│   │  Hiển thị phần thưởng        │                                  │
│   │  [CHƠI LẠI]  [VỀ SẢNH]      │                                  │
│   └──────────────┬────────────────┘                                  │
│                  │                                                    │
│                  └──────────── Quay lại HOME SCENE ──────────────────┘
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 11. Flow UI — Một lượt Rogue-like Run

```
[HOME] 
   → Bấm "Leo Tháp"
   → [MAP SCREEN] Hiện bản đồ nhánh Zone 1
   → Chọn phòng (Quiz / Treasure / Event / Shop / Rest)
   
   → Nếu QUIZ ROOM:
      → [GAMEPLAY SCENE] 3–5 câu hỏi
      → Trả lời đúng: +Gold, +Combo
      → Trả lời sai: -1 HP
      → Hết câu → Quay lại [MAP SCREEN]
   
   → Nếu BOSS ROOM:
      → [BOSS BATTLE SCENE] 
      → Hiển thị Boss HP + Gimmick
      → Trả lời đúng = gây damage
      → Trả lời sai = mất HP
      → Thắng Boss → Chuyển Zone mới
      → Thua Boss → RUN OVER
   
   → Nếu SHOP:
      → [SHOP OVERLAY] Mua power-up bằng Run Gold
      → Quay lại [MAP SCREEN]
   
   → Nếu REST:
      → Hồi 1 HP
      → Quay lại [MAP SCREEN]
   
   → Nếu EVENT:
      → [EVENT POPUP] Hiện tình huống + 2 lựa chọn
      → Kết quả → Quay lại [MAP SCREEN]
   
   → HP = 0:
      → [RUN OVER SCREEN]
      → Hiện: Floor đạt được, Meta Crystals kiếm được
      → [THỬ LẠI] [VỀ SẢNH]
   
   → Chinh phục Floor 15:
      → [VICTORY SCREEN] 🏆
      → Phần thưởng lớn
      → [CHƠI LẠI (KHÓ HƠN)] [VỀ SẢNH]
```

---

## 12. Monetization (Kiếm tiền — Tương lai)

> Phase 3 chưa triển khai IAP. Nhưng thiết kế sẵn hook:

| Hạng mục | Mô tả | Giá (dự kiến) |
|---|---|---|
| **Revival** (Hồi sinh) | Khi chết, cho phép tiếp tục run 1 lần (xem quảng cáo hoặc mua) | Xem 1 ads / 50 gems |
| **Season Pass** | Nhận thêm phần thưởng mùa, skin Boss, exclusive Relic | $2.99/mùa |
| **Meta Crystal Pack** | Mua 💎 để nâng cấp nhanh hơn | $0.99 = 500💎 |

---

## 13. Quyết định thiết kế (ĐÃ CHỐT ✅)

| # | Câu hỏi | Quyết định | Lý do |
|---|---|---|---|
| Q1 | Core gameplay mới? | ✅ **Rogue-like Tower (PvE)** | Không phụ thuộc CCU, addictive loop, meta-progression |
| Q2 | Chết trong run? | ✅ **Mất hết tiến trình run, giữ Meta Crystals** | Tạo căng thẳng (risk) nhưng vẫn có tiến bộ (meta) |
| Q3 | Power-up trong Rogue-like? | ✅ **Không giới hạn dùng, 1 loại/câu** | Khuyến khích sưu tập và sử dụng chiến thuật |
| Q4 | PvP Ranked còn tồn tại? | ✅ **CÓ, nhưng là tính năng PHỤ** | Nơi duy nhất tính RP, end-game content |
| Q5 | Ghost Record cho PvP? | ✅ **CÓ** | Giải quyết vấn đề CCU thấp, trải nghiệm giống người thật |
| Q6 | Bản đồ nhánh? | ✅ **CÓ (Slay the Spire style)** | Tạo chiến lược, replayability, mỗi run đều khác |
| Q7 | Hệ thống Relic? | ✅ **CÓ** | Thêm chiều sâu build, synergy, risk/reward |
| Q8 | Nguyên tố câu hỏi? | ✅ **CÓ** | Tạo chiến thuật chọn nhánh khắc chế Boss |

---

*Tài liệu này là bản thiết kế sống (living document). Cập nhật khi có thay đổi thiết kế.*
