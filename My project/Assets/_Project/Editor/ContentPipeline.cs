using System.Collections.Generic;
using MergeSurvivor.Data;
using UnityEditor;
using UnityEngine;

namespace MergeSurvivor.Editor
{
    /// <summary>
    /// Content pipeline: create and validate game data assets (boards, enemies, items, economy).
    /// Data lives in Resources/MergeSurvivorData so it is loaded at runtime via GameContentLoader.
    /// </summary>
    public static class ContentPipeline
    {
        private const string DataRoot = "Assets/_Project/Resources/MergeSurvivorData";

        [MenuItem("Merge Survivor/Content/Create Default Data Assets")]
        public static void CreateDefaultDataAssets()
        {
            EnsureFolder("Assets/_Project/Resources");
            EnsureFolder(DataRoot);

            CreateOrUpdateEconomyTables();
            CreateOrUpdateCombatConfig();
            CreateOrUpdateMergeRulesConfig();
            CreateOrUpdateSpawnConfig();
            CreateOrUpdateRemoteConfigSimulation();
            CreateOrUpdateEnemyCatalog();
            CreateOrUpdateBoardCatalog();
            CreateOrUpdateItemDatabase();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Default data assets created/updated under {DataRoot}. Runtime loads via Resources.Load(\"MergeSurvivorData/...\").");
        }

        [MenuItem("Merge Survivor/Content/Add New Board")]
        public static void AddNewBoard()
        {
            var catalog = LoadOrCreateAsset<BoardCatalog>("BoardCatalog");
            if (catalog == null) return;
            var so = new SerializedObject(catalog);
            var boardsProp = so.FindProperty("boards");
            if (boardsProp == null) { Debug.LogError("BoardCatalog.boards not found."); return; }
            var index = boardsProp.arraySize;
            boardsProp.InsertArrayElementAtIndex(index);
            var element = boardsProp.GetArrayElementAtIndex(index);
            var nextId = $"board_new_{index}";
            element.FindPropertyRelative("Id").stringValue = nextId;
            element.FindPropertyRelative("DisplayName").stringValue = "New Board";
            element.FindPropertyRelative("DifficultyLabel").stringValue = "Normal";
            element.FindPropertyRelative("EnemyMultiplier").floatValue = 1.2f;
            element.FindPropertyRelative("UnlockCostResource").intValue = 3;
            element.FindPropertyRelative("UnlockObjectiveWave").intValue = 0;
            element.FindPropertyRelative("SpawnCapacityBonus").intValue = 0;
            element.FindPropertyRelative("MergeRewardMultiplier").floatValue = 1f;
            element.FindPropertyRelative("EnemyArchetypeId").stringValue = "grunt";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Added board '{nextId}'. Assign DisplayName and EnemyArchetypeId in Inspector, then run Validate Content IDs.");
        }

        [MenuItem("Merge Survivor/Content/Add New Enemy Archetype")]
        public static void AddNewArchetype()
        {
            var catalog = LoadOrCreateAsset<EnemyCatalog>("EnemyCatalog");
            if (catalog == null) return;
            var so = new SerializedObject(catalog);
            var archetypesProp = so.FindProperty("archetypes");
            if (archetypesProp == null) { Debug.LogError("EnemyCatalog.archetypes not found."); return; }
            var index = archetypesProp.arraySize;
            archetypesProp.InsertArrayElementAtIndex(index);
            var element = archetypesProp.GetArrayElementAtIndex(index);
            var nextId = $"archetype_new_{index}";
            element.FindPropertyRelative("Id").stringValue = nextId;
            element.FindPropertyRelative("DisplayName").stringValue = "New Archetype";
            element.FindPropertyRelative("FlatPowerBonus").intValue = 0;
            element.FindPropertyRelative("WavePowerBonusPerWave").intValue = 0;
            var mods = element.FindPropertyRelative("Modifiers");
            if (mods != null) mods.ClearArray();
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Added archetype '{nextId}'. Edit in Inspector, then run Validate Content IDs.");
        }

        [MenuItem("Merge Survivor/Content/Add New Item")]
        public static void AddNewItem()
        {
            var db = LoadOrCreateAsset<ItemDatabase>("ItemDatabase");
            if (db == null) return;
            var itemId = $"item_new_{System.Guid.NewGuid().ToString("N").Substring(0, 6)}";
            var item = CreateItemAsset(itemId, "New Item", "pawn", 1, 5);
            var so = new SerializedObject(db);
            var itemsProp = so.FindProperty("items");
            if (itemsProp == null) { Debug.LogError("ItemDatabase.items not found."); return; }
            itemsProp.InsertArrayElementAtIndex(itemsProp.arraySize);
            itemsProp.GetArrayElementAtIndex(itemsProp.arraySize - 1).objectReferenceValue = item;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"Added item '{itemId}'. Set FamilyId/Tier/CombatPower in Inspector, then run Validate Content IDs.");
        }

        [MenuItem("Merge Survivor/Content/Validate Content IDs")]
        public static void ValidateContentIds()
        {
            var catalogBoard = LoadAssetAtPath<BoardCatalog>($"{DataRoot}/BoardCatalog.asset");
            var catalogEnemy = LoadAssetAtPath<EnemyCatalog>($"{DataRoot}/EnemyCatalog.asset");
            var db = LoadAssetAtPath<ItemDatabase>($"{DataRoot}/ItemDatabase.asset");

            var errors = new List<string>();
            var seenBoardIds = new HashSet<string>();
            var seenArchetypeIds = new HashSet<string>();
            var seenItemIds = new HashSet<string>();

            if (catalogBoard != null)
            {
                var so = new SerializedObject(catalogBoard);
                var boardsProp = so.FindProperty("boards");
                if (boardsProp != null)
                {
                    for (var i = 0; i < boardsProp.arraySize; i++)
                    {
                        var el = boardsProp.GetArrayElementAtIndex(i);
                        var id = el.FindPropertyRelative("Id").stringValue;
                        var archetypeId = el.FindPropertyRelative("EnemyArchetypeId").stringValue;
                        if (string.IsNullOrWhiteSpace(id)) errors.Add($"Board at index {i}: Id is empty.");
                        else if (!seenBoardIds.Add(id)) errors.Add($"Board duplicate Id: '{id}'.");
                        if (!string.IsNullOrWhiteSpace(archetypeId) && catalogEnemy != null)
                        {
                            if (catalogEnemy.GetById(archetypeId) == null)
                                errors.Add($"Board '{id}' references missing EnemyArchetypeId '{archetypeId}'.");
                        }
                    }
                }
            }

            if (catalogEnemy != null)
            {
                var so = new SerializedObject(catalogEnemy);
                var archProp = so.FindProperty("archetypes");
                if (archProp != null)
                {
                    for (var i = 0; i < archProp.arraySize; i++)
                    {
                        var el = archProp.GetArrayElementAtIndex(i);
                        var id = el.FindPropertyRelative("Id").stringValue;
                        if (string.IsNullOrWhiteSpace(id)) errors.Add($"Enemy archetype at index {i}: Id is empty.");
                        else if (!seenArchetypeIds.Add(id)) errors.Add($"Enemy archetype duplicate Id: '{id}'.");
                    }
                }
            }

            if (db != null)
            {
                var so = new SerializedObject(db);
                var itemsProp = so.FindProperty("items");
                if (itemsProp != null)
                {
                    for (var i = 0; i < itemsProp.arraySize; i++)
                    {
                        var refVal = itemsProp.GetArrayElementAtIndex(i).objectReferenceValue as ItemDefinition;
                        if (refVal == null) continue;
                        var id = refVal.Id;
                        if (string.IsNullOrWhiteSpace(id)) errors.Add($"Item at index {i}: Id is empty.");
                        else if (!seenItemIds.Add(id)) errors.Add($"Item duplicate Id: '{id}'.");
                    }
                }
            }

            if (errors.Count == 0)
            {
                Debug.Log("Validate Content IDs: No issues found.");
                return;
            }
            foreach (var e in errors) Debug.LogWarning($"[Content] {e}");
        }

        private static void CreateOrUpdateEconomyTables()
        {
            var asset = LoadOrCreateAsset<EconomyTables>("EconomyTables");
            if (asset == null) return;
            var so = new SerializedObject(asset);
            SetInt(so, "mergeSoftBase", 5);
            SetInt(so, "mergeTierMultiplier", 5);
            SetInt(so, "spawnUpgradeBaseCost", 50);
            SetInt(so, "chanceUpgradeBaseCost", 120);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private static void CreateOrUpdateCombatConfig()
        {
            var asset = LoadOrCreateAsset<CombatConfig>("CombatConfig");
            if (asset == null) return;
            var so = new SerializedObject(asset);
            SetInt(so, "baseEnemyStrength", 28);
            SetInt(so, "perWaveIncrease", 10);
            SetInt(so, "winSoft", 30);
            SetInt(so, "winResource", 1);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private static void CreateOrUpdateMergeRulesConfig()
        {
            LoadOrCreateAsset<MergeRulesConfig>("MergeRulesConfig");
        }

        private static void CreateOrUpdateSpawnConfig()
        {
            LoadOrCreateAsset<SpawnConfig>("SpawnConfig");
        }

        private static void CreateOrUpdateRemoteConfigSimulation()
        {
            LoadOrCreateAsset<RemoteConfigSimulation>("RemoteConfigSimulation");
        }

        private static void CreateOrUpdateEnemyCatalog()
        {
            var catalog = LoadOrCreateAsset<EnemyCatalog>("EnemyCatalog");
            if (catalog == null) return;
            var so = new SerializedObject(catalog);
            var archProp = so.FindProperty("archetypes");
            if (archProp == null) return;
            if (archProp.arraySize > 0) { so.ApplyModifiedProperties(); return; }
            var list = new List<EnemyArchetypeDefinition>
            {
                new() { Id = "grunt", DisplayName = "Grunt Patrol", FlatPowerBonus = 0, WavePowerBonusPerWave = 0, Modifiers = new List<EnemyModifierDefinition>() },
                new()
                {
                    Id = "shield", DisplayName = "Shield Squad", FlatPowerBonus = 8, WavePowerBonusPerWave = 2,
                    Modifiers = new List<EnemyModifierDefinition>
                    {
                        new() { Order = 10, ModifierType = EnemyModifierType.ArmorPercent, ModifierValue = 0.2f, AllowStacking = false, StackGroupId = "defense" },
                        new() { Order = 20, ModifierType = EnemyModifierType.HealFlat, ModifierValue = 4f, AllowStacking = true, StackGroupId = "sustain" }
                    }
                },
                new()
                {
                    Id = "berserk", DisplayName = "Berserker Mob", FlatPowerBonus = 14, WavePowerBonusPerWave = 4,
                    Modifiers = new List<EnemyModifierDefinition>
                    {
                        new() { Order = 10, ModifierType = EnemyModifierType.RagePercentPerWave, ModifierValue = 0.08f, AllowStacking = true, StackGroupId = "rage" }
                    }
                }
            };
            archProp.ClearArray();
            for (var i = 0; i < list.Count; i++)
            {
                archProp.InsertArrayElementAtIndex(i);
                var el = archProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("Id").stringValue = list[i].Id;
                el.FindPropertyRelative("DisplayName").stringValue = list[i].DisplayName;
                el.FindPropertyRelative("FlatPowerBonus").intValue = list[i].FlatPowerBonus;
                el.FindPropertyRelative("WavePowerBonusPerWave").intValue = list[i].WavePowerBonusPerWave;
                var mods = el.FindPropertyRelative("Modifiers");
                if (mods != null && list[i].Modifiers != null)
                {
                    mods.ClearArray();
                    foreach (var m in list[i].Modifiers)
                    {
                        mods.InsertArrayElementAtIndex(mods.arraySize);
                        var me = mods.GetArrayElementAtIndex(mods.arraySize - 1);
                        me.FindPropertyRelative("Order").intValue = m.Order;
                        me.FindPropertyRelative("ModifierType").enumValueIndex = (int)m.ModifierType;
                        me.FindPropertyRelative("ModifierValue").floatValue = m.ModifierValue;
                        me.FindPropertyRelative("AllowStacking").boolValue = m.AllowStacking;
                        me.FindPropertyRelative("StackGroupId").stringValue = m.StackGroupId ?? "";
                    }
                }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static void CreateOrUpdateBoardCatalog()
        {
            var catalog = LoadOrCreateAsset<BoardCatalog>("BoardCatalog");
            if (catalog == null) return;
            var so = new SerializedObject(catalog);
            var boardsProp = so.FindProperty("boards");
            if (boardsProp == null) return;
            if (boardsProp.arraySize > 0) { so.ApplyModifiedProperties(); return; }
            var list = new List<BoardDefinition>
            {
                new() { Id = "board_garden", DisplayName = "Garden Path", DifficultyLabel = "Easy", EnemyMultiplier = 1f, UnlockCostResource = 0, SpawnCapacityBonus = 0, MergeRewardMultiplier = 1f, EnemyArchetypeId = "grunt" },
                new() { Id = "board_city", DisplayName = "City Crossing", DifficultyLabel = "Normal", EnemyMultiplier = 1.2f, UnlockCostResource = 4, SpawnCapacityBonus = 1, MergeRewardMultiplier = 1.1f, EnemyArchetypeId = "shield" },
                new() { Id = "board_castle", DisplayName = "Castle Siege", DifficultyLabel = "Hard", EnemyMultiplier = 1.4f, UnlockCostResource = 7, SpawnCapacityBonus = 2, MergeRewardMultiplier = 1.25f, EnemyArchetypeId = "berserk" },
                new() { Id = "board_ruins", DisplayName = "Haunted Ruins", DifficultyLabel = "Hard", EnemyMultiplier = 1.5f, UnlockCostResource = 10, UnlockObjectiveWave = 5, SpawnCapacityBonus = 1, MergeRewardMultiplier = 1.2f, EnemyArchetypeId = "berserk" },
                new() { Id = "board_arena", DisplayName = "Champion Arena", DifficultyLabel = "Extreme", EnemyMultiplier = 1.7f, UnlockCostResource = 15, SpawnCapacityBonus = 3, MergeRewardMultiplier = 1.4f, EnemyArchetypeId = "shield" }
            };
            boardsProp.ClearArray();
            for (var i = 0; i < list.Count; i++)
            {
                var b = list[i];
                boardsProp.InsertArrayElementAtIndex(i);
                var el = boardsProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("Id").stringValue = b.Id;
                el.FindPropertyRelative("DisplayName").stringValue = b.DisplayName;
                el.FindPropertyRelative("DifficultyLabel").stringValue = b.DifficultyLabel;
                el.FindPropertyRelative("EnemyMultiplier").floatValue = b.EnemyMultiplier;
                el.FindPropertyRelative("UnlockCostResource").intValue = b.UnlockCostResource;
                el.FindPropertyRelative("UnlockObjectiveWave").intValue = b.UnlockObjectiveWave;
                el.FindPropertyRelative("SpawnCapacityBonus").intValue = b.SpawnCapacityBonus;
                el.FindPropertyRelative("MergeRewardMultiplier").floatValue = b.MergeRewardMultiplier;
                el.FindPropertyRelative("EnemyArchetypeId").stringValue = b.EnemyArchetypeId;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static void CreateOrUpdateItemDatabase()
        {
            var dbPath = $"{DataRoot}/ItemDatabase.asset";
            var db = LoadAssetAtPath<ItemDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            var so = new SerializedObject(db);
            var itemsProp = so.FindProperty("items");
            if (itemsProp != null && itemsProp.arraySize > 0) { so.ApplyModifiedProperties(); return; }
            var items = new List<ItemDefinition>
            {
                CreateItemAsset("pawn_t1", "Pawn", "pawn", 1, 5),
                CreateItemAsset("pawn_t2", "Knight", "pawn", 2, 12),
                CreateItemAsset("pawn_t3", "Rook", "pawn", 3, 24),
                CreateItemAsset("pawn_t4", "Queen", "pawn", 4, 45)
            };
            itemsProp.ClearArray();
            foreach (var item in items)
            {
                itemsProp.InsertArrayElementAtIndex(itemsProp.arraySize);
                itemsProp.GetArrayElementAtIndex(itemsProp.arraySize - 1).objectReferenceValue = item;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(db);
        }

        private static ItemDefinition CreateItemAsset(string id, string displayName, string familyId, int tier, int power)
        {
            var path = $"{DataRoot}/{id}.asset";
            var existing = LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null)
            {
                existing.ConfigureRuntime(id, displayName, familyId, tier, power);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.ConfigureRuntime(id, displayName, familyId, tier, power);
            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        private static T LoadOrCreateAsset<T>(string fileName) where T : ScriptableObject
        {
            var path = $"{DataRoot}/{fileName}.asset";
            var existing = LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T LoadAssetAtPath<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void SetInt(SerializedObject so, string propertyName, int value)
        {
            var p = so.FindProperty(propertyName);
            if (p != null) p.intValue = value;
        }

        private static void EnsureFolder(string path)
        {
            var split = path.Split('/');
            var current = split[0];
            for (var i = 1; i < split.Length; i++)
            {
                var next = $"{current}/{split[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, split[i]);
                current = next;
            }
        }
    }
}
