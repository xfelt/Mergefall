# Asset Manifest — Merge Survivor: Board Quest

This file lists generated/integrated assets, their format, size, project path, the
tool/source used, and license. It complements the art that already shipped in
`Assets/_Project/Art/` (backgrounds, enemies, icons, UI panels, VFX).

All assets listed under **"Generated this pass"** are 100% produced by the scripts
in `Tools/` — no third-party sample content — and are released as **CC0 / public
domain**. Re-run the scripts to regenerate identical output.

---

## Generated this pass

### Audio — `Tools/generate_audio.py`
Pure-Python (stdlib only) procedural synthesis. 16-bit mono PCM WAV @ 44.1 kHz.
Output: `Assets/_Project/Resources/Audio/`

| Asset | Path | Size | Length | Trigger |
|-------|------|------|--------|---------|
| `sfx_pickup.wav` | Resources/Audio/ | ~8 KB | 0.09 s | Piece selected (tap / drag start) |
| `sfx_merge.wav` | Resources/Audio/ | ~62 KB | 0.72 s | Successful 3-merge |
| `sfx_fight_start.wav` | Resources/Audio/ | ~53 KB | 0.61 s | FIGHT pressed |
| `sfx_win.wav` | Resources/Audio/ | ~105 KB | 1.22 s | Victory reveal |
| `sfx_loss.wav` | Resources/Audio/ | ~95 KB | 1.10 s | Defeat reveal |
| `music_gameplay.wav` | Resources/Audio/ | ~1.6 MB | 18.5 s loop | During a run |
| `music_hub.wav` | Resources/Audio/ | ~2.2 MB | 26.7 s loop | At the hub / Meta |

- **Tool/source:** `Tools/generate_audio.py` (additive synthesis, ADSR envelopes,
  one-pole filtering, lightweight reverb). **License:** CC0.
- **Recommended import settings** (apply on first editor open — see Known Issues):
  music loops → **Load Type: Streaming**, **Compression: Vorbis**; SFX →
  **Load Type: Decompress On Load**.

### Item sprites — `Tools/generate_items.py`
Pure-Python faceted-gem renderer (raw PNG via `zlib`, 4× supersampled, RGBA with
alpha). 256×256. Output: `Assets/_Project/Art/Items/`

| Asset | Tier | Hue | Notes |
|-------|------|-----|-------|
| `item_crystal_t1.png` | 1 | Emerald green | Regenerated 32→256 px |
| `item_crystal_t2.png` | 2 | Lapis blue | Regenerated 32→256 px |
| `item_crystal_t3.png` | 3 | Amethyst purple | Regenerated 32→256 px |
| `item_crystal_t4.png` | 4 | Gold topaz | Regenerated 32→256 px |
| `item_crystal_t5.png` | 5 | Ruby crimson | **New tier** |
| `item_crystal_t6.png` | 6 | Radiant diamond | **New tier** |

- **Tool/source:** `Tools/generate_items.py`. **License:** CC0.
- Tiers 1–4 keep their original `.meta` GUIDs (same file paths), so the existing
  `ItemVisualCatalog` / scene references remain valid; only the pixels improved.
- T5/T6 `.meta` files were cloned from `item_crystal_t4.png.meta` (identical sprite
  import settings) with fresh GUIDs.

---

## Wiring / integration changes

| Change | File | Effect |
|--------|------|--------|
| Added `pawn_t5`, `pawn_t6` → sprite mappings | `Assets/_Project/Data/ItemVisualCatalog.asset` | Tiers 5–6 render with art |
| Wired `itemVisualCatalog` into the scene | `Assets/_Project/Gameplay/Scenes/Launch.unity` | Board pieces now show gem sprites (was unassigned → colored rects) |
| Wired `boardBackgroundCatalog` into the scene | `Assets/_Project/Gameplay/Scenes/Launch.unity` | Per-board backgrounds now show (was unassigned → flat color) |
| Extended fallback item ladder to 6 tiers | `Assets/_Project/UI/PrototypeBootstrap.cs` | Merge chain goes T1→…→T6 |
| Added Tier5/Tier6 colors | `Assets/_Project/UI/DesertTheme.cs` | Distinct color fallback for the new tiers |
| New runtime audio service | `Assets/_Project/UI/AudioManager.cs` | Loads `Resources/Audio/*`, plays SFX + looping music |

---

## Pre-existing art (already in repo, not generated this pass)

- **Backgrounds:** `Art/Backgrounds/bg_{canyon,caravan,dunes,oasis,ruins,tent}.png`
- **Enemies:** `Art/Enemies/{bandit,barbarian,gold-scarab}.png`
- **Icons:** `Art/Icons/{currency_soft,currency_resource,gem_premium,icon_*}.png`
- **UI panels (9-slice):** `Art/UI/panel_{brown,tan,grey,white,*_pressed}.png`
- **VFX:** `Art/VFX/vfx_{flame,glow,magic,sand_puff,spark,star}.png`
- **A large unimported library** lives at the repo root under `Asset requested/`
  (desert backgrounds, enemy art, UI sprite pack, VFX, iconography, typography).
  Available for future import; not pulled into `Assets/` this pass to keep the
  build lean.
