using System.Collections;
using System.Reflection;
using MergeSurvivor.Core;
using MergeSurvivor.Economy;
using MergeSurvivor.Gameplay;
using MergeSurvivor.Data;
using MergeSurvivor.Meta;
using MergeSurvivor.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MergeSurvivor.Tests
{
    public sealed class PlayModeBootstrapTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            CleanupSceneObjects();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CleanupSceneObjects();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PrototypeBootstrap_CreatesCoreUiObjects()
        {
            var go = new GameObject("TestBootstrap");
            go.AddComponent<PrototypeBootstrap>();

            yield return null;

            Assert.IsNotNull(Object.FindFirstObjectByType<Canvas>(), "Canvas was not created.");
            Assert.IsNotNull(Object.FindFirstObjectByType<EventSystem>(), "EventSystem was not created.");
            // Spawn/Fight are run-only buttons, hidden in the hub — find them inactive-inclusively.
            Assert.IsNotNull(FindObjectByNameIncludingInactive<Button>("Btn_Spawn Gem"), "Spawn button missing.");
            Assert.IsNotNull(FindObjectByNameIncludingInactive<Button>("Btn_Fight"), "Fight button missing.");
            Assert.IsNotNull(GameObject.Find("Btn_Meta Hub"), "Meta Hub button missing.");
        }

        [UnityTest]
        public IEnumerator PrototypeBootstrap_ButtonsTriggerExpectedUiState()
        {
            var go = new GameObject("TestBootstrap");
            go.AddComponent<PrototypeBootstrap>();

            yield return null;

            var spawnButton = FindObjectByNameIncludingInactive<Button>("Btn_Spawn Gem");
            var fightButton = FindObjectByNameIncludingInactive<Button>("Btn_Fight");
            var metaButton = GameObject.Find("Btn_Meta Hub")?.GetComponent<Button>();
            var returnButton = FindObjectByNameIncludingInactive<Button>("Btn_Return");
            var statusText = GameObject.Find("Status")?.GetComponent<TMP_Text>();
            var metaPanel = FindObjectByNameIncludingInactive<GameObject>("MetaPanel");

            Assert.IsNotNull(spawnButton, "Spawn button component missing.");
            Assert.IsNotNull(fightButton, "Fight button component missing.");
            Assert.IsNotNull(metaButton, "Meta button component missing.");
            Assert.IsNotNull(returnButton, "Return button component missing.");
            Assert.IsNotNull(statusText, "Status text missing.");
            Assert.IsNotNull(metaPanel, "Meta panel missing.");
            Assert.IsFalse(metaPanel.activeSelf, "Meta panel should be closed initially.");

            spawnButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual("Spawned.", statusText.text, "Spawn button did not update status.");

            metaButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(metaPanel.activeSelf, "Meta panel should open after Meta Hub click.");

            returnButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(metaPanel.activeSelf, "Meta panel should close after Return click.");

            fightButton.onClick.Invoke();
            yield return null;
            StringAssert.Contains("/", statusText.text, "Fight status does not include combat score.");
        }

        [UnityTest]
        public IEnumerator PrototypeBootstrap_MergeFlowUpgradesTierAndRewardsSoftCurrency()
        {
            var go = new GameObject("TestBootstrap");
            var bootstrap = go.AddComponent<PrototypeBootstrap>();
            yield return null;

            var session = GetPrivateField<GameSession>(bootstrap, "_session");
            var inventory = GetPrivateField<InventoryService>(bootstrap, "_inventory");
            Assert.IsNotNull(session, "Game session not initialized.");
            Assert.IsNotNull(inventory, "Inventory not initialized.");

            for (var y = 0; y < session.Board.Height; y++)
            {
                for (var x = 0; x < session.Board.Width; x++)
                {
                    session.Board.Set(x, y, null);
                }
            }

            session.Board.Set(0, 0, "pawn_t1");
            session.Board.Set(1, 0, "pawn_t1");
            session.Board.Set(2, 0, "pawn_t1");

            var softBefore = inventory.Get(CurrencyType.Soft);

            var cell00 = GameObject.Find("Cell_0_0")?.GetComponent<CellView>();
            var cell10 = GameObject.Find("Cell_1_0")?.GetComponent<CellView>();
            Assert.IsNotNull(cell00, "Source cell not found.");
            Assert.IsNotNull(cell10, "Target cell not found.");
            Assert.IsNotNull(EventSystem.current, "EventSystem is required for pointer events.");

            cell00.OnPointerClick(new PointerEventData(EventSystem.current));
            cell10.OnPointerClick(new PointerEventData(EventSystem.current));
            yield return null;

            Assert.AreEqual("pawn_t2", session.Board.Get(1, 0), "Merge did not upgrade target cell to tier 2.");
            var softAfter = inventory.Get(CurrencyType.Soft);
            Assert.GreaterOrEqual(softAfter - softBefore, 15, "Merge reward soft currency was not granted.");
        }

        [UnityTest]
        public IEnumerator PrototypeBootstrap_BoardUnlockAndSelectionFlowWorks()
        {
            var go = new GameObject("TestBootstrap");
            var bootstrap = go.AddComponent<PrototypeBootstrap>();
            yield return null;

            var session = GetPrivateField<GameSession>(bootstrap, "_session");
            var inventory = GetPrivateField<InventoryService>(bootstrap, "_inventory");
            Assert.IsNotNull(session, "Session missing.");
            Assert.IsNotNull(inventory, "Inventory missing.");
            Assert.AreEqual(1, session.UnlockedBoardCount, "Should start with one unlocked board.");
            Assert.AreEqual(0, session.CurrentBoardIndex, "Should start on first board.");

            inventory.Add(new GameReward { ProgressionResource = 10 });

            var metaButton = GameObject.Find("Btn_Meta Hub")?.GetComponent<Button>();
            var unlockButton = FindObjectByNameIncludingInactive<Button>("Btn_Unlock Next Board");
            var nextBoardButton = FindObjectByNameIncludingInactive<Button>("Btn_Next Board");
            var boardHud = GameObject.Find("BoardHUD")?.GetComponent<TMP_Text>();

            Assert.IsNotNull(metaButton, "Meta button missing.");
            Assert.IsNotNull(unlockButton, "Unlock button missing.");
            Assert.IsNotNull(nextBoardButton, "Next Board button missing.");
            Assert.IsNotNull(boardHud, "Board HUD missing.");

            metaButton.onClick.Invoke();
            yield return null;
            unlockButton.onClick.Invoke();
            yield return null;

            Assert.AreEqual(2, session.UnlockedBoardCount, "Unlock should increase unlocked boards.");

            nextBoardButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, session.CurrentBoardIndex, "Next board should select second board.");
            StringAssert.Contains("City Crossing", boardHud.text, "Board HUD should show selected board.");
        }

        [UnityTest]
        public IEnumerator PrototypeBootstrap_BoardArchetypeIncreasesEnemyPowerOnAdvancedBoard()
        {
            var go = new GameObject("TestBootstrap");
            var bootstrap = go.AddComponent<PrototypeBootstrap>();
            yield return null;

            var session = GetPrivateField<GameSession>(bootstrap, "_session");
            var inventory = GetPrivateField<InventoryService>(bootstrap, "_inventory");
            Assert.IsNotNull(session, "Session missing.");
            Assert.IsNotNull(inventory, "Inventory missing.");

            // Force deterministic empty board fight so only board/archetype affects enemy power.
            for (var y = 0; y < session.Board.Height; y++)
            {
                for (var x = 0; x < session.Board.Width; x++)
                {
                    session.Board.Set(x, y, null);
                }
            }

            var board1Result = session.Fight();
            Assert.IsFalse(board1Result.Won, "Empty board should lose baseline fight.");

            inventory.Add(new GameReward { ProgressionResource = 10 });
            Assert.IsTrue(session.TryUnlockNextBoard(), "Board 2 unlock failed in test.");
            Assert.IsTrue(session.SelectNextUnlockedBoard(), "Board switch failed in test.");
            Assert.AreEqual(1, session.CurrentBoardIndex, "Expected second board selected.");

            var board2Result = session.Fight();
            Assert.IsFalse(board2Result.Won, "Empty board should still lose advanced board fight.");
            Assert.Greater(board2Result.EnemyPower, board1Result.EnemyPower, "Advanced board/archetype should increase enemy power.");
        }

        [Test]
        public void CombatCalculator_EnemyModifiersApplyAsExpected()
        {
            var armor = CombatCalculator.ApplyEnemyModifier(100, 120, EnemyModifierType.ArmorPercent, 0.25f, 1);
            Assert.AreEqual(75, armor.effectivePlayer);
            Assert.AreEqual(120, armor.effectiveEnemy);

            var rage = CombatCalculator.ApplyEnemyModifier(100, 120, EnemyModifierType.RagePercentPerWave, 0.1f, 3);
            Assert.AreEqual(100, rage.effectivePlayer);
            Assert.AreEqual(144, rage.effectiveEnemy);

            var heal = CombatCalculator.ApplyEnemyModifier(100, 120, EnemyModifierType.HealFlat, 15f, 1);
            Assert.AreEqual(100, heal.effectivePlayer);
            Assert.AreEqual(135, heal.effectiveEnemy);
        }

        [Test]
        public void CombatCalculator_ComposableModifiersApplyInSequence()
        {
            var modifiers = new[]
            {
                new EnemyModifierDefinition { Order = 10, ModifierType = EnemyModifierType.ArmorPercent, ModifierValue = 0.2f, AllowStacking = true, StackGroupId = "defense" },
                new EnemyModifierDefinition { Order = 20, ModifierType = EnemyModifierType.HealFlat, ModifierValue = 10f, AllowStacking = true, StackGroupId = "sustain" },
                new EnemyModifierDefinition { Order = 30, ModifierType = EnemyModifierType.RagePercentPerWave, ModifierValue = 0.1f, AllowStacking = true, StackGroupId = "rage" }
            };

            var result = CombatCalculator.ApplyEnemyModifiers(100, 100, modifiers, 3);

            // Armor -> player 80, Heal -> enemy 110, Rage (wave3 => x0.2) -> +22 => 132.
            Assert.AreEqual(80, result.effectivePlayer);
            Assert.AreEqual(132, result.effectiveEnemy);
        }

        [Test]
        public void CombatCalculator_NonStackingGroupOnlyAppliesFirstModifierByOrder()
        {
            var modifiers = new[]
            {
                new EnemyModifierDefinition { Order = 20, ModifierType = EnemyModifierType.HealFlat, ModifierValue = 10f, AllowStacking = true, StackGroupId = "sustain" },
                new EnemyModifierDefinition { Order = 10, ModifierType = EnemyModifierType.ArmorPercent, ModifierValue = 0.5f, AllowStacking = false, StackGroupId = "defense" },
                new EnemyModifierDefinition { Order = 30, ModifierType = EnemyModifierType.ArmorPercent, ModifierValue = 0.2f, AllowStacking = false, StackGroupId = "defense" }
            };

            var result = CombatCalculator.ApplyEnemyModifiers(100, 100, modifiers, 1);

            Assert.AreEqual(50, result.effectivePlayer);
            Assert.AreEqual(110, result.effectiveEnemy);
        }

        [UnityTest]
        public IEnumerator FullRunFlow_SpawnMergeFightAndRewards()
        {
            var go = new GameObject("TestBootstrap");
            var bootstrap = go.AddComponent<PrototypeBootstrap>();
            yield return null;

            var session = GetPrivateField<GameSession>(bootstrap, "_session");
            var inventory = GetPrivateField<InventoryService>(bootstrap, "_inventory");
            Assert.IsNotNull(session, "Session not initialized.");
            Assert.IsNotNull(inventory, "Inventory not initialized.");

            for (var y = 0; y < session.Board.Height; y++)
                for (var x = 0; x < session.Board.Width; x++)
                    session.Board.Set(x, y, null);

            var softBefore = inventory.Get(CurrencyType.Soft);
            var resourceBefore = inventory.Get(CurrencyType.ProgressionResource);

            Assert.IsTrue(session.TrySpawn(), "First spawn should succeed.");
            Assert.IsTrue(session.TrySpawn(), "Second spawn should succeed.");
            Assert.IsTrue(session.TrySpawn(), "Third spawn should succeed.");
            Assert.Greater(session.Board.OccupiedCount(), 0, "Board should have pieces after spawn.");

            session.Board.Set(0, 0, "pawn_t1");
            session.Board.Set(1, 0, "pawn_t1");
            session.Board.Set(2, 0, "pawn_t1");
            var mergeResult = session.TryMergeAt(1, 0);
            Assert.IsTrue(mergeResult.Success, "Merge of 3x T1 should succeed.");
            Assert.AreEqual(2, mergeResult.UpgradedTier, "Merge should produce T2.");
            var softAfterMerge = inventory.Get(CurrencyType.Soft);
            Assert.GreaterOrEqual(softAfterMerge - softBefore, 15, "Merge should grant soft currency.");

            var fightResult = session.Fight();
            Assert.IsTrue(fightResult.PlayerPower >= 0 && fightResult.EnemyPower >= 0, "Fight should return valid power values.");
            if (fightResult.Won)
            {
                Assert.Greater(session.CurrentWave, 1, "Winning fight should advance wave.");
                Assert.Greater(inventory.Get(CurrencyType.ProgressionResource), resourceBefore, "Win should grant progression resource.");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveLoad_ProgressionPersists()
        {
            var go = new GameObject("TestBootstrap");
            var bootstrap = go.AddComponent<PrototypeBootstrap>();
            yield return null;

            var progression = GetPrivateField<ProgressionService>(bootstrap, "_progression");
            Assert.IsNotNull(progression, "Progression not initialized.");

            progression.Data.UnlockedBoardCount = 2;
            progression.Data.CurrentBoardIndex = 1;
            progression.Data.HighestWave = 5;
            progression.Save();

            var unlockedBefore = progression.Data.UnlockedBoardCount;
            var boardIndexBefore = progression.Data.CurrentBoardIndex;
            var waveBefore = progression.Data.HighestWave;

            Object.DestroyImmediate(bootstrap.gameObject);
            yield return null;

            var save2 = new LocalSaveService();
            save2.TryLoad("merge_survivor_progression_v1", out ProgressionData loaded);
            Assert.IsTrue(loaded != null, "Progression should have been saved.");
            Assert.AreEqual(unlockedBefore, loaded.UnlockedBoardCount, "UnlockedBoardCount should persist.");
            Assert.AreEqual(boardIndexBefore, loaded.CurrentBoardIndex, "CurrentBoardIndex should persist.");
            Assert.AreEqual(waveBefore, loaded.HighestWave, "HighestWave should persist.");
        }

        [UnityTest]
        public IEnumerator SaveLoad_AccountPersists()
        {
            var go = new GameObject("TestBootstrap");
            var bootstrap = go.AddComponent<PrototypeBootstrap>();
            yield return null;

            var inventory = GetPrivateField<InventoryService>(bootstrap, "_inventory");
            Assert.IsNotNull(inventory, "Inventory not initialized.");
            inventory.Add(new GameReward { SoftCurrency = 100, ProgressionResource = 10 });
            PlayerPrefs.SetString("merge_survivor_account_v1", JsonUtility.ToJson(inventory.Data));
            PlayerPrefs.Save();

            var softExpected = inventory.Get(CurrencyType.Soft);
            var resourceExpected = inventory.Get(CurrencyType.ProgressionResource);

            Object.DestroyImmediate(bootstrap.gameObject);
            yield return null;

            var save = new LocalSaveService();
            save.TryLoad("merge_survivor_account_v1", out AccountData loaded);
            Assert.IsTrue(loaded != null, "Account should have been saved.");
            Assert.AreEqual(softExpected, loaded.Soft, "Soft currency should persist.");
            Assert.AreEqual(resourceExpected, loaded.Resource, "Progression resource should persist.");
        }

        private static T FindObjectByNameIncludingInactive<T>(string objectName) where T : Object
        {
            if (typeof(T) == typeof(GameObject))
            {
                foreach (var tr in Resources.FindObjectsOfTypeAll<Transform>())
                {
                    if (tr != null && tr.gameObject.name == objectName && tr.gameObject.scene.IsValid())
                    {
                        return tr.gameObject as T;
                    }
                }

                return null;
            }

            foreach (var component in Resources.FindObjectsOfTypeAll<T>())
            {
                if (component is Component c && c.gameObject.name == objectName && c.gameObject.scene.IsValid())
                {
                    return component;
                }
            }

            return null;
        }

        private static T GetPrivateField<T>(object instance, string fieldName) where T : class
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(instance) as T;
        }

        private static void CleanupSceneObjects()
        {
            PlayerPrefs.DeleteKey("merge_survivor_account_v1");
            PlayerPrefs.DeleteKey("merge_survivor_progression_v1");
            PlayerPrefs.Save();

            foreach (var bootstrap in Object.FindObjectsByType<PrototypeBootstrap>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(bootstrap.gameObject);
            }

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(canvas.gameObject);
            }

            foreach (var eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(eventSystem.gameObject);
            }

            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(camera.gameObject);
            }
        }
    }
}
