using UnityEngine;
using System.Collections.Generic;

namespace MergeSurvivor.Data
{
    /// <summary>Firebase Remote Config keys for balance tuning. Create these in Firebase Console and set default values in code/Resources.</summary>
    public static class RemoteConfigKeys
    {
        public const string BaseEnemyStrength = "balance_base_enemy_strength";
        public const string PerWaveIncrease = "balance_per_wave_increase";
        public const string WinSoft = "balance_win_soft";
        public const string WinResource = "balance_win_resource";
        public const string MergeSoftBase = "economy_merge_soft_base";
        public const string MergeTierMultiplier = "economy_merge_tier_multiplier";
        public const string SpawnUpgradeBaseCost = "economy_spawn_upgrade_base_cost";
        public const string ChanceUpgradeBaseCost = "economy_chance_upgrade_base_cost";
    }
    public enum EnemyModifierType
    {
        None = 0,
        ArmorPercent = 1,
        RagePercentPerWave = 2,
        HealFlat = 3
    }

    [System.Serializable]
    public sealed class EnemyModifierDefinition
    {
        public int Order;
        public EnemyModifierType ModifierType = EnemyModifierType.None;
        public float ModifierValue;
        public bool AllowStacking = true;
        public string StackGroupId = string.Empty;
    }

    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Merge Rules", fileName = "MergeRulesConfig")]
    public sealed class MergeRulesConfig : ScriptableObject
    {
        [SerializeField] private bool allowTwoItemMergeFallback;
        [SerializeField, Min(2)] private int requiredCount = 3;
        public int MergeCount => allowTwoItemMergeFallback ? 2 : requiredCount;
    }

    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Spawn", fileName = "SpawnConfig")]
    public sealed class SpawnConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int baseSpawnCapacity = 16;
        public int BaseSpawnCapacity => baseSpawnCapacity;
    }

    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Combat", fileName = "CombatConfig")]
    public sealed class CombatConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int baseEnemyStrength = 30;
        [SerializeField, Min(1)] private int perWaveIncrease = 10;
        [SerializeField, Min(1)] private int winSoft = 30;
        [SerializeField, Min(0)] private int winResource = 1;
        private int? _runtimeBaseEnemyStrength;
        private int? _runtimePerWaveIncrease;
        private int? _runtimeWinSoft;
        private int? _runtimeWinResource;
        public int BaseEnemyStrength => _runtimeBaseEnemyStrength ?? baseEnemyStrength;
        public int PerWaveIncrease => _runtimePerWaveIncrease ?? perWaveIncrease;
        public int WinSoft => _runtimeWinSoft ?? winSoft;
        public int WinResource => _runtimeWinResource ?? winResource;
        public void SetRuntimeBaseEnemyStrength(int value) { _runtimeBaseEnemyStrength = value; }
        public void SetRuntimePerWaveIncrease(int value) { _runtimePerWaveIncrease = value; }
        public void SetRuntimeWinSoft(int value) { _runtimeWinSoft = value; }
        public void SetRuntimeWinResource(int value) { _runtimeWinResource = value; }
    }

    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Economy", fileName = "EconomyTables")]
    public sealed class EconomyTables : ScriptableObject
    {
        [SerializeField, Min(1)] private int mergeSoftBase = 5;
        [SerializeField, Min(1)] private int mergeTierMultiplier = 5;
        [SerializeField, Min(1)] private int spawnUpgradeBaseCost = 100;
        [SerializeField, Min(1)] private int chanceUpgradeBaseCost = 120;
        private int? _runtimeMergeSoftBase;
        private int? _runtimeMergeTierMultiplier;
        private int? _runtimeSpawnUpgradeBaseCost;
        private int? _runtimeChanceUpgradeBaseCost;
        public int MergeSoftBase => _runtimeMergeSoftBase ?? mergeSoftBase;
        public int MergeTierMultiplier => _runtimeMergeTierMultiplier ?? mergeTierMultiplier;
        public int SpawnUpgradeBaseCost => _runtimeSpawnUpgradeBaseCost ?? spawnUpgradeBaseCost;
        public int ChanceUpgradeBaseCost => _runtimeChanceUpgradeBaseCost ?? chanceUpgradeBaseCost;
        public void SetRuntimeMergeSoftBase(int value) { _runtimeMergeSoftBase = value; }
        public void SetRuntimeMergeTierMultiplier(int value) { _runtimeMergeTierMultiplier = value; }
        public void SetRuntimeSpawnUpgradeBaseCost(int value) { _runtimeSpawnUpgradeBaseCost = value; }
        public void SetRuntimeChanceUpgradeBaseCost(int value) { _runtimeChanceUpgradeBaseCost = value; }
    }

    [System.Serializable]
    public sealed class BoardDefinition
    {
        public string Id;
        public string DisplayName;
        /// <summary>Short label for board select (e.g. "Easy", "Defense", "Rage").</summary>
        public string DifficultyLabel = "Normal";
        public float EnemyMultiplier = 1f;
        public int UnlockCostResource = 3;
        /// <summary>Alternative unlock: reach this wave to unlock without spending resource. 0 = resource-only.</summary>
        public int UnlockObjectiveWave;
        public int SpawnCapacityBonus;
        public float MergeRewardMultiplier = 1f;
        public string EnemyArchetypeId = "grunt";
    }

    [System.Serializable]
    public sealed class EnemyArchetypeDefinition
    {
        public string Id;
        public string DisplayName;
        public int FlatPowerBonus;
        public int WavePowerBonusPerWave;
        public List<EnemyModifierDefinition> Modifiers = new();
    }

    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Board Catalog", fileName = "BoardCatalog")]
    public sealed class BoardCatalog : ScriptableObject
    {
        [SerializeField] private List<BoardDefinition> boards = new();

        public int Count => boards.Count;

        public BoardDefinition Get(int index)
        {
            if (boards.Count == 0)
            {
                return null;
            }

            if (index < 0)
            {
                index = 0;
            }
            else if (index >= boards.Count)
            {
                index = boards.Count - 1;
            }

            return boards[index];
        }

        public void ConfigureRuntime(List<BoardDefinition> runtimeBoards)
        {
            boards = runtimeBoards ?? new List<BoardDefinition>();
        }
    }

    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Enemy Catalog", fileName = "EnemyCatalog")]
    public sealed class EnemyCatalog : ScriptableObject
    {
        [SerializeField] private List<EnemyArchetypeDefinition> archetypes = new();

        public EnemyArchetypeDefinition GetById(string id)
        {
            for (var i = 0; i < archetypes.Count; i++)
            {
                var candidate = archetypes[i];
                if (candidate != null && candidate.Id == id)
                {
                    return candidate;
                }
            }

            return null;
        }

        public void ConfigureRuntime(List<EnemyArchetypeDefinition> runtimeArchetypes)
        {
            archetypes = runtimeArchetypes ?? new List<EnemyArchetypeDefinition>();
        }
    }
}
