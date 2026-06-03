using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeSurvivor.Data
{
    /// <summary>
    /// A real-money in-app purchase that grants premium currency (Gems). The <see cref="ProductId"/>
    /// must match a product created in Google Play Console. Consumable unless <see cref="IsOneTime"/>.
    /// </summary>
    [Serializable]
    public sealed class CoinPack
    {
        [SerializeField] private string productId = "gems_pouch";
        [SerializeField] private string displayName = "Pouch of Gems";
        [Tooltip("Base premium currency granted (before bonus%).")]
        [SerializeField] private int gems = 100;
        [Tooltip("Extra % on top of base gems (e.g. 0.2 = +20%).")]
        [SerializeField] private float bonusPercent;
        [Tooltip("Display-only price string. Real price comes from the store at runtime.")]
        [SerializeField] private string priceLabel = "$0.99";
        [SerializeField] private bool isBestValue;
        [Tooltip("One-time purchase (NonConsumable), e.g. a starter bundle. Otherwise Consumable.")]
        [SerializeField] private bool isOneTime;
        [Tooltip("Optional character granted with this pack (e.g. starter bundle). Empty = none.")]
        [SerializeField] private string bundleCharacterId = string.Empty;

        public string ProductId => productId;
        public string DisplayName => displayName;
        public int Gems => gems;
        public float BonusPercent => bonusPercent;
        public string PriceLabel => priceLabel;
        public bool IsBestValue => isBestValue;
        public bool IsOneTime => isOneTime;
        public string BundleCharacterId => bundleCharacterId;

        /// <summary>Total gems granted including the bonus%.</summary>
        public int TotalGems => Mathf.RoundToInt(gems * (1f + Mathf.Max(0f, bonusPercent)));

        public void ConfigureRuntime(
            string newProductId,
            string newDisplayName,
            int newGems,
            float newBonusPercent,
            string newPriceLabel,
            bool newIsBestValue = false,
            bool newIsOneTime = false,
            string newBundleCharacterId = "")
        {
            productId = newProductId;
            displayName = newDisplayName;
            gems = newGems;
            bonusPercent = newBonusPercent;
            priceLabel = newPriceLabel;
            isBestValue = newIsBestValue;
            isOneTime = newIsOneTime;
            bundleCharacterId = newBundleCharacterId;
        }
    }

    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Coin Pack Catalog", fileName = "CoinPackCatalog")]
    public sealed class CoinPackCatalog : ScriptableObject
    {
        [SerializeField] private List<CoinPack> packs = new();

        public IReadOnlyList<CoinPack> Packs => packs;
        public int Count => packs.Count;

        public CoinPack GetByProductId(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            for (var i = 0; i < packs.Count; i++)
            {
                if (packs[i] != null && packs[i].ProductId == productId) return packs[i];
            }
            return null;
        }

        public IEnumerable<string> ProductIds()
        {
            for (var i = 0; i < packs.Count; i++)
            {
                if (packs[i] != null && !string.IsNullOrEmpty(packs[i].ProductId)) yield return packs[i].ProductId;
            }
        }

        public void ConfigureRuntime(List<CoinPack> runtimePacks)
        {
            packs = runtimePacks ?? new List<CoinPack>();
        }
    }

    /// <summary>
    /// Lucky Chest (gacha) tuning. Rarity weights are relative; pity guarantees an Epic+ pull after
    /// <see cref="PityCount"/> pulls without one. Odds must be disclosed in-game (Google Play policy).
    /// </summary>
    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Gacha", fileName = "GachaConfig")]
    public sealed class GachaConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float commonWeight = 60f;
        [SerializeField, Min(0f)] private float rareWeight = 28f;
        [SerializeField, Min(0f)] private float epicWeight = 10f;
        [SerializeField, Min(0f)] private float legendaryWeight = 2f;

        [SerializeField, Min(1)] private int singleCostGems = 100;
        [SerializeField, Min(1)] private int tenCostGems = 900;
        [Tooltip("Pulls without an Epic+ that force a guaranteed Epic+ on the next pull. 0 = no pity.")]
        [SerializeField, Min(0)] private int pityCount = 10;

        [Header("Duplicate -> shards awarded")]
        [SerializeField] private int shardsCommon = 5;
        [SerializeField] private int shardsRare = 15;
        [SerializeField] private int shardsEpic = 40;
        [SerializeField] private int shardsLegendary = 100;

        [Header("Shards required to craft a character directly")]
        [SerializeField] private int craftCostCommon = 40;
        [SerializeField] private int craftCostRare = 120;
        [SerializeField] private int craftCostEpic = 320;
        [SerializeField] private int craftCostLegendary = 800;

        public int SingleCostGems => singleCostGems;
        public int TenCostGems => tenCostGems;
        public int PityCount => pityCount;

        public float Weight(CharacterRarity rarity) => rarity switch
        {
            CharacterRarity.Common => commonWeight,
            CharacterRarity.Rare => rareWeight,
            CharacterRarity.Epic => epicWeight,
            CharacterRarity.Legendary => legendaryWeight,
            _ => 0f
        };

        public float TotalWeight => commonWeight + rareWeight + epicWeight + legendaryWeight;

        /// <summary>Disclosed probability (0..1) for a single non-pity pull.</summary>
        public float Probability(CharacterRarity rarity)
        {
            var total = TotalWeight;
            return total <= 0f ? 0f : Weight(rarity) / total;
        }

        public int ShardsForDuplicate(CharacterRarity rarity) => rarity switch
        {
            CharacterRarity.Common => shardsCommon,
            CharacterRarity.Rare => shardsRare,
            CharacterRarity.Epic => shardsEpic,
            CharacterRarity.Legendary => shardsLegendary,
            _ => 0
        };

        public int CraftCost(CharacterRarity rarity) => rarity switch
        {
            CharacterRarity.Common => craftCostCommon,
            CharacterRarity.Rare => craftCostRare,
            CharacterRarity.Epic => craftCostEpic,
            CharacterRarity.Legendary => craftCostLegendary,
            _ => int.MaxValue
        };

        public void ConfigureRuntime(float common, float rare, float epic, float legendary, int single, int ten, int pity)
        {
            commonWeight = common;
            rareWeight = rare;
            epicWeight = epic;
            legendaryWeight = legendary;
            singleCostGems = single;
            tenCostGems = ten;
            pityCount = pity;
        }
    }

    [Serializable]
    public sealed class DailyRewardEntry
    {
        [SerializeField] private int gold = 50;
        [SerializeField] private int gems;
        [Tooltip("Grants a free Lucky Chest pull when claimed.")]
        [SerializeField] private bool freeChest;

        public int Gold => gold;
        public int Gems => gems;
        public bool FreeChest => freeChest;

        public DailyRewardEntry() { }

        public DailyRewardEntry(int gold, int gems, bool freeChest)
        {
            this.gold = gold;
            this.gems = gems;
            this.freeChest = freeChest;
        }
    }

    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Daily Reward Table", fileName = "DailyRewardTable")]
    public sealed class DailyRewardTable : ScriptableObject
    {
        [Tooltip("One entry per day in the streak cycle (day 1..N). The last day is the big payout.")]
        [SerializeField] private List<DailyRewardEntry> days = new();

        public int Count => days.Count;

        /// <summary>Get the reward for a 0-based day index, clamped to the table size.</summary>
        public DailyRewardEntry Get(int dayIndex)
        {
            if (days.Count == 0) return new DailyRewardEntry(0, 0, false);
            if (dayIndex < 0) dayIndex = 0;
            else if (dayIndex >= days.Count) dayIndex = days.Count - 1;
            return days[dayIndex];
        }

        public void ConfigureRuntime(List<DailyRewardEntry> runtimeDays)
        {
            days = runtimeDays ?? new List<DailyRewardEntry>();
        }
    }

    /// <summary>
    /// Prestige (rebirth) tuning. Reaching <see cref="UnlockWave"/> lets the player reset their run
    /// for permanent prestige points that compound into a power/gold multiplier.
    /// </summary>
    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Prestige", fileName = "PrestigeConfig")]
    public sealed class PrestigeConfig : ScriptableObject
    {
        [SerializeField, Min(2)] private int unlockWave = 10;
        [Tooltip("Prestige points granted per wave reached beyond the unlock threshold.")]
        [SerializeField, Min(0f)] private float pointsPerWaveOverThreshold = 1f;
        [Tooltip("Permanent multiplier added per prestige point (e.g. 0.02 = +2% per point).")]
        [SerializeField, Min(0f)] private float multiplierPerPoint = 0.02f;
        [Tooltip("Hard cap on the total prestige multiplier (keeps pay-to-win 'strong but capped').")]
        [SerializeField, Min(1f)] private float maxMultiplier = 10f;

        public int UnlockWave => unlockWave;
        public float PointsPerWaveOverThreshold => pointsPerWaveOverThreshold;
        public float MultiplierPerPoint => multiplierPerPoint;
        public float MaxMultiplier => maxMultiplier;

        /// <summary>Prestige points a player would gain by prestiging at the given highest wave.</summary>
        public int PointsForWave(int highestWave)
        {
            if (highestWave < unlockWave) return 0;
            return Mathf.Max(0, Mathf.FloorToInt((highestWave - unlockWave + 1) * pointsPerWaveOverThreshold));
        }

        public void ConfigureRuntime(int unlock, float pointsPerWave, float multPerPoint, float maxMult)
        {
            unlockWave = unlock;
            pointsPerWaveOverThreshold = pointsPerWave;
            multiplierPerPoint = multPerPoint;
            maxMultiplier = maxMult;
        }
    }
}
