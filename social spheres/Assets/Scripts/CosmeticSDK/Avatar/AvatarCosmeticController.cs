using System.Collections.Generic;
using UnityEngine;

namespace GTAG.CosmeticSDK
{
    public class AvatarCosmeticController : MonoBehaviour
    {
        [System.Serializable]
        public class SlotAnchor
        {
            public EquipSlot slot;
            public Transform anchor;
        }

        [SerializeField] private List<SlotAnchor> boneMap = new List<SlotAnchor>();

        private readonly Dictionary<EquipSlot, Transform> _anchors = new();
        private readonly Dictionary<EquipSlot, GameObject> _instances = new();
        private CosmeticLoadout _currentLoadout = new CosmeticLoadout();

        private void Awake()
        {
            foreach (var entry in boneMap)
                if (entry.anchor != null)
                    _anchors[entry.slot] = entry.anchor;
        }

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

        public bool EquipItem(string cosmeticId, Color? tintOverride = null)
        {
            CosmeticDefinition def = CosmeticRegistry.Instance?.Get(cosmeticId);

            if (def == null)
            {
                Debug.LogWarning("[AvatarCosmeticController] Unknown cosmetic: " + cosmeticId);
                return false;
            }

            UnequipSlot(def.slot);
            SpawnItem(def.slot, cosmeticId, tintOverride ?? def.defaultTint);
            _currentLoadout.equipped[def.slot] = cosmeticId;

            return true;
        }

        public void UnequipSlot(EquipSlot slot)
        {
            if (_instances.TryGetValue(slot, out GameObject go))
                Destroy(go);

            _instances.Remove(slot);
            _currentLoadout.equipped.Remove(slot);
        }

        public void UnequipAll()
        {
            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
                UnequipSlot(slot);
        }

        public CosmeticLoadout GetCurrentLoadout()
        {
            return _currentLoadout;
        }

        private void SpawnItem(EquipSlot slot, string cosmeticId, Color tint)
        {
            CosmeticDefinition def = CosmeticRegistry.Instance?.Get(cosmeticId);

            if (def == null || def.modelPrefab == null)
                return;

            if (!_anchors.TryGetValue(slot, out Transform anchor))
            {
                Debug.LogWarning("[AvatarCosmeticController] No anchor for slot: " + slot);
                return;
            }

            GameObject instance = Instantiate(def.modelPrefab, anchor, false);

            Vector3 prefabPosition = instance.transform.localPosition;
            Vector3 prefabRotation = instance.transform.localEulerAngles;
            Vector3 prefabScale = instance.transform.localScale;

            instance.transform.localPosition = prefabPosition + def.positionOffset;
            instance.transform.localEulerAngles = prefabRotation + def.rotationOffset;
            instance.transform.localScale = Vector3.Scale(prefabScale, def.scaleMultiplier);

            if (def.isTintable)
                ApplyTintToInstance(instance, tint);

            _instances[slot] = instance;
        }

        private void ApplyTint(EquipSlot slot, Color tint)
        {
            if (!_instances.TryGetValue(slot, out GameObject go))
                return;

            string id = _currentLoadout.GetItemId(slot);
            CosmeticDefinition def = id != null ? CosmeticRegistry.Instance?.Get(id) : null;

            if (def != null && def.isTintable)
                ApplyTintToInstance(go, tint);
        }

        private static void ApplyTintToInstance(GameObject go, Color tint)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                foreach (Material mat in r.materials)
                    if (mat.HasProperty("_Color"))
                        mat.color = tint;
        }
    }
}