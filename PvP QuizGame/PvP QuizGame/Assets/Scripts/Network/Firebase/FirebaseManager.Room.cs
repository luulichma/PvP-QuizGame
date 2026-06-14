using UnityEngine;
using System;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions;

/// <summary>
/// [REFACTOR-P2] FirebaseManager — phần ROOM (join, presence, in-match sync, leave).
/// API cho FirebaseMatchProvider: GetRoomRef, ReadSeed, UpdateMyScore, SubmitMyAnswer,
/// HostAdvanceQuestion, HostEndMatch, LeaveRoom.
/// [PHASE-2 HOOK] Tier/Rank plan: push thêm tierRankPoints/currentTier khi end match tại đây.
/// </summary>
public partial class FirebaseManager
{
    private string _roomToDelete;

    private void JoinExistingRoom(string roomId)
    {
        _root.Child("rooms").Child(roomId).GetValueAsync().ContinueWithOnMainThread(t => {
            if (t.IsFaulted) { OnMatchmakingError?.Invoke("Không đọc được room."); return; }

            // FIX-CANCEL: Nếu đã cancel trong lúc đọc room → bỏ qua
            if (_isMatchmakingCancelled)
            {
                Debug.Log("[FirebaseManager] Matchmaking đã bị cancel trong lúc JoinExistingRoom — bỏ qua.");
                return;
            }

            var snap = t.Result;

            CurrentRoomId = roomId;

            // Tìm uid đối thủ
            foreach (var child in snap.Child("players").Children)
            {
                if (child.Key != LocalUserId)
                {
                    OpponentId = child.Key;
                    var nameNode = child.Child("name");
                    if (nameNode.Value != null) OpponentName = nameNode.Value.ToString();

                    var avatarNode = child.Child("avatar");
                    if (avatarNode.Value != null) OpponentAvatarIndex = int.Parse(avatarNode.Value.ToString());
                    else OpponentAvatarIndex = 0;

                    break;
                }
            }

            IsHost = string.Compare(LocalUserId, OpponentId, StringComparison.Ordinal) < 0;
            SetupRoomPresence();
            Debug.Log($"[FirebaseManager] Đã join room {roomId} (Host={IsHost}, Opp={OpponentName}).");
            // UX-06: Hủy timeout khi match thành công
            if (_matchmakingTimeoutCoroutine != null)
            {
                StopCoroutine(_matchmakingTimeoutCoroutine);
                _matchmakingTimeoutCoroutine = null;
            }
            OnMatchFound?.Invoke();
        });
    }

    // ==================== PRESENCE ====================

    private void SetupRoomPresence()
    {
        if (string.IsNullOrEmpty(CurrentRoomId) || string.IsNullOrEmpty(LocalUserId)) return;

        // Đặt mình online + onDisconnect set offline
        var presenceRef = _root.Child("rooms").Child(CurrentRoomId).Child("players").Child(LocalUserId).Child("online");
        presenceRef.SetValueAsync(true);
        presenceRef.OnDisconnect().SetValue(false);

        // Listen trạng thái online của đối thủ
        var oppPresence = _root.Child("rooms").Child(CurrentRoomId).Child("players").Child(OpponentId).Child("online");
        oppPresence.ValueChanged += OnOpponentPresenceChanged;
    }

    private void OnOpponentPresenceChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) return;
        if (args.Snapshot.Value == null) return;
        if (args.Snapshot.Value.ToString().ToLower() == "false")
        {
            Debug.LogWarning("[FirebaseManager] Đối thủ ngắt kết nối!");
            OnOpponentDisconnected?.Invoke();
        }
    }

    // ==================== ROOM API (cho FirebaseMatchProvider gọi) ====================

    public DatabaseReference GetRoomRef() =>
        string.IsNullOrEmpty(CurrentRoomId) ? null : _root.Child("rooms").Child(CurrentRoomId);

    public DatabaseReference GetRootRef() => _root;

    public async Task<int> ReadSeedFromRoom()
    {
        if (string.IsNullOrEmpty(CurrentRoomId)) return -1;
        var snap = await _root.Child("rooms").Child(CurrentRoomId).Child("seed").GetValueAsync();
        if (snap.Exists && int.TryParse(snap.Value.ToString(), out int seed)) return seed;
        return -1;
    }

    public async Task<int> ReadQuestionCountFromRoom()
    {
        if (string.IsNullOrEmpty(CurrentRoomId)) return 10;
        var snap = await _root.Child("rooms").Child(CurrentRoomId).Child("questionCount").GetValueAsync();
        if (snap.Exists && int.TryParse(snap.Value.ToString(), out int count)) return count;
        return 10;
    }

    public void UpdateMyScore(int score)
    {
        if (!IsConnected || !IsAuthenticated || string.IsNullOrEmpty(CurrentRoomId)) return;
        _root.Child("rooms").Child(CurrentRoomId).Child("scores").Child(LocalUserId).SetValueAsync(score);
    }

    public void SubmitMyAnswer(int answerIndex)
    {
        if (!IsConnected || !IsAuthenticated || string.IsNullOrEmpty(CurrentRoomId)) return;
        _root.Child("rooms").Child(CurrentRoomId).Child("answers").Child(LocalUserId).SetValueAsync(answerIndex);
    }

    /// <summary>Host gọi sau mỗi câu để clear answers và tăng currentQ.</summary>
    public void HostAdvanceQuestion(int newQuestionIndex)
    {
        if (!IsHost || string.IsNullOrEmpty(CurrentRoomId)) return;
        var roomRef = _root.Child("rooms").Child(CurrentRoomId);
        roomRef.Child("answers").RemoveValueAsync();
        roomRef.Child("currentQ").SetValueAsync(newQuestionIndex);
    }

    /// <summary>Host gọi khi trận kết thúc.</summary>
    public async Task HostEndMatch(string winnerUidOrDraw)
    {
        if (!IsHost || string.IsNullOrEmpty(CurrentRoomId)) return;
        var roomRef = _root.Child("rooms").Child(CurrentRoomId);
        await roomRef.Child("state").SetValueAsync("ended");
        await roomRef.Child("winner").SetValueAsync(winnerUidOrDraw);
    }

    /// <summary>Cleanup khi rời room (về Home).</summary>
    public void LeaveRoom()
    {
        if (!IsConnected || string.IsNullOrEmpty(CurrentRoomId)) return;

        // Bỏ presence listener
        if (!string.IsNullOrEmpty(OpponentId))
        {
            _root.Child("rooms").Child(CurrentRoomId).Child("players").Child(OpponentId)
                 .Child("online").ValueChanged -= OnOpponentPresenceChanged;
        }

        // Đặt mình offline
        _root.Child("rooms").Child(CurrentRoomId).Child("players").Child(LocalUserId)
             .Child("online").SetValueAsync(false);

        // Host xoá toàn bộ room sau 5s (cho cả 2 client kịp đọc trạng thái ended)
        if (IsHost)
        {
            var roomToDelete = CurrentRoomId;
            Invoke(nameof(DeleteRoomDelayed), 5f);
            _roomToDelete = roomToDelete;
        }

        CurrentRoomId = null;
        OpponentId = null;
        OpponentName = null;
        IsHost = false;
    }

    private void DeleteRoomDelayed()
    {
        if (!string.IsNullOrEmpty(_roomToDelete) && _root != null)
        {
            _root.Child("rooms").Child(_roomToDelete).RemoveValueAsync();
            _roomToDelete = null;
        }
    }
}
