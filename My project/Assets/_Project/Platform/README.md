# Merge Survivor — Platform (Android SDK integration)

This folder contains **IStoreService**, **IAdsService**, **IAnalyticsService**, **IRemoteConfigService** and their implementations: stubs by default, real SDKs when the scripting defines and packages are in place.

## Required config files

| File | Where | Purpose |
|------|--------|---------|
| **google-services.json** | `Assets/` (root) or `Assets/StreamingAssets/` | Firebase (Analytics, Remote Config, optional Crashlytics). Download from [Firebase Console](https://console.firebase.google.com) → Project settings → Your apps → Android app. Do **not** commit if it contains secrets; use CI/project settings to inject for builds. |
| **AppPlatformConfig** | Create via menu **Merge Survivor > Platform Config**, then place a copy in **Resources/** as `AppPlatformConfig` | ScriptableObject holding product IDs and ad unit IDs. Loaded at runtime from `Resources.Load<AppPlatformConfig>("AppPlatformConfig")`. |

## Where to set real product / ad unit IDs

- **In-App Purchases:** In the **AppPlatformConfig** asset, set **Billing Product Premium Pack** (and add more products in code if needed). In Google Play Console, create products with the same IDs (e.g. `premium_pack_01`). Do not commit real IDs if the repo is public; use a local override or CI.
- **AdMob:** In **AppPlatformConfig**, set **Ad Unit Rewarded** and **Ad Unit Interstitial** to your AdMob ad unit IDs (e.g. `ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY`). Create units in AdMob console. Use test IDs during development (see AdMob docs).
- **Firebase:** Project is identified by **google-services.json**; **Firebase Project** in AppPlatformConfig is for reference only.

## Scripting defines (Player Settings > Script Compilation Defines)

- **MERGE_SURVIVOR_USE_IAP** — Use Google Play Billing via Unity IAP (package `com.unity.purchasing`).
- **MERGE_SURVIVOR_USE_ADMOB** — Use AdMob (requires [Google Mobile Ads Unity plugin](https://developers.google.com/admob/unity/quick-start)).
- **MERGE_SURVIVOR_USE_FIREBASE** — Use Firebase Analytics and Remote Config (requires [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup)).
- **MERGE_SURVIVOR_USE_CRASHLYTICS** — Optional; enable only if Firebase Crashlytics package is installed (with MERGE_SURVIVOR_USE_FIREBASE).

For **balance tuning via Remote Config** (enemy strength, win rewards, upgrade costs), see **Docs/REMOTE_CONFIG_KEYS.md**: which keys to create in Firebase Console and how to simulate values in the editor.

Without these defines, **PlatformServiceFactory** returns stub implementations so the project runs without the SDKs.

## Build / export steps (Android)

1. **Unity IAP:** Add package **In-App Purchasing** (Package Manager). For Google Play, follow [Unity IAP Google Play](https://docs.unity3d.com/Packages/com.unity.purchasing@3.11/manual/UnityIAPGoogleConfiguration.html); build with **Google Play** as target store.
2. **AdMob:** Import the [Google Mobile Ads Unity plugin](https://developers.google.com/admob/unity/quick-start). Add **MERGE_SURVIVOR_USE_ADMOB** when ready.
3. **Firebase:** Import [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup) (Core, Analytics, Remote Config; optionally Crashlytics). Add **google-services.json** to `Assets/` (or as per Firebase Unity docs). Add **MERGE_SURVIVOR_USE_FIREBASE**.
4. **Export:** File → Build Settings → Android → Export / Build. Use a keystore for release; do not commit the keystore or passwords (use env vars or CI secrets for **KEYSTORE_PASSWORD** etc.).

## No secrets in repo

- Keep **google-services.json** out of version control if it is considered sensitive, or use a placeholder and inject the real file in CI.
- Store real product/ad unit IDs in **AppPlatformConfig** only in a private or override asset; or set them via Editor scripts reading environment variables / project settings so they are not committed.
