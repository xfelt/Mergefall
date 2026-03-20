using UnityEngine;

namespace MergeSurvivor.Data
{
    /// <summary>
    /// Loads game content from Resources (ScriptableObjects). If an asset is missing,
    /// returns null so bootstrap can fall back to runtime-created defaults.
    /// Place default assets under Resources/MergeSurvivorData/ (see Editor content pipeline).
    /// </summary>
    public static class GameContentLoader
    {
        private const string ResourceFolder = "MergeSurvivorData";

        public static BoardCatalog LoadBoardCatalog()
        {
            return Resources.Load<BoardCatalog>($"{ResourceFolder}/BoardCatalog");
        }

        public static EnemyCatalog LoadEnemyCatalog()
        {
            return Resources.Load<EnemyCatalog>($"{ResourceFolder}/EnemyCatalog");
        }

        public static ItemDatabase LoadItemDatabase()
        {
            return Resources.Load<ItemDatabase>($"{ResourceFolder}/ItemDatabase");
        }

        public static EconomyTables LoadEconomyTables()
        {
            return Resources.Load<EconomyTables>($"{ResourceFolder}/EconomyTables");
        }

        public static MergeRulesConfig LoadMergeRulesConfig()
        {
            return Resources.Load<MergeRulesConfig>($"{ResourceFolder}/MergeRulesConfig");
        }

        public static SpawnConfig LoadSpawnConfig()
        {
            return Resources.Load<SpawnConfig>($"{ResourceFolder}/SpawnConfig");
        }

        public static CombatConfig LoadCombatConfig()
        {
            return Resources.Load<CombatConfig>($"{ResourceFolder}/CombatConfig");
        }

        /// <summary>Optional. If present, BalanceRemoteConfigApplier uses these values to simulate Remote Config (e.g. for testing without Firebase).</summary>
        public static RemoteConfigSimulation LoadRemoteConfigSimulation()
        {
            return Resources.Load<RemoteConfigSimulation>($"{ResourceFolder}/RemoteConfigSimulation");
        }
    }
}
