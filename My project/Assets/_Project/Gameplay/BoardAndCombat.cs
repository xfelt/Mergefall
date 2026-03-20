using MergeSurvivor.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MergeSurvivor.Gameplay
{
    public sealed class BoardState
    {
        private readonly string[,] _cells;
        public int Width { get; }
        public int Height { get; }

        public BoardState(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new string[width, height];
        }

        public string Get(int x, int y) => _cells[x, y];
        public void Set(int x, int y, string itemId) => _cells[x, y] = itemId;
        public bool IsEmpty(int x, int y) => string.IsNullOrEmpty(_cells[x, y]);

        public void Swap((int x, int y) a, (int x, int y) b)
        {
            (_cells[a.x, a.y], _cells[b.x, b.y]) = (_cells[b.x, b.y], _cells[a.x, a.y]);
        }

        public bool TryGetFirstEmpty(out (int x, int y) cell)
        {
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (IsEmpty(x, y))
                {
                    cell = (x, y);
                    return true;
                }
            cell = (-1, -1);
            return false;
        }

        public int OccupiedCount()
        {
            var c = 0;
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (!IsEmpty(x, y)) c++;
            return c;
        }

        /// <summary>Clear all cells (session-only run reset).</summary>
        public void Clear()
        {
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                _cells[x, y] = null;
        }
    }

    public readonly struct MergeResult
    {
        public MergeResult(bool success, string upgradedId, int upgradedTier)
        {
            Success = success;
            UpgradedId = upgradedId;
            UpgradedTier = upgradedTier;
        }
        public bool Success { get; }
        public string UpgradedId { get; }
        public int UpgradedTier { get; }
    }

    public sealed class MergeResolver
    {
        private readonly ItemDatabase _items;
        public MergeResolver(ItemDatabase items) => _items = items;

        public MergeResult TryMerge(BoardState board, int tx, int ty, int mergeCount)
        {
            var id = board.Get(tx, ty);
            if (string.IsNullOrEmpty(id)) return default;
            var def = _items.GetById(id);
            var next = _items.GetNextTier(def);
            if (next == null) return default;

            var found = 0;
            for (var y = 0; y < board.Height; y++)
            for (var x = 0; x < board.Width; x++)
                if (board.Get(x, y) == id) found++;
            if (found < mergeCount) return default;

            var consumed = 0;
            for (var y = 0; y < board.Height && consumed < mergeCount; y++)
            for (var x = 0; x < board.Width && consumed < mergeCount; x++)
                if (board.Get(x, y) == id)
                {
                    board.Set(x, y, null);
                    consumed++;
                }
            board.Set(tx, ty, next.Id);
            return new MergeResult(true, next.Id, next.Tier);
        }
    }

    public readonly struct CombatResult
    {
        public CombatResult(bool won, int playerPower, int enemyPower)
        {
            Won = won;
            PlayerPower = playerPower;
            EnemyPower = enemyPower;
        }
        public bool Won { get; }
        public int PlayerPower { get; }
        public int EnemyPower { get; }
    }

    public static class CombatCalculator
    {
        public static CombatResult Resolve(int playerPower, int enemyPower)
            => new(playerPower >= enemyPower, playerPower, enemyPower);

        public static (int effectivePlayer, int effectiveEnemy) ApplyEnemyModifier(
            int playerPower,
            int enemyPower,
            EnemyModifierType modifierType,
            float modifierValue,
            int wave)
        {
            var effectivePlayer = playerPower;
            var effectiveEnemy = enemyPower;

            switch (modifierType)
            {
                case EnemyModifierType.ArmorPercent:
                {
                    var clamped = Mathf.Clamp01(modifierValue);
                    effectivePlayer = Mathf.Max(0, Mathf.RoundToInt(playerPower * (1f - clamped)));
                    break;
                }
                case EnemyModifierType.RagePercentPerWave:
                {
                    var scale = Mathf.Max(0f, modifierValue);
                    var rageBonus = Mathf.RoundToInt(enemyPower * scale * Mathf.Max(0, wave - 1));
                    effectiveEnemy += rageBonus;
                    break;
                }
                case EnemyModifierType.HealFlat:
                {
                    effectiveEnemy += Mathf.Max(0, Mathf.RoundToInt(modifierValue));
                    break;
                }
            }

            return (effectivePlayer, effectiveEnemy);
        }

        public static (int effectivePlayer, int effectiveEnemy) ApplyEnemyModifiers(
            int playerPower,
            int enemyPower,
            IReadOnlyList<EnemyModifierDefinition> modifiers,
            int wave)
        {
            var effectivePlayer = playerPower;
            var effectiveEnemy = enemyPower;
            if (modifiers == null)
            {
                return (effectivePlayer, effectiveEnemy);
            }

            var sorted = modifiers
                .Where(m => m != null)
                .Select((modifier, index) => (modifier, index))
                .OrderBy(x => x.modifier.Order)
                .ThenBy(x => x.index);

            var appliedGroups = new HashSet<string>();
            foreach (var entry in sorted)
            {
                var modifier = entry.modifier;
                if (modifier.ModifierType == EnemyModifierType.None)
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

                (effectivePlayer, effectiveEnemy) = ApplyEnemyModifier(
                    effectivePlayer,
                    effectiveEnemy,
                    modifier.ModifierType,
                    modifier.ModifierValue,
                    wave);

                if (!modifier.AllowStacking)
                {
                    appliedGroups.Add(stackGroup);
                }
            }

            return (effectivePlayer, effectiveEnemy);
        }
    }
}
