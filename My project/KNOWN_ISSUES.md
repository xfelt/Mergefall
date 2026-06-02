# Known Issues & Limitations

Status of the playability pass (Phases A–E) and what remains out of scope or blocked
on external credentials.

## Requires the Unity editor to finalize (could not be verified headlessly)

This pass was authored and **compile-verified with `dotnet build` against the Unity
managed DLLs** (the editor itself could not be launched here — no activated license,
return code 198). The following need a one-time editor open to confirm visually:

- **`.meta` generation for new files.** Files added outside the editor
  (`AudioManager.cs`, the `Resources/Audio/` folder + WAVs) ship without `.meta`;
  Unity generates them on first import. Nothing references them by GUID, so this is
  safe — commit the generated metas afterward. Set music loops to **Streaming** load
  type (see `RELEASE_CHECKLIST.md` §7).
- **Item sprite reimport.** `item_crystal_t1..t6.png` were regenerated at 256×256.
  T1–T4 reuse their existing `.meta` (stale 32 px sprite rect is recomputed by Unity
  on reimport in Single mode). T5/T6 use cloned metas with fresh GUIDs.
- **Scene catalog wiring.** `Launch.unity` now references `ItemVisualCatalog` and
  `BoardBackgroundCatalog` by GUID/fileID; confirm the Inspector shows them assigned.
- **Visual polish.** Bar/animation timings, tutorial highlight placement, and layout
  on a real 1080×1920 / notch device should be eyeballed.

## Phase E — SDK wiring (not done; intentionally deferred)

Billing / AdMob / Firebase remain **stubs** behind scripting defines
(`MERGE_SURVIVOR_USE_IAP`, `MERGE_SURVIVOR_USE_ADMOB`, `MERGE_SURVIVOR_USE_FIREBASE`).
The project compiles cleanly without those SDK packages by default. Activating them
requires external credentials the repo cannot contain:

- A real **Firebase project** + `google-services.json`.
- Real **AdMob** app ID and rewarded/interstitial ad unit IDs.
- **Play Console** app + upload **keystore** for signed release builds.

See `README_MergeSurvivor.md` and `RELEASE_CHECKLIST.md` §6 for the wiring steps.

## Design / content notes

- **Merge ladder now reaches Tier 6.** New `pawn_t5` (power 85) and `pawn_t6`
  (power 160) extend squad power. This shifts late-run balance upward; re-run the
  offline balance simulator (`Merge Survivor → Balance → …`) if tuning matters.
- **Art style.** Generated gem sprites and procedural audio are deliberately simple
  ("clean arcade") and CC0. They are production-safe placeholders that read clearly;
  swap in bespoke art/audio later without code changes (same paths / `Resources` keys).
- **Tutorial is coach-mark + Next-driven**, not gated on the player performing the
  actual merge. It dims the board during the 3 steps and frees interaction afterward.
- **`Asset requested/`** (repo root) holds a large unimported art library; not pulled
  into the build this pass.
