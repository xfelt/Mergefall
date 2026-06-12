using System.Collections.Generic;
using MergeSurvivor.Core;
using MergeSurvivor.Data;

namespace MergeSurvivor.Meta
{
    /// <summary>
    /// Persisted character collection. Flat lists/fields only — JsonUtility (used by LocalSaveService)
    /// cannot serialize dictionaries. Shards are stored per rarity, indexed by (int)CharacterRarity.
    /// </summary>
    [System.Serializable]
    public sealed class CollectionData
    {
        public List<string> Owned = new();
        public List<int> ShardsByRarity = new() { 0, 0, 0, 0 };
        public string ActiveId = string.Empty;
        public int PityCounter;    // pulls since the last Epic+ (for gacha pity)
        public int LifetimePulls;
    }

    public interface ICollectionService
    {
        CollectionData Data { get; }
        bool IsOwned(string id);
        /// <summary>Adds the character to the collection. Returns true if it was newly unlocked. Auto-equips the first owned character.</summary>
        bool Unlock(string id);
        int GetShards(CharacterRarity rarity);
        void AddShards(CharacterRarity rarity, int amount);
        /// <summary>Spend shards of the character's rarity to unlock it. Returns true on success.</summary>
        bool TryCraft(CharacterDefinition character, int shardCost);
        string ActiveId { get; }
        bool SetActive(string id);
        int OwnedCount { get; }

        // Aggregated equipped-character + collection bonuses (the pay-to-win levers).
        float SquadPowerPercent();
        float GoldGainPercent();
        float MergeRewardPercent();
        int ExtraSpawnCapacity();
        float GachaLuckPercent();
        int StartingGoldFlat();

        void Save();
    }

    public sealed class CollectionService : ICollectionService
    {
        private const string Key = "merge_survivor_collection_v1";
        /// <summary>Small global squad-power bonus per owned character — rewards collecting them all.</summary>
        public const float CollectionBonusPerCharacter = 0.01f;

        private readonly ISaveService _save;
        private readonly CharacterCatalog _catalog;

        public CollectionData Data { get; }

        public CollectionService(ISaveService save, CharacterCatalog catalog)
        {
            _save = save;
            _catalog = catalog;
            _save.TryLoad(Key, out CollectionData data);
            Data = data ?? new CollectionData();
            while (Data.ShardsByRarity.Count < 4) Data.ShardsByRarity.Add(0);
        }

        public bool IsOwned(string id) => !string.IsNullOrEmpty(id) && Data.Owned.Contains(id);

        public bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || Data.Owned.Contains(id)) return false;
            Data.Owned.Add(id);
            if (string.IsNullOrEmpty(Data.ActiveId)) Data.ActiveId = id;
            return true;
        }

        public int GetShards(CharacterRarity rarity)
        {
            var i = (int)rarity;
            return i >= 0 && i < Data.ShardsByRarity.Count ? Data.ShardsByRarity[i] : 0;
        }

        public void AddShards(CharacterRarity rarity, int amount)
        {
            if (amount == 0) return;
            var i = (int)rarity;
            if (i < 0 || i >= Data.ShardsByRarity.Count) return;
            Data.ShardsByRarity[i] = System.Math.Max(0, Data.ShardsByRarity[i] + amount);
        }

        public bool TryCraft(CharacterDefinition character, int shardCost)
        {
            if (character == null || IsOwned(character.Id)) return false;
            if (GetShards(character.Rarity) < shardCost) return false;
            AddShards(character.Rarity, -shardCost);
            Unlock(character.Id);
            return true;
        }

        public string ActiveId => Data.ActiveId;
        public int OwnedCount => Data.Owned.Count;

        public bool SetActive(string id)
        {
            if (!IsOwned(id)) return false;
            Data.ActiveId = id;
            return true;
        }

        private CharacterDefinition Active()
            => _catalog == null ? null : _catalog.GetById(Data.ActiveId);

        private float ActiveBonus(CharacterBonusType type)
        {
            var active = Active();
            return active != null && active.BonusType == type ? active.BonusValue : 0f;
        }

        public float SquadPowerPercent()
            => ActiveBonus(CharacterBonusType.SquadPowerPercent) + OwnedCount * CollectionBonusPerCharacter;

        public float GoldGainPercent() => ActiveBonus(CharacterBonusType.GoldGainPercent);
        public float MergeRewardPercent() => ActiveBonus(CharacterBonusType.MergeRewardPercent);
        public int ExtraSpawnCapacity() => UnityEngine.Mathf.RoundToInt(ActiveBonus(CharacterBonusType.ExtraSpawnCapacity));
        public float GachaLuckPercent() => ActiveBonus(CharacterBonusType.GachaLuckPercent);
        public int StartingGoldFlat() => UnityEngine.Mathf.RoundToInt(ActiveBonus(CharacterBonusType.StartingGoldFlat));

        public void Save() => _save.Save(Key, Data);
    }
}
