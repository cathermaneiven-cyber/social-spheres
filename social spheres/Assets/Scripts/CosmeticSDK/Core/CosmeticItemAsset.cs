using UnityEngine;

namespace GTAG.CosmeticSDK
{
    // Create via: Right-click in Project → GTAG → Cosmetic Item
    [CreateAssetMenu(
        fileName = "NewCosmetic",
        menuName  = "GTAG/Cosmetic Item",
        order     = 10)]
    public class CosmeticItemAsset : ScriptableObject
    {
        [SerializeField] private CosmeticDefinition _definition = new CosmeticDefinition();

        public CosmeticDefinition Definition => _definition;

        private void OnValidate()
        {
            // Auto-fill the ID from the asset filename if left blank
            if (string.IsNullOrWhiteSpace(_definition.cosmeticId))
                _definition.cosmeticId = name.ToLower().Replace(" ", "_");
        }
    }
}
