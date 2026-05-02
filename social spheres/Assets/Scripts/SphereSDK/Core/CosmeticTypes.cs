using System;
using UnityEngine;

namespace GTAG.CosmeticSDK
{
    // Matches the Cosmetics GameObjects already in your Player hierarchy:
    // HeadCosmetics, FaceCosmetics, BodyCosmetics, LeftHandCosmetics, RightHandCosmetics
    public enum EquipSlot
    {
        Head,       // → HeadCosmetics
        Face,       // → FaceCosmetics
        Body,       // → BodyCosmetics
        LeftHand,   // → LeftHandCosmetics
        RightHand   // → RightHandCosmetics
    }

    public enum CosmeticRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public class CosmeticDefinition
    {
        [Tooltip("Unique ID, e.g. 'hat_tophat'. No spaces. Must be stable across builds.")]
        public string cosmeticId;

        [Tooltip("Name shown in the UI.")]
        public string displayName;

        [TextArea(2, 4)]
        public string description;

        public CosmeticRarity rarity = CosmeticRarity.Common;

        public EquipSlot slot = EquipSlot.Head;

        [Tooltip("Prefab instantiated on the bone anchor.")]
        public GameObject modelPrefab;

        [Tooltip("Icon shown in the cosmetics menu.")]
        public Sprite previewIcon;

        public bool isTintable = false;
        public Color defaultTint = Color.white;

        [Tooltip("Position offset from the anchor bone.")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("Euler rotation offset from the anchor bone.")]
        public Vector3 rotationOffset = Vector3.zero;

        public Vector3 scaleMultiplier = Vector3.one;

        public bool IsValid() =>
            !string.IsNullOrWhiteSpace(cosmeticId) &&
            !string.IsNullOrWhiteSpace(displayName) &&
            modelPrefab != null;

        public override string ToString() =>
            $"[Cosmetic id={cosmeticId} slot={slot} rarity={rarity}]";
    }

    [Serializable]
    public class CosmeticLoadout
    {
        public SerializableDictionary<EquipSlot, string> equipped =
            new SerializableDictionary<EquipSlot, string>();

        public SerializableDictionary<string, Color> tintOverrides =
            new SerializableDictionary<string, Color>();

        public bool HasItem(EquipSlot slot) =>
            equipped.ContainsKey(slot) && !string.IsNullOrEmpty(equipped[slot]);

        public string GetItemId(EquipSlot slot) =>
            equipped.TryGetValue(slot, out var id) ? id : null;

        public Color GetTint(string id) =>
            tintOverrides.TryGetValue(id, out var c) ? c : Color.white;

        public string Serialize() => JsonUtility.ToJson(this);

        public static CosmeticLoadout Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return new CosmeticLoadout();
            try   { return JsonUtility.FromJson<CosmeticLoadout>(json); }
            catch { return new CosmeticLoadout(); }
        }
    }
}
