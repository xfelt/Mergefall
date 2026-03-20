using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeSurvivor.Data
{
    [Serializable]
    public sealed class RemoteConfigEntry
    {
        public string Key = string.Empty;
        public int Value;
    }

    /// <summary>Optional simulation overrides for Remote Config. Place in Resources/MergeSurvivorData/RemoteConfigSimulation to test balance without Firebase. Editor menu: Merge Survivor > Remote Config > Simulate Values.</summary>
    [CreateAssetMenu(menuName = "Merge Survivor/Configs/Remote Config Simulation", fileName = "RemoteConfigSimulation")]
    public sealed class RemoteConfigSimulation : ScriptableObject
    {
        [SerializeField] private List<RemoteConfigEntry> _entries = new();

        public IReadOnlyList<RemoteConfigEntry> Entries => _entries;

        public void SetOverrides(List<RemoteConfigEntry> entries)
        {
            _entries = entries ?? new List<RemoteConfigEntry>();
        }

        /// <summary>Build a dictionary of key -> value for keys that are non-empty. Simulation overrides take precedence over real Remote Config when applied.</summary>
        public Dictionary<string, int> ToOverrideDictionary()
        {
            var dict = new Dictionary<string, int>(StringComparer.Ordinal);
            if (_entries == null) return dict;
            for (var i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.Key)) continue;
                dict[e.Key.Trim()] = e.Value;
            }
            return dict;
        }
    }
}
