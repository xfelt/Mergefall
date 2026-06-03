using MergeSurvivor.Core;
using MergeSurvivor.Data;
using UnityEngine;

namespace MergeSurvivor.Meta
{
    [System.Serializable]
    public sealed class PrestigeData
    {
        public int Level;
        public int Points;
    }

    public interface IPrestigeService
    {
        PrestigeData Data { get; }
        int UnlockWave { get; }
        bool CanPrestige(int highestWave);
        int PreviewGain(int highestWave);
        /// <summary>Permanent multiplier (>= 1) applied to squad power and gold gains.</summary>
        float Multiplier();
        /// <summary>Commit a prestige at the given highest wave. Returns the points gained (0 if not eligible).</summary>
        int Prestige(int highestWave);
        void Save();
    }

    public sealed class PrestigeService : IPrestigeService
    {
        private const string Key = "merge_survivor_prestige_v1";
        private readonly ISaveService _save;
        private readonly PrestigeConfig _config;

        public PrestigeData Data { get; }

        public PrestigeService(ISaveService save, PrestigeConfig config)
        {
            _save = save;
            _config = config;
            _save.TryLoad(Key, out PrestigeData data);
            Data = data ?? new PrestigeData();
        }

        public int UnlockWave => _config != null ? _config.UnlockWave : int.MaxValue;

        public bool CanPrestige(int highestWave)
            => _config != null && highestWave >= _config.UnlockWave && _config.PointsForWave(highestWave) > 0;

        public int PreviewGain(int highestWave)
            => _config != null ? _config.PointsForWave(highestWave) : 0;

        public float Multiplier()
        {
            if (_config == null) return 1f;
            return Mathf.Min(_config.MaxMultiplier, 1f + Data.Points * _config.MultiplierPerPoint);
        }

        public int Prestige(int highestWave)
        {
            if (!CanPrestige(highestWave)) return 0;
            var gained = _config.PointsForWave(highestWave);
            Data.Points += gained;
            Data.Level++;
            Save();
            return gained;
        }

        public void Save() => _save.Save(Key, Data);
    }
}
