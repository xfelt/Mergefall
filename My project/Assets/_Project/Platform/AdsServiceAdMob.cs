#if MERGE_SURVIVOR_USE_ADMOB
using System;
using UnityEngine;
using GoogleMobileAds.Api;

namespace MergeSurvivor.Platform
{
    /// <summary>AdMob rewarded + interstitial. Ad unit IDs from AppPlatformConfig (AdUnitRewarded, AdUnitInterstitial).</summary>
    public sealed class AdsServiceAdMob : IAdsService
    {
        private readonly AppPlatformConfig _config;
        private RewardedAd _rewardedAd;
        private InterstitialAd _interstitialAd;
        private bool _sdkInitialized;

        public AdsServiceAdMob(AppPlatformConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Initialize()
        {
            if (_sdkInitialized) return;
            MobileAds.Initialize(initStatus =>
            {
                _sdkInitialized = true;
                Debug.Log("[Ads] AdMob initialized.");
                LoadRewarded();
                LoadInterstitial();
            });
        }

        public void ShowRewarded(string placement, Action<bool> onCompleted)
        {
            if (!_sdkInitialized) { onCompleted?.Invoke(false); return; }
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                _rewardedAd.Show(reward =>
                {
                    onCompleted?.Invoke(true);
                    _rewardedAd?.Destroy();
                    _rewardedAd = null;
                    LoadRewarded();
                });
            }
            else
            {
                LoadRewarded(); // retry next time
                onCompleted?.Invoke(false);
            }
        }

        public void ShowInterstitial(Action onClosed = null)
        {
            if (!_sdkInitialized) { onClosed?.Invoke(); return; }
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                _interstitialAd.OnAdFullScreenContentClosed += () =>
                {
                    onClosed?.Invoke();
                    _interstitialAd?.Destroy();
                    _interstitialAd = null;
                    LoadInterstitial();
                };
                _interstitialAd.OnAdFullScreenContentFailed += _ =>
                {
                    onClosed?.Invoke();
                    _interstitialAd?.Destroy();
                    _interstitialAd = null;
                    LoadInterstitial();
                };
                _interstitialAd.Show();
            }
            else
            {
                LoadInterstitial();
                onClosed?.Invoke();
            }
        }

        private void LoadRewarded()
        {
            var adUnitId = _config.AdUnitRewarded;
            if (string.IsNullOrEmpty(adUnitId)) return;
            var request = new AdRequest();
            RewardedAd.Load(adUnitId, request, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null) { Debug.LogWarning($"[Ads] Rewarded load failed: {error.GetMessage()}"); return; }
                _rewardedAd = ad;
            });
        }

        private void LoadInterstitial()
        {
            var adUnitId = _config.AdUnitInterstitial;
            if (string.IsNullOrEmpty(adUnitId)) return;
            var request = new AdRequest();
            InterstitialAd.Load(adUnitId, request, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null) { Debug.LogWarning($"[Ads] Interstitial load failed: {error.GetMessage()}"); return; }
                _interstitialAd = ad;
            });
        }
    }
}
#endif
