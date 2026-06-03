using UnityEngine;

namespace MergeSurvivor.Platform
{
    /// <summary>
    /// Platform config: product IDs, ad unit IDs, Firebase project.
    /// Create via menu: Merge Survivor > Platform Config. Place in Resources/ as "AppPlatformConfig" for runtime load.
    /// Set real product/ad unit IDs here or override via project settings; do not commit secrets.
    /// </summary>
    [CreateAssetMenu(menuName = "Merge Survivor/Platform Config", fileName = "AppPlatformConfig")]
    public sealed class AppPlatformConfig : ScriptableObject
    {
        [Tooltip("Android application ID (e.g. com.company.mergesurvivor)")]
        [SerializeField] private string androidPackage = "com.company.mergesurvivor";
        [Tooltip("Use Player Settings bundle version + version code for Android builds.")]
        [SerializeField] private bool useProjectSettingsVersion = true;
        [Tooltip("Android bundle version override used when Use Project Settings Version is disabled.")]
        [SerializeField] private string androidBundleVersion = "0.1.0";
        [Tooltip("Android bundle version code override used when Use Project Settings Version is disabled.")]
        [SerializeField] private int androidBundleVersionCode = 1;
        [Tooltip("Google Play in-app product ID (consumable or non-consumable)")]
        [SerializeField] private string billingProductPremiumPack = "premium_pack_01";
        [Tooltip("Real-money coin pack product IDs. Must match products created in Google Play Console and the CoinPackCatalog. These are the IDs registered with the store at startup.")]
        [SerializeField] private string[] coinPackProductIds =
        {
            "gems_pouch",
            "gems_stack",
            "gems_chest",
            "gems_vault",
            "gems_hoard",
            "starter_bundle"
        };
        [Tooltip("AdMob rewarded ad unit ID (ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY)")]
        [SerializeField] private string adUnitRewarded = "ca-app-pub-xxxx/rewarded";
        [Tooltip("AdMob interstitial ad unit ID")]
        [SerializeField] private string adUnitInterstitial = "ca-app-pub-xxxx/interstitial";
        [Tooltip("Firebase project ID (for reference; google-services.json defines the actual project)")]
        [SerializeField] private string firebaseProject = "merge-survivor-firebase";

        public string AndroidPackage => androidPackage;
        public bool UseProjectSettingsVersion => useProjectSettingsVersion;
        public string AndroidBundleVersion => androidBundleVersion;
        public int AndroidBundleVersionCode => androidBundleVersionCode;
        public string BillingProductPremiumPack => billingProductPremiumPack;
        public string[] CoinPackProductIds => coinPackProductIds;
        public string AdUnitRewarded => adUnitRewarded;
        public string AdUnitInterstitial => adUnitInterstitial;
        public string FirebaseProject => firebaseProject;
    }
}
