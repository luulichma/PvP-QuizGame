# PHÂN TÍCH & FIX BUG DO AI KHÁC GÂY RA

> **Ngày tạo:** 25/05/2026  
> **Mục tiêu:** Liệt kê toàn bộ lỗi mà AI trước đã gây ra, phân loại theo mức độ nghiêm trọng, và đề xuất cách fix cụ thể.

---

## TỔNG QUAN

AI trước đã sửa/thêm code vào nhiều file nhưng gây ra các vấn đề sau:
1. **Tên element UXML ↔ C# không khớp** (nguyên nhân chính gây NullReferenceException)
2. **Dùng cú pháp CSS web trong USS** (Unity không hỗ trợ)
3. **Logic bug** (AFK timeout, timing)
4. **Dead code** (field khai báo nhưng không dùng)
5. **Text tiếng Việt không dấu** trong UXML

---

## PHẦN A — CÁC BUG ĐÃ ĐƯỢC FIX (bởi phiên trước)

| # | File | Mô tả | Trạng thái |
|---|------|--------|------------|
| A1 | `GlobalStyles.uss` | `background-image: linear-gradient(...)` — Unity USS không hỗ trợ → lỗi "Invalid value for image texture" + render rác lên màn hình | ✅ ĐÃ FIX |
| A2 | `GameController.cs` | `AfkTimeoutRoutine` chỉ start 1 lần trong `ChangeState(Playing)`, cancel trong `RevealAndAdvance` nhưng không restart cho câu tiếp → chỉ câu 1 có AFK protection | ✅ ĐÃ FIX |
| A3 | `GameController.cs` | Turn summary `WaitForSeconds(2.0f)` + `ShowAnswerFeedback` 1.5s = 3.5s/câu, quá chậm. Giảm xuống 1.0s + 0.8s UI | ✅ ĐÃ FIX |
| A4 | `GameplayUIController_UXML.cs` | Field `avatarSprites` khai báo nhưng không dùng (dead code sau khi chuyển sang AvatarHelper) | ✅ ĐÃ FIX |
| A5 | `GameplayUIController_UXML.cs` | Query `"timer-fill"` nhưng UXML đặt tên `"timer-ring-fill"` → `_timerFill` luôn null → thanh timer không hoạt động | ✅ ĐÃ FIX |
| A6 | `AuthPopup.uxml` | Tên element khác hoàn toàn so với code C#: `main-container` ↔ `main-choice-container`, `login-btn` ↔ `goto-login-btn`, `error-label` ↔ `auth-error`, v.v. → NullReferenceException dòng 201 | ✅ ĐÃ FIX |
| A7 | `AuthPopup.uxml` | Thiếu hoàn toàn `forgot-container`, `forgot-pass-btn` — code C# có logic Forgot Password nhưng không có UI | ✅ ĐÃ FIX |
| A8 | Tất cả UXML files | Text tiếng Việt viết không dấu ("DANG NHAP", "TIM TRAN DAU"...) | ✅ ĐÃ FIX |
| A9 | `TimerController.cs` | Comment nói "180 giây" nhưng giá trị thực là 15f | ✅ ĐÃ FIX |

---

## PHẦN B — CÁC BUG CHƯA FIX (cần làm tiếp)

### B1. ProfilePopup — Button Lưu/Đóng không hoạt động ⚠️ CRITICAL

**File:** `MainMenuUIController_UXML.cs` dòng 499-501  
**Nguyên nhân:** Code C# query tên element sai so với `ProfilePopup.uxml`:

| C# query (sai) | UXML thực tế (đúng) |
|---|---|
| `name-input` | `profile-name-field` |
| `save-profile-btn` | `profile-save-btn` |
| `close-profile-btn` | `profile-close-btn` |

**Hậu quả:** `saveBtn` và `closeBtn` đều null → không đăng ký được `clicked` event → bấm nút không có phản hồi.

**Fix:** Đổi tên query trong C# cho khớp UXML.

---

### B2. ProfilePopup — overlay/popup query sai ⚠️ CRITICAL

**File:** `MainMenuUIController_UXML.cs` dòng 495-496  
**Nguyên nhân:** Code query `Q("overlay")` và `Q("popup")` nhưng UXML đặt tên `profile-overlay` và `profile-container`.  
**Hậu quả:** `overlay` và `popupCard` bị fallback sang `_profilePopup.Children().First()` (TemplateContainer) → animation sai hoặc crash.

**Fix:** Query đúng tên `profile-overlay` và `profile-container`.

---

### B3. ProfilePopup — Avatar Grid không tồn tại ⚠️ MEDIUM

**File:** `MainMenuUIController_UXML.cs` dòng 523-547  
**Nguyên nhân:** Code loop tìm `avatar-0` đến `avatar-7` nhưng `ProfilePopup.uxml` chỉ có 1 VisualElement `profile-avatar` — không có grid button nào.  
**Hậu quả:** Toàn bộ avatar selection loop chạy vô nghĩa (tất cả `avatarBtn` đều null). Tuy không crash (có null check), nhưng feature avatar selection không hoạt động.

**Fix:** Hoặc (a) thêm avatar grid vào UXML, hoặc (b) chuyển sang dùng AvatarHelper (initial letter) nhất quán — bỏ avatar grid sprite.

---

### B4. ProfilePopup — `avatarSprites` field vẫn tồn tại ⚠️ LOW

**File:** `MainMenuUIController_UXML.cs` dòng 22  
**Nguyên nhân:** `[SerializeField] private Sprite[] avatarSprites;` vẫn được khai báo và dùng ở dòng 529-532 cho avatar grid (mà không tồn tại trong UXML). Nhưng GameplayUI đã chuyển sang AvatarHelper.  
**Hậu quả:** Hệ thống avatar không nhất quán — Home dùng sprites, Gameplay dùng InitialLetter. Nếu `avatarSprites` không gán trong Inspector → null.

**Fix:** Quyết định 1 hướng: dùng AvatarHelper (initial letter) cho tất cả, hoặc dùng sprite grid cho tất cả. Đề xuất: dùng AvatarHelper cho nhất quán.

---

### B5. LogoutConfirmPopup — tên button sai ⚠️ CRITICAL

**File:** `MainMenuUIController_UXML.cs` dòng 426-429  
**Nguyên nhân:** Code query sai tên:

| C# query (sai) | UXML thực tế (đúng) |
|---|---|
| `logout-title` | Không có (Label không đặt name) |
| `logout-msg` | `message-label` |
| `confirm-logout-btn` | `logout-yes-btn` |
| `cancel-logout-btn` | `logout-no-btn` |

**Hậu quả:** `confirmBtn` và `cancelBtn` đều null → bấm Đăng xuất/Hủy không phản hồi. `msgLabel` null → crash NullReferenceException ở dòng 447.

**Fix:** Đổi query cho khớp UXML, hoặc thêm name vào UXML Label.

---

### B6. LogoutConfirmPopup — overlay/popup query sai ⚠️ MEDIUM

**File:** `MainMenuUIController_UXML.cs` dòng 422-423  
**Nguyên nhân:** Tương tự B2 — UXML dùng tên `overlay` (đúng) nhưng `popup-container` chứ không phải `popup`.  
**Hậu quả:** Animation fallback sai, popup có thể không animate đúng.

**Fix:** Query `popup-container` thay vì `popup`.

---

### B7. SettingsPopup — popup card query có thể sai ⚠️ LOW

**File:** `MainMenuUIController_UXML.cs` dòng 342-343  
**Vấn đề:** Query `Q("popup")` — UXML SettingsPopup dùng tên `popup` → OK. Nhưng fallback `overlay.Children().First()` nguy hiểm nếu UXML thay đổi.

**Trạng thái:** Hoạt động đúng hiện tại, nhưng fragile code.

---

### B8. AudioManager — 4 AudioClip mới chưa gán ⚠️ LOW

**File:** `AudioManager.cs` dòng 26-29  
**Vấn đề:** AI kia thêm `countdownTickSound`, `countdownGoSound`, `timerUrgentSound`, `swooshSound` nhưng không gán trong Inspector. `PlaySFX` xử lý null an toàn (không crash), nhưng không có âm thanh.

**Fix:** Cần tìm/tạo SFX clips và gán trong Unity Inspector, hoặc bỏ đi nếu không cần.

---

### B9. GameplayUIController — ShowToast method có thể thiếu ⚠️ MEDIUM

**File:** `GameplayUIController_UXML.cs` dòng 713  
**Vấn đề:** `HandleStreakChanged` gọi `ShowToast(msg, 1.5f)` — cần verify method này tồn tại.

**Fix:** Kiểm tra, nếu thiếu thì thêm (tương tự `ShowInfoToast` trong MainMenu).

---

## PHẦN C — KẾ HOẠCH HÀNH ĐỘNG (ưu tiên)

### Đợt 1: Fix CRITICAL (Popup buttons không hoạt động)

| Task | File cần sửa | Chi tiết |
|------|-------------|----------|
| C1 | `MainMenuUIController_UXML.cs` | Fix Profile popup: đổi `name-input` → `profile-name-field`, `save-profile-btn` → `profile-save-btn`, `close-profile-btn` → `profile-close-btn` |
| C2 | `MainMenuUIController_UXML.cs` | Fix Profile overlay: đổi query `overlay`/`popup` → `profile-overlay`/`profile-container` |
| C3 | `MainMenuUIController_UXML.cs` | Fix Logout popup: đổi `confirm-logout-btn` → `logout-yes-btn`, `cancel-logout-btn` → `logout-no-btn`, `logout-msg` → `message-label` |
| C4 | `LogoutConfirmPopup.uxml` | Thêm `name` cho Label title (hoặc đổi C# bỏ query title) |
| C5 | `MainMenuUIController_UXML.cs` | Fix Logout overlay: query `popup-container` thay vì `popup` |

### Đợt 2: Nhất quán hóa Avatar system

| Task | File cần sửa | Chi tiết |
|------|-------------|----------|
| C6 | `MainMenuUIController_UXML.cs` | Xóa `avatarSprites` field, xóa avatar grid loop (dòng 523-547), dùng AvatarHelper cho profile avatar |
| C7 | `ProfilePopup.uxml` | Giữ nguyên layout đơn giản (không cần avatar grid), chỉ hiển thị initial letter avatar |

### Đợt 3: Polish & verify

| Task | File cần sửa | Chi tiết |
|------|-------------|----------|
| C8 | `GameplayUIController_UXML.cs` | Verify `ShowToast` method tồn tại |
| C9 | `AudioManager.cs` | Quyết định có giữ 4 SFX clips mới hay không |
| C10 | Tổng thể | Test toàn bộ flow: Init → Auth → Home → Profile → Settings → Logout → Gameplay → Result |

---

## PHẦN D — NHẬN XÉT CHUNG

### Vấn đề gốc rễ của AI kia:
1. **Sửa C# mà không cập nhật UXML** (hoặc ngược lại) — đây là nguyên nhân #1 gây crash
2. **Dùng cú pháp CSS web** (`linear-gradient`) trong Unity USS — không tương thích
3. **Không kiểm tra null** trước khi dùng kết quả `Q<>()` — nếu UXML và C# lệch tên → NullReferenceException
4. **Không thống nhất hệ thống** — chỗ dùng AvatarHelper, chỗ dùng sprites, chỗ khai báo field nhưng không dùng
5. **Viết text tiếng Việt không dấu** trong UXML — có thể do thiếu font hỗ trợ hoặc đơn giản là lười

### Đánh giá những gì AI kia làm ĐÚNG:
- Logic BUG-01 (flag `_isRevealing` chống reveal trùng) — correct
- Logic BUG-02 (`_currentP2Answer` cho offline mode) — correct  
- ScoreManager `OnStreakChanged` event — correct
- `AvatarHelper.cs` static helper — code clean, hoạt động tốt
- `SpinnerHelper.cs` — code clean
- `SceneTransition.cs` — code clean
- `PlayerDataManager.ClearData()` fix — correct
- `QuizManager` shuffle fix — correct
- `FirebaseManager.IsAnonymous` property — correct
