# Mergefall — Editor Verification Report

- **Date:** 2026-06-02
- **Unity version:** 6000.3.11f1 (Unity 6) — confirmed
- **License:** Unity Personal, Assigned, with `com.unity.editor.headless` entitlement (batchmode works)
- **Project:** `My project/` (cloned repo `xfelt/Mergefall`)
- **Resolutions tested:** 1080×1920 and 390×844 (portrait)
- **Verification method:** Headless batchmode for compile + Test Runner; an explicit, render-to-RenderTexture
  capture harness (`Assets/_Project/Tests/VisualCaptureManual.cs`, NUnit `[Explicit]`) loads the real
  `Launch.unity` scene and dumps PNGs for visual review. (`ScreenCapture.CaptureScreenshot` and
  `WaitForEndOfFrame` do not work in batchmode, so an explicit `Camera.Render()` path is used.)
- **Screenshots:** `Logs/screenshots/1080x1920/` and `Logs/screenshots/390x844/` (Logs is git-ignored).
- **Overall status:** **PASS** — compiles clean, all tests green, full loop plays end-to-end with art,
  audio, a 3-step tutorial, and a revive prompt. One open item is a design decision (item-tier count), not a bug.

> Note: the earlier `dotnet build` workaround is obsolete — a valid Unity Personal license is now active,
> so full batchmode compile/test/render is the authoritative path.

---

## 1. Compilation — zero errors — **PASS**

- Headless import + compile → exit 0, **0 CS errors, 0 CS warnings** from project scripts.
- Benign noise only: one Unity-internal ILPP "duplicate hint path" warning; `Curl error 42` on shutdown.
- Acceptable per brief: missing `google-services.json` / SDK defines (`MERGE_SURVIVOR_USE_IAP/_ADMOB/_FIREBASE`).

## 2. Play mode — game loop end to end

| Check | Result | Note |
|---|---|---|
| Starts without exceptions | **PASS** | No exceptions in tests or capture run. |
| Board renders item sprites | **FIXED** | `itemVisualCatalog` was unassigned in `Launch.unity`; wired `Data/ItemVisualCatalog.asset`. |
| Board background + cell states | **FIXED** | `boardBackgroundCatalog` wired; per-board desert backgrounds render; empty cells show `-`. |
| Drag/tap interaction works | **PASS** | Covered by PlayMode tests (`CellView.OnPointerClick` select→merge). |
| Merge 3 identical → animation/VFX | **FIXED** | On-cell scale/color pulse **plus** a UI sparkle burst centered on the merged cell (replaced a world-space ParticleSystem that rendered off-cell). |
| FIGHT button visible/tappable/lower | **PASS** | Red full-width button in lower-mid area. |
| Tapping FIGHT opens overlay | **PASS** | `FightResultPanel` opens; plays a fight-start sound. |
| Fight overlay shows win/loss clearly | **FIXED** | "Victory!"/"Defeat" + colored "X vs Y" score (titles were zero-width-clipped; `Label` helper fixed). |
| Win → reward, continue returns | **PASS** | Continue returns; win shows reward line. |
| Loss → revive prompt | **FIXED** | New revive prompt ("You fell in battle") with **Revive** (stub) / **Give Up** (→ hub). |
| Audio (merge/fight-start/win/loss/music) | **FIXED** | Generated 5 WAV assets (`Assets/_Project/Audio/`), added fight-start + looping music source, wired all into the scene. Capture log: `music=music_loop isPlaying=True merge=True win=True loss=True start=True`. |
| Meta Hub opens, upgrades visible | **FIXED** | "Merchant Tent" opens; title now visible; styled buttons. |

## 3. Tutorial first-run — **FIXED → PASS**

Implemented a 3-step interactive tutorial (dim overlay + coaching card + Skip/Next), launched from the
onboarding "Begin Journey" button and gated by `merge_survivor_tutorial_done`:

| Check | Result | Note |
|---|---|---|
| Overlay appears on first run | **PASS** | Onboarding splash → Begin Journey → tutorial. |
| Step 1 highlights two matching items | **PASS** | Two gold highlight frames over the matching crystals. |
| Step 2 shows merge result | **PASS** | Advancing performs the merge → cell becomes T2 (asserted in test). |
| Step 3 highlights FIGHT | **PASS** | Highlight frame moves onto the FIGHT button. |
| Skip button works | **PASS** | Closes overlay, sets done flag (test `Tutorial_Skip_*`). |
| Does NOT reappear on 2nd run | **PASS** | Gated by PlayerPref; asserted with a second bootstrap. |

Covered by tests: `Tutorial_FirstRun_ShowsStepsMergesAndDoesNotRepeat`, `Tutorial_Skip_ClosesAndSetsDoneFlag`.

## 4. Visual quality bar

| Screen | Result | Note |
|---|---|---|
| Board — items distinct across **6** tiers | **FIXED → PASS** | Replaced the 4 temporary crystal sprites with a fresh set of **6** faceted gem tiers — Quartz / Amber / Turquoise / Amethyst / Emerald / Sunstone — each a distinct hue with tier-scaling glow/sparkle. Item list + visual catalog + content pipeline all extended to 6 (`pawn_t1..t6`, powers 5/12/24/45/80/140). Merge chains t1→t6 and caps cleanly. See `Logs/screenshots/.../10_all_tiers.png`. |
| Fight — win/loss differentiated | **FIXED** | Red "Defeat" / gold "Victory!" + colored score. |
| Enemy portrait | **FIXED** | Black background keyed out → transparent (alpha-feathered) on `Art/Enemies/*.png`. |
| Meta Hub — styled | **PASS** | Title + styled green/blue buttons. |
| Fonts legible, not Times New Roman | **PASS** | TMP LiberationSans SDF; fixed tofu onboarding glyphs (→ `•`). |

## 5. Safe area (390×844) — **FIXED → PASS**

- Added `ApplySafeArea()` insetting the root container to `Screen.safeArea` (no-op without device insets).
- At 390×844 the board + FIGHT button remain centered and unclipped; HUD fits within the top bound.

## 6. PlayMode tests — **FIXED → PASS (14/14, +1 explicit capture skipped)**

- Originally 3 stale failures from the hub/run-state UI overhaul (active-only `GameObject.Find` on run-gated
  buttons; fight score moved to the result panel). Fixed lookups + assertions.
- Added: `Tutorial_FirstRun_*`, `Tutorial_Skip_*`, `Loss_ShowsRevivePrompt*`.

## 7. EditMode / balance tests — **FIXED → PASS (1/1)**

- `win_rate should be numeric` failed under **fr-FR** (decimal `,`): CSV is written `InvariantCulture` (`"1.000"`)
  but parsed with CurrentCulture. Fixed to parse with `InvariantCulture`.

---

## Fixes & features this session (all verified in editor)

| File | Change |
|---|---|
| `Editor/BalanceSimulationEditorTests.cs` | Parse CSV numbers with `InvariantCulture` (fr-FR fix). |
| `Tests/PlayModeBootstrapTests.cs` | Fix stale run-gated-button lookups; add tutorial/skip/revive tests. |
| `UI/PrototypeBootstrap.cs` | Fix `Label` width (panel titles); `ApplySafeArea`; bullet glyph; **3-step tutorial**; **revive prompt**; **fight-start SFX + looping music**; **on-cell UI merge burst**. |
| `Gameplay/Scenes/Launch.unity` | Wire `itemVisualCatalog`, `boardBackgroundCatalog`, and 5 audio clips. |
| `Art/Enemies/*.png` | Keyed out opaque black backgrounds → transparent. |
| `Art/Items/item_gem_t1..t6.png` (new) | Fresh 6-tier faceted gem art (replaced the 4 temporary crystals). |
| `Data/ItemVisualCatalog.asset` | Rebuilt with 6 tier→sprite entries. |
| `Editor/ContentPipeline.cs` | Item generator extended to the 6 gem tiers. |
| `Audio/*.wav` (new) | Generated merge / fight-start / win / loss SFX + 8s seamless music loop. |
| `Tests/VisualCaptureManual.cs` (new) | `[Explicit]` render-to-RenderTexture capture harness + audio diagnostic + 6-tier showcase. |

## Production-asset pass (replaced all placeholder art/audio)

- **UI**: regenerated the 9-slice panel/button sprites — buttons are now glossy, rounded, gradient-shaded
  (authored near-white so the theme tint reads cleanly); modals use a beveled parchment frame.
- **Items**: 6 faceted gem tiers (distinct hues, glow, sparkle).
- **Enemies**: portraits composited into themed circular medallions (gradient + gold rim + framed figure),
  replacing the flat silhouettes.
- **HUD icons**: clean coin / faceted diamond / crystal-cluster currency icons.
- **Audio**: re-synthesized as stereo, produced sound — additive **bell** synthesis, ADSR envelopes, and
  convolution **reverb**; merge chime, cinematic fight-start impact, victory arpeggio, somber defeat sting,
  and a 16s seamless ambient music loop (bass + pads + arpeggio + soft kick). Verified playing in-scene.

All art/audio is now original first-party production-quality content authored procedurally (the AI image
service was unavailable — hard free-tier quota). Backgrounds and the LilitaOne font were already final and kept.

## Remaining items (decisions / external only)

1. **`google-services.json` + real SDK defines** — deferred by design (platform stubs in place); requires
   real accounts/credentials (AdMob, Firebase, Play Billing) to finalize.
