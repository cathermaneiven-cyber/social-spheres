using UnityEngine;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using GTAG.CosmeticSDK;

/// <summary>
/// Place this prefab in a map scene as a cosmetic shop trigger.
/// When the local player walks into the collider:
///   - If cost = 0, equips/unequips the cosmetic for free.
///   - If cost > 0, charges PlayFab currency and pays the map creator their cut.
///
/// This is one of the allowed prefabs map creators can place. No extra scripting needed.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ToggleCosmeticButton : MonoBehaviour
{
    [Header("Cosmetic")]
    [Tooltip("cosmeticId to give — must match a registered CosmeticDefinition.")]
    public string cosmeticId = "hat_tophat";

    [Header("Pricing")]
    [Tooltip("Set to 0 for free. Otherwise this amount of currency is charged.")]
    public int cost = 0;

    [Tooltip("PlayFab virtual currency code, e.g. 'GT' for GTag Coins.")]
    public string currencyCode = "GT";

    [Header("Map Creator Payout")]
    [Tooltip("PlayFab Player ID of the map creator. They receive the full purchase amount.")]
    public string creatorPlayFabId = "";

    [Header("UI (optional)")]
    [Tooltip("World-space UI shown when player is in range. Leave empty if not needed.")]
    public GameObject promptUI;

    [Tooltip("Label that shows the item name and price. Leave empty if not needed.")]
    public TMPro.TextMeshPro promptLabel;

    private float _cooldown;
    private bool  _isPurchasing;

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;

        // Auto-fill the label if assigned
        if (promptLabel != null)
        {
            var def   = CosmeticRegistry.Instance?.Get(cosmeticId);
            string nm = def != null ? def.displayName : cosmeticId;
            promptLabel.text = cost > 0 ? $"{nm}\n{cost} {currencyCode}" : $"{nm}\nFREE";
        }

        if (promptUI != null) promptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < _cooldown || _isPurchasing) return;
        _cooldown = Time.time + 0.5f;

        PhotonCosmeticSync localSync = FindLocalSync();
        if (localSync == null) return;

        if (promptUI != null) promptUI.SetActive(true);

        var def = CosmeticRegistry.Instance?.Get(cosmeticId);
        if (def == null)
        {
            Debug.LogError($"[ToggleCosmeticButton] Cosmetic not found: {cosmeticId}");
            return;
        }

        var controller = localSync.GetComponent<AvatarCosmeticController>();
        if (controller == null) return;

        var  loadout       = controller.GetCurrentLoadout();
        bool alreadyWearing = loadout.HasItem(def.slot) && loadout.GetItemId(def.slot) == cosmeticId;

        // Always allow unequipping for free
        if (alreadyWearing)
        {
            localSync.NetworkUnequip(def.slot);
            Debug.Log($"[ToggleCosmeticButton] Unequipped {cosmeticId}");
            return;
        }

        if (cost <= 0)
        {
            localSync.NetworkEquip(cosmeticId);
            Debug.Log($"[ToggleCosmeticButton] Equipped {cosmeticId} (free)");
        }
        else
        {
            PurchaseWithPlayFab(localSync);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    // ── PlayFab Purchase Flow ─────────────────────────────────────────────────
    // Step 1: Check balance  →  Step 2: Subtract from buyer  →  Step 3: Pay creator  →  Equip

    private void PurchaseWithPlayFab(PhotonCosmeticSync localSync)
    {
        _isPurchasing = true;

        // Step 1 — check the player actually has enough currency
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            result =>
            {
                result.VirtualCurrency.TryGetValue(currencyCode, out int balance);

                if (balance < cost)
                {
                    Debug.LogWarning($"[ToggleCosmeticButton] Not enough {currencyCode} (have {balance}, need {cost}).");
                    _isPurchasing = false;
                    return;
                }

                SubtractCurrency(localSync);
            },
            error =>
            {
                Debug.LogError($"[ToggleCosmeticButton] Balance check failed: {error.GenerateErrorReport()}");
                _isPurchasing = false;
            });
    }

    private void SubtractCurrency(PhotonCosmeticSync localSync)
    {
        // Step 2 — deduct currency from the buyer
        PlayFabClientAPI.SubtractUserVirtualCurrency(
            new SubtractUserVirtualCurrencyRequest
            {
                VirtualCurrency = currencyCode,
                Amount          = cost
            },
            result =>
            {
                Debug.Log($"[ToggleCosmeticButton] Charged {cost} {currencyCode}. Balance now: {result.Balance}");
                PayCreator(localSync);
            },
            error =>
            {
                Debug.LogError($"[ToggleCosmeticButton] Charge failed: {error.GenerateErrorReport()}");
                _isPurchasing = false;
            });
    }

    private void PayCreator(PhotonCosmeticSync localSync)
    {
        // Step 3 — pay the map creator via CloudScript
        // If no creator is set, skip straight to equipping
        if (string.IsNullOrEmpty(creatorPlayFabId))
        {
            FinishPurchase(localSync);
            return;
        }

        // This calls the "PayMapCreator" CloudScript function on your PlayFab title.
        // See the CloudScript section in the README for the JS code to deploy.
        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName = "PayMapCreator",
                FunctionParameter = new
                {
                    creatorPlayFabId = creatorPlayFabId,
                    currencyCode     = currencyCode,
                    amount           = cost
                },
                GeneratePlayStreamEvent = true
            },
            result =>
            {
                Debug.Log($"[ToggleCosmeticButton] Paid creator {creatorPlayFabId}: {cost} {currencyCode}");
                FinishPurchase(localSync);
            },
            error =>
            {
                // Buyer was already charged — still give the cosmetic even if creator payout fails
                Debug.LogWarning($"[ToggleCosmeticButton] Creator payout failed (item still given): {error.GenerateErrorReport()}");
                FinishPurchase(localSync);
            });
    }

    private void FinishPurchase(PhotonCosmeticSync localSync)
    {
        localSync.NetworkEquip(cosmeticId);
        _isPurchasing = false;
        Debug.Log($"[ToggleCosmeticButton] Done — equipped {cosmeticId}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PhotonCosmeticSync FindLocalSync()
    {
        foreach (var sync in FindObjectsOfType<PhotonCosmeticSync>())
            if (sync.isLocalPlayer) return sync;
        Debug.LogError("[ToggleCosmeticButton] No local PhotonCosmeticSync found.");
        return null;
    }
}