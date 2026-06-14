using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [PHASE-2] Popup hiện khi user vào HomeScene sau khi mùa giải kết thúc.
/// Tự build VisualElement (không cần VisualTreeAsset template) — tránh phải wiring trong Editor.
///
/// Hiển thị: tên mùa kết thúc, tier đạt được, danh sách reward (money + power-up), tier mới.
/// </summary>
public class EndOfSeasonPopupController
{
    private readonly VisualElement _parent;
    private VisualElement _root;

    public bool IsOpen => _root != null && _root.parent != null;

    public EndOfSeasonPopupController(VisualElement parent)
    {
        _parent = parent;
    }

    public void Show(SeasonManager.EndOfSeasonResult res)
    {
        if (IsOpen || _parent == null) return;

        var l = LocalizationManager.Instance;
        string title = l != null && l.IsReady
            ? string.Format(l.GetText("season_ended_title", "MÙA {0} ĐÃ KẾT THÚC"), res.seasonId)
            : $"MÙA {res.seasonId} ĐÃ KẾT THÚC";
        string subtitle = l != null && l.IsReady
            ? l.GetText("season_ended_subtitle", "Phần thưởng mùa giải")
            : "Phần thưởng mùa giải";
        string tierName = l != null && l.IsReady
            ? l.GetText($"tier_{res.newTier}_name", res.newTier.ToString())
            : res.newTier.ToString();
        string demoted = l != null && l.IsReady
            ? string.Format(l.GetText("season_ended_demoted", "Bạn bị giáng xuống {0}"), tierName)
            : $"Bạn bị giáng xuống {tierName}";
        string claimText = l != null && l.IsReady
            ? l.GetText("season_ended_claim", "NHẬN THƯỞNG")
            : "NHẬN THƯỞNG";

        _root = new VisualElement();
        _root.style.position = Position.Absolute;
        _root.style.top = 0; _root.style.bottom = 0; _root.style.left = 0; _root.style.right = 0;
        _root.style.alignItems = Align.Center;
        _root.style.justifyContent = Justify.Center;
        _root.style.backgroundColor = new Color(0, 0, 0, 0.78f);

        var card = new VisualElement();
        card.style.minWidth = 500;
        card.style.maxWidth = 800;
        card.style.backgroundColor = new Color(0.10f, 0.05f, 0.18f, 0.95f);
        card.AddToClassList("glass-panel");
        card.style.borderTopLeftRadius = 24; card.style.borderTopRightRadius = 24;
        card.style.borderBottomLeftRadius = 24; card.style.borderBottomRightRadius = 24;
        card.style.paddingTop = 30; card.style.paddingBottom = 30;
        card.style.paddingLeft = 32; card.style.paddingRight = 32;
        _root.Add(card);

        var titleLabel = new Label(title);
        titleLabel.style.color = new Color(1f, 0.84f, 0.4f);
        titleLabel.style.fontSize = 36;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        titleLabel.style.marginBottom = 12;
        card.Add(titleLabel);

        if (!string.IsNullOrEmpty(res.badge))
        {
            // [Icon Fix] Parse badge "T{tier}S{season}" — render icon + label "S{n}".
            // Backward compat: nếu badge cũ vẫn chứa emoji (saved data trước), fallback raw.
            (string iconCls, string tintCls, string display) = ParseBadge(res.badge);
            var badgeRow = UIIconHelper.MakeIconLabel(iconCls, $"+ {display}",
                tintCls, iconSizePx: 36f, fontSizePx: 30, textColor: Color.white);
            badgeRow.style.justifyContent = Justify.Center;
            badgeRow.style.marginBottom = 8;
            card.Add(badgeRow);
        }

        var subtitleLabel = new Label(subtitle);
        subtitleLabel.style.color = new Color(1, 1, 1, 0.65f);
        subtitleLabel.style.fontSize = 22;
        subtitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        subtitleLabel.style.marginBottom = 16;
        card.Add(subtitleLabel);

        // [Icon Fix] Reward rows = PNG icon + Label
        var moneyRow = UIIconHelper.MakeIconLabel("icon-coins", $"+{res.rewardMoney}$",
            IconTint.Money, iconSizePx: 32f, fontSizePx: 32, textColor: new Color(1f, 0.82f, 0.4f));
        moneyRow.style.justifyContent = Justify.Center;
        var moneyLbl = moneyRow.Q<Label>();
        if (moneyLbl != null) moneyLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        moneyRow.style.marginBottom = 8;
        card.Add(moneyRow);

        if (res.rewardPowerUps != null)
        {
            foreach (var kv in res.rewardPowerUps)
            {
                string iconCls = kv.Key switch
                {
                    PowerUpManager.PU_5050   => "icon-scissors",
                    PowerUpManager.PU_TIME   => "icon-timer",
                    PowerUpManager.PU_SHIELD => "icon-shield",
                    _ => "icon-gift"
                };
                var line = UIIconHelper.MakeIconLabel(iconCls, $"×{kv.Value}",
                    null, iconSizePx: 26f, fontSizePx: 26, textColor: Color.white);
                line.style.justifyContent = Justify.Center;
                line.style.marginBottom = 4;
                card.Add(line);
            }
        }

        var demotedLabel = new Label(demoted);
        demotedLabel.style.color = new Color(1f, 0.4f, 0.5f);
        demotedLabel.style.fontSize = 22;
        demotedLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        demotedLabel.style.marginTop = 12;
        demotedLabel.style.marginBottom = 18;
        card.Add(demotedLabel);

        var btn = new Button(() => Close()) { text = claimText };
        btn.AddToClassList("btn");
        btn.AddToClassList("btn-glow");
        btn.style.height = 76;
        btn.style.fontSize = 28;
        card.Add(btn);

        _parent.Add(_root);
    }

    public void Close()
    {
        if (!IsOpen) return;
        _root.RemoveFromHierarchy();
        _root = null;
    }

    /// <summary>
    /// [Icon Fix] Parse format badge "T{tier}S{season}" → icon class + tint + display text.
    /// Fallback an toàn cho badge cũ (có emoji): hiện raw, không icon.
    /// </summary>
    private static (string iconCls, string tintCls, string display) ParseBadge(string badge)
    {
        if (!string.IsNullOrEmpty(badge) && badge.Length >= 4 && badge[0] == 'T')
        {
            int sIdx = badge.IndexOf('S');
            if (sIdx > 1 && int.TryParse(badge.Substring(1, sIdx - 1), out int tier))
            {
                string season = badge.Substring(sIdx); // "S2"
                switch (tier)
                {
                    case 1: return ("icon-award", IconTint.Bronze,  season);
                    case 2: return ("icon-award", IconTint.Silver,  season);
                    case 3: return ("icon-award", IconTint.Gold,    season);
                    case 4: return ("icon-gem",   IconTint.Diamond, season);
                    case 5: return ("icon-crown", IconTint.Legend,  season);
                }
            }
        }
        // Badge cũ (chứa emoji) hoặc lạ — vẫn hiện text raw, dùng icon mặc định
        return ("icon-trophy", IconTint.Gold, badge);
    }
}
