using System;
using UnityEngine;

namespace GTAG.CosmeticSDK
{
    // Use this when creating cosmetics from code rather than ScriptableObject assets.
    // Useful for loading cosmetics from a server, modding support, or testing.
    //
    // Example:
    //   var def = new CosmeticBuilder("hat_crown")
    //       .WithDisplayName("Golden Crown")
    //       .WithSlot(EquipSlot.Head)
    //       .WithRarity(CosmeticRarity.Legendary)
    //       .WithModel(crownPrefab)
    //       .WithTint(Color.yellow)
    //       .Build();
    //
    //   CosmeticRegistry.Instance.Register(def);

    public class CosmeticBuilder
    {
        private readonly CosmeticDefinition _def;

        public CosmeticBuilder(string cosmeticId)
        {
            _def = new CosmeticDefinition { cosmeticId = cosmeticId };
        }

        public CosmeticBuilder WithDisplayName(string name)        { _def.displayName    = name;   return this; }
        public CosmeticBuilder WithDescription(string desc)        { _def.description    = desc;   return this; }
        public CosmeticBuilder WithRarity(CosmeticRarity rarity)   { _def.rarity         = rarity; return this; }
        public CosmeticBuilder WithSlot(EquipSlot slot)            { _def.slot           = slot;   return this; }
        public CosmeticBuilder WithModel(GameObject prefab)        { _def.modelPrefab    = prefab; return this; }
        public CosmeticBuilder WithPreviewIcon(Sprite icon)        { _def.previewIcon    = icon;   return this; }
        public CosmeticBuilder WithPositionOffset(Vector3 offset)  { _def.positionOffset = offset; return this; }
        public CosmeticBuilder WithRotationOffset(Vector3 euler)   { _def.rotationOffset = euler;  return this; }
        public CosmeticBuilder WithScale(Vector3 scale)            { _def.scaleMultiplier = scale; return this; }

        public CosmeticBuilder WithTint(Color tint, bool tintable = true)
        {
            _def.isTintable  = tintable;
            _def.defaultTint = tint;
            return this;
        }

        // Returns the completed definition. Throws if required fields are missing.
        public CosmeticDefinition Build()
        {
            if (!_def.IsValid())
                throw new InvalidOperationException(
                    $"CosmeticBuilder: '{_def.cosmeticId}' is missing cosmeticId, displayName, or modelPrefab.");
            return _def;
        }

        // Returns the definition, or null if invalid (no exception thrown).
        public CosmeticDefinition TryBuild() => _def.IsValid() ? _def : null;
    }
}
