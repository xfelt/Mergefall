using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeSurvivor.Platform
{
    public interface IStoreService { void Initialize(); void Purchase(string productId); }
    public interface IAdsService
    {
        void Initialize();
        void ShowRewarded(string placement, Action<bool> onCompleted);
        void ShowInterstitial(Action onClosed = null);
    }
    public interface IAnalyticsService { void Initialize(); void TrackEvent(string name, Dictionary<string, object> parameters = null); }
    public interface IRemoteConfigService { void Initialize(); int GetInt(string key, int fallback); }
    public interface ICloudSaveService { void Initialize(); }

    public sealed class StoreServiceStub : IStoreService
    {
        public void Initialize() { Debug.Log("[Store] Stub init"); }
        public void Purchase(string productId) { Debug.Log($"[Store] TODO Billing purchase: {productId}"); }
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
