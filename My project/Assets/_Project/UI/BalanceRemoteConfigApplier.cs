using System.Collections.Generic;
using MergeSurvivor.Data;
using MergeSurvivor.Platform;

namespace MergeSurvivor.UI
{
    /// <summary>Applies Remote Config (and optional simulation overrides) to CombatConfig and EconomyTables. Use local config values as fallback when a key is missing.</summary>
    public static class BalanceRemoteConfigApplier
    {
        /// <summary>Read balance keys from Remote Config with local fallbacks; apply optional simulation overrides on top. Call after configs and Remote Config are initialized.</summary>
        public static void Apply(
            IRemoteConfigService remoteConfig,
            CombatConfig combatConfig,
            EconomyTables economyTables,
            IReadOnlyDictionary<string, int> simulationOverrides = null)
        {
            if (remoteConfig == null || combatConfig == null || economyTables == null) return;

            int GetInt(string key, int fallback)
            {
                if (simulationOverrides != null && simulationOverrides.TryGetValue(key, out var simulated))
                    return simulated;
                return remoteConfig.GetInt(key, fallback);
            }

            combatConfig.SetRuntimeBaseEnemyStrength(GetInt(RemoteConfigKeys.BaseEnemyStrength, combatConfig.BaseEnemyStrength));
            combatConfig.SetRuntimePerWaveIncrease(GetInt(RemoteConfigKeys.PerWaveIncrease, combatConfig.PerWaveIncrease));
            combatConfig.SetRuntimeWinSoft(GetInt(RemoteConfigKeys.WinSoft, combatConfig.WinSoft));
            combatConfig.SetRuntimeWinResource(GetInt(RemoteConfigKeys.WinResource, combatConfig.WinResource));

            economyTables.SetRuntimeMergeSoftBase(GetInt(RemoteConfigKeys.MergeSoftBase, economyTables.MergeSoftBase));
            economyTables.SetRuntimeMergeTierMultiplier(GetInt(RemoteConfigKeys.MergeTierMultiplier, economyTables.MergeTierMultiplier));
            economyTables.SetRuntimeSpawnUpgradeBaseCost(GetInt(RemoteConfigKeys.SpawnUpgradeBaseCost, economyTables.SpawnUpgradeBaseCost));
            economyTables.SetRuntimeChanceUpgradeBaseCost(GetInt(RemoteConfigKeys.ChanceUpgradeBaseCost, economyTables.ChanceUpgradeBaseCost));
        }
    }
}
