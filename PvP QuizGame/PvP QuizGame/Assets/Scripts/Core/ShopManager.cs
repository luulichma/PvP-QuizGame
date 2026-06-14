using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [PHASE-2] Quản lý logic mua Power-Up & Bundles.
///
/// Theo economy-design v2.0:
///   §5.2 Power-Up đơn: 50:50=150$, +Time=100$, Shield=200$
///   §6.3 Bundles:
///       starter (Khởi Động):  2× 5050 + 3× Time            = 500$ (gốc 600)
///       warrior (Chiến Binh): 1× 5050 + 2× Time + 1× Shield = 400$ (gốc 550)
///       champion (Vô Địch):   3× 5050 + 3× Time + 3× Shield = 1000$ (gốc 1350)
///   Bulk x5 -10% (làm tròn) cho mua đơn.
///
/// Trạng thái lưu trong PlayerData (đã có sẵn các field power-up từ Bước 1.1).
/// </summary>
public static class ShopManager
{
    // ==================== EVENTS ====================
    public static event Action<string, int> OnPurchaseSuccess; // (itemId, quantity)
    public static event Action<string, string> OnPurchaseFailed; // (itemId, reason)

    // ==================== PRICING ====================
    public const int PRICE_5050   = 150;
    public const int PRICE_TIME   = 100;
    public const int PRICE_SHIELD = 200;

    public const float BULK_DISCOUNT = 0.10f; // x5 giảm 10%

    public const int PRICE_BUNDLE_STARTER  = 500;
    public const int PRICE_BUNDLE_WARRIOR  = 400;
    public const int PRICE_BUNDLE_CHAMPION = 1000;

    public const string BUNDLE_STARTER  = "bundle_starter";
    public const string BUNDLE_WARRIOR  = "bundle_warrior";
    public const string BUNDLE_CHAMPION = "bundle_champion";

    // ==================== PRICE LOOKUP ====================
    public static int GetUnitPrice(string itemId)
    {
        return itemId switch
        {
            PowerUpManager.PU_5050   => PRICE_5050,
            PowerUpManager.PU_TIME   => PRICE_TIME,
            PowerUpManager.PU_SHIELD => PRICE_SHIELD,
            _ => 0
        };
    }

    /// <summary>Tính giá khi mua quantity đơn vị (áp BULK_DISCOUNT nếu quantity >= 5).</summary>
    public static int GetTotalPrice(string itemId, int quantity)
    {
        int unit = GetUnitPrice(itemId);
        if (unit <= 0 || quantity <= 0) return 0;
        int total = unit * quantity;
        if (quantity >= 5) total = Mathf.RoundToInt(total * (1f - BULK_DISCOUNT));
        return total;
    }

    // ==================== BUY POWER-UP ====================
    public static bool BuyItem(string itemId, int quantity = 1)
    {
        if (quantity <= 0)
        {
            OnPurchaseFailed?.Invoke(itemId, "invalid_quantity");
            return false;
        }

        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null)
        {
            OnPurchaseFailed?.Invoke(itemId, "no_data");
            return false;
        }

        int unitPrice = GetUnitPrice(itemId);
        if (unitPrice <= 0)
        {
            OnPurchaseFailed?.Invoke(itemId, "unknown_item");
            return false;
        }

        int total = GetTotalPrice(itemId, quantity);
        if (!pd.TrySpendMoney(total))
        {
            Debug.LogWarning($"[ShopManager] Không đủ tiền: cần {total}$, có {pd.money}$");
            OnPurchaseFailed?.Invoke(itemId, "insufficient_money");
            return false;
        }

        pd.AddPowerUp(itemId, quantity);
        Persist();

        Debug.Log($"<color=green>[ShopManager] Mua {itemId} ×{quantity} (−{total}$). Tổng: {pd.GetPowerUpCount(itemId)}, tiền còn: {pd.money}$</color>");
        OnPurchaseSuccess?.Invoke(itemId, quantity);
        return true;
    }

    // ==================== BUY BUNDLE ====================
    public static bool BuyBundle(string bundleId)
    {
        var pd = PlayerDataManager.Instance?.Data;
        if (pd == null)
        {
            OnPurchaseFailed?.Invoke(bundleId, "no_data");
            return false;
        }

        var contents = GetBundleContents(bundleId);
        int price = GetBundlePrice(bundleId);
        if (contents == null || price <= 0)
        {
            OnPurchaseFailed?.Invoke(bundleId, "unknown_bundle");
            return false;
        }

        if (!pd.TrySpendMoney(price))
        {
            OnPurchaseFailed?.Invoke(bundleId, "insufficient_money");
            return false;
        }

        foreach (var kv in contents)
        {
            pd.AddPowerUp(kv.Key, kv.Value);
        }
        Persist();

        Debug.Log($"<color=green>[ShopManager] Mua {bundleId} (−{price}$). Tiền còn: {pd.money}$</color>");
        OnPurchaseSuccess?.Invoke(bundleId, 1);
        return true;
    }

    public static int GetBundlePrice(string bundleId) => bundleId switch
    {
        BUNDLE_STARTER  => PRICE_BUNDLE_STARTER,
        BUNDLE_WARRIOR  => PRICE_BUNDLE_WARRIOR,
        BUNDLE_CHAMPION => PRICE_BUNDLE_CHAMPION,
        _ => 0
    };

    public static Dictionary<string, int> GetBundleContents(string bundleId) => bundleId switch
    {
        BUNDLE_STARTER => new Dictionary<string, int> {
            { PowerUpManager.PU_5050, 2 },
            { PowerUpManager.PU_TIME, 3 }
        },
        BUNDLE_WARRIOR => new Dictionary<string, int> {
            { PowerUpManager.PU_5050, 1 },
            { PowerUpManager.PU_TIME, 2 },
            { PowerUpManager.PU_SHIELD, 1 }
        },
        BUNDLE_CHAMPION => new Dictionary<string, int> {
            { PowerUpManager.PU_5050, 3 },
            { PowerUpManager.PU_TIME, 3 },
            { PowerUpManager.PU_SHIELD, 3 }
        },
        _ => null
    };

    // ==================== PERSIST ====================
    private static void Persist()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SaveData();
        }
        // Sync cloud nếu authenticated (fire-and-forget)
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsConnected && FirebaseManager.Instance.IsAuthenticated)
        {
            _ = FirebaseManager.Instance.SaveProfileToCloud();
        }
    }
}
