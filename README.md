# Merge Survivor: Board Quest (Prototype Foundation)

**Main project and docs:** Open **`My project`** as the Unity project. See **`My project/README_MergeSurvivor.md`** for current state, phases, how to run and test, and CI.

Unity mobile prototype foundation for an Android-first hybrid-casual loop:

1. Merge on board
2. Resolve survival fight
3. Upgrade base
4. Repeat and progress

## Project Structure

- `Assets/_Project/Core` - shared contracts and cross-module primitives
- `Assets/_Project/Gameplay` - board state, merge resolver, combat simulation, session flow
- `Assets/_Project/Meta` - progression upgrades and persistence
- `Assets/_Project/Economy` - account data, currencies, inventory, shop contract
- `Assets/_Project/Platform` - SDK boundary interfaces and editor-safe stubs
- `Assets/_Project/UI` - runtime launch bootstrap, HUD, board cell interactions
- `Assets/_Project/Data` - ScriptableObject definitions and economy/combat configs
- `Assets/_Project/Editor` - scene scaffolding utility
- `Assets/_Project/Tests` - pure-logic tests (merge + combat)

## Current Playable Loop

- Runtime bootstrap auto-creates a playable UI in any scene.
- Board is 4x4 with drag/drop and tap-swap support.
- Merge rule defaults to 3 identical items -> next tier.
- Optional 2-item merge fallback exists through `MergeRulesConfig`.
- Fight converts current board items to squad power and resolves against wave strength.
- Win grants soft currency + progression resource.
- Loss triggers revive hook through ads stub (TODO SDK hookup).
- Meta hub provides upgrades:
  - spawn capacity bonus
  - starting item chance bonus
- Account + progression are persisted locally using `PlayerPrefs` JSON.

## Launch Scene

- Auto-playable mode: press Play in any scene, `PrototypeBootstrap` starts automatically.
- Optional authored scene:
  - Use menu: `Merge Survivor/Create Launch Scene`
  - Scene path: `Assets/_Project/Gameplay/Scenes/Launch.unity`

## Android / Google Play Readiness Notes

Central platform config object:
- `AppPlatformConfig` contains:
  - Android package + version placeholders
  - Billing product ID placeholders
  - AdMob unit ID placeholders
  - Firebase project placeholder

TODO integration markers already added in platform stubs:
- `StoreServiceStub` -> Google Play Billing client + purchase acknowledge flow
- `AdsServiceStub` -> AdMob rewarded/interstitial load/show lifecycle
- `AnalyticsServiceStub` -> Firebase Analytics events
- `RemoteConfigServiceStub` -> Firebase Remote Config fetch/activate
- `CloudSaveServiceStub` -> future cloud save provider

Release pipeline TODOs:
- Configure package id + versioning strategy in Player Settings for Android
- Add keystore and signing config for release
- Build AAB (`Build App Bundle`) for Play Console upload
- Add `google-services.json` for Firebase setup
- Replace placeholder product/ad unit IDs with production IDs

## Next Implementation Priorities

1. Replace stubs with real Billing/AdMob/Firebase SDK adapters.
2. Move runtime item/config defaults into authored ScriptableObject assets.
3. Expand merge content families and board events.
4. Add richer fight simulation and enemy archetypes.
5. Add LiveOps-ready remote economy tuning keys and offer surfaces.
