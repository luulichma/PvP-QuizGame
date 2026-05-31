# THIẾT KẾ HỆ THỐNG XẾP HẠNG, ĐIỂM SỐ VÀ TIỀN TỆ (RANK DESIGN)

Tài liệu này định nghĩa rõ ràng cơ chế phân tách giữa 3 loại tài nguyên mà người chơi thu thập được trong PvP Quiz Game, nhằm định hình động lực (motivation) và vòng lặp trò chơi (gameplay loop).

---

## 1. Cấu trúc 3 Loại Tài Nguyên (Resources)

Hệ thống ghi nhận và phân chia rạch ròi 3 giá trị sau cho mỗi người chơi:

### 1.1. Tiền (Money / Coins)
- **Mục đích:** Đơn vị tiền tệ in-game, dùng để chi tiêu (kinh tế nội bộ).
- **Ứng dụng:** Mua sắm tại Cửa hàng (In-game Shop) ở Phase 2 (mua Avatar, Khung viền, Hiệu ứng hoặc biểu tượng cảm xúc).
- **Đặc điểm:** Tăng lên khi thắng/hòa/thua (thắng được nhiều nhất), giảm đi khi tiêu xài. Không phản ánh trình độ thực sự của người chơi (vì có thể cày cuốc).

### 1.2. Điểm Xếp Hạng (Rank Points / Score)
- **Mục đích:** Thể hiện kỹ năng, thành tích và vị thế của người chơi trong cộng đồng.
- **Ứng dụng:** 
  1. **Leaderboard:** Bảng xếp hạng sẽ hoàn toàn dựa trên trường `rankPoints`. Ai nhiều điểm xếp hạng nhất sẽ đứng Top 1.
  2. **Thành tựu (Achievements):** Mở khóa các huy hiệu hoặc danh hiệu dựa trên các mốc điểm xếp hạng (VD: Đạt 1.000 điểm mở khóa danh hiệu "Học Giả").
- **Đặc điểm:** Chỉ tích lũy qua các trận đấu. Tùy thuộc vào định hướng, Điểm có thể bị trừ khi Thua (giống như Rank Elo) để tăng tính cạnh tranh, hoặc chỉ cộng lên nhưng cộng ít.

### 1.3. Kinh Nghiệm & Cấp Độ (EXP & Level)
- **Mục đích:** Đo lường thời gian gắn bó và độ "già dặn" của tài khoản.
- **Ứng dụng:** 
  1. Quy định độ khó / số lượng câu hỏi trong chế độ Đấu Máy (Offline).
     - *Ví dụ: Level 1-5 (5 câu/trận), Level 6-15 (10 câu/trận), Level 16+ (15 câu/trận).*
  2. **Hệ số phần thưởng (Multiplier):** Level càng cao, trận đấu càng kéo dài (nhiều câu hỏi hơn), do đó phần thưởng (Tiền và Điểm Xếp Hạng) kiếm được sau mỗi trận sẽ được nhân hệ số cao hơn.
- **Đặc điểm:** Chỉ tăng lên, không bao giờ giảm.

---

## 2. Hệ Số Thưởng Theo Level (Level Multiplier)

Để đảm bảo tính công bằng khi người chơi Level cao phải trả lời nhiều câu hỏi hơn, hệ thống sẽ áp dụng hệ số nhân phần thưởng.

**Công thức dự kiến:**
`Hệ số (Multiplier) = 1.0 + (Level * 0.1)`

*Ví dụ:* 
- Người chơi Level 1: Multiplier = 1.1x
- Người chơi Level 10: Multiplier = 2.0x (Nhận gấp đôi phần thưởng so với Level 1 do phải đấu 10 câu thay vì 5 câu).

---

## 3. Cập Nhật Database Schema (Firebase)

Dữ liệu của người dùng trên Firebase `users/{uid}` sẽ cần cập nhật cấu trúc:

```json
{
  "users": {
    "uid_123": {
      "displayName": "PlayerOne",
      "level": 5,             // <- Quyết định số câu hỏi và hệ số thưởng
      "currentExp": 150,      // <- Tích lũy để lên level
      "money": 2500,          // <- Dùng để mua sắm
      "rankPoints": 1200,     // <- (TRƯỜNG MỚI) Dùng để xếp hạng Leaderboard
      "avatarIndex": 1
    }
  }
}
```

---

## 4. Query cho Bảng Xếp Hạng (Leaderboard)

Với trường `rankPoints` mới, code lấy dữ liệu cho Bảng xếp hạng sẽ là:

```csharp
// Lấy 100 người có Điểm Xếp Hạng (Rank Points) cao nhất
FirebaseDatabase.DefaultInstance
    .GetReference("users")
    .OrderByChild("rankPoints")
    .LimitToLast(100)
    .GetValueAsync();
```

---

## 5. Cập nhật vào mã nguồn (Mục tiêu tiếp theo)

1. Cập nhật `PlayerData.cs` (ScriptableObject local) thêm biến `rankPoints`.
2. Cập nhật `PlayerDataManager.cs` để hỗ trợ Add/Save `rankPoints` lên Firebase Cloud.
3. Điều chỉnh `ScoreManager.cs` để tính toán cả 3 loại: `expAwarded`, `moneyAwarded`, và `rankPointsAwarded` theo hệ số Level.
