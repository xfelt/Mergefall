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
- [ ] **Ads declaration**: The app shows rewarded ads (AdMob) — declare it and complete ad provider details.
- [ ] **Data safety**: Complete the Data safety form covering analytics, device/ad identifiers, and purchase data (Firebase + AdMob + IAP).
- [ ] **Gacha odds disclosure**: The Lucky Chest is a loot box — disclose drop-rate odds (shown in-game) and declare the mechanic per Google Play policy.
- [ ] **In-app products**: Create the coin-pack products in Play Console matching `AppPlatformConfig.CoinPackProductIds` (`gems_pouch`, `gems_stack`, `gems_chest`, `gems_vault`, `gems_hoard`; `starter_bundle` = non-consumable). Set prices and activate them.

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
| **Google Play Billing (in-app products)** | **AppPlatformConfig** asset: already created at `Assets/_Project/Resources/AppPlatformConfig.asset` (product IDs pre-filled to match `DATA_SAFETY.md`). Adjust **Billing Product Premium Pack** and the **Coin Pack Product IDs** array there if products change. To enable real billing: add the In-App Purchasing package, add `UnityEngine.Purchasing` to `MergeSurvivor.Platform.asmdef`, rename `Platform/StoreServiceGooglePlay.cs.off` → `.cs`, add the `MERGE_SURVIVOR_USE_IAP` define, and uncomment the factory branch in `PlatformServiceFactory.CreateStore`. Gem grants on purchase are handled by `StorefrontService` (no extra wiring needed). |
| **AdMob ad unit IDs** | Same **AppPlatformConfig**: currently set to **Google's official test ad unit IDs** — safe for development, must be replaced with your production units (e.g. `ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY`) before release. Used by **AdsServiceAdMob** / **PlatformServiceFactory.CreateAds()**. Do not commit real IDs if they are environment-specific; use Remote Config or build-time substitution for variants. |
| **Firebase** | **google-services.json**: Download from Firebase Console (Project settings → Your apps → Android app). Place in project root (or `Assets/` if your setup expects it). Do not commit if it contains secrets; use a template and CI to inject. **Firebase project ID** can be set in **AppPlatformConfig** for reference. **Remote Config / Analytics** are wired via **RemoteConfigServiceFirebase** and **AnalyticsServiceFirebase**; ensure the app is registered in Firebase with the same package name. |

Summary:

- **Resources/AppPlatformConfig** (ScriptableObject): Play Billing product IDs, AdMob rewarded/interstitial unit IDs, Firebase project reference.
- **google-services.json**: Firebase Android app config (package name, project ID, etc.); path as required by Firebase SDK/Unity.
- **Player Settings → Android → Package Name**: Must match Firebase and Play Console.

---

## 7. Playability-pass assets (art / audio / scenes)

These steps cover the visual/audio/tutorial pass (Phases A–D). See `ASSETS.md`.

- [ ] **First editor open after pulling generated assets**: let Unity import the new
  WAVs (`Assets/_Project/Resources/Audio/`) and PNGs (`Assets/_Project/Art/Items/`).
  `.meta` files for the audio clips are generated by Unity on first import; commit
  them afterwards.
- [ ] **Audio import settings** (Inspector, per clip):
  - `music_gameplay`, `music_hub` → **Load Type: Streaming**, **Compression: Vorbis**
    (keeps them out of resident memory on device).
  - All `sfx_*` → **Load Type: Decompress On Load**, **Compression: Vorbis** or PCM.
- [ ] **Scenes in build**: confirm `Assets/_Project/Gameplay/Scenes/Launch.unity` is
  **scene index 0** in Build Settings. The board piece art and per-board backgrounds
  are wired on the `PrototypeBootstrap` in that scene (`itemVisualCatalog`,
  `boardBackgroundCatalog`). In a bare scene the runtime `AutoSpawn` path falls back
  to flat colors.
- [ ] **Sprite reimport**: verify `item_crystal_t1..t6` import as **Sprite (2D and UI)**,
  Single mode, and the board shows gems (not colored rectangles).
- [ ] **First-run tutorial**: launch on a clean install (or clear app data) and confirm
  the 3-step coach tutorial fires once and is skippable; relaunch to confirm it does
  not re-fire.
- [ ] **Audio mute**: `AudioManager` persists a mute flag in PlayerPrefs
  (`merge_survivor_audio_muted`); confirm audio plays on a fresh install.

---

## Pre-release quick checks

- [ ] Run **PlayMode tests** (bootstrap, merge, buttons, board unlock, combat modifiers; full run flow; save/load; **MonetizationTests**: gacha/collection/daily/prestige/storefront).
- [ ] Run **Editor test**: **Balance simulation** produces valid CSV and summary. *(Note: this asserts numeric CSV via culture-sensitive parsing and currently fails on non-`en-US` machines, e.g. fr-FR — a pre-existing balance-tool culture bug, unrelated to gameplay/monetization.)*
- [ ] **Monetization smoke** (stubs simulate success in-editor): buy a coin pack → Gems granted; Lucky Chest pull → hero + dupe→shards + pity; equip a hero → squad power rises; win → Double Reward ad doubles gold; lose → Revive ad continues the run; claim the daily; prestige at the unlock wave → multiplier compounds.
- [ ] Smoke test on a physical Android device: login/guest, one full run (spawn, merge, fight, rewards), and return to hub.
- [ ] Confirm **privacy policy** and **store listing** are complete and **Ads** (if any) declared.
