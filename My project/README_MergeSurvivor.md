# Merge Survivor: Board Quest

> ## Last editor verification
> - **Date:** 2026-06-02
> - **Unity:** 6000.3.11f1 (Unity 6), Unity Personal license (headless entitlement active)
> - **Resolutions tested:** 1080×1920 and 390×844 (portrait)
> - **Overall status:** **PASS**
> - Compiles with 0 errors; all EditMode (1/1) and PlayMode (14/14, +1 explicit capture) tests pass.
> - Board renders real item sprites + per-board backgrounds; fight panel shows Victory/Defeat + score; on-cell merge VFX; safe-area handling.
> - **Gaps closed this pass:** procedural audio (merge/fight-start/win/loss + looping music, wired & playing); 3-step interactive tutorial with Skip (highlight items → merge → highlight Fight); revive prompt on loss; transparent enemy portraits; correctly-positioned merge burst; **6 distinct gem tiers** (Quartz→Sunstone) with fresh art + balanced powers replacing the temporary 4.
> - **Production-asset pass:** replaced all placeholder art/audio with original production-quality content — glossy 9-slice UI panels/buttons, parchment modals, 6 faceted gem tiers, framed enemy-portrait medallions, clean HUD currency icons, and re-synthesized stereo audio (bell SFX + reverb + 16s music loop).
> - **Open (external only):** `google-services.json` + real SDK defines (AdMob/Firebase/Billing) deferred by design — need real credentials.
> - Full details: see `VERIFICATION_REPORT.md`.

Unity Android project for the merge/fight/meta loop. Code lives under **`Assets/_Project`**: Core, Data, Gameplay, Meta, Economy, Platform, UI, Editor, Tests.

## Current state

- **Merge/fight/meta loop**: Spawn → merge (drag/tap) → fight wave → rewards → unlock boards → Meta Hub upgrades → persist.
- **Board progression**: Boards carry enemy multiplier, archetype, spawn capacity bonus, merge reward multiplier; unlock/select and difficulty scaling per board.
- **Enemy archetypes and modifiers**: Ordered modifiers with stack rules (Armor, Rage, Heal); analytics event per applied modifier.
- **Balance simulator**: Offline simulation with Standard / Onboarding / Late-Game profiles; dry-run with suggested (damped) deltas; outputs in `Logs/`.
- **Platform**: Billing, Ads, Analytics, RemoteConfig, CloudSave **stubs** in place; real SDKs wired via scripting defines (`MERGE_SURVIVOR_USE_IAP`, `MERGE_SURVIVOR_USE_ADMOB`, `MERGE_SURVIVOR_USE_FIREBASE`).

## Production phases (5–10): done vs TODO

| Phase | Scope | Status |
|-------|--------|--------|
| **5** | Core loop (merge/fight/meta, rewards, persistence) | Done |
| **6** | Board progression, enemy archetypes + modifiers | Done |
| **7** | Balance simulator (profiles, dry-run, damped deltas) | Done |
| **8** | Platform stubs (Billing/Ads/Analytics/RemoteConfig/CloudSave) | Done |
| **9** | Android AAB build + release checklist + keystore/CI env | Done |
| **10** | PlayMode + smoke + balance editor tests | Done |

**Still TODO**

- Replace stubs with real implementations (Play Billing, AdMob, Firebase Analytics/Remote Config).
- Add `google-services.json` and production product/ad unit IDs (see `RELEASE_CHECKLIST.md`).
- Optional: consolidate default board/enemy data (currently fallbacks in `PrototypeBootstrap`, `ContentPipeline`, and `BalanceSimulationTool`) into single source (e.g. seeded Resources or Content Pipeline only).

## Structure
- `Assets/_Project/Core` base contracts and rewards
- `Assets/_Project/Data` ScriptableObject data/config definitions
- `Assets/_Project/Gameplay` board, merge, combat, game session logic
- `Assets/_Project/Economy` currencies and inventory/account model
- `Assets/_Project/Meta` progression upgrades and local save
- `Assets/_Project/Platform` Billing/Ads/Analytics/RemoteConfig/CloudSave stubs
- `Assets/_Project/UI` runtime playable bootstrap UI
- `Assets/_Project/Editor` launch scene tooling

## Current Loop
1. Spawn pieces
2. Drag-drop or tap-swap to merge
3. Fight wave based on board power
4. Win resources
5. Unlock and rotate boards (difficulty scales per board)
6. Upgrade base in Meta Hub
7. Persist account/progression

Boards now carry:
- enemy multiplier
- enemy archetype (flat + per-wave power bonuses)
- spawn capacity bonus
- merge reward multiplier

Enemy archetypes now support multiple ordered modifiers with stack rules:
- explicit order execution
- per-group stacking control
- analytics event emitted per applied modifier

Balancing helper:
- Menu `Merge Survivor/Balance/Run Offline Balance Simulation`
- Optional menus for target profiles:
  - `Run Simulation (Onboarding Profile)`
  - `Run Simulation (Late-Game Profile)`
- Generates profile-specific outputs:
  - `Logs/balance-sim-standard.csv`, `Logs/balance-sim-summary-standard.md`
  - `Logs/balance-sim-onboarding.csv`, `Logs/balance-sim-summary-onboarding.md`
  - `Logs/balance-sim-lategame.csv`, `Logs/balance-sim-summary-lategame.md`
- Also updates latest aliases:
  - `Logs/balance-sim.csv`
  - `Logs/balance-sim-summary.md`
- Summary includes per-board diagnosis + suggested parameter delta table
- Suggested deltas are damped and capped per pass (safer iterative convergence)

## Android build (AAB)

### 1) Configure package + version source
- Open/Create `AppPlatformConfig` via `Merge Survivor > Platform Config`.
- Put a copy at `Assets/Resources/AppPlatformConfig.asset` so Editor/runtime can load it.
- Set:
  - `Android Package` (must match Play Console + Firebase app package).
  - `Use Project Settings Version`:
    - `On`: build uses `Project Settings > Player > Version` + `Bundle Version Code`.
    - `Off`: build uses `Android Bundle Version` + `Android Bundle Version Code` from `AppPlatformConfig`.

### 2) Apply baseline Android defaults (optional helper)
- Run `Merge Survivor > Setup > Apply Android Defaults`.
- This sets package/version (from `AppPlatformConfig` when available), Min SDK 26, target SDK auto, and enables AAB output.

### 3) Build AAB
- Dev bundle: `Merge Survivor > Build > Build AAB for dev`
  - Development build + debugging enabled.
- Release bundle: `Merge Survivor > Build > Build AAB for release`
  - Production build flags + release signing checks.
- Optional chooser: `Merge Survivor > Build > Build AAB (choose flavor)`.
- Output path: `Builds/Android/*.aab`.

### 4) Keystore for release (local or CI)
- Release build requires a custom keystore.
- Two supported modes:
  1. **Player Settings**: set keystore and alias under `Project Settings > Player > Android > Publishing Settings`.
  2. **Env/CI injection** (preferred for CI):
     - `MS_ANDROID_KEYSTORE_PATH` (or `ANDROID_KEYSTORE_PATH`)
     - `MS_ANDROID_KEYSTORE_PASSWORD` (or `KEYSTORE_PASSWORD`)
     - `MS_ANDROID_KEYALIAS_NAME` (or `KEYALIAS_NAME`)
     - `MS_ANDROID_KEYALIAS_PASSWORD` (or `KEYALIAS_PASSWORD`)
- Do not commit keystore files or passwords.

### 5) Play Console upload package
- Upload the generated **`.aab`** to Internal/Closed/Open testing or Production track.
- Complete/update required release metadata:
  - Store listing (title, short/full description, screenshots, feature graphic).
  - Content rating questionnaire.
  - Target audience + app access declarations.
  - Data safety + privacy policy URL.
  - Ads declaration (if AdMob/ads are used).

## How to run and test

- **Unity version**: **6000.3.x** (Unity 6). See `ProjectSettings/ProjectVersion.txt`.
- **Open project**: Open the **My project** folder as the Unity project root.
- **Launch scene**: Use **Merge Survivor > Create Launch Scene** if needed; scene at `Assets/_Project/Gameplay/Scenes/Launch.unity`. Or press Play in any scene; `PrototypeBootstrap` creates the playable UI at runtime.
- **Balance simulation / dry-run**:
  - In Editor: **Merge Survivor > Balance > Run Offline Balance Simulation** (or Onboarding / Late-Game profile, or **Run Dry-Run With Suggested Deltas**).
  - Batch (CI):  
    `Unity.exe -batchmode -nographics -projectPath "<path-to-My project>" -executeMethod MergeSurvivor.Editor.BalanceSimulationTool.RunOfflineBalanceSimulationBatch -quit`  
    Optional dry-run: `MergeSurvivor.Editor.BalanceSimulationTool.RunDryRunWithSuggestedDeltasBatch`.
- **Tests**:
  - **PlayMode**: Test runner, filter to **PlayModeBootstrapTests** (bootstrap UI, merge, fight, board unlock, modifiers, save/load).
  - **Editor**: **BalanceSimulationEditorTests** (balance run produces valid CSV and summary in `Logs/`).
  - **Smoke (batch)**:  
    `Unity.exe -batchmode -nographics -projectPath "<path>" -executeMethod MergeSurvivor.Editor.RuntimeSmokeCheck.Run -quit`
- **CI**: Minimal steps (batch compile, PlayMode tests, optional balance dry-run) are in **`CI_CHECKLIST.md`**.

## Next priorities

1. Replace Billing/Ads/Analytics/RemoteConfig stubs with real SDKs and add `google-services.json`.
2. Set production product/ad unit IDs in `AppPlatformConfig` (or env); see `RELEASE_CHECKLIST.md`.
3. Optional: single source for default boards/enemies (Content Pipeline or Resources) to avoid duplicated fallback data.

## Verification (as of last check)

- **Assembly references**: All asmdefs valid; Editor references Platform; no broken script references.
- **Build**: Unity batch compile succeeds (no missing packages in default setup).
- **Tests**: EditMode (balance regression) and PlayMode (bootstrap, merge, fight, modifiers, save/load) pass; runtime smoke check passes.
- **IAP**: `com.unity.purchasing` is not in the default manifest so the project compiles without it. Google Play Billing is implemented in `StoreServiceGooglePlay.cs.off`; to enable, add the IAP package, add `UnityEngine.Purchasing` to `MergeSurvivor.Platform.asmdef`, rename that file to `.cs`, and uncomment the factory branch (see `PlatformServiceFactory.CreateStore` and `Assets/_Project/Platform/README.md`).

## Android integration TODOs
- Replace `StoreServiceStub` with Google Play Billing implementation (restore and wire `StoreServiceGooglePlay` as above).
- Replace `AdsServiceStub` with AdMob rewarded/interstitial.
- Replace `AnalyticsServiceStub` / `RemoteConfigServiceStub` with Firebase SDK.
- Add `google-services.json`.
