using System;
using System.Collections.Generic;
using MergeSurvivor.Data;

namespace MergeSurvivor.Meta
{
    /// <summary>Outcome of a single Lucky Chest pull.</summary>
    public struct GachaResult
    {
        public string CharacterId;
        public CharacterRarity Rarity;
        public bool IsNew;        // false = duplicate, converted to shards
        public int ShardsAwarded; // shards granted when duplicate
    }

    public interface IGachaService
    {
        int SingleCostGems { get; }
        int TenCostGems { get; }
        int PityCounter { get; }
        int PityCount { get; }
        /// <summary>Disclosed odds (0..1) for the four rarities, for in-game display (store policy).</summary>
        float Probability(CharacterRarity rarity);
        /// <summary>Roll <paramref name="count"/> pulls, applying pity and writing results into the collection. Does NOT spend currency — the caller authorizes payment first.</summary>
        IReadOnlyList<GachaResult> Pull(int count);
    }

    /// <summary>
    /// Lucky Chest gacha. Rolls rarity by weighted chance with pity (guaranteed Epic+ after N dry pulls)
    /// and a 10-pull Rare+ floor. Owns no currency — payment (gems or a rewarded ad) is handled by the
    /// caller; this just decides outcomes and updates the collection. RNG is injectable for tests.
    /// </summary>
    public sealed class GachaService : IGachaService
    {
        private static readonly CharacterRarity[] AllRarities =
        {
            CharacterRarity.Common, CharacterRarity.Rare, CharacterRarity.Epic, CharacterRarity.Legendary
        };

        private readonly CharacterCatalog _catalog;
        private readonly GachaConfig _config;
        private readonly ICollectionService _collection;
        private readonly Random _rng;

        public GachaService(CharacterCatalog catalog, GachaConfig config, ICollectionService collection, Random rng = null)
        {
            _catalog = catalog;
            _config = config;
            _collection = collection;
            _rng = rng ?? new Random();
        }

        public int SingleCostGems => _config != null ? _config.SingleCostGems : 100;
        public int TenCostGems => _config != null ? _config.TenCostGems : 900;
        public int PityCounter => _collection?.Data.PityCounter ?? 0;
        public int PityCount => _config != null ? _config.PityCount : 0;
        public float Probability(CharacterRarity rarity) => _config != null ? _config.Probability(rarity) : 0f;

        public IReadOnlyList<GachaResult> Pull(int count)
        {
            var results = new List<GachaResult>();
            if (_catalog == null || _config == null || _collection == null || count <= 0) return results;

            var luck = Math.Max(0f, _collection.GachaLuckPercent());
            var rolledRarePlus = false;

            for (var i = 0; i < count; i++)
            {
                CharacterRarity rarity;
                var pityActive = _config.PityCount > 0 && _collection.Data.PityCounter + 1 >= _config.PityCount;

                if (pityActive)
                {
                    // Guarantee Epic+ and reset the dry streak.
                    rarity = RollAmong(luck, CharacterRarity.Epic, CharacterRarity.Legendary);
                }
                else if (i == count - 1 && count >= 10 && !rolledRarePlus)
                {
                    // 10-pull floor: the last pull is at least Rare if nothing Rare+ has dropped.
                    rarity = RollAmong(luck, CharacterRarity.Rare, CharacterRarity.Epic, CharacterRarity.Legendary);
                }
                else
                {
                    rarity = RollAmong(luck, AllRarities);
                }

                if (rarity >= CharacterRarity.Rare) rolledRarePlus = true;

                // Pity tracking: any Epic+ resets the counter.
                if (rarity >= CharacterRarity.Epic) _collection.Data.PityCounter = 0;
                else _collection.Data.PityCounter++;
                _collection.Data.LifetimePulls++;

                results.Add(GrantOne(rarity));
            }

            _collection.Save();
            return results;
        }

        private GachaResult GrantOne(CharacterRarity rarity)
        {
            var pool = _catalog.ByRarity(rarity);
            // Fall back down the rarity ladder if a tier has no authored characters.
            var probe = rarity;
            while (pool.Count == 0 && probe > CharacterRarity.Common)
            {
                probe--;
                pool = _catalog.ByRarity(probe);
            }
            if (pool.Count == 0)
            {
                // Nothing in the catalog at all.
                return new GachaResult { CharacterId = string.Empty, Rarity = rarity, IsNew = false, ShardsAwarded = 0 };
            }

            var pick = pool[_rng.Next(pool.Count)];
            var resolvedRarity = pick.Rarity;

            if (_collection.IsOwned(pick.Id))
            {
                var shards = _config.ShardsForDuplicate(resolvedRarity);
                _collection.AddShards(resolvedRarity, shards);
                return new GachaResult { CharacterId = pick.Id, Rarity = resolvedRarity, IsNew = false, ShardsAwarded = shards };
            }

            _collection.Unlock(pick.Id);
            return new GachaResult { CharacterId = pick.Id, Rarity = resolvedRarity, IsNew = true, ShardsAwarded = 0 };
        }

        /// <summary>Weighted rarity pick over a subset; Epic/Legendary weights are boosted by gacha luck.</summary>
        private CharacterRarity RollAmong(float luck, params CharacterRarity[] candidates)
        {
            var total = 0.0;
            foreach (var r in candidates) total += WeightWithLuck(r, luck);
            if (total <= 0.0) return candidates[0];

            var roll = _rng.NextDouble() * total;
            var acc = 0.0;
            foreach (var r in candidates)
            {
                acc += WeightWithLuck(r, luck);
                if (roll < acc) return r;
            }
            return candidates[candidates.Length - 1];
        }

        private double WeightWithLuck(CharacterRarity rarity, float luck)
        {
            double w = _config.Weight(rarity);
            if (rarity >= CharacterRarity.Epic) w *= 1.0 + luck;
            return w;
        }
    }
}
