using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GTAG.CosmeticSDK
{
    // Singleton that holds every known cosmetic definition.
    // Place one instance in your persistent scene (it calls DontDestroyOnLoad).
    //
    // To populate:
    //   - Drag CosmeticItemAsset ScriptableObjects into the Built-in Cosmetics list, OR
    //   - Call CosmeticRegistry.Instance.Register(def) at runtime

    public class CosmeticRegistry : MonoBehaviour
    {
        public static CosmeticRegistry Instance { get; private set; }

        [Header("Built-in Cosmetics")]
        [Tooltip("Drag CosmeticItemAsset ScriptableObjects here.")]
        [SerializeField] private List<CosmeticItemAsset> builtInCosmetics = new List<CosmeticItemAsset>();

        [Header("Settings")]
        [SerializeField] private bool logWarnings = true;

        private readonly Dictionary<string, CosmeticDefinition> _store =
            new Dictionary<string, CosmeticDefinition>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var asset in builtInCosmetics)
                if (asset != null) Register(asset.Definition);

            if (logWarnings)
                Debug.Log($"[CosmeticRegistry] Ready — {_store.Count} cosmetic(s) loaded.");
        }

        // Register a cosmetic at runtime (e.g. loaded from a server or mod).
        public void Register(CosmeticDefinition def)
        {
            if (def == null || !def.IsValid())
            {
                if (logWarnings) Debug.LogWarning("[CosmeticRegistry] Skipping null or invalid definition.");
                return;
            }
            if (_store.ContainsKey(def.cosmeticId))
            {
                if (logWarnings) Debug.LogWarning($"[CosmeticRegistry] Duplicate id '{def.cosmeticId}'. Skipping.");
                return;
            }
            _store[def.cosmeticId] = def;
        }

        public void Unregister(string cosmeticId) => _store.Remove(cosmeticId);

        // Returns null if not found — always check before using.
        public CosmeticDefinition Get(string cosmeticId)
        {
            _store.TryGetValue(cosmeticId, out var def);
            return def;
        }

        public bool Contains(string cosmeticId) => _store.ContainsKey(cosmeticId);

        public IEnumerable<CosmeticDefinition> All()                               => _store.Values;
        public IEnumerable<CosmeticDefinition> AllForSlot(EquipSlot slot)          => _store.Values.Where(d => d.slot == slot);
        public IEnumerable<CosmeticDefinition> AllOfRarity(CosmeticRarity rarity)  => _store.Values.Where(d => d.rarity == rarity);

        public int Count => _store.Count;
    }
}
