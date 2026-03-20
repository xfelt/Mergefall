# Art Integration Guide — Desert Alchemist's Caravan

> **Theme:** Sun-baked desert, elemental crystals, moving caravan, merchant tent hub.  
> **Style:** TON Visual — silhouettes nettes, outlines épais, 2–3 tons cel/flat, interactables chauds saturés, fonds froids profonds.  
> **Last updated:** 2026-03-14

---

## Current State (Post Visual Pass)

| System | Status | Details |
|---|---|---|
| Typography | **DONE** | `TextMeshProUGUI` everywhere. `LilitaOne-Regular SDF.asset` generated. |
| Color palette | **DONE** | `DesertTheme.cs` centralized palette. |
| Board cells | **DONE (code)** | Desert-themed amber slots via code color. |
| Item visuals | **DONE (code)** | `ItemVisualCatalog` SO created. `item_crystal_t1–t4.png` assigned. |
| Merge feedback | **DONE** | 2-phase EaseOutBack bounce + gold flash. `MergeVFX.prefab` exists. |
| HUD | **DONE** | Structured bar: Wave + Soft + Premium (with `gems_diamond` icon) + Resource. |
| Buttons | **DONE (code)** | Color-coded by action via `DesertTheme`. |
| Panels | **DONE (code)** | Themed with `DesertTheme` panel colors + titles. |
| Onboarding | **DONE** | Branded "Mergefall / Board Quest" overlay. |
| CanvasScaler | **DONE** | 1080×1920 reference, 0.5 match, Scale With Screen Size. |

### Assets in project

| Folder | Files | Status |
|---|---|---|
| `Art/Fonts/` | `LilitaOne-Regular.ttf`, `LilitaOne-Regular SDF.asset` | Ready |
| `Art/UI/` | 6 panel PNGs (tan, brown, grey, white + pressed) | Imported, **need 9-slice borders** |
| `Art/Icons/` | 8 icons (currency_soft, currency_resource, gem_premium, fight, shield, camp, hub, pouch, flask) | Imported |
| `Art/Items/` | `item_crystal_t1–t4.png` (32×32 placeholders) | Assigned in `ItemVisualCatalog.asset` |
| `Art/Enemies/` | bandit, barbarian, gold-scarab | Imported, **not wired** |
| `Art/Backgrounds/` | 6 BGs (dunes, oasis, canyon, ruins, caravan, tent) | Imported, **not wired** |
| `Art/VFX/` | 6 textures + `MergeVFX.prefab` | Prefab exists, **needs particle config** |

### ScriptableObject assets

| Asset | Location | Status |
|---|---|---|
| `ItemVisualCatalog.asset` | `Assets/_Project/` (+ duplicate at `Data/`) | Created, wired to PrototypeBootstrap |
| `BoardBackgroundCatalog.asset` | `Assets/_Project/Data/Catalogs/` | Created, **entries not populated** |
| `LilitaOne-Regular SDF.asset` | `Assets/_Project/Art/Fonts/` | Generated |

---

## Priority Roadmap

### P0 — Publish Blockers (RESOLVED)

All previously-critical blockers are resolved:
- [x] TMP Font Asset generation
- [x] ItemVisualCatalog SO creation + sprite assignment
- [x] Premium gem icon in HUD (`gems_diamond.png`)
- [x] Item crystal sprites T1–T4 (placeholder 32×32, functional)

---

### P1 — High Priority (Visual Polish & Hardening)

| # | Task | Where | Est. | Details |
|---|---|---|---|---|
| 1 | **9-slice borders on panel sprites** | Unity Sprite Editor | 15 min | `Art/UI/panel_tan.png`, `panel_brown.png`, etc. Set border pixels, then assign as `Image.sprite` on buttons/panels in `PrototypeBootstrap.BuildUi()` |
| 2 | **Wire board backgrounds** | `BoardBackgroundCatalog.asset` + `BoardBackgroundView` | 30 min | Populate entries: bg_dunes→garden, bg_canyon→city, bg_ruins→castle/ruins, bg_dunes→arena. Add `BoardBackgroundView` to scene or instantiate in code. |
| 3 | **Enemy portraits in CombatPanel** | `PrototypeBootstrap.ShowFightResult()` | 30 min | Add `Image` for enemy portrait. Map archetype ID → sprite (bandit→grunt, gold-scarab→shield, barbarian→berserk). |
| 4 | **HUD currency icons (soft + resource)** | `PrototypeBootstrap.BuildUi()` | 20 min | Add `currency_soft.png` icon before `_softLabel`, `currency_resource.png` before `_resourceLabel` (same pattern as premium icon). |
| 5 | **Button sprites (9-slice)** | `PrototypeBootstrap.ButtonGo()` | 20 min | Replace flat `Image.color` with `panel_tan.png` / `panel_tan_pressed.png` sprites. Use `Image.type = Sliced`. |
| 6 | **Panel background sprites** | `PrototypeBootstrap.BuildMeta/FightResult/BoardSelect` | 20 min | Replace flat color `Image` with `panel_brown.png` sprite (9-sliced). |
| 7 | **Replace 32×32 item crystals with 256×256 final art** | `Art/Items/` | — | Current placeholders work but are visually low-res. Need proper crystal/golem sprites. |
| 8 | **App icon** | Player Settings | 15 min | No custom icon configured. Need 432×432 icon asset for all Android adaptive icon slots. |
| 9 | **Splash screen** | Player Settings | 10 min | Default Unity splash. Add custom splash with caravan/game branding. |
| 10 | **Bundle ID + product name** | Player Settings | 5 min | Currently `com.DefaultCompany.2D-URP` / "My project". Change to real package name. |

---

### P2 — Important (Game Feel & UX)

| # | Task | Where | Est. | Details |
|---|---|---|---|---|
| 11 | **MergeVFX particle configuration** | `Art/VFX/MergeVFX.prefab` | 30 min | Prefab exists but needs particle modules configured (burst emission, `vfx_star.png` texture, gold tint, short lifetime). Trigger from `PlayMergeHighlight()`. |
| 12 | **Panel slide-in / fade-in animations** | `PrototypeBootstrap` | 45 min | All panels currently `SetActive(true/false)`. Add coroutine-based slide + alpha transitions. |
| 13 | **Flying coin reward animation** | `PrototypeBootstrap.ShowFightResult()` | 45 min | Animate coin sprite from reward panel → HUD currency position on "Continue" press. |
| 14 | **Audio SFX** | New `Audio/` folder | — | No audio assets exist. Need: merge_success, fight_win, fight_loss, button_tap, spawn. |
| 15 | **Onboarding caravan illustration** | `PrototypeBootstrap.BuildOnboardingOverlay()` | 15 min | `bg_caravan.png` exists. Add as background Image behind onboarding text. |
| 16 | **Selected cell highlight** | `CellView` | 20 min | No visual feedback when a cell is tapped/selected before drop. Add glow outline or scale pulse. |
| 17 | **Empty board state** | `PrototypeBootstrap` | 15 min | No visual hint when board is empty (all cells "-"). Show "Tap Spawn to begin" text overlay. |
| 18 | **Revive flow (rewarded ad)** | `GameSession.Fight()` | 30 min | TODO in code: "revive via rewarded ad or premium." Currently auto-fires `ShowRewarded` without UI confirmation. |

---

### P3 — Code Quality & Robustness

| # | Task | Where | Est. | Details |
|---|---|---|---|---|
| 19 | **BoardState bounds checks** | `Gameplay/BoardAndCombat.cs` | 15 min | `Get`, `Set`, `IsEmpty`, `Swap` have no bounds validation — can throw `IndexOutOfRangeException`. |
| 20 | **Null safety in views** | `HUDView.cs`, `BoardBackgroundView.cs` | 10 min | No null checks on serialized fields before use. |
| 21 | **Save/Load error handling** | `Meta/MetaServices.cs` | 15 min | `JsonUtility.FromJson` can fail on corrupt data. Wrap in try/catch. |
| 22 | **ItemDatabase duplicate ID guard** | `Data/ItemDatabase.cs` | 10 min | `Warm()` uses `ToDictionary` which throws on duplicate keys. Add validation. |
| 23 | **Unit tests for core logic** | `Tests/` | 1–2h | No unit tests for `BoardState`, `MergeResolver`, `CombatCalculator`, `ItemDatabase`, `ProgressionService`. |
| 24 | **Remove PlaceholderArt.cs** | `UI/PlaceholderArt.cs` | 5 min | Deprecated, just delegates to `DesertTheme`. Remove and update the one reference in `PrototypeBootstrap`. |
| 25 | **Namespace cleanup** | `HUDView.cs`, `BoardBackgroundView.cs`, `BoardBackgroundCatalog.cs` | 10 min | These 3 files are in global namespace; others use `MergeSurvivor.*`. |
| 26 | **Build scene list** | `EditorBuildSettings` | 5 min | `Launch.unity` not in build list; only `SampleScene.unity` is listed. |
| 27 | **Keystore configuration** | Player Settings | 10 min | No release keystore configured for Android signing. |

---

### P4 — Nice-to-Have (Post-Launch Polish)

| # | Task | Details |
|---|---|---|
| 28 | Move DesertTheme to ScriptableObject | Allow runtime theme tweaking without recompile |
| 29 | Migrate from runtime UI generation to prefabs | `PrototypeBootstrap` builds entire UI in code — fragile and hard to iterate |
| 30 | Implement IAP / Ads / Firebase for real | Currently stubs (`PlatformServices.cs` TODOs) |
| 31 | Add more enemy archetypes + art | Only 3 enemies (bandit, barbarian, gold-scarab) |
| 32 | Board-specific music/ambient | Per-board audio atmosphere |
| 33 | Localization support | All strings hardcoded in code |
| 34 | Accessibility pass | Font scaling, color-blind friendly tiers, screen reader |

---

## Key Code Entry Points

| What to wire | File | Field/Method |
|---|---|---|
| ItemVisualCatalog SO | `PrototypeBootstrap.cs` | `[SerializeField] itemVisualCatalog` |
| Premium icon sprite | `PrototypeBootstrap.cs` | `[SerializeField] premiumIconSprite` (fallback: `Resources.Load("Icons/gems_diamond")`) |
| TMP Font Asset | Project Settings → TextMeshPro → Default Font Asset | — |
| Board backgrounds | `BoardBackgroundCatalog.asset` + `BoardBackgroundView.cs` | Populate entries, add view to scene |
| Merge VFX prefab | `Art/VFX/MergeVFX.prefab` | Configure ParticleSystem, trigger from `PlayMergeHighlight()` |
| Enemy portraits | Add to `EnemyArchetypeDefinition` or visual catalog | Show in `ShowFightResult()` |
