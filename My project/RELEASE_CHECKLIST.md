# Survivor: Board Quest — Release Checklist

Use this checklist before shipping an Android build to internal test, closed/open testing, or production.

---

## 1. Android build settings

- [ ] **Player Settings → Android**
  - **Package Name**: Set to your application ID (e.g. `com.yourcompany.mergesurvivor`). Must match the one registered in Google Play Console and in Firebase.
  - **Version**: Bump **Bundle Version Code** (integer) for each upload; **Version** (e.g. `1.0.0`) for display.
  - **Minimum API Level**: At least API 24 (Android 7.0) unless you need older; target API level as required by Play Store (check current requirements).
  - **Scripting Backend**: IL2CPP recommended for release; set **Target Architectures** (ARM64 required for Play Store).
  - **Install Location**: Auto.
- [ ] **Quality / Graphics**: Set default render pipeline and tier for your target devices.
- [ ] **Stripping**: Use appropriate code stripping level (e.g. Medium) to reduce AAB size; test that all used code paths still work.

---

## 2. Signing

- [ ] **Project Settings → Player → Android → Publishing Settings**
  - Create or select a **Keystore** (recommended: new keystore per app, backed up securely).
  - **Key alias** and **passwords** stored securely (e.g. CI secrets); never commit keystore or passwords to the repo.
- [ ] For **Google Play App Signing**: Upload your upload key (or let Google create one). Store the upload keystore and credentials safely; losing them complicates future updates.

---

## 3. AAB (Android App Bundle)

- [ ] **Build Settings → Android → Build App Bundle (Google Play)** checked.
- [ ] Build AAB via **File → Build Settings → Build** (or CI script).
- [ ] Verify the generated `.aab` installs and runs on a test device/emulator (e.g. `bundletool` or “Internal testing” track).
- [ ] Ensure **Split Application Binary** is enabled if you use it (reduces download size per device).

---

## 4. Store listing (Google Play Console)

- [ ] **Store listing**: Short and full description, screenshots (phone/tablet as required), feature graphic, optional video.
- [ ] **Content rating**: Questionnaire completed and rating obtained.
- [ ] **Target audience**: Age groups and store presence options.
- [ ] **Privacy policy**: URL set (see below).
- [ ] **App access**: If restricted (e.g. login), provide test credentials or instructions for reviewers.
- [ ] **Ads declaration**: If the app shows ads, declare it and complete ad provider details (e.g. AdMob).

---

## 5. Privacy policy

- [ ] Publish a **privacy policy** URL (hosted on your site or a dedicated page).
- [ ] Policy must cover:
  - Data collected (e.g. analytics, device identifiers, ad-related data if using AdMob).
  - How data is used (analytics, ads, remote config, crash reporting).
  - Third-party SDKs (Firebase, AdMob, Unity, etc.) and links to their privacy policies.
  - User rights (access, deletion, opt-out where applicable).
  - Contact for privacy requests.
- [ ] Add the **exact URL** in Play Console under Store listing → Privacy policy.

---

## 6. Where to plug IDs and config

| What | Where |
|------|--------|
| **Google Play Billing (in-app products)** | **AppPlatformConfig** asset: Create via **Merge Survivor → Platform Config**, place in `Resources/` as **AppPlatformConfig**. Set **Billing Product IDs** (e.g. `premium_pack_01`). Wire this in **PlatformServiceFactory** / **StoreServiceGooglePlay** (replace stub with real Play Billing implementation). |
| **AdMob ad unit IDs** | Same **AppPlatformConfig**: Set **Ad Unit Rewarded** and **Ad Unit Interstitial** (e.g. `ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY`). Used by **AdsServiceAdMob** / **PlatformServiceFactory.CreateAds()**. Do not commit real IDs if they are environment-specific; use Remote Config or build-time substitution for variants. |
| **Firebase** | **google-services.json**: Download from Firebase Console (Project settings → Your apps → Android app). Place in project root (or `Assets/` if your setup expects it). Do not commit if it contains secrets; use a template and CI to inject. **Firebase project ID** can be set in **AppPlatformConfig** for reference. **Remote Config / Analytics** are wired via **RemoteConfigServiceFirebase** and **AnalyticsServiceFirebase**; ensure the app is registered in Firebase with the same package name. |

Summary:

- **Resources/AppPlatformConfig** (ScriptableObject): Play Billing product IDs, AdMob rewarded/interstitial unit IDs, Firebase project reference.
- **google-services.json**: Firebase Android app config (package name, project ID, etc.); path as required by Firebase SDK/Unity.
- **Player Settings → Android → Package Name**: Must match Firebase and Play Console.

---

## Pre-release quick checks

- [ ] Run **PlayMode tests** (bootstrap, merge, buttons, board unlock, combat modifiers; full run flow; save/load).
- [ ] Run **Editor test**: **Balance simulation** produces valid CSV and summary.
- [ ] Smoke test on a physical Android device: login/guest, one full run (spawn, merge, fight, rewards), and return to hub.
- [ ] Confirm **privacy policy** and **store listing** are complete and **Ads** (if any) declared.
