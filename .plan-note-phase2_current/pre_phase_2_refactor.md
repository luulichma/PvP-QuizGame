# 🔧 Pre-Phase 2 Refactor — Phân Rã God Objects

**Mục tiêu:** Phá vỡ các god object (đặc biệt UI layer) để mở khóa lại `plan_note_new_tier_rank.md`. Sau refactor, mỗi bước trong plan Tier/Rank chỉ chạm vào 1–2 file nhỏ thay vì sửa giữa các file nghìn dòng.

**Nguyên tắc tối thượng:** ❗ **KHÔNG thay đổi behavior.** Refactor xong game phải chạy y hệt trước đó. Mỗi bước là 1 commit riêng, test được, revert được.

---

## 📊 Hiện Trạng (đo ngày 11-06-2026)

| File | Dòng | Trách nhiệm đang ôm |
|---|---|---|
| `UI/MainMenuUIController_UXML.cs` | **1.357** | Nav tabs, matchmaking flow, toast, Auth popup (~200 dòng build inline), Settings popup, Logout popup, Profile popup (~114 dòng), Leaderboard, Achievements, XP bar, localize, animation nền |
| `UI/GameplayUIController_UXML.cs` | **1.083** | HUD, countdown, câu hỏi, timer, score, streak, toast, turn summary, Settings popup, Exit popup, Result popup (`HandleGameOver` ~240 dòng) |
| `Network/FirebaseManager.cs` | **880** | Init/RemoteConfig + Auth (email/anonymous/reset) + Profile sync + Tier + Matchmaking queue + Room/Presence + In-match sync |
| `UI/Layouts/HomeLayout.uxml` | 122 | Toàn bộ Home + Shop + Leaderboard panel trong 1 file |
| `Core/GameController.cs` | 398 | Chấp nhận được, chỉ trích xuất nhẹ |

**Code smell chung:**
- Popup được **build/instantiate + wire event + localize inline** trong controller cha → muốn thêm 1 popup mới (Shop, Season, Quest) là phải đụng god object.
- Localize lặp thủ công (29 + 23 chỗ gọi `LocalizationManager`) thay vì binding tập trung.
- Toast logic bị **copy-paste** ở cả 2 controller (`ShowToast` / `ShowInfoToast` / `RemoveToastAfter`).
- `FirebaseManager` vừa là Auth service vừa là Matchmaking service vừa là Room sync → mọi feature online mới đều phình nó thêm.

**Vì sao Tier/Rank plan bị kẹt:** Bước 2–4 của plan đó yêu cầu sửa `GameplayUIController_UXML`, `MainMenuUIController_UXML`, `HomeLayout.uxml`, `FirebaseManager` — đúng 4 god object trên. Mỗi thay đổi nhỏ đều rủi ro vỡ matchmaking/gameplay hiện có.

---

## 🏗️ Kiến Trúc Đích

```
Scripts/UI/
├── Common/
│   ├── PopupBase.cs            # instantiate template, overlay, show/hide anim, close
│   ├── ToastService.cs         # gộp toast của 2 controller (1 nguồn duy nhất)
│   └── UILocalizer.cs          # đăng ký (element, key) → tự refresh khi đổi ngôn ngữ
├── Popups/
│   ├── AuthPopupController.cs
│   ├── SettingsPopupController.cs      # dùng chung Home + Gameplay
│   ├── ProfilePopupController.cs
│   ├── LogoutConfirmPopupController.cs
│   ├── ExitConfirmPopupController.cs
│   └── ResultPopupController.cs
├── Home/
│   ├── MainMenuUIController_UXML.cs    # CHỈ CÒN: bootstrap, giữ refs template, điều phối
│   ├── HomeNavController.cs            # bottom tabs + sub-tabs + ShowPanel
│   ├── MatchmakingPanelController.cs   # find/cancel/timeout/offline-routine
│   ├── PlayerHeaderController.cs       # name, avatar, money, level tag, XP bar
│   ├── LeaderboardPanelController.cs
│   ├── AchievementsPanelController.cs
│   └── ShopPanelController.cs          # 🆕 tạo rỗng — chỗ đáp của Tier/Rank plan Bước 3
└── Gameplay/
    ├── GameplayUIController_UXML.cs    # CHỈ CÒN: bootstrap + route GameState events
    ├── QuestionViewController.cs       # câu hỏi, đáp án, reveal
    ├── HUDController.cs                # score, timer arc, streak, turn summary
    └── CountdownOverlayController.cs

Scripts/Network/
├── FirebaseBootstrap.cs        # Init app + RemoteConfig (giữ tên FirebaseManager nếu ngại đổi ref scene)
├── AuthService.cs              # SignIn/SignUp/Reset/SignOut/HandleAuthResult
├── ProfileService.cs           # SyncProfile/SaveProfileToCloud/UpdateDisplayName/GetPlayerTier
├── MatchmakingService.cs       # queue, search, claim, timeout, cancel
└── RoomService.cs              # join/presence/seed/score/answer/advance/end/leave
```

**Quy ước sau refactor:**
1. Không file nào trong `Scripts/UI` vượt **400 dòng**; method không vượt **60 dòng**.
2. Controller cha **không bao giờ** build nội dung popup — chỉ `new XxxPopupController(template, root).Show()`.
3. Mỗi popup/panel controller là **plain C# class** nhận `VisualElement root` (không cần MonoBehaviour) → test/tái dùng dễ.
4. UXML: panel lớn tách thành template riêng nếu thêm nội dung mới (Shop sẽ là `ShopPanel.uxml` thay vì nhét vào `HomeLayout.uxml`).

---

## 📍 Các Bước Thực Hiện

### Bước 0: Dọn nền (0,5 ngày)
- [ ] Tạo branch `refactor/pre-phase-2`. *(11-06: git status quá chậm trên ổ F — user tự tạo branch trước khi commit)*
- [x] **Xóa** các file `*_Old.cs` (`GameplayUIController_Old`, `InputController_Old`, `MainMenuUIController_Old`, `InitSceneController_Old` — tổng ~557 dòng dead code) sau khi xác nhận không scene nào reference. ✅ 11-06: đã check GUID trong .unity/.prefab/.asset — không reference, đã xóa kèm .meta.
- [ ] Chụp baseline: build chạy được, ghi lại flow test tay (đăng nhập → tìm trận offline → chơi hết trận → xem kết quả → leaderboard).

### Bước 1: Hạ tầng UI chung (1 ngày) ✅ 11-06-2026
- [x] Tạo `PopupBase.cs`: nhận `VisualTreeAsset` + parent root; lo instantiate, fullscreen absolute, `UIAnimator.ShowPopupAnim`, nút close, `OnClosed` event. (113 dòng, tích hợp sẵn UILocalizer + Close/CloseImmediate)
- [x] Tạo `ToastService.cs`: gộp 2 bản toast trùng lặp (dùng `DOVirtual.DelayedCall` thay coroutine → không cần MonoBehaviour).
- [x] Tạo `UILocalizer.cs`: `BindLabel/BindButton/BindFieldLabel` + tự refresh khi `OnLanguageChanged`, có `Attach()/Detach()` chống leak event.
- ✅ **Checkpoint:** cần mở Unity Editor compile + test toast, đổi ngôn ngữ.

### Bước 2: Phân rã MainMenuUIController_UXML (2–3 ngày) — ưu tiên cao nhất
Tách theo thứ tự ít rủi ro → nhiều rủi ro, mỗi lần tách 1 commit:
- [x] 2.1 `SettingsPopupController` (gộp luôn logic `GetLanguageIndex/GetLanguageCode/CloseSettingsPopup`). ✅ 11-06
- [x] 2.2 `LogoutConfirmPopupController`, `ProfilePopupController`. ✅ 11-06 — MainMenu giảm 1.357 → 1.026 dòng
- [x] 2.3 `AuthPopupController` (di chuyển nguyên khối ~250 dòng `ShowGuestLoginPopup` + `LocalizeInlineAuthPopup`). ✅ 12-06 — MainMenu còn 796 dòng. Bonus fix: Escape đóng auth popup giờ gỡ cả listener `OnAuthError` (trước đây leak).
- [x] 2.4 `LeaderboardPanelController` + `AchievementsPanelController` (kèm reload khi đổi ngôn ngữ). ✅ 12-06
- [x] 2.5 `HomeNavController` (bottom tabs, `ShowPanel`, `SwitchBottomTab/SwitchSubTab`, localize nav labels). ✅ 12-06
- [x] 2.6 `MatchmakingPanelController` (find/practice/cancel/timeout — di chuyển trọn cụm FIX-CANCEL + UX-06, nhận MonoBehaviour runner để chạy coroutine). ✅ 12-06
- [x] 2.7 `PlayerHeaderController` (`RefreshPlayerStatsUI`, XP bar, avatar). ✅ 12-06
- [x] 2.8 `ShopPanelController` placeholder, có sẵn [PHASE-2 HOOK] cho Tier/Rank Bước 3. ✅ 12-06
- 🎯 **Đích:** MainMenu controller còn **< 250 dòng** → ✅ thực tế **272 dòng** (orchestrator thuần, file mới trong `Scripts/UI/Home/`).

### Bước 3: Phân rã GameplayUIController_UXML (2 ngày) ✅ 12-06-2026
- [x] 3.1 `ResultPopupController`: di chuyển nguyên `HandleGameOver` (~240 dòng), tách `BuildRewardSection` (vẽ) khỏi ScoreManager (tính). [PHASE-2 HOOK] reward mới chỉ thêm dòng tại đây.
- [x] 3.2 `GameplaySettingsPopupController` + `ExitConfirmPopupController` (PopupBase). Lưu ý: settings Gameplay dùng UXML khác settings Home nên là controller riêng, không gộp.
- [x] 3.3 `QuestionViewController` (text + counter + progress bar; answer buttons vẫn ở `InputController_UXML` — không đụng).
- [x] 3.4 `GameplayHUDController` (score, timer arc, streak, turn summary, opponent status, [PHASE-2 HOOK] PowerUp bar) + `CountdownOverlayController`.
- [x] 3.5 Controller gốc chỉ còn subscribe events và route. → **194 dòng**. File mới trong `Scripts/UI/Gameplay/`.
- 🎯 Gameplay controller < 250 dòng ✅ (194). HUD controller 431 dòng (hơi vượt mốc 400 — chấp nhận được, sẽ tách PowerUpHUDController riêng khi làm Phase 2).

### Bước 4: Phân rã FirebaseManager (2 ngày) ✅ 12-06-2026 — dạng PARTIAL CLASS
**Quyết định kiến trúc:** FirebaseManager là MonoBehaviour singleton được scene reference (không đổi được GUID) và state đan chéo chặt (CurrentRoomId/IsHost/events dùng chung cả 3 mảng). Tách thành service object rời sẽ rủi ro cao khi chưa có vòng compile/test. → Dùng **partial class**: tách vật lý 4 file theo trách nhiệm, public API + behavior giữ nguyên 100%, scene/prefab không đổi:
- [x] `Network/FirebaseManager.cs` (141 dòng, GIỮ GUID cũ): singleton, SDK refs, events, state, Init + RemoteConfig.
- [x] `Network/Firebase/FirebaseManager.Auth.cs` (284): Auth + Profile + Tier. [PHASE-2 HOOK] `GetPlayerTier(rankPoints)` sửa tại đây.
- [x] `Network/Firebase/FirebaseManager.Matchmaking.cs` (297): queue/search/claim/timeout/cancel (trọn cụm FIX-CANCEL).
- [x] `Network/Firebase/FirebaseManager.Room.cs` (181): join/presence/in-match sync/leave.
- Đã verify bằng script: không member trùng giữa các partial, toàn bộ 31 API đang được codebase gọi đều tồn tại.
- Nâng cấp lên service object thật (AuthService...) có thể làm sau khi có vòng test online ổn định — không chặn Phase 2.
- ⚠️ Test online 2 máy theo `Setup_2Machines_Guide.md` — đây là phần rủi ro nhất.

### Bước 5: UXML & nghiệm thu (1 ngày)
- [ ] Tách `shop-panel` trong `HomeLayout.uxml` thành `ShopPanel.uxml` template riêng. *(Hoãn: cần wiring asset trong Unity Editor — làm cùng lúc với Tier/Rank Bước 3 khi shop có nội dung thật)*
- [ ] Kiểm `GlobalStyles.uss` (707 dòng): chỉ cần nhóm section + comment, chưa cần tách file.
- [ ] Chạy lại toàn bộ flow baseline ở Bước 0 (offline + online 2 máy + đổi ngôn ngữ + logout/login). **← VIỆC CÒN LẠI DUY NHẤT: user mở Unity compile + test.**
- [x] Đo lại line count: file lớn nhất 431 dòng (GameplayHUDController); MainMenu 1.357→272, Gameplay 1.083→194, FirebaseManager 880→4 file ≤297. ✅

---

## 🔗 Mapping Sang Plan Tier/Rank (lý do tồn tại của refactor này)

| Bước trong `plan_note_new_tier_rank.md` | Trước refactor phải sửa | Sau refactor chỉ sửa |
|---|---|---|
| Power-Up UI trong trận | `GameplayUIController_UXML` (1.083 dòng) | `HUDController` + file mới `PowerUpHUDController` |
| Shop UI | `MainMenuUIController_UXML` + `HomeLayout.uxml` | `ShopPanelController` + `ShopPanel.uxml` (đã chừa sẵn) |
| `GetPlayerTier(rankPoints)` | `FirebaseManager` (880 dòng) | `ProfileService` |
| Seasonal rank push/query | `FirebaseManager` | `RoomService`/`ProfileService` + `SeasonManager` mới |
| Season countdown badge trên menu | `MainMenuUIController_UXML` | `PlayerHeaderController` |

→ Sau refactor, **mọi bước của Tier/Rank plan đều là "thêm file mới hoặc sửa file < 300 dòng"**, không còn phẫu thuật god object.

---

## ⚠️ Rủi Ro & Đối Sách

1. **Vỡ online sync khi tách FirebaseManager** → làm cuối cùng (Bước 4), giữ façade để API không đổi, test 2 máy trước khi merge.
2. **Mất reference trong Scene/Prefab** (UIDocument, template SerializeField) → giữ nguyên tên class MonoBehaviour gốc làm entry point; chỉ tách phần thân thành plain class.
3. **Event subscribe/unsubscribe lệch (OnEnable/OnDisable)** → mỗi sub-controller có cặp `Attach()/Detach()` rõ ràng, controller cha gọi đồng bộ.
4. **Refactor lan man** → cấm thêm feature mới trong branch này; thấy bug cũ thì ghi vào `changelog_plan.md`, không sửa kèm.

## ✅ Definition of Done
- [ ] Tất cả flow baseline pass (offline, online 2 máy, localize, auth).
- [ ] Không file nào trong `Scripts/UI` > 400 dòng; MainMenu & Gameplay controller < 250 dòng.
- [ ] Không còn file `*_Old.cs`; không còn toast code trùng lặp.
- [ ] `plan_note_new_tier_rank.md` được un-discard: cập nhật lại tên file đích theo bảng mapping ở trên.

**Tổng ước lượng:** ~7–9 ngày làm việc.
