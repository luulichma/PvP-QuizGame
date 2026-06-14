using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;

/// <summary>
/// [REFACTOR-P2] FirebaseManager — phần AUTH + PROFILE + TIER CONFIG.
/// (SignIn/SignUp/Reset/SignOut, sync profile cloud ↔ local, tier theo level)
/// [PHASE-2 HOOK] Tier/Rank plan Bước 1: đổi GetPlayerTier(int level) → GetPlayerTier(int rankPoints),
/// và bổ sung field mới (powerUp, currentTier, tierRankPoints...) vào SaveProfileToCloud/HandleAuthResult.
/// </summary>
public partial class FirebaseManager
{
    // ==================== AUTHENTICATION ====================

    /// <summary>Đăng nhập ẩn danh + load profile từ cloud (hoặc tạo mới).</summary>
    public async Task<bool> SignInAnonymousAndLoadProfile(string desiredDisplayName = null)
    {
        if (!IsConnected || _auth == null)
        {
            OnAuthError?.Invoke("Firebase chưa sẵn sàng.");
            return false;
        }

        try
        {
            var authResult = await _auth.SignInAnonymouslyAsync();
            return await HandleAuthResult(authResult.User, desiredDisplayName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] SignIn Failed: {ex.Message}");
            OnAuthError?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>Đồng bộ lại profile từ Cloud nếu đã authenticated.</summary>
    public async Task<bool> SyncProfile()
    {
        if (!IsConnected || _auth == null || _auth.CurrentUser == null) return false;
        return await HandleAuthResult(_auth.CurrentUser);
    }

    /// <summary>Đăng ký tài khoản mới bằng Email/Password.</summary>
    public async Task<bool> SignUpWithEmail(string email, string password, string displayName)
    {
        if (!IsConnected || _auth == null) { OnAuthError?.Invoke("Firebase chưa sẵn sàng."); return false; }

        try
        {
            var authResult = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
            return await HandleAuthResult(authResult.User, displayName);
        }
        catch (FirebaseException ex)
        {
            string errorMsg = GetFriendlyAuthError(ex);
            Debug.LogError($"[FirebaseManager] SignUp Failed: {errorMsg}");
            OnAuthError?.Invoke(errorMsg);
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] SignUp Failed (Unexpected): {ex.Message}");
            OnAuthError?.Invoke("Lỗi không xác định.");
            return false;
        }
    }

    /// <summary>Đăng nhập tài khoản bằng Email/Password.</summary>
    public async Task<bool> SignInWithEmail(string email, string password)
    {
        if (!IsConnected || _auth == null) { OnAuthError?.Invoke("Firebase chưa sẵn sàng."); return false; }

        try
        {
            var authResult = await _auth.SignInWithEmailAndPasswordAsync(email, password);
            return await HandleAuthResult(authResult.User);
        }
        catch (FirebaseException ex)
        {
            string errorMsg = GetFriendlyAuthError(ex);
            Debug.LogError($"[FirebaseManager] SignIn Failed: {errorMsg}");
            OnAuthError?.Invoke(errorMsg);
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] SignIn Failed (Unexpected): {ex.Message}");
            OnAuthError?.Invoke("Lỗi không xác định.");
            return false;
        }
    }

    /// <summary>Gửi email đặt lại mật khẩu.</summary>
    public async Task<bool> SendPasswordResetEmail(string email)
    {
        if (!IsConnected || _auth == null) { OnAuthError?.Invoke("Firebase chưa sẵn sàng."); return false; }

        try
        {
            await _auth.SendPasswordResetEmailAsync(email);
            return true;
        }
        catch (FirebaseException ex)
        {
            string errorMsg = GetFriendlyAuthError(ex);
            Debug.LogError($"[FirebaseManager] Password Reset Failed: {errorMsg}");
            OnAuthError?.Invoke(errorMsg);
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseManager] Password Reset Failed (Unexpected): {ex.Message}");
            OnAuthError?.Invoke("Lỗi không xác định.");
            return false;
        }
    }

    private string GetFriendlyAuthError(FirebaseException ex)
    {
        AuthError errorCode = (AuthError)ex.ErrorCode;
        string message = errorCode switch
        {
            AuthError.InvalidEmail => "Email không hợp lệ. Vui lòng kiểm tra lại định dạng.",
            AuthError.WrongPassword => "Mật khẩu không chính xác.",
            AuthError.UserNotFound => "Tài khoản không tồn tại.",
            AuthError.EmailAlreadyInUse => "Email này đã được sử dụng cho một tài khoản khác.",
            AuthError.WeakPassword => "Mật khẩu quá yếu. Vui lòng nhập ít nhất 6 ký tự.",
            AuthError.AccountExistsWithDifferentCredentials => "Email này đã liên kết với một phương thức đăng nhập khác.",
            AuthError.NetworkRequestFailed => "Lỗi kết nối mạng. Vui lòng thử lại.",
            AuthError.TooManyRequests => "Quá nhiều yêu cầu. Vui lòng thử lại sau.",
            _ => $"Lỗi hệ thống ({errorCode}): {ex.Message}"
        };
        return message;
    }

    private async Task<bool> HandleAuthResult(FirebaseUser user, string desiredDisplayName = null)
    {
        string uid = user.UserId;
        Debug.Log($"[FirebaseManager] Auth OK. UID = {uid}");

        // Load profile từ cloud
        var snapshot = await _root.Child("users").Child(uid).GetValueAsync();
        var pd = PlayerDataManager.Instance?.Data;

        if (snapshot.Exists)
        {
            if (snapshot.Child("displayName").Value != null)
                LocalDisplayName = snapshot.Child("displayName").Value.ToString();

            // Sync về PlayerData local
            if (pd != null)
            {
                pd.playerName = LocalDisplayName;
                if (snapshot.Child("level").Value != null) pd.level = int.Parse(snapshot.Child("level").Value.ToString());
                if (snapshot.Child("currentExp").Value != null) pd.currentExp = int.Parse(snapshot.Child("currentExp").Value.ToString());
                if (snapshot.Child("money").Value != null) pd.money = int.Parse(snapshot.Child("money").Value.ToString());
                if (snapshot.Child("rankPoints").Value != null) pd.rankPoints = int.Parse(snapshot.Child("rankPoints").Value.ToString());
                if (snapshot.Child("avatarIndex").Value != null) pd.avatarIndex = int.Parse(snapshot.Child("avatarIndex").Value.ToString());

                // Achievements Sync
                if (snapshot.Child("botWins").Value != null) pd.botWins = int.Parse(snapshot.Child("botWins").Value.ToString());
                if (snapshot.Child("totalMoneyEarned").Value != null) pd.totalMoneyEarned = int.Parse(snapshot.Child("totalMoneyEarned").Value.ToString());
                if (snapshot.Child("currentWinStreak").Value != null) pd.currentWinStreak = int.Parse(snapshot.Child("currentWinStreak").Value.ToString());
                if (snapshot.Child("highestWinStreak").Value != null) pd.highestWinStreak = int.Parse(snapshot.Child("highestWinStreak").Value.ToString());

                pd.unlockedAchievements.Clear();
                if (snapshot.Child("unlockedAchievements").Value != null)
                {
                    string rawList = snapshot.Child("unlockedAchievements").Value.ToString();
                    if (!string.IsNullOrEmpty(rawList))
                    {
                        pd.unlockedAchievements = new List<string>(rawList.Split(','));
                    }
                }

                // [PHASE-2] Power-Up inventory sync
                if (snapshot.Child("powerUp_5050").Value != null)
                    pd.powerUp_5050 = int.Parse(snapshot.Child("powerUp_5050").Value.ToString());
                if (snapshot.Child("powerUp_extraTime").Value != null)
                    pd.powerUp_extraTime = int.Parse(snapshot.Child("powerUp_extraTime").Value.ToString());
                if (snapshot.Child("powerUp_shield").Value != null)
                    pd.powerUp_shield = int.Parse(snapshot.Child("powerUp_shield").Value.ToString());

                // [PHASE-2] Tier & Season sync
                if (snapshot.Child("currentTier").Value != null)
                    pd.currentTier = int.Parse(snapshot.Child("currentTier").Value.ToString());
                if (snapshot.Child("highestTierThisSeason").Value != null)
                    pd.highestTierThisSeason = int.Parse(snapshot.Child("highestTierThisSeason").Value.ToString());
                if (snapshot.Child("lastSeasonProcessed").Value != null)
                    pd.lastSeasonProcessed = int.Parse(snapshot.Child("lastSeasonProcessed").Value.ToString());
                if (snapshot.Child("seasonBadges").Value != null)
                    pd.seasonBadges = snapshot.Child("seasonBadges").Value.ToString();

                // [PHASE-2] Daily Quests sync
                if (snapshot.Child("dailyQuestsData").Value != null)
                    pd.dailyQuestsData = snapshot.Child("dailyQuestsData").Value.ToString();

                // Tự đồng bộ tier với RP (defense-in-depth nếu cloud có drift)
                pd.RecomputeTier();
            }

            Debug.Log($"[FirebaseManager] Loaded cloud profile: {LocalDisplayName}");
        }
        else
        {
            // Profile mới
            LocalDisplayName = !string.IsNullOrEmpty(desiredDisplayName)
                ? desiredDisplayName
                : (pd?.playerName ?? "Player");

            // Đảm bảo local data có tên mới trước khi đẩy lên cloud
            if (pd != null) pd.playerName = LocalDisplayName;

            await SaveProfileToCloud();
            Debug.Log($"[FirebaseManager] Created new cloud profile: {LocalDisplayName}");
        }

        // Luôn lưu lại dữ liệu local để đảm bảo đồng bộ PlayerPrefs (PlayerName)
        PlayerDataManager.Instance?.SaveData();

        // Cập nhật lastSeen
        await _root.Child("users").Child(uid).Child("lastSeen").SetValueAsync(ServerValue.Timestamp);

        OnAuthSuccess?.Invoke();
        return true;
    }

    /// <summary>Đăng xuất khỏi Firebase và reset trạng thái local.</summary>
    public void SignOut()
    {
        if (_auth != null)
        {
            _auth.SignOut();
            Debug.Log("[FirebaseManager] Đã đăng xuất khỏi Firebase.");
        }

        // BUG-05 FIX: Không set IsConnected = false — Firebase SDK vẫn connected
        // IsConnected đại diện cho trạng thái kết nối SDK, không phải authentication
        LocalDisplayName = "Player";
        CurrentRoomId = null;
        OpponentId = null;
        OpponentName = null;
    }

    // ==================== PROFILE ====================

    /// <summary>Đẩy PlayerData hiện tại lên cloud.</summary>
    public async Task SaveProfileToCloud()
    {
        if (!IsConnected || !IsAuthenticated) return;
        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null) return;

        var data = new Dictionary<string, object> {
            { "displayName", LocalDisplayName },
            { "level",       pd.level },
            { "currentExp",  pd.currentExp },
            { "money",       pd.money },
            { "rankPoints",  pd.rankPoints },
            { "avatarIndex", pd.avatarIndex },
            { "botWins",     pd.botWins },
            { "totalMoneyEarned", pd.totalMoneyEarned },
            { "currentWinStreak", pd.currentWinStreak },
            { "highestWinStreak", pd.highestWinStreak },
            { "unlockedAchievements", string.Join(",", pd.unlockedAchievements) },
            { "isGuest", IsAnonymous },
            { "lastSeen",    ServerValue.Timestamp },

            // [PHASE-2] Power-Up inventory
            { "powerUp_5050",       pd.powerUp_5050 },
            { "powerUp_extraTime",  pd.powerUp_extraTime },
            { "powerUp_shield",     pd.powerUp_shield },

            // [PHASE-2] Tier & Season
            { "currentTier",            pd.currentTier },
            { "highestTierThisSeason",  pd.highestTierThisSeason },
            { "lastSeasonProcessed",    pd.lastSeasonProcessed },
            { "seasonBadges",           pd.seasonBadges ?? "" },

            // [PHASE-2] Daily Quests
            { "dailyQuestsData",        pd.dailyQuestsData ?? "" }
        };

        await _root.Child("users").Child(LocalUserId).UpdateChildrenAsync(data);
        Debug.Log($"[FirebaseManager] Đã sync profile lên cloud.");
    }

    public void UpdateDisplayName(string newName)
    {
        LocalDisplayName = newName;
        if (PlayerDataManager.Instance?.Data != null)
        {
            PlayerDataManager.Instance.Data.playerName = newName;
            PlayerDataManager.Instance.SaveData();
        }
        if (IsConnected && IsAuthenticated)
            _root.Child("users").Child(LocalUserId).Child("displayName").SetValueAsync(newName);
    }

    // ==================== TIER & MATCH CONFIG ====================

    /// <summary>
    /// [PHASE-2] Tier được tính từ Rank Points, không phải Level.
    /// Bronze<500, Silver<1500, Gold<3000, Diamond<5000, Legend5000+.
    /// Tham số được đặt tên là `rankPoints` thay vì `level` từ Phase 2.
    /// </summary>
    public int GetPlayerTier(int rankPoints)
    {
        return PlayerData.ComputeTier(rankPoints);
    }

    /// <summary>
    /// [PHASE-2] Số câu/trận theo tier (economy-design v2.0 §2.2).
    /// Bronze=10, Silver=15, Gold=20, Diamond=25, Legend=30.
    /// </summary>
    public int GetQuestionCountForTier(int tier)
    {
        return tier switch {
            1 => 10,
            2 => 15,
            3 => 20,
            4 => 25,
            5 => 30,
            _ => 10
        };
    }
}
