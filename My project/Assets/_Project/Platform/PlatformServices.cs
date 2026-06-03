using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeSurvivor.Platform
{
    public interface IStoreService
    {
        void Initialize();
        /// <summary>Register the product IDs the store should know about (coin packs, bundles). Call before Purchase.</summary>
        void RegisterProducts(IEnumerable<string> productIds);
        void Purchase(string productId);
        /// <summary>Raised with the product ID when a purchase completes successfully. Subscribe to grant content.</summary>
        event Action<string> PurchaseCompleted;
    }
    public interface IAdsService
    {
        void Initialize();
        void ShowRewarded(string placement, Action<bool> onCompleted);
        void ShowInterstitial(Action onClosed = null);
    }

    /// <summary>Named rewarded-ad placements used across the meta UI (for analytics + per-placement caps).</summary>
    public static class AdPlacements
    {
        public const string DoubleWinReward = "double_win_reward";
        public const string FreeGems = "free_gems";
        public const string FreeSpin = "free_spin";
        public const string Revive = "revive";
        public const string DailyDouble = "daily_double";
    }
    public interface IAnalyticsService { void Initialize(); void TrackEvent(string name, Dictionary<string, object> parameters = null); }
    public interface IRemoteConfigService { void Initialize(); int GetInt(string key, int fallback); }
    public interface ICloudSaveService { void Initialize(); }

    public sealed class StoreServiceStub : IStoreService
    {
        private readonly HashSet<string> _products = new();
        public event Action<string> PurchaseCompleted;

        public void Initialize() { Debug.Log("[Store] Stub init"); }

        public void RegisterProducts(IEnumerable<string> productIds)
        {
            if (productIds == null) return;
            foreach (var id in productIds)
            {
                if (!string.IsNullOrEmpty(id)) _products.Add(id);
            }
        }

        public void Purchase(string productId)
        {
            // Stub: simulate an instant successful purchase so the grant flow is testable in-editor.
            Debug.Log($"[Store] Stub purchase (simulated success): {productId}");
            PurchaseCompleted?.Invoke(productId);
        }
    }

    public sealed class AdsServiceStub : IAdsService
    {
        public void Initialize() { Debug.Log("[Ads] Stub init"); }
        public void ShowRewarded(string placement, Action<bool> onCompleted)
        {
            Debug.Log($"[Ads] TODO AdMob rewarded: {placement}");
            onCompleted?.Invoke(true);
        }
        public void ShowInterstitial(Action onClosed = null) { Debug.Log("[Ads] TODO AdMob interstitial"); onClosed?.Invoke(); }
    }

    public sealed class AnalyticsServiceStub : IAnalyticsService
    {
        public void Initialize() { Debug.Log("[Analytics] Stub init"); }
        public void TrackEvent(string name, Dictionary<string, object> parameters = null)
            => Debug.Log($"[Analytics] {name}");
    }

    public sealed class RemoteConfigServiceStub : IRemoteConfigService
    {
        public void Initialize() { Debug.Log("[RC] Stub init"); }
        public int GetInt(string key, int fallback) => fallback;
    }

    public sealed class CloudSaveServiceStub : ICloudSaveService
    {
        public void Initialize() { Debug.Log("[CloudSave] Stub init"); }
    }
}
