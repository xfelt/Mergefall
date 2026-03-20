# Firebase Remote Config — Balance Keys for Tuning

Use these **parameter keys** in Firebase Remote Config to tune balance at runtime without a client update. The game reads them after `IRemoteConfigService.Initialize()` and overrides the local **CombatConfig** and **EconomyTables** values when a key is present. Local assets remain the **fallback** when a key is missing or fetch fails.

## Keys to create in Firebase Console

Create each key as **Number** (integer) in Firebase Remote Config. Set default values in the Firebase Console to match your desired production defaults; the game uses `CombatConfig` / `EconomyTables` from Resources only when the key is not provided by Remote Config.

| Key | Description | Typical fallback (local asset) |
|-----|-------------|--------------------------------|
| `balance_base_enemy_strength` | Base enemy power at wave 1. | CombatConfig: baseEnemyStrength (e.g. 30) |
| `balance_per_wave_increase` | Extra enemy power per wave. | CombatConfig: perWaveIncrease (e.g. 10) |
| `balance_win_soft` | Soft currency (coins) per fight win. | CombatConfig: winSoft (e.g. 30) |
| `balance_win_resource` | Progression resource per fight win. | CombatConfig: winResource (e.g. 1) |
| `economy_merge_soft_base` | Base soft reward per merge. | EconomyTables: mergeSoftBase (e.g. 5) |
| `economy_merge_tier_multiplier` | Extra soft per merge tier. | EconomyTables: mergeTierMultiplier (e.g. 5) |
| `economy_spawn_upgrade_base_cost` | Base cost for spawn capacity upgrade (cost = base × (level + 1)). | EconomyTables: spawnUpgradeBaseCost (e.g. 100) |
| `economy_chance_upgrade_base_cost` | Base cost for starting chance upgrade. | EconomyTables: chanceUpgradeBaseCost (e.g. 120) |

## Flow

1. Game loads **CombatConfig** and **EconomyTables** from `Resources/MergeSurvivorData/` (local fallbacks).
2. **IRemoteConfigService** (stub or Firebase) is initialized.
3. **BalanceRemoteConfigApplier.Apply()** runs: for each key above, value = Remote Config value if available, else local config value. Optional **RemoteConfigSimulation** in Resources overrides Remote Config when present (for testing).
4. Runtime values are applied to the configs via `SetRuntime*`; gameplay uses the resulting numbers.

## Testing without Firebase

- Use **Merge Survivor > Remote Config > Simulate Remote Config...** to open the simulation window.
- Set values and **Save**; they are stored in `RemoteConfigSimulation` under `Resources/MergeSurvivorData/`.
- At runtime, if that asset exists, its entries override Remote Config. Use **Clear simulation** in the window to remove overrides and rely on real Remote Config / local defaults.

## Constant names in code

Use `MergeSurvivor.Data.RemoteConfigKeys` for the same key strings (e.g. `RemoteConfigKeys.BaseEnemyStrength` → `"balance_base_enemy_strength"`).
