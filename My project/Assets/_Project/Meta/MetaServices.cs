using MergeSurvivor.Core;
using MergeSurvivor.Data;
using UnityEngine;

namespace MergeSurvivor.Meta
{
    [System.Serializable]
    public sealed class ProgressionData
    {
        public int HighestWave = 1;
        public int SpawnCapacityUpgrade;
        public int StartingChanceUpgrade;
        public int CurrentBoardIndex;
        public int UnlockedBoardCount = 1;
    }

    public interface IProgressionService
    {
        ProgressionData Data { get; }
        int SpawnCapacityBonus();
        int SpawnUpgradeCost();
        int ChanceUpgradeCost();
        void UpgradeSpawnCapacity();
        void UpgradeStartingChance();
        void EnsureBoardState(int boardCount);
        bool TrySelectBoard(int index);
        bool TryUnlockNextBoard(int boardCount);
        void Save();
    }

    public sealed class LocalSaveService : ISaveService
    {
        public void Save<T>(string key, T data)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public bool TryLoad<T>(string key, out T data) where T : new()
        {
            if (!PlayerPrefs.HasKey(key))
            {
                data = new T();
                return false;
            }

            data = JsonUtility.FromJson<T>(PlayerPrefs.GetString(key));
            data ??= new T();
            return true;
        }
    }

    public sealed class ProgressionService : IProgressionService
    {
        private const string Key = "merge_survivor_progression_v1";
        private readonly ISaveService _save;
        private readonly EconomyTables _economy;
        public ProgressionData Data { get; }

        public ProgressionService(ISaveService save, EconomyTables economy)
        {
            _save = save;
            _economy = economy;
            _save.TryLoad(Key, out ProgressionData data);
            Data = data ?? new ProgressionData();
        }

        public int SpawnCapacityBonus() => Data.SpawnCapacityUpgrade * 2;
        public int SpawnUpgradeCost() => _economy.SpawnUpgradeBaseCost * (Data.SpawnCapacityUpgrade + 1);
        public int ChanceUpgradeCost() => _economy.ChanceUpgradeBaseCost * (Data.StartingChanceUpgrade + 1);
        public void UpgradeSpawnCapacity() => Data.SpawnCapacityUpgrade++;
        public void UpgradeStartingChance() => Data.StartingChanceUpgrade++;

        public void EnsureBoardState(int boardCount)
        {
            if (boardCount <= 0)
            {
                Data.UnlockedBoardCount = 0;
                Data.CurrentBoardIndex = 0;
                return;
            }

            if (Data.UnlockedBoardCount <= 0)
            {
                Data.UnlockedBoardCount = 1;
            }

            if (Data.UnlockedBoardCount > boardCount)
            {
                Data.UnlockedBoardCount = boardCount;
            }

            if (Data.CurrentBoardIndex < 0)
            {
                Data.CurrentBoardIndex = 0;
            }
            else if (Data.CurrentBoardIndex >= Data.UnlockedBoardCount)
            {
                Data.CurrentBoardIndex = Data.UnlockedBoardCount - 1;
            }
        }

        public bool TrySelectBoard(int index)
        {
            if (index < 0 || index >= Data.UnlockedBoardCount)
            {
                return false;
            }

            Data.CurrentBoardIndex = index;
            return true;
        }

        public bool TryUnlockNextBoard(int boardCount)
        {
            EnsureBoardState(boardCount);
            if (Data.UnlockedBoardCount >= boardCount)
            {
                return false;
            }

            Data.UnlockedBoardCount++;
            return true;
        }

        public void Save() => _save.Save(Key, Data);
    }
}
