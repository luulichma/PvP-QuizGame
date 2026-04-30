# Báo cáo sửa lỗi Localization trên bản Build (Android)

## Vấn đề gặp phải
- **Hiện tượng:** Trong môi trường Editor hoặc PC, tính năng đa ngôn ngữ (Localization) dùng file JSON local (`vi.json`, `en.json`) vẫn hoạt động bình thường, nhưng khi build sang Android thì Game không load được.
- **Nguyên nhân cốt lõi:** Các file JSON lưu trong thư mục `StreamingAssets`. Trên Android, thư mục này bị đóng gói bên trong file `.apk` (như một file nén zip) với đường dẫn dạng `jar:file://`. Các hàm C# chuẩn như `System.IO.File.Exists()` và `System.IO.File.ReadAllText()` không thể đọc được nội dung bên trong file `.apk`, dẫn đến việc load dữ liệu thất bại.

## Các thay đổi đã thực hiện
**File đã chỉnh sửa:** `Assets/Scripts/Core/LocalizationManager.cs`

1. **Chuyển đổi logic tải file thành Coroutine:**
   - Thay đổi phương thức `LoadLocalLanguage` từ đồng bộ (synchronous) sang bất đồng bộ bằng cách sử dụng `IEnumerator LoadLocalLanguageCoroutine`.
   - Trong `LoadLocalLanguage`, ta gọi `StartCoroutine(LoadLocalLanguageCoroutine(langCode));`.

2. **Áp dụng UnityWebRequest cho Android/WebGL:**
   - Cập nhật hàm để kiểm tra định dạng đường dẫn. Nếu đường dẫn chứa `://` hoặc `:///` (biểu thị nó là URL như trên Android/WebGL), hệ thống sẽ sử dụng `UnityWebRequest.Get` để tải nội dung (`request.downloadHandler.text`).
   - Nếu là đường dẫn file thông thường (trên PC/Editor/iOS), hệ thống vẫn giữ nguyên phương pháp cũ là dùng `System.IO.File.ReadAllText()` để tối ưu tốc độ.

3. **Cập nhật luồng khởi tạo (InitLocalization):**
   - Đảm bảo trong hàm `InitLocalization`, hệ thống sử dụng `yield return StartCoroutine(LoadLocalLanguageCoroutine(savedLang))` để đồng bộ luồng tải game với việc đọc tệp cấu hình ngôn ngữ.

## Kết quả
Sau khi thay đổi, Game đã có thể sử dụng `UnityWebRequest` để chui vào trong gói APK và đọc được nội dung của `vi.json` hoặc `en.json`. Lỗi mất chữ/không load được localize khi build Android đã được khắc phục hoàn toàn.
