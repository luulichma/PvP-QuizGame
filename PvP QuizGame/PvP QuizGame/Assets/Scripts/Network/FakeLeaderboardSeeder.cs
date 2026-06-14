using System.Collections.Generic;

/// <summary>
/// [Seed] Sinh fake users cho BXH khi chưa có nhiều người chơi thật.
///
/// - Danh sách deterministic (không random mỗi lần fetch) — Top 1 hôm nay vẫn là
///   Top 1 ngày mai, tránh trải nghiệm BXH "nhảy múa".
/// - Có UID prefix "bot_" để dễ phân biệt với user thật trên Firebase và đảm bảo
///   matchmaking thật KHÔNG bao giờ ghép trúng fake user (FakeUid chỉ tồn tại
///   client-side, không lên cloud).
/// - Khi user thật đã đông (>= MinRealUsers), seeder tự dừng (xem
///   <see cref="LeaderboardManager.FetchTopRankPlayersAsync"/>).
///
/// Thêm/bớt entry ở mảng s_Seed nếu muốn balance phân bố.
/// </summary>
public static class FakeLeaderboardSeeder
{
    /// <summary>Khi BXH thật ít hơn ngưỡng này → trộn fake vào để đầy BXH.</summary>
    public const int MinRealUsers = 30;

    /// <summary>Prefix UID cho fake — KHÔNG được thay đổi nếu đã production
    /// (sẽ ảnh hưởng so sánh "có phải mình không" trong UI).</summary>
    public const string FakeUidPrefix = "bot_";

    /// <summary>True nếu uid này là fake user do seeder sinh ra.</summary>
    public static bool IsFakeUid(string uid)
        => !string.IsNullOrEmpty(uid) && uid.StartsWith(FakeUidPrefix);

    // (displayName, rankPoints, avatarIndex)
    // Phân bố: Bronze (T1, <500): 12, Silver (T2, 500-1499): 10, Gold (T3, 1500-2999): 10,
    //          Diamond (T4, 3000-4999): 8, Legend (T5, 5000+): 5  → tổng 45 entries.
    private static readonly (string name, int rp, int avatar)[] s_Seed = new (string, int, int)[]
    {
        // ===== Tier 5 — Legend =====
        ("KingOfQuiz",      8420, 0),
        ("ThanhDragon",     7280, 1),
        ("ProMaster99",     6510, 2),
        ("HoangVip",        5890, 3),
        ("LinhBoss",        5210, 4),

        // ===== Tier 4 — Diamond =====
        ("MinhDuc_PvP",     4720, 0),
        ("NguyenAnh",       4310, 1),
        ("QuocBao_HN",      3950, 2),
        ("Tuan_SaiGon",     3640, 3),
        ("Phuong_DN",       3380, 4),
        ("VietHoang",       3210, 0),
        ("TienDat99",       3120, 1),
        ("QuizLord",        3050, 2),

        // ===== Tier 3 — Gold =====
        ("ThuyLinh_22",     2890, 3),
        ("HieuNguyen",      2640, 4),
        ("MyTam_Fan",       2410, 0),
        ("BaoNam_Dev",      2280, 1),
        ("LamPhong",        2110, 2),
        ("AnhKhoi",         1950, 3),
        ("HangNga2k4",      1820, 4),
        ("PhucVinh",        1710, 0),
        ("TrucMy",          1610, 1),
        ("DuyKhang",        1550, 2),

        // ===== Tier 2 — Silver =====
        ("ThaoVy_HCM",      1430, 3),
        ("MinhTri_Dev",     1310, 4),
        ("HuyenMy",         1180, 0),
        ("QuangHieu",       1050, 1),
        ("KhanhLinh",        940, 2),
        ("VanAn_77",         830, 3),
        ("BaoTram",          720, 4),
        ("Thanh_BMT",        650, 0),
        ("HaiYen_DN",        580, 1),
        ("Tung_HUST",        520, 2),

        // ===== Tier 1 — Bronze =====
        ("Newbie123",        470, 3),
        ("QuizStarter",      420, 4),
        ("Nam2k6",           380, 0),
        ("AnhTuan",          340, 1),
        ("LamDuy",           300, 2),
        ("MyHanh",           260, 3),
        ("Thinh99",          220, 4),
        ("HoangAnh",         180, 0),
        ("BaoNgoc",          150, 1),
        ("Tien_DL",          120, 2),
        ("Linh_FAN",          90, 3),
        ("PlayerX",           50, 4),
    };

    /// <summary>
    /// Trả về danh sách fake LeaderboardEntry — đã set sẵn tier theo RP.
    /// Caller có trách nhiệm merge với data thật rồi sort + gán rank.
    /// </summary>
    public static List<LeaderboardEntry> GetFakeEntries()
    {
        var list = new List<LeaderboardEntry>(s_Seed.Length);
        for (int i = 0; i < s_Seed.Length; i++)
        {
            var s = s_Seed[i];
            list.Add(new LeaderboardEntry
            {
                uid = FakeUidPrefix + i.ToString("D3"),  // bot_000, bot_001, ...
                displayName = s.name,
                rankPoints = s.rp,
                avatarIndex = s.avatar,
                tier = PlayerData.ComputeTier(s.rp),
                rank = 0 // caller sẽ tính lại
            });
        }
        return list;
    }
}
