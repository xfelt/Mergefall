using System.Collections.Generic;
using System.Linq;
using MergeSurvivor.Core;
using MergeSurvivor.Data;
using MergeSurvivor.Economy;
using MergeSurvivor.Meta;
using MergeSurvivor.Platform;

namespace MergeSurvivor.Gameplay
{
    public sealed class GameSession
    {
        private readonly BoardCatalog _boardCatalog;
        private readonly EnemyCatalog _enemyCatalog;
        private readonly ItemDatabase _items;
        private readonly MergeRulesConfig _mergeRules;
        private readonly SpawnConfig _spawnConfig;
        private readonly CombatConfig _combatConfig;
        private readonly EconomyTables _economy;
        private readonly IInventoryService _inventory;
        private readonly IProgressionService _progression;
        private readonly IAdsService _ads;
        private readonly IAnalyticsService _analytics;
        private readonly MergeResolver _resolver;
        private int _wave = 1;

        public GameSession(
            BoardState board,
            BoardCatalog boardCatalog,
            EnemyCatalog enemyCatalog,
            ItemDatabase items,
            MergeRulesConfig mergeRules,
            SpawnConfig spawnConfig,
            CombatConfig combatConfig,
            EconomyTables economy,
            IInventoryService inventory,
            IProgressionService progression,
            IAdsService ads,
            IAnalyticsService analytics)
        {
            Board = board;
            _boardCatalog = boardCatalog;
            _enemyCatalog = enemyCatalog;
            _items = items;
            _mergeRules = mergeRules;
            _spawnConfig = spawnConfig;
            _combatConfig = combatConfig;
            _economy = economy;
            _inventory = inventory;
            _progression = progression;
            _ads = ads;
            _analytics = analytics;
            _resolver = new MergeResolver(items);
            _progression.EnsureBoardState(_boardCatalog?.Count ?? 0);
        }

        public BoardState Board { get; }

        /// <summary>Reset run state for current session only: clear board and set wave to 1. Progression (boards, upgrades) is unchanged.</summary>
        public void ResetRun()
        {
            Board.Clear();
            _wave = 1;
        }

        public int CurrentWave => _wave;
        public int CurrentBoardIndex => _progression.Data.CurrentBoardIndex;
        public int UnlockedBoardCount => _progression.Data.UnlockedBoardCount;

        public BoardDefinition CurrentBoard
        {
            get
            {
                if (_boardCatalog == null || _boardCatalog.Count == 0)
                {
                    return null;
                }

                return _boardCatalog.Get(_progression.Data.CurrentBoardIndex);
            }
        }

        public EnemyArchetypeDefinition CurrentEnemyArchetype
        {
            get
            {
                var board = CurrentBoard;
                if (board == null || _enemyCatalog == null)
                {
                    return null;
                }

                return _enemyCatalog.GetById(board.EnemyArchetypeId);
            }
        }

        /// <summary>Select board by index. Only unlocked boards can be selected.</summary>
        public bool SelectBoard(int index)
        {
            var selected = _progression.TrySelectBoard(index);
            if (selected)
            {
                _progression.Save();
            }
            return selected;
        }

        public bool SelectNextUnlockedBoard()
        {
            if (UnlockedBoardCount <= 1)
            {
                return false;
            }

            var next = (CurrentBoardIndex + 1) % UnlockedBoardCount;
            return SelectBoard(next);
        }

        public bool TryUnlockNextBoard()
        {
            var unlocked = _progression.TryUnlockNextBoard(_boardCatalog?.Count ?? 0);
            if (!unlocked)
            {
                return false;
            }

            _progression.Save();
            return true;
        }

        public bool TrySpawn()
        {
            var boardSpawnBonus = CurrentBoard?.SpawnCapacityBonus ?? 0;
            var cap = _spawnConfig.BaseSpawnCapacity + _progression.SpawnCapacityBonus() + boardSpawnBonus;
            if (Board.OccupiedCount() >= cap) return false;
            if (!Board.TryGetFirstEmpty(out var cell)) return false;
            var tier1 = _items.GetTierOneItems();
            if (tier1.Count == 0) return false;
            var id = tier1[UnityEngine.Random.Range(0, tier1.Count)].Id;
            Board.Set(cell.x, cell.y, id);
            return true;
        }

        public MergeResult TryMergeAt(int x, int y)
        {
            var result = _resolver.TryMerge(Board, x, y, _mergeRules.MergeCount);
            if (result.Success)
            {
                var mergeMultiplier = CurrentBoard?.MergeRewardMultiplier ?? 1f;
                _inventory.Add(new GameReward
                {
                    SoftCurrency = UnityEngine.Mathf.RoundToInt((_economy.MergeSoftBase + result.UpgradedTier * _economy.MergeTierMultiplier) * mergeMultiplier)
                });
            }
            return result;
        }

        public int SquadPower()
        {
            var total = 0;
            for (var y = 0; y < Board.Height; y++)
            for (var x = 0; x < Board.Width; x++)
            {
                var id = Board.Get(x, y);
                if (string.IsNullOrEmpty(id)) continue;
                var item = _items.GetById(id);
                if (item != null) total += item.CombatPower;
            }
            return total;
        }

        public CombatResult Fight()
        {
            var boardMultiplier = CurrentBoard?.EnemyMultiplier ?? 1f;
            var archetype = CurrentEnemyArchetype;
            var baseEnemy = _combatConfig.BaseEnemyStrength + (_wave - 1) * _combatConfig.PerWaveIncrease;
            var archetypeBonus = (archetype?.FlatPowerBonus ?? 0) + (_wave - 1) * (archetype?.WavePowerBonusPerWave ?? 0);
            var enemy = UnityEngine.Mathf.RoundToInt(baseEnemy * boardMultiplier) + archetypeBonus;
            var player = SquadPower();
            var (effectivePlayer, effectiveEnemy) = CombatCalculator.ApplyEnemyModifiers(player, enemy, archetype?.Modifiers, _wave);
            TrackEnemyModifiers(archetype, player, enemy, effectivePlayer, effectiveEnemy);
            var result = CombatCalculator.Resolve(effectivePlayer, effectiveEnemy);
            if (result.Won)
            {
                _wave++;
                if (_wave > _progression.Data.HighestWave) _progression.Data.HighestWave = _wave;
                _inventory.Add(new GameReward
                {
                    SoftCurrency = _combatConfig.WinSoft,
                    ProgressionResource = _combatConfig.WinResource
                });
                _analytics.TrackEvent("fight_win", new Dictionary<string, object> { { "wave", _wave } });
            }
            else
            {
                // TODO: revive via rewarded ad or premium.
                _ads.ShowRewarded("revive_hook", _ => { });
                _analytics.TrackEvent("fight_loss");
            }
            _progression.Save();
            return result;
        }

        private void TrackEnemyModifiers(
            EnemyArchetypeDefinition archetype,
            int playerBefore,
            int enemyBefore,
            int playerAfter,
            int enemyAfter)
        {
            if (archetype?.Modifiers == null)
            {
                return;
            }

            var appliedGroups = new HashSet<string>();
            var sorted = archetype.Modifiers
                .Where(x => x != null)
                .Select((modifier, index) => (modifier, index))
                .OrderBy(x => x.modifier.Order)
                .ThenBy(x => x.index);

            foreach (var entry in sorted)
            {
                var modifier = entry.modifier;
                if (modifier == null || modifier.ModifierType == EnemyModifierType.None)
                {
                    continue;
                }

                var stackGroup = string.IsNullOrWhiteSpace(modifier.StackGroupId)
                    ? modifier.ModifierType.ToString()
                    : modifier.StackGroupId;
                if (!modifier.AllowStacking && appliedGroups.Contains(stackGroup))
                {
                    continue;
                }

                _analytics.TrackEvent("fight_enemy_modifier_applied", new Dictionary<string, object>
                {
                    { "board_id", CurrentBoard?.Id ?? "unknown" },
                    { "archetype_id", archetype.Id ?? "unknown" },
                    { "modifier", modifier.ModifierType.ToString() },
                    { "wave", _wave },
                    { "player_before", playerBefore },
                    { "enemy_before", enemyBefore },
                    { "player_after", playerAfter },
                    { "enemy_after", enemyAfter }
                });

                if (!modifier.AllowStacking)
                {
                    appliedGroups.Add(stackGroup);
                }
            }
        }
    }
}
