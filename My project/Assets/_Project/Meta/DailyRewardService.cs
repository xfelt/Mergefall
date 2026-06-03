using System;
using System.Collections.Generic;
using System.Globalization;
using MergeSurvivor.Core;
using MergeSurvivor.Data;
using MergeSurvivor.Economy;

namespace MergeSurvivor.Meta
{
    /// <summary>Persisted daily-reward + ad-cap state. Dates are ISO UTC strings (culture-safe).</summary>
    [System.Serializable]
    public sealed class DailyRewardData
    {
        public string LastClaimDateIso = string.Empty;
        public int StreakIndex;            // 0-based position in the reward table for the NEXT claim
        public string AdCapsDateIso = string.Empty;
        public List<AdViewCount> AdViews = new();
    }

    [System.Serializable]
    public sealed class AdViewCount
    {
        public string Placement;
        public int Count;
    }

    public struct DailyClaimResult
    {
        public bool Success;
        public int DayIndex;            // which table entry was granted
        public DailyRewardEntry Entry;  // null when Success is false
    }

    public interface IDailyRewardService
    {
        DailyRewardData Data { get; }
        bool CanClaim();
        /// <summary>Day index (0-based) that the next claim will grant.</summary>
        int NextDayIndex();
        /// <summary>Claim today's reward: grants gold+gems to inventory and returns the entry (caller handles FreeChest).</summary>
        DailyClaimResult Claim();
        void RegisterAdView(string placement);
        int AdViewsToday(string placement);
        int RemainingAdGrants(string placement, int dailyCap);
        void Save();
    }

    public sealed class DailyRewardService : IDailyRewardService
    {
        private const string Key = "merge_survivor_daily_v1";
        private readonly ISaveService _save;
        private readonly DailyRewardTable _table;
        private readonly IInventoryService _inventory;
        private readonly Func<DateTime> _utcNow; // injectable clock for tests

        public DailyRewardData Data { get; }

        public DailyRewardService(ISaveService save, DailyRewardTable table, IInventoryService inventory, Func<DateTime> utcNow = null)
        {
            _save = save;
            _table = table;
            _inventory = inventory;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _save.TryLoad(Key, out DailyRewardData data);
            Data = data ?? new DailyRewardData();
        }

        private DateTime Today => _utcNow().Date;
        private static string Iso(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static bool TryParse(string iso, out DateTime date)
            => DateTime.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

        public bool CanClaim()
        {
            if (string.IsNullOrEmpty(Data.LastClaimDateIso)) return true;
            return !(TryParse(Data.LastClaimDateIso, out var last) && last.Date == Today);
        }

        public int NextDayIndex()
        {
            var count = _table != null && _table.Count > 0 ? _table.Count : 1;
            // A skipped day (gap > 1) restarts the streak at day 0.
            if (!string.IsNullOrEmpty(Data.LastClaimDateIso) && TryParse(Data.LastClaimDateIso, out var last))
            {
                if (last.Date.AddDays(1) != Today) return 0;
            }
            return Data.StreakIndex % count;
        }

        public DailyClaimResult Claim()
        {
            if (!CanClaim() || _table == null || _table.Count == 0)
                return new DailyClaimResult { Success = false };

            var dayIndex = NextDayIndex();
            var entry = _table.Get(dayIndex);

            _inventory?.Add(new GameReward { SoftCurrency = entry.Gold, PremiumCurrency = entry.Gems });

            Data.LastClaimDateIso = Iso(Today);
            Data.StreakIndex = (dayIndex + 1) % _table.Count;
            Save();

            return new DailyClaimResult { Success = true, DayIndex = dayIndex, Entry = entry };
        }

        private void ResetAdCapsIfNewDay()
        {
            var todayIso = Iso(Today);
            if (Data.AdCapsDateIso != todayIso)
            {
                Data.AdCapsDateIso = todayIso;
                Data.AdViews.Clear();
            }
        }

        private AdViewCount FindOrAdd(string placement)
        {
            for (var i = 0; i < Data.AdViews.Count; i++)
            {
                if (Data.AdViews[i].Placement == placement) return Data.AdViews[i];
            }
            var entry = new AdViewCount { Placement = placement, Count = 0 };
            Data.AdViews.Add(entry);
            return entry;
        }

        public void RegisterAdView(string placement)
        {
            ResetAdCapsIfNewDay();
            FindOrAdd(placement).Count++;
            Save();
        }

        public int AdViewsToday(string placement)
        {
            ResetAdCapsIfNewDay();
            return FindOrAdd(placement).Count;
        }

        public int RemainingAdGrants(string placement, int dailyCap)
            => Math.Max(0, dailyCap - AdViewsToday(placement));

        public void Save() => _save.Save(Key, Data);
    }
}
