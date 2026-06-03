using System;
using System.Collections.Generic;
using MergeSurvivor.Core;
using MergeSurvivor.Data;
using MergeSurvivor.Economy;
using MergeSurvivor.Meta;
using NUnit.Framework;
using UnityEngine;

namespace MergeSurvivor.Tests
{
    /// <summary>
    /// EditMode coverage for the pure meta/monetization logic: gacha odds + pity, collection
    /// unlock/craft + bonus aggregation, daily streak (culture-safe dates), prestige multiplier/cap,
    /// and storefront grant mapping. No Unity scene needed.
    /// </summary>
    public sealed class MonetizationTests
    {
        private sealed class MemorySave : ISaveService
        {
            private readonly Dictionary<string, string> _store = new();
            public void Save<T>(string key, T data) => _store[key] = JsonUtility.ToJson(data);
            public bool TryLoad<T>(string key, out T data) where T : new()
            {
                if (_store.TryGetValue(key, out var json))
                {
                    data = JsonUtility.FromJson<T>(json) ?? new T();
                    return true;
                }
                data = new T();
                return false;
            }
        }

        private static CharacterDefinition MakeChar(string id, CharacterRarity rarity, CharacterBonusType bonus, float value, int gems)
        {
            var c = ScriptableObject.CreateInstance<CharacterDefinition>();
            c.ConfigureRuntime(id, id.ToUpperInvariant(), rarity, bonus, value, gems);
            return c;
        }

        private static CharacterCatalog MakeCatalog()
        {
            var cat = ScriptableObject.CreateInstance<CharacterCatalog>();
            cat.ConfigureRuntime(new List<CharacterDefinition>
            {
                MakeChar("c1", CharacterRarity.Common, CharacterBonusType.SquadPowerPercent, 0.05f, 100),
                MakeChar("c2", CharacterRarity.Common, CharacterBonusType.GoldGainPercent, 0.05f, 100),
                MakeChar("r1", CharacterRarity.Rare, CharacterBonusType.SquadPowerPercent, 0.12f, 400),
                MakeChar("e1", CharacterRarity.Epic, CharacterBonusType.SquadPowerPercent, 0.22f, 900),
                MakeChar("l1", CharacterRarity.Legendary, CharacterBonusType.SquadPowerPercent, 0.40f, 2000),
            });
            return cat;
        }

        [Test]
        public void Gacha_Pity_GuaranteesEpicPlus()
        {
            var cat = MakeCatalog();
            var cfg = ScriptableObject.CreateInstance<GachaConfig>();
            cfg.ConfigureRuntime(100f, 0f, 0f, 0f, 100, 900, 5); // common-only weights, pity at 5
            var col = new CollectionService(new MemorySave(), cat);
            var gacha = new GachaService(cat, cfg, col, new System.Random(1234));

            var results = gacha.Pull(5);

            Assert.AreEqual(5, results.Count);
            for (var i = 0; i < 4; i++)
                Assert.AreEqual(CharacterRarity.Common, results[i].Rarity, $"pull {i} should be Common");
            Assert.GreaterOrEqual((int)results[4].Rarity, (int)CharacterRarity.Epic, "5th pull should be guaranteed Epic+ by pity");
            Assert.AreEqual(0, col.Data.PityCounter, "pity resets after an Epic+");
        }

        [Test]
        public void Gacha_Distribution_CommonMostFrequent()
        {
            var cat = MakeCatalog();
            var cfg = ScriptableObject.CreateInstance<GachaConfig>(); // default 60/28/10/2
            var col = new CollectionService(new MemorySave(), cat);
            var gacha = new GachaService(cat, cfg, col, new System.Random(99));

            int common = 0, legendary = 0;
            foreach (var r in gacha.Pull(300))
            {
                if (r.Rarity == CharacterRarity.Common) common++;
                else if (r.Rarity == CharacterRarity.Legendary) legendary++;
            }

            Assert.Greater(common, legendary, "common should be far more frequent than legendary");
        }

        [Test]
        public void Collection_UnlockAndCraft()
        {
            var cat = MakeCatalog();
            var col = new CollectionService(new MemorySave(), cat);

            Assert.IsFalse(col.IsOwned("e1"));
            Assert.IsTrue(col.Unlock("e1"));
            Assert.IsFalse(col.Unlock("e1"), "re-unlock should be a no-op");
            Assert.AreEqual("e1", col.ActiveId, "first owned character auto-equips");

            var rare = cat.GetById("r1");
            Assert.IsFalse(col.TryCraft(rare, 50), "cannot craft without shards");
            col.AddShards(CharacterRarity.Rare, 60);
            Assert.IsTrue(col.TryCraft(rare, 50));
            Assert.IsTrue(col.IsOwned("r1"));
            Assert.AreEqual(10, col.GetShards(CharacterRarity.Rare), "shards spent on craft");
        }

        [Test]
        public void Collection_BonusAggregation()
        {
            var cat = MakeCatalog();
            var col = new CollectionService(new MemorySave(), cat);
            col.Unlock("e1"); // SquadPowerPercent 0.22, auto-equipped, owned count 1

            // equipped 0.22 + collection bonus (0.01 * 1 owned)
            Assert.AreEqual(0.23f, col.SquadPowerPercent(), 0.0001f);
            Assert.AreEqual(0f, col.GoldGainPercent(), 0.0001f);
        }

        [Test]
        public void Daily_StreakProgression_CultureSafe()
        {
            var table = ScriptableObject.CreateInstance<DailyRewardTable>();
            table.ConfigureRuntime(new List<DailyRewardEntry>
            {
                new(100, 0, false), new(150, 0, false), new(200, 20, false)
            });
            var inv = new InventoryService(new AccountData());
            var clock = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var daily = new DailyRewardService(new MemorySave(), table, inv, () => clock);

            Assert.IsTrue(daily.CanClaim());
            Assert.AreEqual(0, daily.NextDayIndex());
            Assert.IsTrue(daily.Claim().Success);
            Assert.AreEqual(100, inv.Get(CurrencyType.Soft));
            Assert.IsFalse(daily.CanClaim(), "cannot claim twice the same day");

            clock = clock.AddDays(1);
            Assert.IsTrue(daily.CanClaim());
            Assert.AreEqual(1, daily.NextDayIndex(), "consecutive day advances the streak");
            daily.Claim();
            Assert.AreEqual(250, inv.Get(CurrencyType.Soft));

            clock = clock.AddDays(3); // skipped days
            Assert.AreEqual(0, daily.NextDayIndex(), "a skipped day resets the streak");
        }

        [Test]
        public void Daily_AdCaps_ResetNextDay()
        {
            var table = ScriptableObject.CreateInstance<DailyRewardTable>();
            table.ConfigureRuntime(new List<DailyRewardEntry> { new(100, 0, false) });
            var clock = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);
            var daily = new DailyRewardService(new MemorySave(), table, new InventoryService(new AccountData()), () => clock);

            daily.RegisterAdView("free_gems");
            daily.RegisterAdView("free_gems");
            Assert.AreEqual(2, daily.AdViewsToday("free_gems"));
            Assert.AreEqual(3, daily.RemainingAdGrants("free_gems", 5));

            clock = clock.AddDays(1);
            Assert.AreEqual(0, daily.AdViewsToday("free_gems"), "ad caps reset on a new UTC day");
        }

        [Test]
        public void Prestige_MultiplierAndCap()
        {
            var cfg = ScriptableObject.CreateInstance<PrestigeConfig>();
            cfg.ConfigureRuntime(10, 1f, 0.02f, 1.05f); // low cap to verify clamping
            var p = new PrestigeService(new MemorySave(), cfg);

            Assert.IsFalse(p.CanPrestige(9));
            Assert.IsTrue(p.CanPrestige(10));
            Assert.AreEqual(1, p.PreviewGain(10));
            Assert.AreEqual(1f, p.Multiplier(), 0.0001f);

            var gained = p.Prestige(15); // (15-10+1)*1 = 6
            Assert.AreEqual(6, gained);
            Assert.AreEqual(1.05f, p.Multiplier(), 0.0001f, "multiplier is capped at maxMultiplier");
        }

        [Test]
        public void Storefront_GrantsGemsAndBundle()
        {
            var cat = MakeCatalog();
            var col = new CollectionService(new MemorySave(), cat);
            var inv = new InventoryService(new AccountData());
            var packs = ScriptableObject.CreateInstance<CoinPackCatalog>();
            var pack = new CoinPack();
            pack.ConfigureRuntime("gems_x", "X", 100, 0.2f, "$1", false, false, "e1");
            packs.ConfigureRuntime(new List<CoinPack> { pack });
            var store = new StorefrontService(packs, inv, col);

            var grant = store.GrantPurchase("gems_x");
            Assert.IsTrue(grant.Success);
            Assert.AreEqual(120, grant.GemsGranted, "100 base + 20% bonus");
            Assert.AreEqual(120, inv.Get(CurrencyType.Premium));
            Assert.IsTrue(col.IsOwned("e1"), "bundle character granted");

            Assert.IsFalse(store.GrantPurchase("unknown").Success);
        }

        [Test]
        public void CoinPack_TotalGems_IncludesBonus()
        {
            var p = new CoinPack();
            p.ConfigureRuntime("a", "A", 1000, 0.3f, "$x");
            Assert.AreEqual(1300, p.TotalGems);
        }
    }
}
