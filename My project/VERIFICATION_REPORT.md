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
- **Overall status:** **PARTIAL PASS** — code/tests/visuals are solid; remaining gaps are missing assets
  (audio) and unimplemented scope (multi-step tutorial, revive prompt).

> Note: The earlier `dotnet build` workaround is no longer required — a valid Unity Personal license is now
> active on this machine, so full batchmode compile/test/render is the authoritative path.

---

## 1. Compilation — zero errors — **PASS**

- Headless import + compile (`Unity.exe -batchmode -quit -projectPath …`) → exit 0,
  `Exiting batchmode successfully now!`, **0 CS errors, 0 CS warnings** from project scripts.
- Only benign noise: one Unity-internal ILPP "duplicate hint path" warning (not project code) and a
  `Curl error 42: Callback aborted` during shutdown (Accelerator/analytics network teardown).
- Expected/acceptable per brief: missing `google-services.json`, missing SDK defines
  (`MERGE_SURVIVOR_USE_IAP` / `_ADMOB` / `_FIREBASE`).

## 2. Play mode — game loop end to end

| Check | Result | Note |
|---|---|---|
| Starts without exceptions | **PASS** | Only stub-service `Debug.Log` lines; no exceptions in tests or capture run. |
| Board renders item sprites (not colored rectangles) | **FIXED** | `itemVisualCatalog` was unassigned in `Launch.unity` → board fell back to placeholder colors. Wired the populated `Data/ItemVisualCatalog.asset`; board now shows crystal sprites. |
| Board background + cell states visible | **FIXED** | `boardBackgroundCatalog` was unassigned. Wired `Data/Catalogs/BoardBackgroundCatalog.asset`; per-board desert backgrounds now render; empty cells show `-`. |
| Drag/tap interaction works | **PASS** | Exercised by PlayMode tests (`CellView.OnPointerClick` select→merge path) and the capture harness. |
| Merge 3 identical → animation/VFX | **PASS (partial)** | On-cell scale/color merge animation works and merge upgrades the tier (T1→"Knight T2"). The particle burst prefab **fires but is mispositioned** (world-space `ParticleSystem` inside a screen-space canvas — known uGUI limitation). |
| FIGHT button visible, tappable, lower-screen | **PASS** | Red "Fight" button, full-width, mid-lower area. |
| Tapping FIGHT opens fight overlay | **PASS** | `FightResultPanel` opens on fight. |
| Fight overlay shows win/loss clearly | **FIXED** | Panel title ("Victory!"/"Defeat") and score ("27 vs 28", gold vs red) were invisible (zero-width label rect). Fixed the `Label` helper; now clearly differentiated by color + text. No animated *progress bar* — result is shown as an instant panel. |
| Win → reward, tap continue returns | **PASS** | "Continue" returns to board; on win, rewards line (`+soft / +resource`) is shown. |
| Loss → revive prompt | **FAIL (gap)** | No revive prompt exists. Loss shows "Merge more pieces, then Fight again" and Continue → returns to hub. Needs implementation (even a stub). |
| Meta Hub opens, upgrade cards visible | **FIXED** | "Merchant Tent" opens; title/subtitle were invisible (same label bug) — now shown. Cards are styled green/blue buttons (not elaborate cards). |
| Audio (merge/fight/win/loss/music) | **FAIL (gap)** | **No audio assets exist in the project** (`.wav/.mp3/.ogg` search empty); `sfxMerge/sfxFightWin/sfxFightLoss` are null and there is no background music. Requires real audio assets. |

## 3. Tutorial first-run — **PARTIAL**

| Check | Result | Note |
|---|---|---|
| Overlay appears on first run | **PARTIAL** | A static **onboarding splash** appears (title + 3 bullets + "Begin Journey"), not the spec'd interactive tutorial. Tofu bullet glyphs fixed (font lacked ✦/⚔/⛺ → now `•`). |
| Step 1 highlights two matching items | **NOT IMPLEMENTED** | No step-based highlighting exists. |
| Step 2 shows merge result | **NOT IMPLEMENTED** | — |
| Step 3 highlights FIGHT button | **NOT IMPLEMENTED** | — |
| Skip button works | **PARTIAL** | "Begin Journey" dismisses the splash; there is no separate "Skip". |
| Does NOT reappear on 2nd run | **PASS** | Gated by `merge_survivor_onboarding_done` PlayerPref (PlayerPrefs persistence covered by `SaveLoad_*` tests). |

## 4. Visual quality bar

| Screen | Result | Note |
|---|---|---|
| Board — items distinct across tiers | **PASS** | T1 (crystal) vs T2 ("Knight") sprites are distinct. NB: the game implements **4** item tiers (`pawn_t1..t4`), not 6; the brief's "6 tiers" does not match content. The "Pawn T1" text label still overlays each sprite (minor clutter). |
| Fight — win/loss differentiated | **FIXED** | "Defeat" in red, "Victory!" in gold/green, colored score. |
| Meta Hub — styled | **PARTIAL** | Styled buttons + visible title now; still a flat brown panel with large empty space; not card-styled. |
| Fonts legible, not Times New Roman | **PASS** | Clean sans (TMP LiberationSans SDF); legible at both resolutions. |

Remaining prototype-ish elements (documented, not blocking): flat single-color panels with empty space,
enemy portrait art (`Art/Enemies/bandit.png`) has an opaque black background box, item tier text over sprites.

## 5. Safe area (390×844) — **FIXED**

- There was **no safe-area handling** (`Screen.safeArea` unreferenced); the top HUD bar was flush to the
  screen edge and would sit under a notch/status bar.
- Added `ApplySafeArea()` insetting the root container to `Screen.safeArea` (no-op on hardware without
  insets / in editor batch).
- At 390×844 the board and FIGHT button remain centered and unclipped; HUD fits within the top bound.

## 6. PlayMode tests (`PlayModeBootstrapTests`) — **FIXED → PASS (11/11)**

- **Before:** 3 failures (`CreatesCoreUiObjects`, `ButtonsTriggerExpectedUiState`, `BoardUnlockAndSelectionFlowWorks`).
- **Root cause:** the recent UI overhaul added a hub→run state; Spawn/Fight/Next-Board buttons start
  **inactive** at the hub, but the tests located them with `GameObject.Find` (active-only). The fight score
  also moved from the status label into the `FightResultPanel` ("X vs Y").
- **Fix:** locate run-gated buttons via the existing include-inactive helper (their click handlers act on
  the session directly, so `onClick.Invoke()` remains valid) and assert on the fight result panel instead of
  the status string. Product behavior unchanged.

## 7. EditMode / balance tests (`BalanceSimulationEditorTests`) — **FIXED → PASS (1/1)**

- **Before:** `BalanceSimulator_ProducesValidCsvAndSummary` failed: `win_rate should be numeric`.
- **Root cause:** machine culture is **fr-FR** (decimal separator `,`). The CSV is written with
  `InvariantCulture` (`"1.000"`), but the test parsed with `float.TryParse` using CurrentCulture, which
  rejects the `.` decimal. The simulator was correct; the test was culture-fragile.
- **Fix:** parse with `NumberStyles` + `CultureInfo.InvariantCulture`, matching the writer.

---

## Fixes made this session (all verified in editor)

| File | Change |
|---|---|
| `Assets/_Project/Editor/BalanceSimulationEditorTests.cs` | Parse CSV numbers with `InvariantCulture` (fix fr-FR failure). |
| `Assets/_Project/Tests/PlayModeBootstrapTests.cs` | Locate run-gated buttons including inactive; assert fight result panel instead of status `/`. |
| `Assets/_Project/UI/PrototypeBootstrap.cs` | Fix `Label` helper so point-anchored panel titles/subtitles get a real width (were clipped to nothing); add `ApplySafeArea`; replace tofu onboarding glyphs with `•`. |
| `Assets/_Project/Gameplay/Scenes/Launch.unity` | Wire `itemVisualCatalog` + `boardBackgroundCatalog` (items + backgrounds now render). |
| `Assets/_Project/Tests/VisualCaptureManual.cs` | New `[Explicit]` capture harness (excluded from default suite). |

## Remaining issues requiring external assets or human decisions

1. **Audio** — no audio clips exist anywhere in the project. Add merge/fight/win/loss SFX and background
   music, then assign `sfxMerge/sfxFightWin/sfxFightLoss` (and a music source) on the `Launch.unity`
   bootstrap. *(Blocked on assets.)*
2. **Tutorial** — only a static onboarding splash exists. The 3-step highlighted tutorial (highlight items →
   show merge → highlight FIGHT, with a Skip button) is not implemented. *(Scope/feature decision.)*
3. **Revive prompt on loss** — not implemented; loss returns to hub. *(Feature decision — even a stub.)*
4. **Enemy portrait art** — `Art/Enemies/*.png` have opaque black backgrounds; need transparent cutouts.
5. **Merge particle VFX positioning** — world-space `ParticleSystem` does not map to screen-space UI;
   needs a UI-particle approach or a nested world/camera canvas if the burst-on-cell effect is desired.
6. **Tier count** — content has 4 item tiers; the brief references 6. Confirm intended tier count.
7. **`google-services.json` + real SDK defines** — deferred by design (stubs in place).
