using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// [PHASE-2] Controller cho PowerUpBar trong trận đấu.
///
/// Wire trực tiếp 3 nút (pu-5050-btn, pu-time-btn, pu-shield-btn) → PowerUpManager.
/// Refresh UI khi:
///   - Mở trận (Attach)
///   - PowerUpManager.OnPowerUpUsed / OnShieldSaved
///   - Vào câu mới (qua RefreshOnNewQuestion) — để cập nhật trạng thái disable.
///
/// Local-only (không sync sang đối thủ — xem plan_note_new_tier_rank §3).
/// </summary>
public class PowerUpHUDController
{
    private readonly VisualElement _root;

    private Button _btn5050;
    private Button _btnTime;
    private Button _btnShield;
    private Label _count5050;
    private Label _countTime;
    private Label _countShield;
    private Label _name5050;
    private Label _nameTime;
    private Label _nameShield;

    private Action _on5050;
    private Action _onTime;
    private Action _onShield;
    private Action<string> _onUsed;
    private Action _onShieldSaved;
    private Action _onLang;

    public PowerUpHUDController(VisualElement root)
    {
        _root = root;
    }

    public void Attach()
    {
        if (_root == null) return;

        _btn5050  = _root.Q<Button>("pu-5050-btn");
        _btnTime  = _root.Q<Button>("pu-time-btn");
        _btnShield = _root.Q<Button>("pu-shield-btn");

        _count5050  = _root.Q<Label>("pu-5050-count");
        _countTime  = _root.Q<Label>("pu-time-count");
        _countShield = _root.Q<Label>("pu-shield-count");

        _name5050  = _root.Q<Label>("pu-5050-label");
        _nameTime  = _root.Q<Label>("pu-time-label");
        _nameShield = _root.Q<Label>("pu-shield-label");

        if (_btn5050 != null)
        {
            _on5050 = () => { PowerUpManager.Instance?.Use5050(); };
            _btn5050.clicked += _on5050;
        }
        if (_btnTime != null)
        {
            _onTime = () => { PowerUpManager.Instance?.UseExtraTime(); };
            _btnTime.clicked += _onTime;
        }
        if (_btnShield != null)
        {
            _onShield = () => { PowerUpManager.Instance?.UseShield(); };
            _btnShield.clicked += _onShield;
        }

        _onUsed = (_) => Refresh();
        _onShieldSaved = Refresh;
        _onLang = LocalizeLabels;
        PowerUpManager.OnPowerUpUsed += _onUsed;
        PowerUpManager.OnShieldSaved += _onShieldSaved;
        LocalizationManager.OnLanguageChanged += _onLang;

        LocalizeLabels();
        Refresh();
    }

    public void Detach()
    {
        if (_btn5050 != null && _on5050 != null) _btn5050.clicked -= _on5050;
        if (_btnTime != null && _onTime != null) _btnTime.clicked -= _onTime;
        if (_btnShield != null && _onShield != null) _btnShield.clicked -= _onShield;
        if (_onUsed != null) PowerUpManager.OnPowerUpUsed -= _onUsed;
        if (_onShieldSaved != null) PowerUpManager.OnShieldSaved -= _onShieldSaved;
        if (_onLang != null) LocalizationManager.OnLanguageChanged -= _onLang;
    }

    /// <summary>Gọi khi vào câu mới — cập nhật trạng thái nút (không thay đổi count).</summary>
    public void RefreshOnNewQuestion() => Refresh();

    private void LocalizeLabels()
    {
        var lm = LocalizationManager.Instance;
        if (lm == null) return;
        if (_name5050 != null) _name5050.text = lm.GetText("pu_5050_short", "50:50");
        if (_nameTime != null) _nameTime.text = lm.GetText("pu_time_short", "+5s");
        if (_nameShield != null) _nameShield.text = lm.GetText("pu_shield_short", "Shield");
    }

    private void Refresh()
    {
        var pd = PlayerDataManager.Instance?.Data;
        var pum = PowerUpManager.Instance;
        if (pd == null || pum == null) return;

        SetButtonState(_btn5050, _count5050, pd.powerUp_5050, pum.HasUsed5050InMatch, pum.CanUse(PowerUpManager.PU_5050));
        SetButtonState(_btnTime, _countTime, pd.powerUp_extraTime, pum.HasUsedTimeInMatch, pum.CanUse(PowerUpManager.PU_TIME));
        SetButtonState(_btnShield, _countShield, pd.powerUp_shield, pum.HasUsedShieldInMatch, pum.CanUse(PowerUpManager.PU_SHIELD));

        // Shield active → highlight nút
        if (_btnShield != null)
        {
            if (pum.IsShieldActive)
                _btnShield.AddToClassList("powerup-active");
            else
                _btnShield.RemoveFromClassList("powerup-active");
        }
    }

    private void SetButtonState(Button btn, Label countLabel, int count, bool usedInMatch, bool canUse)
    {
        if (btn == null) return;
        if (countLabel != null) countLabel.text = $"x{count}";

        btn.SetEnabled(canUse);
        if (canUse)
            btn.RemoveFromClassList("powerup-disabled");
        else
        {
            if (!btn.ClassListContains("powerup-disabled"))
                btn.AddToClassList("powerup-disabled");
        }
    }
}
