# Sơ đồ Cơ sở dữ liệu (ERD) - PvP Quiz Game

Dưới đây là sơ đồ thực thể liên kết (ERD) mô tả cấu trúc của Firebase Realtime Database sau khi đã cập nhật hệ thống Thành tựu và Xếp hạng ở Phase 1.

```mermaid
erDiagram
    USERS ||--o{ MATCHMAKING_QUEUE : "joins"
    USERS ||--o{ ROOMS : "plays in"
    
    USERS {
        string uid PK
        string displayName
        int level
        int currentExp
        int money
        int rankPoints "Điểm xếp hạng"
        int avatarIndex
        long lastSeen "Timestamp"
        
        %% Achievement Stats
        int botWins "Số trận thắng Bot"
        int totalMoneyEarned "Tổng tiền kiếm được"
        int currentWinStreak "Chuỗi thắng hiện tại"
        int highestWinStreak "Chuỗi thắng kỷ lục"
        string unlockedAchievements "ID các thành tựu (phân tách bởi dấu phẩy)"
    }

    MATCHMAKING_QUEUE {
        string uid PK "ID người chơi"
        string name "Tên hiển thị"
        int avatar
        long joinedAt "Timestamp lúc bắt đầu tìm trận"
    }

    ROOMS {
        string roomId PK
        long createdAt "Timestamp"
        int seed "Random seed cho câu hỏi"
        string state "waiting / playing / finished"
        int currentQ "Chỉ số câu hỏi hiện tại"
        int questionCount "Tổng số câu hỏi"
    }

    %% Nested objects inside ROOMS
    ROOM_PLAYERS {
        string uid PK
        string name
        int avatar
        boolean online
    }
    
    ROOM_SCORES {
        string uid PK
        int score
    }

    ROOM_ANSWERS {
        string uid PK
        int answerIndex
    }

    ROOMS ||--|{ ROOM_PLAYERS : "contains"
    ROOMS ||--o{ ROOM_SCORES : "tracks"
    ROOMS ||--o{ ROOM_ANSWERS : "records"
```

## Giải thích các cập nhật mới (Phase 1)
- **rankPoints**: Được thêm vào `USERS` để theo dõi điểm số phục vụ cho Bảng xếp hạng.
- **Achievement Stats**: Các trường `botWins`, `totalMoneyEarned`, `currentWinStreak`, `highestWinStreak` và `unlockedAchievements` được thêm vào `USERS` để hệ thống `AchievementManager` có thể theo dõi và cấp phần thưởng tự động khi người chơi hoàn thành nhiệm vụ.
