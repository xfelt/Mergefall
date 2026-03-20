# Content pipeline: adding boards, archetypes, and items

All gameplay content (boards, enemy archetypes, items, economy numbers) is stored in **ScriptableObject** assets under `Assets/_Project/Resources/MergeSurvivorData/`. The game loads them at runtime via `GameContentLoader`; no gameplay code needs to be edited to add or change content.

## First-time setup

1. In Unity menu: **Merge Survivor → Content → Create Default Data Assets**.
2. This creates `Assets/_Project/Resources/MergeSurvivorData/` and populates it with default BoardCatalog, EnemyCatalog, ItemDatabase, EconomyTables, and configs (Combat, Merge, Spawn).
3. After that, edit assets in the Inspector or use the content menu to add new entries.

## Adding a new board

**Without editing gameplay code:**

1. **Merge Survivor → Content → Add New Board**.  
   This appends a new board to `BoardCatalog` with a generated Id (e.g. `board_new_5`), default name "New Board", and archetype "grunt".
2. Select `Assets/_Project/Resources/MergeSurvivorData/BoardCatalog` in the Project window.
3. In the Inspector, open the **Boards** list and edit the new entry:
   - **Id**: unique key (e.g. `board_forest`).
   - **Display Name**: name shown in UI.
   - **Difficulty Label**: e.g. "Easy", "Normal", "Hard".
   - **Enemy Multiplier**, **Unlock Cost Resource**, **Spawn Capacity Bonus**, **Merge Reward Multiplier** as desired.
   - **Enemy Archetype Id**: must match an Id in `EnemyCatalog` (e.g. `grunt`, `shield`, `berserk`).
4. **Merge Survivor → Content → Validate Content IDs** to ensure the new board’s `EnemyArchetypeId` exists and there are no duplicate IDs.

## Adding a new enemy archetype

**Without editing gameplay code:**

1. **Merge Survivor → Content → Add New Enemy Archetype**.  
   This appends a new archetype to `EnemyCatalog` with a generated Id (e.g. `archetype_new_3`).
2. Select `Assets/_Project/Resources/MergeSurvivorData/EnemyCatalog`.
3. In the Inspector, edit the new archetype:
   - **Id**: unique key (e.g. `healer`). Use this same Id in boards’ **Enemy Archetype Id**.
   - **Display Name**: name shown in UI.
   - **Flat Power Bonus**, **Wave Power Bonus Per Wave**: scaling for combat.
   - **Modifiers**: optional list (Armor %, Heal, Rage per wave, etc.).
4. Run **Validate Content IDs** so any board that references this archetype is valid.

## Adding a new item

**Without editing gameplay code:**

1. **Merge Survivor → Content → Add New Item**.  
   This creates a new `ItemDefinition` asset and adds it to `ItemDatabase` (e.g. Id `item_new_abc123`).
2. Select the new item asset under `Assets/_Project/Resources/MergeSurvivorData/<item_id>.asset` (or the **Item Database** asset and expand **Items** to find it).
3. In the Inspector set:
   - **Id**: unique key (e.g. `mage_t1`).
   - **Display Name**, **Family Id** (e.g. `mage` for merge chain), **Tier**, **Combat Power**.
   - **Icon**: optional sprite.
4. For merge chains, ensure there is one item per tier (e.g. `mage_t1`, `mage_t2`, …) and that **Family Id** and **Tier** are consistent.
5. Run **Validate Content IDs** to catch duplicate item IDs.

## Economy and combat numbers

- **EconomyTables** (`MergeSurvivorData/EconomyTables`): merge soft base, tier multiplier, spawn/chance upgrade base costs. Edit in Inspector.
- **CombatConfig** (`MergeSurvivorData/CombatConfig`): base enemy strength, per-wave increase, win rewards. Edit in Inspector.

No code changes are required; the game reads these ScriptableObjects at runtime.

## Validation

- **Merge Survivor → Content → Validate Content IDs** checks:
  - Board IDs are unique and non-empty.
  - Every board’s **Enemy Archetype Id** exists in `EnemyCatalog`.
  - Enemy archetype IDs are unique.
  - Item IDs (from referenced `ItemDefinition` assets) are unique.

Warnings are printed to the Console. Fix any issues in the Inspector and re-run validation.

## Loading at runtime

- Content is loaded from **Resources** via `GameContentLoader` (e.g. `GameContentLoader.LoadBoardCatalog()`). Assets must live under `Assets/_Project/Resources/MergeSurvivorData/` so they are included in the build.
- If an asset is missing, the bootstrap falls back to built-in defaults so the game still runs (e.g. in a new clone before running the content menu).

## Optional: Addressables

To switch to Addressables later, keep the same ScriptableObject types and replace `GameContentLoader`’s `Resources.Load<T>(...)` calls with Addressables loading; the rest of the content pipeline (Editor menu, validation, docs) stays the same.
