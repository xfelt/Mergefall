using UnityEngine;

namespace MergeSurvivor.Platform
{
    /// <summary>
    /// Creates platform service implementations. Use real SDKs when the corresponding
    /// scripting define is set and AppPlatformConfig is available; otherwise returns stubs.
    /// Config: place AppPlatformConfig in Resources/ or assign at runtime; set real product/ad
    /// unit IDs there (do not commit secrets—use project settings or env for production).
    /// </summary>
    public static class PlatformServiceFactory
    {
        private const string ConfigResourcePath = "AppPlatformConfig";

        /// <summary>Load config from Resources or return null. Create asset via menu: Merge Survivor > Platform Config.</summary>
        public static AppPlatformConfig GetConfig()
        {
            return Resources.Load<AppPlatformConfig>(ConfigResourcePath);
        }

        public static IStoreService CreateStore(AppPlatformConfig config = null)
        {
            // To use Google Play Billing: add com.unity.purchasing (e.g. Edit > Project Settings > Services),
            // add "UnityEngine.Purchasing" to this asmdef, rename StoreServiceGooglePlay.cs.off → .cs (update to IAP 5.x API if needed), then uncomment:
            // #if MERGE_SURVIVOR_USE_IAP
            // var c = config ?? GetConfig();
            // if (c != null) return new StoreServiceGooglePlay(c);
            // #endif
            return new StoreServiceStub();
        }

        public static IAdsService CreateAds(AppPlatformConfig config = null)
        {
#if MERGE_SURVIVOR_USE_ADMOB
            var c = config ?? GetConfig();
            if (c != null) return new AdsServiceAdMob(c);
#endif
            return new AdsServiceStub();
        }

        public static IAnalyticsService CreateAnalytics(AppPlatformConfig config = null)
        {
#if MERGE_SURVIVOR_USE_FIREBASE
            var c = config ?? GetConfig();
            if (c != null) return new AnalyticsServiceFirebase(c);
#endif
            return new AnalyticsServiceStub();
        }

        public static IRemoteConfigService CreateRemoteConfig(AppPlatformConfig config = null)
        {
#if MERGE_SURVIVOR_USE_FIREBASE
            var c = config ?? GetConfig();
            if (c != null) return new RemoteConfigServiceFirebase(c);
#endif
            return new RemoteConfigServiceStub();
        }

        public static ICloudSaveService CreateCloudSave()
        {
            return new CloudSaveServiceStub();
        }
    }
}
