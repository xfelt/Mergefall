using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MergeSurvivor.Data
{
    [CreateAssetMenu(menuName = "Merge Survivor/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new();
        private Dictionary<string, ItemDefinition> _byId;
        private Dictionary<(string, int), ItemDefinition> _byFamilyTier;

        public void ConfigureRuntime(List<ItemDefinition> runtimeItems)
        {
            items = runtimeItems;
            Warm();
        }

        public void Warm()
        {
            _byId = items.Where(i => i != null).ToDictionary(i => i.Id, i => i);
            _byFamilyTier = items.Where(i => i != null).ToDictionary(i => (i.FamilyId, i.Tier), i => i);
        }

        public ItemDefinition GetById(string id)
        {
            _byId ??= new Dictionary<string, ItemDefinition>();
            if (_byId.Count == 0) Warm();
            _byId.TryGetValue(id, out var item);
            return item;
        }

        public ItemDefinition GetNextTier(ItemDefinition current)
        {
            if (current == null) return null;
            _byFamilyTier ??= new Dictionary<(string, int), ItemDefinition>();
            if (_byFamilyTier.Count == 0) Warm();
            _byFamilyTier.TryGetValue((current.FamilyId, current.Tier + 1), out var next);
            return next;
        }

        public List<ItemDefinition> GetTierOneItems()
        {
            return items.Where(i => i != null && i.Tier == 1).ToList();
        }
    }
}
