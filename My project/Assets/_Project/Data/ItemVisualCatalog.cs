using System.Collections.Generic;
using UnityEngine;

namespace MergeSurvivor.Data
{
    [CreateAssetMenu(menuName = "Merge Survivor/Item Visual Catalog", fileName = "ItemVisualCatalog")]
    public sealed class ItemVisualCatalog : ScriptableObject
    {
        [System.Serializable]
        public sealed class ItemVisualEntry
        {
            public string ItemId;
            public Sprite Sprite;
            [Tooltip("Optional outline/glow variant for selected state")]
            public Sprite SpriteSelected;
        }

        [System.Serializable]
        public sealed class TierFallbackEntry
        {
            public int Tier;
            public Sprite Sprite;
            public Color TintColor = Color.white;
        }

        [SerializeField] private List<ItemVisualEntry> itemSprites = new();
        [SerializeField] private List<TierFallbackEntry> tierFallbacks = new();
        [SerializeField] private Sprite emptyCellSprite;

        private Dictionary<string, ItemVisualEntry> _byId;
        private Dictionary<int, TierFallbackEntry> _byTier;

        public Sprite EmptyCellSprite => emptyCellSprite;

        public void Warm()
        {
            _byId = new Dictionary<string, ItemVisualEntry>();
            foreach (var entry in itemSprites)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.ItemId))
                    _byId[entry.ItemId] = entry;
            }

            _byTier = new Dictionary<int, TierFallbackEntry>();
            foreach (var entry in tierFallbacks)
            {
                if (entry != null)
                    _byTier[entry.Tier] = entry;
            }
        }

        public bool TryGetSprite(string itemId, out Sprite sprite, out Color tint)
        {
            _byId ??= new Dictionary<string, ItemVisualEntry>();
            if (_byId.Count == 0 && itemSprites.Count > 0) Warm();

            if (!string.IsNullOrEmpty(itemId) && _byId.TryGetValue(itemId, out var entry) && entry.Sprite != null)
            {
                sprite = entry.Sprite;
                tint = Color.white;
                return true;
            }

            sprite = null;
            tint = Color.white;
            return false;
        }

        public bool TryGetTierFallback(int tier, out Sprite sprite, out Color tint)
        {
            _byTier ??= new Dictionary<int, TierFallbackEntry>();
            if (_byTier.Count == 0 && tierFallbacks.Count > 0) Warm();

            if (_byTier.TryGetValue(tier, out var entry))
            {
                sprite = entry.Sprite;
                tint = entry.TintColor;
                return true;
            }

            sprite = null;
            tint = Color.white;
            return false;
        }

        public Sprite GetSelectedSprite(string itemId)
        {
            _byId ??= new Dictionary<string, ItemVisualEntry>();
            if (_byId.Count == 0 && itemSprites.Count > 0) Warm();

            if (!string.IsNullOrEmpty(itemId) && _byId.TryGetValue(itemId, out var entry))
                return entry.SpriteSelected;

            return null;
        }
    }
}
