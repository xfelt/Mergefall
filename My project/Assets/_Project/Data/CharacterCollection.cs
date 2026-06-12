using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MergeSurvivor.Data
{
    /// <summary>Rarity tiers for collectible characters. Higher rarity = stronger passive bonus and rarer in gacha.</summary>
    public enum CharacterRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    /// <summary>
    /// Passive squad bonus a character grants while equipped. Only the equipped character's bonus
    /// applies (plus a small global collection bonus); this is the main pay-to-win lever.
    /// </summary>
    public enum CharacterBonusType
    {
        None = 0,
        SquadPowerPercent = 1,   // +x to squad power multiplier (e.g. 0.20 = +20%)
        GoldGainPercent = 2,     // +x to soft-currency gains
        MergeRewardPercent = 3,  // +x to per-merge soft reward
        ExtraSpawnCapacity = 4,  // +n flat board spawn slots (bonusValue rounded)
        GachaLuckPercent = 5,    // +x shifted toward higher rarity in the Lucky Chest
        StartingGoldFlat = 6     // +n gold granted at the start of each run
    }

    [CreateAssetMenu(menuName = "Merge Survivor/Character Definition", fileName = "CharacterDefinition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string id = "hero_common_01";
        [SerializeField] private string displayName = "Wanderer";
        [SerializeField] private CharacterRarity rarity = CharacterRarity.Common;
        [SerializeField] private CharacterBonusType bonusType = CharacterBonusType.SquadPowerPercent;
        [SerializeField] private float bonusValue = 0.05f;
        [SerializeField] private Sprite icon;
        [Tooltip("Cost in premium currency (Gems) to unlock this character directly in the shop.")]
        [SerializeField] private int directUnlockCostGems = 200;
        [TextArea] [SerializeField] private string lore = string.Empty;

        public string Id => id;
        public string DisplayName => displayName;
        public CharacterRarity Rarity => rarity;
        public CharacterBonusType BonusType => bonusType;
        public float BonusValue => bonusValue;
        public Sprite Icon => icon;
        public int DirectUnlockCostGems => directUnlockCostGems;
        public string Lore => lore;

        public void ConfigureRuntime(
            string newId,
            string newName,
            CharacterRarity newRarity,
            CharacterBonusType newBonusType,
            float newBonusValue,
            int newDirectUnlockCostGems,
            string newLore = "")
        {
            id = newId;
            displayName = newName;
            rarity = newRarity;
            bonusType = newBonusType;
            bonusValue = newBonusValue;
            directUnlockCostGems = newDirectUnlockCostGems;
            lore = newLore;
        }
    }

    [CreateAssetMenu(menuName = "Merge Survivor/Character Catalog", fileName = "CharacterCatalog")]
    public sealed class CharacterCatalog : ScriptableObject
    {
        [SerializeField] private List<CharacterDefinition> characters = new();
        private Dictionary<string, CharacterDefinition> _byId;

        public int Count => characters.Count;
        public IReadOnlyList<CharacterDefinition> All => characters;

        public void ConfigureRuntime(List<CharacterDefinition> runtimeCharacters)
        {
            characters = runtimeCharacters ?? new List<CharacterDefinition>();
            Warm();
        }

        public void Warm()
        {
            _byId = characters.Where(c => c != null).ToDictionary(c => c.Id, c => c);
        }

        public CharacterDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _byId ??= new Dictionary<string, CharacterDefinition>();
            if (_byId.Count == 0) Warm();
            _byId.TryGetValue(id, out var character);
            return character;
        }

        public List<CharacterDefinition> ByRarity(CharacterRarity rarity)
        {
            return characters.Where(c => c != null && c.Rarity == rarity).ToList();
        }
    }
}
