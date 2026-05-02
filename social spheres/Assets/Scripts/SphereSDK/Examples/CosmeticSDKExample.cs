using UnityEngine;
using Photon.Realtime;
using GTAG.CosmeticSDK;

// EXAMPLE — shows how to wire up the SDK in your specific project.
// Attach this wherever you handle player spawning/joining.
// Safe to delete once you understand the pattern.

public class CosmeticSDKExample : MonoBehaviour
{
    [Header("References — set from your spawn logic")]
    [SerializeField] private PhotonCosmeticSync localPlayerSync;

    [Header("Example Prefabs")]
    [SerializeField] private GameObject topHatPrefab;
    [SerializeField] private GameObject crownPrefab;

    private void Start()
    {
        RegisterExampleCosmetics();
    }

    // ── Step 1: Register cosmetics ─────────────────────────────────────────────
    // You only need to call this once, usually in a GameManager or on Start.
    // In production you'd load these from a config file or server.

    void RegisterExampleCosmetics()
    {
        var topHat = new CosmeticBuilder("hat_tophat")
            .WithDisplayName("Top Hat")
            .WithDescription("Classic and stylish.")
            .WithSlot(EquipSlot.Head)
            .WithRarity(CosmeticRarity.Uncommon)
            .WithModel(topHatPrefab)
            .WithTint(Color.black)
            .WithPositionOffset(new Vector3(0f, 0.12f, 0f))
            .Build();

        var crown = new CosmeticBuilder("hat_crown")
            .WithDisplayName("Crown")
            .WithDescription("For the winner.")
            .WithSlot(EquipSlot.Head)
            .WithRarity(CosmeticRarity.Legendary)
            .WithModel(crownPrefab)
            .WithTint(Color.yellow)
            .Build();

        CosmeticRegistry.Instance.Register(topHat);
        CosmeticRegistry.Instance.Register(crown);
    }

    // ── Step 2: Equip items ────────────────────────────────────────────────────
    // Hook these to UI buttons. The sync component handles broadcasting to all players.

    public void OnEquipTopHat()   => localPlayerSync.NetworkEquip("hat_tophat");
    public void OnEquipCrown()    => localPlayerSync.NetworkEquip("hat_crown", Color.cyan);
    public void OnUnequipHead()   => localPlayerSync.NetworkUnequip(EquipSlot.Head);

    // ── Step 3: Wiring up the sync when a player spawns ────────────────────────
    // Call this from wherever you spawn / instantiate player objects.
    // Pass in your LoadBalancingClient, the Player reference, and whether it's local.

    public void OnLocalPlayerSpawned(GameObject playerObject, LoadBalancingClient photonClient, Player photonPlayer)
    {
        var sync = playerObject.GetComponent<PhotonCosmeticSync>();
        sync.Init(photonClient, photonPlayer, local: true);
    }

    public void OnRemotePlayerSpawned(GameObject playerObject, LoadBalancingClient photonClient, Player photonPlayer)
    {
        var sync = playerObject.GetComponent<PhotonCosmeticSync>();
        sync.Init(photonClient, photonPlayer, local: false);
    }

    // ── Step 4: Applying a saved loadout ──────────────────────────────────────
    // If you store loadouts (e.g. in PlayerPrefs or a backend), apply them like this:

    public void ApplySavedLoadout(string serializedJson)
    {
        var loadout = CosmeticLoadout.Deserialize(serializedJson);
        localPlayerSync.NetworkApplyLoadout(loadout);
    }

    // ── Step 5: Querying the registry ─────────────────────────────────────────

    public void LogAllHeadCosmetics()
    {
        foreach (var def in CosmeticRegistry.Instance.AllForSlot(EquipSlot.Head))
            Debug.Log($"  {def.cosmeticId} — {def.displayName} [{def.rarity}]");
    }
}