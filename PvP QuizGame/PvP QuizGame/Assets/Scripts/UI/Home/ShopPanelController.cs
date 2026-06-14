using System;
using UnityEngine.UIElements;

/// <summary>
/// [PHASE-2] Panel Cửa Hàng — load tiền, item, wire button click → ShopManager.
///
/// UI hierarchy đến từ ShopPanel.uxml (Instance trong HomeLayout.uxml#shop-panel).
/// </summary>
public class ShopPanelController
{
    private readonly VisualElement _root; // toàn bộ home root
    private readonly UILocalizer _localizer = new UILocalizer();

    // Money
    private Label _balanceLabel;

    // Power-up cards
    private Label _owned5050, _ownedTime, _ownedShield;
    private Label _price5050, _priceTime, _priceShield;
    private Button _buy5050x1, _buy5050x5;
    private Button _buyTimex1, _buyTimex5;
    private Button _buyShieldx1, _buyShieldx5;

    // Bundles
    private Label _priceStarter, _priceWarrior, _priceChampion;
    private Button _buyStarter, _buyWarrior, _buyChampion;

    // Event handlers (lưu reference để Detach)
    private Action _on5050x1, _on5050x5, _onTimex1, _onTimex5, _onShieldx1, _onShieldx5;
    private Action _onStarter, _onWarrior, _onChampion;
    private Action<string, int> _onPurchaseSuccess;
    private Action<string, string> _onPurchaseFailed;

    // [UI Refactor] Anchor button của thao tác cuối — fly text spawn gần button đó
    private VisualElement _lastClickedAnchor;

    public ShopPanelController(VisualElement root)
    {
        _root = root;

        _balanceLabel = root.Q<Label>("shop-balance-label");

        // Owned counts
        _owned5050  = root.Q<Label>("card-5050-owned");
        _ownedTime  = root.Q<Label>("card-time-owned");
        _ownedShield = root.Q<Label>("card-shield-owned");

        // Prices
        _price5050  = root.Q<Label>("card-5050-price");
        _priceTime  = root.Q<Label>("card-time-price");
        _priceShield = root.Q<Label>("card-shield-price");
        _priceStarter = root.Q<Label>("card-starter-price");
        _priceWarrior = root.Q<Label>("card-warrior-price");
        _priceChampion = root.Q<Label>("card-champion-price");

        // Buy buttons
        _buy5050x1 = root.Q<Button>("card-5050-buy1");
        _buy5050x5 = root.Q<Button>("card-5050-buy5");
        _buyTimex1 = root.Q<Button>("card-time-buy1");
        _buyTimex5 = root.Q<Button>("card-time-buy5");
        _buyShieldx1 = root.Q<Button>("card-shield-buy1");
        _buyShieldx5 = root.Q<Button>("card-shield-buy5");
        _buyStarter  = root.Q<Button>("card-starter-buy");
        _buyWarrior  = root.Q<Button>("card-warrior-buy");
        _buyChampion = root.Q<Button>("card-champion-buy");

        // Localization
        _localizer.BindLabel(root.Q<Label>("shop-title-label"), "menu_shop_title", "CỬA HÀNG");
        _localizer.BindLabel(root.Q<Label>("shop-section-powerups"), "shop_section_powerups", "POWER-UP");
        _localizer.BindLabel(root.Q<Label>("shop-section-bundles"), "shop_section_bundles", "GÓI ƯU ĐÃI");
        _localizer.BindLabel(root.Q<Label>("card-5050-title"), "pu_5050_name", "50:50");
        _localizer.BindLabel(root.Q<Label>("card-5050-desc"), "pu_5050_desc", "Loại bỏ 2 đáp án sai");
        _localizer.BindLabel(root.Q<Label>("card-time-title"), "pu_time_name", "Thêm giờ (+5s)");
        _localizer.BindLabel(root.Q<Label>("card-time-desc"), "pu_time_desc", "+5 giây cho câu hỏi hiện tại");
        _localizer.BindLabel(root.Q<Label>("card-shield-title"), "pu_shield_name", "Lá chắn");
        _localizer.BindLabel(root.Q<Label>("card-shield-desc"), "pu_shield_desc", "Giữ streak khi trả lời sai");
        _localizer.BindLabel(root.Q<Label>("card-starter-title"), "bundle_starter_name", "Gói Khởi Động");
        _localizer.BindLabel(root.Q<Label>("card-starter-desc"), "bundle_starter_desc", "2× 50:50 + 3× Thêm giờ (Tiết kiệm 17%)");
        _localizer.BindLabel(root.Q<Label>("card-warrior-title"), "bundle_warrior_name", "Gói Chiến Binh");
        _localizer.BindLabel(root.Q<Label>("card-warrior-desc"), "bundle_warrior_desc", "1× 50:50 + 2× Thêm giờ + 1× Lá chắn (Tiết kiệm 27%)");
        _localizer.BindLabel(root.Q<Label>("card-champion-title"), "bundle_champion_name", "Gói Vô Địch");
        _localizer.BindLabel(root.Q<Label>("card-champion-desc"), "bundle_champion_desc", "3× 50:50 + 3× Thêm giờ + 3× Lá chắn (Tiết kiệm 26%)");
        // Buttons "MUA x1" / "MUA x5"
        _localizer.Bind(l => {
            string buy1 = l.GetText("shop_buy_x1", "MUA x1");
            string buy5 = l.GetText("shop_buy_x5", "MUA x5 -10%");
            string buyBundle = l.GetText("shop_buy_bundle", "MUA NGAY");
            if (_buy5050x1 != null) _buy5050x1.text = buy1;
            if (_buyTimex1 != null) _buyTimex1.text = buy1;
            if (_buyShieldx1 != null) _buyShieldx1.text = buy1;
            if (_buy5050x5 != null) _buy5050x5.text = buy5;
            if (_buyTimex5 != null) _buyTimex5.text = buy5;
            if (_buyShieldx5 != null) _buyShieldx5.text = buy5;
            if (_buyStarter != null) _buyStarter.text = buyBundle;
            if (_buyWarrior != null) _buyWarrior.text = buyBundle;
            if (_buyChampion != null) _buyChampion.text = buyBundle;
        });
    }

    public void Attach()
    {
        // Wire click handlers — track anchor để fly text bay ra từ button đó
        _on5050x1 = () => { _lastClickedAnchor = _buy5050x1; TryBuy(PowerUpManager.PU_5050, 1); };
        _on5050x5 = () => { _lastClickedAnchor = _buy5050x5; TryBuy(PowerUpManager.PU_5050, 5); };
        _onTimex1 = () => { _lastClickedAnchor = _buyTimex1; TryBuy(PowerUpManager.PU_TIME, 1); };
        _onTimex5 = () => { _lastClickedAnchor = _buyTimex5; TryBuy(PowerUpManager.PU_TIME, 5); };
        _onShieldx1 = () => { _lastClickedAnchor = _buyShieldx1; TryBuy(PowerUpManager.PU_SHIELD, 1); };
        _onShieldx5 = () => { _lastClickedAnchor = _buyShieldx5; TryBuy(PowerUpManager.PU_SHIELD, 5); };
        _onStarter  = () => { _lastClickedAnchor = _buyStarter;  TryBuyBundle(ShopManager.BUNDLE_STARTER); };
        _onWarrior  = () => { _lastClickedAnchor = _buyWarrior;  TryBuyBundle(ShopManager.BUNDLE_WARRIOR); };
        _onChampion = () => { _lastClickedAnchor = _buyChampion; TryBuyBundle(ShopManager.BUNDLE_CHAMPION); };

        if (_buy5050x1 != null) _buy5050x1.clicked += _on5050x1;
        if (_buy5050x5 != null) _buy5050x5.clicked += _on5050x5;
        if (_buyTimex1 != null) _buyTimex1.clicked += _onTimex1;
        if (_buyTimex5 != null) _buyTimex5.clicked += _onTimex5;
        if (_buyShieldx1 != null) _buyShieldx1.clicked += _onShieldx1;
        if (_buyShieldx5 != null) _buyShieldx5.clicked += _onShieldx5;
        if (_buyStarter != null)  _buyStarter.clicked += _onStarter;
        if (_buyWarrior != null)  _buyWarrior.clicked += _onWarrior;
        if (_buyChampion != null) _buyChampion.clicked += _onChampion;

        // ShopManager events
        _onPurchaseSuccess = (id, qty) => { Refresh(); ShowSuccess(id, qty); };
        _onPurchaseFailed = (id, reason) => ShowFailure(id, reason);
        ShopManager.OnPurchaseSuccess += _onPurchaseSuccess;
        ShopManager.OnPurchaseFailed += _onPurchaseFailed;

        _localizer.Attach();
        _localizer.Refresh();
        Refresh();
    }

    public void Detach()
    {
        if (_buy5050x1 != null && _on5050x1 != null) _buy5050x1.clicked -= _on5050x1;
        if (_buy5050x5 != null && _on5050x5 != null) _buy5050x5.clicked -= _on5050x5;
        if (_buyTimex1 != null && _onTimex1 != null) _buyTimex1.clicked -= _onTimex1;
        if (_buyTimex5 != null && _onTimex5 != null) _buyTimex5.clicked -= _onTimex5;
        if (_buyShieldx1 != null && _onShieldx1 != null) _buyShieldx1.clicked -= _onShieldx1;
        if (_buyShieldx5 != null && _onShieldx5 != null) _buyShieldx5.clicked -= _onShieldx5;
        if (_buyStarter != null && _onStarter != null) _buyStarter.clicked -= _onStarter;
        if (_buyWarrior != null && _onWarrior != null) _buyWarrior.clicked -= _onWarrior;
        if (_buyChampion != null && _onChampion != null) _buyChampion.clicked -= _onChampion;

        if (_onPurchaseSuccess != null) ShopManager.OnPurchaseSuccess -= _onPurchaseSuccess;
        if (_onPurchaseFailed != null) ShopManager.OnPurchaseFailed -= _onPurchaseFailed;

        _localizer.Detach();
    }

    /// <summary>Cập nhật UI từ PlayerData (tiền + count items + giá hiển thị).</summary>
    public void Refresh()
    {
        var pd = PlayerDataManager.Instance?.Data;
        var l = LocalizationManager.Instance;
        if (pd == null) return;

        if (_balanceLabel != null) _balanceLabel.text = $"${pd.money:N0}";

        string ownedFmt = l != null && l.IsReady ? l.GetText("shop_owned", "Có: {0}") : "Có: {0}";
        if (_owned5050 != null)  _owned5050.text = string.Format(ownedFmt, pd.powerUp_5050);
        if (_ownedTime != null)  _ownedTime.text = string.Format(ownedFmt, pd.powerUp_extraTime);
        if (_ownedShield != null) _ownedShield.text = string.Format(ownedFmt, pd.powerUp_shield);

        if (_price5050 != null) _price5050.text = $"{ShopManager.PRICE_5050}$";
        if (_priceTime != null) _priceTime.text = $"{ShopManager.PRICE_TIME}$";
        if (_priceShield != null) _priceShield.text = $"{ShopManager.PRICE_SHIELD}$";
        if (_priceStarter != null) _priceStarter.text = $"{ShopManager.PRICE_BUNDLE_STARTER}$";
        if (_priceWarrior != null) _priceWarrior.text = $"{ShopManager.PRICE_BUNDLE_WARRIOR}$";
        if (_priceChampion != null) _priceChampion.text = $"{ShopManager.PRICE_BUNDLE_CHAMPION}$";
    }

    private void TryBuy(string itemId, int qty)
    {
        ShopManager.BuyItem(itemId, qty);
    }

    private void TryBuyBundle(string bundleId)
    {
        ShopManager.BuyBundle(bundleId);
    }

    private void ShowSuccess(string id, int qty)
    {
        // [Icon Fix] Bỏ emoji trong fly text (FlyTextService chỉ render text plain
        // — không có font emoji fallback trên Android build). Tên power-up đã
        // đủ nhận dạng trong context vừa mua.
        var l = LocalizationManager.Instance;
        bool isBundle = id == ShopManager.BUNDLE_STARTER
                     || id == ShopManager.BUNDLE_WARRIOR
                     || id == ShopManager.BUNDLE_CHAMPION;
        string txt;
        if (isBundle)
        {
            string okText = (l != null && l.IsReady) ? l.GetText("shop_buy_success_quick", "ĐÃ MUA!") : "ĐÃ MUA!";
            txt = okText;
        }
        else
        {
            string itemName = id switch
            {
                PowerUpManager.PU_5050   => "50:50",
                PowerUpManager.PU_TIME   => "+5s",
                PowerUpManager.PU_SHIELD => "Shield",
                _ => "Item"
            };
            txt = $"+{qty} {itemName}";
        }
        FlyTextService.SpawnSuccess(_lastClickedAnchor, txt);
    }

    private void ShowFailure(string id, string reason)
    {
        var l = LocalizationManager.Instance;
        string key = reason switch
        {
            "insufficient_money" => "shop_buy_failed_quick",
            "already_used"        => "powerup_already_used",
            "empty"               => "powerup_empty",
            _ => "shop_purchase_failed"
        };
        string fallback = reason switch
        {
            "insufficient_money" => "THIẾU TIỀN!",
            "already_used"        => "ĐÃ DÙNG!",
            "empty"               => "HẾT!",
            _ => "LỖI!"
        };
        string msg = (l != null && l.IsReady) ? l.GetText(key, fallback) : fallback;
        FlyTextService.SpawnError(_lastClickedAnchor, msg);
    }
}
