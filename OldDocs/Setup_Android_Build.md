# Hướng dẫn Build Android cho PvP Quiz Game
**Ngày:** 29/04/2026
**Tiền đề:** Đã làm xong setup Firebase ở `Setup_2Machines_Guide.md` (Phần A) và game đã chạy được trên Standalone Windows / Editor.

---

## PHẦN 0 — CHUẨN BỊ MỘT LẦN

### 0.1. Cài Android Build Support trong Unity Hub
1. Mở **Unity Hub** → tab **Installs**.
2. Tìm phiên bản Unity dự án đang dùng (Unity 6) → click ⚙️ → **Add modules**.
3. Tích các module:
   - ✅ **Android Build Support**
   - ✅ **OpenJDK** (con của Android Build Support)
   - ✅ **Android SDK & NDK Tools** (con của Android Build Support)
4. Install → đợi ~10 phút.

### 0.2. Bật Developer Options + USB Debugging trên 2 điện thoại Android
1. **Settings → About phone** → tap **Build number** 7 lần → "You are now a developer".
2. **Settings → Developer options** → bật:
   - ✅ **USB debugging**
   - ✅ **Stay awake** (lúc test cho khỏi tắt màn hình)
   - ✅ **Disable adb authorization timeout** (nếu có)
3. Cắm điện thoại vào máy lập trình qua USB → trên điện thoại sẽ hiện popup "Allow USB debugging?" → tích "Always allow" → OK.

### 0.3. Verify ADB hoạt động
Mở terminal/PowerShell, gõ:
```
adb devices
```
Nếu thấy serial number của máy → OK. Nếu báo `unauthorized` → unplug, cắm lại, accept popup.

> ADB nằm trong: `<Unity Editor folder>/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe`. Có thể add vào PATH cho tiện.

---

## PHẦN 1 — THÊM ANDROID APP VÀO FIREBASE

### 1.1. Mở Firebase Console → Project Settings
1. Vào project Firebase đã tạo trước đó.
2. Project Settings (⚙️) → tab **General** → kéo xuống **"Your apps"**.

### 1.2. Add app → chọn icon Android (🤖)
1. **Android package name:** rất quan trọng — phải GIỐNG HỆT với Bundle Identifier bạn đặt trong Unity. Format đề xuất:
   ```
   com.<yourname>.pvpquizgame
   ```
   Ví dụ: `com.chiennt.pvpquizgame`
2. **App nickname:** đặt gì cũng được (vd `PvPQuiz Android`).
3. **Debug signing certificate SHA-1:** **bỏ trống** — Anonymous Auth không cần SHA-1.
4. Register app.

### 1.3. Tải `google-services.json`
1. Sau khi register → bấm **Download google-services.json**.
2. Đặt file vào: `Assets/google-services.json` (CHÍNH XÁC ở root của Assets, không vào StreamingAssets).
3. Firebase Unity SDK sẽ tự pickup file này khi build.

### 1.4. Bỏ qua các bước "Add Firebase SDK / Add init code"
Vì Unity Firebase SDK đã import sẵn trong project rồi. Click **Next → Continue to console**.

---

## PHẦN 2 — CẤU HÌNH UNITY PLAYER SETTINGS

Mở **File → Build Settings** → click **Android** → **Switch Platform** (đợi Unity reimport ~5 phút lần đầu).

Sau đó **Player Settings** (nút trong cùng dialog) → chọn tab **🤖 Android** (icon Android):

### 2.1. Identification
| Field | Giá trị |
|---|---|
| **Override Default Package Name** | ✅ tích |
| **Package Name** | `com.chiennt.pvpquizgame` (giống Firebase) |
| **Version** | `1.0.0` |
| **Bundle Version Code** | `1` |
| **Minimum API Level** | **Android 7.0 (API 24)** trở lên (Firebase tối thiểu 21, để 24 cho an toàn) |
| **Target API Level** | **Automatic (highest installed)** hoặc **Android 14 (API 34)** |

> **Lưu ý:** Package Name phải khớp 100% với cái đăng ký trong Firebase Console. Sai 1 ký tự là Firebase báo "App not registered".

### 2.2. Configuration
| Field | Giá trị |
|---|---|
| **Scripting Backend** | **IL2CPP** ⚠️ BẮT BUỘC |
| **Api Compatibility Level** | **.NET Standard 2.1** |
| **Target Architectures** | ✅ ARMv7 + ✅ ARM64 (Play Store yêu cầu cả 2) |
| **Internet Access** | **Require** |
| **Active Input Handling** | **Both** |

### 2.3. Publishing Settings (chỉ debug build)
- **Custom Main Manifest, Custom Main Gradle Template:** không cần (để Firebase plugin tự merge).
- **Keystore:** lúc test có thể skip (Unity tự dùng debug keystore). Khi nào release Play Store mới cần.

### 2.4. Resolution and Presentation
| Field | Giá trị |
|---|---|
| **Default Orientation** | **Portrait** (game đang thiết kế dọc) |
| **Use 32-bit Display Buffer** | ✅ |

### 2.5. Quality / Graphics
- Để mặc định, không cần đổi.

---

## PHẦN 3 — RESOLVE DEPENDENCIES (FIREBASE)

Firebase Unity SDK dùng **External Dependency Manager (EDM4U)** để tải các .aar Android.

### 3.1. Force Resolve
1. Menu **Assets → External Dependency Manager → Android Resolver → Force Resolve**.
2. Đợi ~3-5 phút (tải Maven dependencies).
3. Console không có error đỏ là OK.

### 3.2. Nếu báo lỗi "Could not find com.google.firebase:..."
- Menu **Assets → External Dependency Manager → Android Resolver → Settings**.
- Tích ✅ **Enable Auto-Resolution** + ✅ **Use Jetifier**.
- Apply → Force Resolve lại.

### 3.3. Nếu báo lỗi `Could not determine java version`
- Edit → Preferences → External Tools → JDK = **Set automatically using Android Studio JDK** hoặc trỏ tới OpenJDK của Unity Hub: `<Unity Hub>/Editor/<version>/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/`.

---

## PHẦN 4 — BUILD APK

### 4.1. Build APK đơn giản (file .apk để cài tay)
1. **File → Build Settings**.
2. ✅ **Development Build** (để có thể đọc log qua adb logcat).
3. ✅ **Script Debugging** (option).
4. Build → chọn folder `Builds/Android/` → đặt tên `PvPQuiz.apk`.
5. Đợi build (~10-15 phút lần đầu, sau đó nhanh hơn).

### 4.2. Build & Run (cài thẳng vào điện thoại đang cắm USB)
1. Cắm 1 điện thoại qua USB.
2. Build Settings → **Run Device** → chọn điện thoại đó (refresh nếu chưa thấy).
3. **Build And Run** → tự install và mở app trên điện thoại.

### 4.3. Cài lên điện thoại thứ 2
- Sau khi có file `PvPQuiz.apk`:
- **Cách 1 (USB):**
  ```
  adb -s <serial_2> install -r Builds/Android/PvPQuiz.apk
  ```
- **Cách 2 (Wireless):** copy `.apk` qua Zalo/Telegram/Drive vào máy 2 → tap mở → cho phép "Install from unknown sources".

---

## PHẦN 5 — KẾT NỐI 2 MÁY ĐỂ TEST PvP

### 5.1. Yêu cầu
- Cả 2 điện thoại có **internet** (Wi-Fi hoặc 4G).
- KHÔNG cần chung Wi-Fi — Firebase Realtime Database làm trung gian.

### 5.2. Quy trình test (giống Standalone)
1. Máy A mở app → InitScene loading → AuthPopup → nhập tên `ChienA` → Confirm.
2. HomeScene → bấm **TÌM TRẬN** → đợi ở matchmaking panel.
3. Máy B trong vòng 30s mở app → nhập `ChienB` → bấm **TÌM TRẬN**.
4. Cả 2 đồng thời vào GameplayScene → countdown → câu hỏi giống nhau → chơi.

### 5.3. Đọc log trên thiết bị Android
Khi gặp bug, **giữ điện thoại cắm USB** trong khi chơi và chạy:
```
adb logcat -s Unity:V *:S
```
- `-s Unity:V` lọc chỉ log từ Unity.
- `*:S` tắt mọi tag khác cho gọn.

Bạn sẽ thấy mọi `Debug.Log` trong code Unity hiển thị real-time. Copy log gửi nếu có lỗi.

> Mẹo: Mở 2 cửa sổ terminal — mỗi cái track 1 máy bằng:
> ```
> adb -s <serial_máy_A> logcat -s Unity:V *:S
> adb -s <serial_máy_B> logcat -s Unity:V *:S
> ```

---

## PHẦN 6 — LỖI THƯỜNG GẶP TRÊN ANDROID

### 6.1. Build fail: "Could not find google-services.json"
- File phải đặt ở `Assets/google-services.json` (KHÔNG phải StreamingAssets).
- Sau khi đặt, để Unity reimport (chờ vài giây), rồi build lại.

### 6.2. App crash ngay khi mở: "Firebase App not initialized"
- Package Name trong Player Settings ≠ Package Name trong Firebase Console.
- Mở `Assets/google-services.json`, tìm field `package_name` → đối chiếu với Player Settings.

### 6.3. App mở được nhưng `[FirebaseManager] Lỗi đăng nhập`
- Anonymous Auth chưa enable trong Firebase Console.
- Vào **Authentication → Sign-in method → Anonymous → Enable → Save**.

### 6.4. APK quá to (>100MB)
- **Player Settings → Other Settings → Strip Engine Code** ✅ tích.
- **Managed Stripping Level**: **Medium** hoặc **High**.
- **Compression Method:** **LZ4HC** (smaller).

### 6.5. Build chạy được trên Editor nhưng đen màn hình trên Android
- **Player Settings → Other Settings → Color Space:** đổi giữa Linear/Gamma.
- **Graphics APIs:** chọn **OpenGLES3** đầu tiên (đẩy Vulkan xuống).

### 6.6. "INSTALL_FAILED_UPDATE_INCOMPATIBLE"
- Phiên bản cũ ký bằng keystore khác. Uninstall app cũ trước:
  ```
  adb uninstall com.chiennt.pvpquizgame
  ```
  Rồi cài lại.

### 6.7. Lỗi `Bundle Version Code already exists` khi muốn upload Play Store
- Tăng Bundle Version Code mỗi lần build (1 → 2 → 3...).
- Lúc test nội bộ thì không cần lo.

### 6.8. UI Toolkit hiển thị lệch trên màn hình điện thoại
- UXML hiện đang thiết kế cho 1920x1080. Trên điện thoại tỷ lệ khác.
- Mở `PanelSettings` (asset SO trong project, gắn vào UIDocument):
  - **Scale Mode:** Scale With Screen Size
  - **Reference Resolution:** 1080 × 1920 (portrait)
  - **Screen Match Mode:** Match Width Or Height
  - **Match:** 0.5 (cân bằng)

### 6.9. Toggle / TextField không bấm được
- Có thể do EventSystem hoặc PanelSettings.
- Kiểm tra **Event System** trong scene — UIDocument trong UI Toolkit dùng event của chính nó, không cần Unity EventSystem cũ.
- Nếu có script Old (UGUI) đang attach và chiếm priority, tắt nó.

---

## PHẦN 7 — DEBUG TIPS NÂNG CAO

### 7.1. Chrome DevTools Inspector (Firebase data live)
Mở https://console.firebase.google.com → Realtime Database → tab **Data** → để màn hình bên cạnh khi test trên 2 điện thoại. Bạn sẽ thấy:
- `matchmakingQueue` xuất hiện UID 1 → biến mất khi UID 2 vào → `rooms/<id>` được tạo
- `rooms/<id>/answers` cập nhật từng round
- `rooms/<id>/scores` cập nhật mỗi câu trả lời đúng
- `rooms/<id>/state` đổi `waiting → playing → ended`

### 7.2. Force quit để test disconnect
Trên điện thoại Android: **Settings → Apps → PvPQuiz → Force stop** giữa trận. Máy còn lại sau 5-10s sẽ thấy `[FirebaseManager] Đối thủ ngắt kết nối!` và hiển thị Win.

### 7.3. Reset profile (test cloud save)
Để test tính năng "data còn nguyên sau khi xoá local":
1. Chơi vài trận trên máy A → có level 3, money 500.
2. Uninstall app trên máy A:
   ```
   adb uninstall com.chiennt.pvpquizgame
   ```
3. Cài lại APK → mở app → InitScene → SignIn lại sẽ tạo UID MỚI (vì Anonymous không có persistent UID khi uninstall).

> ⚠️ **Limitation:** Anonymous Auth UID mất khi uninstall. Để giữ profile xuyên thiết bị, cần Email/Password hoặc Google Sign-In — đó là enhancement P3 sau này.

### 7.4. Inspect APK
Để xem APK có đúng package, có chứa `google-services.json` không:
```
adb shell pm path com.chiennt.pvpquizgame
```
Hoặc dùng Android Studio APK Analyzer (Build → Analyze APK).

---

## PHẦN 8 — CHECKLIST TRƯỚC KHI BUILD

- [ ] Đã làm xong setup Firebase Android (Phần 1)
- [ ] Đã đặt `Assets/google-services.json` đúng project
- [ ] Player Settings: Package Name khớp Firebase (Phần 2.1)
- [ ] Scripting Backend = **IL2CPP**, ARMv7 + ARM64 tích
- [ ] Min API Level ≥ 24
- [ ] Active Input Handling = Both
- [ ] Default Orientation = Portrait
- [ ] External Dependency Manager → Force Resolve không lỗi
- [ ] Build Settings: 3 scenes theo thứ tự (Init=0, Home=1, Gameplay=2)
- [ ] Development Build ✅ (lúc test) → cho debug log

---

## PHẦN 9 — PERFORMANCE TIP CHO ANDROID

| Setting | Giá trị tối ưu |
|---|---|
| Quality Level (Android tab trong Quality settings) | **Medium** hoặc **Low** |
| Texture Compression | **ASTC** (chất lượng cao, hỗ trợ thiết bị mới) |
| MSAA | 2x (game 2D không cần cao) |
| Vsync Count | Don't Sync |
| Application.targetFrameRate | Đặt 60 trong code: `Application.targetFrameRate = 60;` (trong `GameManager.Awake()`) |

---

**Build thành công thì test theo Phần 5 ở trên!**

Có lỗi build cụ thể (copy console log) hoặc app crash trên điện thoại (chạy `adb logcat -s Unity:V *:S` rồi copy log) thì gửi tôi debug tiếp nhé.
