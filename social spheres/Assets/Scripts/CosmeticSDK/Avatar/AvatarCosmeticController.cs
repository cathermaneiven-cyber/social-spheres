using System.Collections.Generic;
using UnityEngine;

namespace GTAG.CosmeticSDK
{
    // Attach this to your Player root GameObject.
    //
    // In the Inspector, map each EquipSlot to the matching child Transform.
    // Based on your hierarchy these are:
    //
    //   Head      →  Player/Head/HeadCosmetics
    //   Face      →  Player/Head/FaceCosmetics
    //   Body      →  Player/Body/BodyCosmetics
    //   LeftHand  →  Player/LeftHand/LeftHandCosmetics
    //   RightHand →  Player/RightHand/RightHandCosmetics

    public class AvatarCosmeticController : MonoBehaviour
    {
        [System.Serializable]
        public class SlotAnchor
        {
            public EquipSlot slot;
            [Tooltip("The Transform where cosmetics in this slot are parented.")]
            public Transform anchor;
        }

        [Header("Bone Anchors")]
        [SerializeField] private List<SlotAnchor> boneMap = new List<SlotAnchor>();

        private readonly Dictionary<EquipSlot, Transform>  _anchors   = new();
        private readonly Dictionary<EquipSlot, GameObject> _instances = new();
        private CosmeticLoadout _currentLoadout = new CosmeticLoadout();

        private void Awake()
        {
            foreach (var entry in boneMap)
                if (entry.anchor != null)
                    _anchors[entry.slot] = entry.anchor;
        }

        // Apply a full loadout — diffs against current state so only changed slots re-instantiate.
        public void ApplyLoadout(CosmeticLoadout loadout)
        {
            if (loadout == null) loadout = new CosmeticLoadout();

            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            {
                string newId = loadout.GetItemId(slot);
                string oldId = _currentLoadout.GetItemId(slot);

                if (newId == oldId)
                {
                    if (newId != null) ApplyTint(slot, loadout.GetTint(newId));
                    continue;
                }

                UnequipSlot(slot);
                if (!string.IsNullOrEmpty(newId))
                    SpawnItem(slot, newId, loadout.GetTint(newId));
            }

            _currentLoadout = loadout;
        }

        // Equip a single item by ID — finds its slot automatically from the registry.
        public bool EquipItem(string cosmeticId, Color? tintOverride = null)
        {
            var def = CosmeticRegistry.Instance?.Get(cosmeticId);
            if (def == null)
            {
                Debug.LogWarning($"[AvatarCosmeticController] Unknown cosmetic '{cosmeticId}'");
                return false;
            }
            UnequipSlot(def.slot);
            SpawnItem(def.slot, cosmeticId, tintOverride ?? def.defaultTint);
            _currentLoadout.equipped[def.slot] = cosmeticId;
            return true;
        }

        public void UnequipSlot(EquipSlot slot)
        {
            if (_instances.TryGetValue(slot, out var go)) Destroy(go);
            _instances.Remove(slot);
            _currentLoadout.equipped.Remove(slot);
        }

        public void UnequipAll()
        {
            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
                UnequipSlot(slot);
        }

        public CosmeticLoadout GetCurrentLoadout() => _currentLoadout;

        private void SpawnItem(EquipSlot slot, string cosmeticId, Color tint)
        {
            var def = CosmeticRegistry.Instance?.Get(cosmeticId);
            if (def?.modelPrefab == null) return;

            if (!_anchors.TryGetValue(slot, out var anchor))
            {
                Debug.LogWarning($"[AvatarCosmeticController] No anchor for slot {slot}. Did you fill in the Bone Map?");
                return;
            }

            var instance = Instantiate(def.modelPrefab, anchor);
            instance.transform.localPosition    = def.positionOffset;
            instance.transform.localEulerAngles = def.rotationOffset;
            instance.transform.localScale       = def.scaleMultiplier;

            if (def.isTintable) ApplyTintToInstance(instance, tint);

            _instances[slot] = instance;
        }

        private void ApplyTint(EquipSlot slot, Color tint)
        {
            if (!_instances.TryGetValue(slot, out var go)) return;
            var id  = _currentLoadout.GetItemId(slot);
            var def = id != null ? CosmeticRegistry.Instance?.Get(id) : null;
            if (def is { isTintable: true }) ApplyTintToInstance(go, tint);
        }

        private static void ApplyTintToInstance(GameObject go, Color tint)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                foreach (var mat in r.materials)
                    if (mat.HasProperty("_Color")) mat.color = tint;
        }
    }
}
