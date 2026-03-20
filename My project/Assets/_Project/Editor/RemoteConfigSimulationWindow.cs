using System.Collections.Generic;
using MergeSurvivor.Data;
using UnityEditor;
using UnityEngine;

namespace MergeSurvivor.Editor
{
    /// <summary>Editor window to set simulated Remote Config values for testing. Values are stored in RemoteConfigSimulation in Resources; at runtime they override real Remote Config when the asset is present.</summary>
    public sealed class RemoteConfigSimulationWindow : EditorWindow
    {
        private const string DataRoot = "Assets/_Project/Resources/MergeSurvivorData";
        private const string SimulationAssetName = "RemoteConfigSimulation";

        private RemoteConfigSimulation _asset;
        private readonly Dictionary<string, int> _values = new Dictionary<string, int>();
        private Vector2 _scroll;
        private bool _dirty;

        private static readonly string[] Keys =
        {
            RemoteConfigKeys.BaseEnemyStrength,
            RemoteConfigKeys.PerWaveIncrease,
            RemoteConfigKeys.WinSoft,
            RemoteConfigKeys.WinResource,
            RemoteConfigKeys.MergeSoftBase,
            RemoteConfigKeys.MergeTierMultiplier,
            RemoteConfigKeys.SpawnUpgradeBaseCost,
            RemoteConfigKeys.ChanceUpgradeBaseCost
        };

        [MenuItem("Merge Survivor/Remote Config/Simulate Remote Config...")]
        public static void Open()
        {
            var w = GetWindow<RemoteConfigSimulationWindow>("Simulate Remote Config");
            w.LoadAsset();
        }

        private void LoadAsset()
        {
            EnsureFolder();
            var path = $"{DataRoot}/{SimulationAssetName}.asset";
            _asset = AssetDatabase.LoadAssetAtPath<RemoteConfigSimulation>(path);
            if (_asset == null)
            {
                _asset = CreateInstance<RemoteConfigSimulation>();
                AssetDatabase.CreateAsset(_asset, path);
                AssetDatabase.SaveAssets();
            }

            _values.Clear();
            foreach (var e in _asset.Entries)
            {
                if (e != null && !string.IsNullOrEmpty(e.Key))
                    _values[e.Key] = e.Value;
            }
            foreach (var key in Keys)
            {
                if (!_values.ContainsKey(key))
                    _values[key] = 0;
            }
            _dirty = false;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                AssetDatabase.CreateFolder("Assets", "_Project");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            if (!AssetDatabase.IsValidFolder(DataRoot))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "MergeSurvivorData");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.HelpBox(
                "These values override Firebase Remote Config at runtime when RemoteConfigSimulation is in Resources/MergeSurvivorData. Use for testing without Firebase. Save stores all keys; Clear removes overrides so real Remote Config is used.",
                MessageType.Info);

            if (_asset == null)
            {
                if (GUILayout.Button("Load / Create Simulation Asset"))
                    LoadAsset();
                EditorGUILayout.EndScrollView();
                return;
            }

            foreach (var key in Keys)
            {
                if (!_values.ContainsKey(key)) _values[key] = 0;
                var prev = _values[key];
                var next = EditorGUILayout.IntField(KeyToLabel(key), prev);
                if (next != prev)
                {
                    _values[key] = next;
                    _dirty = true;
                }
            }

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Reset to local defaults (from CombatConfig & EconomyTables)"))
            {
                ResetToLocalDefaults();
                _dirty = true;
            }

            if (GUILayout.Button("Clear simulation (use real Remote Config only)"))
            {
                foreach (var key in Keys)
                    _values[key] = 0;
                _asset.SetOverrides(new List<RemoteConfigEntry>());
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssets();
                _dirty = false;
            }

            if (_dirty && GUILayout.Button("Save"))
            {
                Save();
            }

            EditorGUILayout.EndScrollView();
        }

        private void Save()
        {
            var list = new List<RemoteConfigEntry>();
            foreach (var key in Keys)
            {
                if (_values.TryGetValue(key, out var val))
                    list.Add(new RemoteConfigEntry { Key = key, Value = val });
            }
            _asset.SetOverrides(list);
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            _dirty = false;
        }

        private void ResetToLocalDefaults()
        {
            var combat = AssetDatabase.LoadAssetAtPath<CombatConfig>($"{DataRoot}/CombatConfig.asset");
            var economy = AssetDatabase.LoadAssetAtPath<EconomyTables>($"{DataRoot}/EconomyTables.asset");
            if (combat != null)
            {
                _values[RemoteConfigKeys.BaseEnemyStrength] = combat.BaseEnemyStrength;
                _values[RemoteConfigKeys.PerWaveIncrease] = combat.PerWaveIncrease;
                _values[RemoteConfigKeys.WinSoft] = combat.WinSoft;
                _values[RemoteConfigKeys.WinResource] = combat.WinResource;
            }
            if (economy != null)
            {
                _values[RemoteConfigKeys.MergeSoftBase] = economy.MergeSoftBase;
                _values[RemoteConfigKeys.MergeTierMultiplier] = economy.MergeTierMultiplier;
                _values[RemoteConfigKeys.SpawnUpgradeBaseCost] = economy.SpawnUpgradeBaseCost;
                _values[RemoteConfigKeys.ChanceUpgradeBaseCost] = economy.ChanceUpgradeBaseCost;
            }
        }

        private static string KeyToLabel(string key)
        {
            if (key == null) return "";
            return key.Replace("balance_", "").Replace("economy_", "").Replace("_", " ");
        }
    }
}
