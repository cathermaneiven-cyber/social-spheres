using System;
using System.Collections.Generic;
using UnityEngine;

namespace GTAG.CosmeticSDK
{
    // Unity's JsonUtility can't serialize a regular Dictionary.
    // This wrapper stores keys and values as parallel lists so it survives ToJson/FromJson,
    // which is what we use to sync loadouts over Photon custom properties.
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        [SerializeField] private List<TKey>   keys   = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        private Dictionary<TKey, TValue> _cache;

        private Dictionary<TKey, TValue> Cache
        {
            get
            {
                if (_cache == null) Rebuild();
                return _cache;
            }
        }

        private void Rebuild()
        {
            _cache = new Dictionary<TKey, TValue>();
            int count = Math.Min(keys.Count, values.Count);
            for (int i = 0; i < count; i++)
                _cache[keys[i]] = values[i];
        }

        public bool ContainsKey(TKey key) => Cache.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) =>
            Cache.TryGetValue(key, out value);

        public TValue this[TKey key]
        {
            get => Cache[key];
            set
            {
                _cache = null;
                int idx = keys.IndexOf(key);
                if (idx >= 0) { values[idx] = value; }
                else          { keys.Add(key); values.Add(value); }
            }
        }

        public void Remove(TKey key)
        {
            int idx = keys.IndexOf(key);
            if (idx < 0) return;
            keys.RemoveAt(idx);
            values.RemoveAt(idx);
            _cache = null;
        }

        public IEnumerable<TKey>   Keys   => Cache.Keys;
        public IEnumerable<TValue> Values => Cache.Values;
    }
}
