using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [UI Refactor] Wrapper cho rank-panel: Season banner + 5 tier filter chip + Leaderboard.
///
/// Hành vi:
///  - Khi vào Rank tab (Load) → refresh banner + default chọn tier hiện tại của user → load BXH.
///  - User bấm tier khác → reload BXH theo tier đó (không đụng banner).
///  - Intermission state → banner đổi style vàng + message admin + countdown nextSeason.
///  - Auto refresh banner mỗi 60s qua coroutine (để countdown sống).
/// </summary>
public class RankPanelController
{
    private readonly MonoBehaviour _runner;
    private readonly LeaderboardPanelController _leaderboard;
    private readonly UILocalizer _localizer = new UILocalizer();

    // Season banner
    private readonly VisualElement _banner;
    private readonly Label _bannerTitle;
    private readonly Label _bannerInfo;
    private readonly Label _bannerMessage;
    private readonly Label _bannerCountdown;

    // Tier filter
    private readonly Button[] _tierTabs = new Button[5]; // index 0 = tier 1 (Bronze)
    private int _selectedTier = 1;

    private Coroutine _tickCoroutine;

    public RankPanelController(VisualElement root, LeaderboardPanelController leaderboard, MonoBehaviour runner)
    {
        _runner = runner;
        _leaderboard = leaderboard;

        _banner = root.Q<VisualElement>("rank-season-banner");
        _bannerTitle = root.Q<Label>("rank-season-title");
        _bannerInfo = root.Q<Label>("rank-season-info");
        _bannerMessage = root.Q<Label>("rank-season-message");
        _bannerCountdown = root.Q<Label>("rank-season-countdown");

        for (int i = 0; i < 5; i++)
        {
            int idx = i; // capture
            _tierTabs[i] = root.Q<Button>($"tier-tab-{i + 1}");
            if (_tierTabs[i] != null)
                _tierTabs[i].clicked += () => SelectTier(idx + 1);
        }

        // [Icon Fix] Localize tier tab labels — icon đã render bằng PNG <VisualElement>
        // trong UXML, ở đây chỉ thay text của Label con bên trong button.
        _localizer.Bind(l =>
        {
            for (int i = 0; i < 5; i++)
            {
                if (_tierTabs[i] == null) continue;
                var lbl = _tierTabs[i].Q<Label>();
                if (lbl != null) lbl.text = l.GetText($"tier_{i + 1}_name", "?").ToUpper();
            }
        });
    }

    public void Attach()
    {
        _localizer.Attach();
        _localizer.Refresh();
    }

    public void Detach()
    {
        _localizer.Detach();
        StopTick();
    }

    /// <summary>Gọi từ HomeNavController.onShowRank khi user vào tab.</summary>
    public void Load()
    {
        // Default chọn tier hiện tại của user
        int myTier = PlayerDataManager.Instance?.Data?.currentTier ?? 1;
        SelectTier(myTier);
        RefreshBanner();

        // Tick countdown cho banner intermission (chỉ chạy khi panel mở)
        StartTick();
    }

    /// <summary>Gọi khi rời tab Rank (HomeNav switch sang tab khác).</summary>
    public void OnHidden() => StopTick();

    private void SelectTier(int tier)
    {
        _selectedTier = Mathf.Clamp(tier, 1, 5);

        // Highlight tier tab active
        for (int i = 0; i < 5; i++)
        {
            if (_tierTabs[i] == null) continue;
            if (i + 1 == _selectedTier) _tierTabs[i].AddToClassList("tier-tab-active");
            else _tierTabs[i].RemoveFromClassList("tier-tab-active");
        }

        // Reload BXH cho tier mới
        _leaderboard?.Load(_selectedTier);
    }

    private void RefreshBanner()
    {
        if (_banner == null) return;
        var sm = SeasonManager.Instance;
        var l = LocalizationManager.Instance;
        bool intermission = sm != null && sm.IsIntermission;

        // Đổi style banner theo state
        if (intermission)
        {
            if (!_banner.ClassListContains("intermission-banner-style"))
                _banner.AddToClassList("intermission-banner-style");
        }
        else
        {
            _banner.RemoveFromClassList("intermission-banner-style");
        }

        if (intermission)
        {
            // Intermission: hiện "MÙA X SẮP MỞ" + message admin + countdown
            int nextId = sm.NextSeasonId > 0 ? sm.NextSeasonId : sm.CurrentSeason + 1;
            string titleFmt = (l != null && l.IsReady)
                ? l.GetText("season_intermission_title", "MÙA {0} SẮP MỞ")
                : "MÙA {0} SẮP MỞ";
            if (_bannerTitle != null) _bannerTitle.text = string.Format(titleFmt, nextId);

            string subInfo = (l != null && l.IsReady)
                ? l.GetText("season_intermission_label", "Mùa mới sắp mở!")
                : "Mùa mới sắp mở!";
            if (_bannerInfo != null) _bannerInfo.text = subInfo;

            // Message ưu tiên admin set
            if (_bannerMessage != null)
            {
                string msg = !string.IsNullOrEmpty(sm.IntermissionMessage)
                    ? sm.IntermissionMessage
                    : ((l != null && l.IsReady)
                        ? l.GetText("season_intermission_message_default", "Hãy chuẩn bị cho cuộc đua mới!")
                        : "Hãy chuẩn bị cho cuộc đua mới!");
                _bannerMessage.text = msg;
                _bannerMessage.style.display = DisplayStyle.Flex;
            }

            // Countdown — chỉ nếu admin set nextSeasonStartDate
            if (_bannerCountdown != null)
            {
                var ts = sm.TimeUntilNextSeason;
                if (ts.HasValue && ts.Value.TotalSeconds > 0)
                {
                    int days = ts.Value.Days;
                    int hours = ts.Value.Hours;
                    string fmt = (l != null && l.IsReady)
                        ? l.GetText("season_intermission_countdown", "Sau: {0}d {1}h")
                        : "Sau: {0}d {1}h";
                    _bannerCountdown.text = string.Format(fmt, days, hours);
                    _bannerCountdown.style.display = DisplayStyle.Flex;
                }
                else _bannerCountdown.style.display = DisplayStyle.None;
            }
        }
        else
        {
            // Normal hoặc SeasonManager chưa init → fallback hiển thị Mùa 1
            int season = sm?.CurrentSeason ?? 1;
            int daysLeft = sm?.DaysLeftInSeason ?? 30;

            if (_bannerTitle != null)
            {
                string fmt = (l != null && l.IsReady) ? l.GetText("rank_season_title", "MÙA {0}") : "MÙA {0}";
                _bannerTitle.text = string.Format(fmt, season);
            }
            if (_bannerInfo != null)
            {
                string fmt = (l != null && l.IsReady) ? l.GetText("rank_season_days_left", "Còn {0} ngày") : "Còn {0} ngày";
                _bannerInfo.text = string.Format(fmt, daysLeft);
            }
            if (_bannerMessage != null) _bannerMessage.style.display = DisplayStyle.None;
            if (_bannerCountdown != null) _bannerCountdown.style.display = DisplayStyle.None;
        }
    }

    private void StartTick()
    {
        if (_runner == null || _tickCoroutine != null) return;
        _tickCoroutine = _runner.StartCoroutine(TickRoutine());
    }

    private void StopTick()
    {
        if (_runner != null && _tickCoroutine != null)
        {
            _runner.StopCoroutine(_tickCoroutine);
            _tickCoroutine = null;
        }
    }

    private IEnumerator TickRoutine()
    {
        var wait = new WaitForSecondsRealtime(60f);
        while (true)
        {
            yield return wait;
            RefreshBanner();
        }
    }
}
