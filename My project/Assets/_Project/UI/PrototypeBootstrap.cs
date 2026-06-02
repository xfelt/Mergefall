using System.Collections;
using System.Collections.Generic;
using MergeSurvivor.Data;
using MergeSurvivor.Economy;
using MergeSurvivor.Gameplay;
using MergeSurvivor.Meta;
using MergeSurvivor.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MergeSurvivor.UI
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private ItemVisualCatalog itemVisualCatalog;
        [SerializeField] private Sprite premiumIconSprite;
        [Tooltip("9-sliced sprite for buttons (e.g. panel_tan). Leave empty for flat color.")]
        [SerializeField] private Sprite panelButtonSprite;
        [Tooltip("9-sliced sprite for modal/panel backgrounds (e.g. panel_brown). Leave empty for flat color.")]
        [SerializeField] private Sprite panelBackgroundSprite;
        [Tooltip("Soft currency icon (e.g. currency_soft). Leave empty to use Resources/Icons/currency_soft if present.")]
        [SerializeField] private Sprite softIconSprite;
        [Tooltip("Progression resource icon (e.g. currency_resource). Leave empty to use Resources/Icons/currency_resource if present.")]
        [SerializeField] private Sprite resourceIconSprite;
        [Tooltip("Optional. Assign to show a different background image per board (Art/Backgrounds).")]
        [SerializeField] private BoardBackgroundCatalog boardBackgroundCatalog;
        [Tooltip("Fight result panel: archetype grunt (maps to bandit art).")]
        [SerializeField] private Sprite enemyPortraitGrunt;
        [Tooltip("Fight result panel: archetype shield (maps to scarab art).")]
        [SerializeField] private Sprite enemyPortraitShield;
        [Tooltip("Fight result panel: archetype berserk (maps to barbarian art).")]
        [SerializeField] private Sprite enemyPortraitBerserk;
        [Tooltip("Optional. Burst VFX on merge (e.g. Art/VFX/MergeVFX prefab). Assign in Inspector.")]
        [SerializeField] private GameObject mergeVfxPrefab;
        [Tooltip("Optional. Short clip when pieces merge.")]
        [SerializeField] private AudioClip sfxMerge;
        [Tooltip("Optional. Fight panel when you win.")]
        [SerializeField] private AudioClip sfxFightWin;
        [Tooltip("Optional. Fight panel when you lose.")]
        [SerializeField] private AudioClip sfxFightLoss;

        private GameSession _session;
        private AudioSource _uiAudio;
        private AudioManager _audio;
        private Image _boardBackgroundImage;
        private Image _enemyPortraitImage;
        private ItemDatabase _items;
        private BoardCatalog _boardCatalog;
        private EnemyCatalog _enemyCatalog;
        private CombatConfig _combatConfig;
        private InventoryService _inventory;
        private ProgressionService _progression;
        private TMP_Text _status;
        private TMP_Text _hud;
        private TMP_Text _boardHud;
        private TMP_Text _waveLabel;
        private TMP_Text _softLabel;
        private TMP_Text _premiumLabel;
        private TMP_Text _resourceLabel;
        private GameObject _metaPanel;
        private GameObject _fightResultPanel;
        private TMP_Text _fightResultTitle;
        private TMP_Text _fightResultSubtitle;
        private TMP_Text _fightResultRewards;
        private Image _playerBarFill;
        private Image _enemyBarFill;
        private TMP_Text _playerPowerLabel;
        private TMP_Text _enemyPowerLabel;
        private GameObject _fightContinueButton;
        private bool _resolvingFight;
        private GameObject _onboardingOverlay;
        private GameObject _tutorialOverlay;
        private GameObject _tutorialHighlightHolder;
        private TMP_Text _tutorialText;
        private TMP_Text _tutorialStepLabel;
        private bool _tutorialActive;
        private bool _tutorialNext;
        private bool _tutorialSkip;
        private GameObject _nextBoardButton;
        private GameObject _boardSelectPanel;
        private (int x, int y)? _selected;
        private readonly List<CellView> _cells = new();
        private readonly List<Image> _tutorialHighlights = new();

        private bool _isInRun;
        private bool _boardSelectForStartRun;
        private bool _lastFightWasLoss;
        private GameObject _startRunButton;
        private GameObject _endRunButton;
        private GameObject _spawnButton;
        private GameObject _fightButton;
        private GameObject _boardSelectButton;

        private const string OnboardingDoneKey = "merge_survivor_onboarding_done";
        private const string TutorialDoneKey = "merge_survivor_tutorial_done";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawn()
        {
            if (FindFirstObjectByType<PrototypeBootstrap>() != null) return;
            var go = new GameObject("PrototypeBootstrap");
            go.AddComponent<PrototypeBootstrap>();
        }

        private void Awake()
        {
            SetupServices();
            EnsureUiAudio();
            BuildUi();
            _isInRun = false;
            RefreshAll();
            RefreshRunStateUI();
            SetStatus("At hub. Start a run to play.");
            _audio.PlayMusic(AudioManager.MusicHub);
            ShowOnboardingIfFirstTime();
        }

        private void EnsureUiAudio()
        {
            _uiAudio = GetComponent<AudioSource>();
            if (_uiAudio == null) _uiAudio = gameObject.AddComponent<AudioSource>();
            _uiAudio.playOnAwake = false;
            _uiAudio.spatialBlend = 0f;
            _audio = AudioManager.Ensure();
        }

        private void SetupServices()
        {
            _items = GameContentLoader.LoadItemDatabase();
            if (_items == null)
            {
                _items = ScriptableObject.CreateInstance<ItemDatabase>();
                var i1 = ScriptableObject.CreateInstance<ItemDefinition>(); i1.ConfigureRuntime("pawn_t1", "Pawn", "pawn", 1, 5);
                var i2 = ScriptableObject.CreateInstance<ItemDefinition>(); i2.ConfigureRuntime("pawn_t2", "Knight", "pawn", 2, 12);
                var i3 = ScriptableObject.CreateInstance<ItemDefinition>(); i3.ConfigureRuntime("pawn_t3", "Rook", "pawn", 3, 24);
                var i4 = ScriptableObject.CreateInstance<ItemDefinition>(); i4.ConfigureRuntime("pawn_t4", "Queen", "pawn", 4, 45);
                var i5 = ScriptableObject.CreateInstance<ItemDefinition>(); i5.ConfigureRuntime("pawn_t5", "King", "pawn", 5, 85);
                var i6 = ScriptableObject.CreateInstance<ItemDefinition>(); i6.ConfigureRuntime("pawn_t6", "Emperor", "pawn", 6, 160);
                _items.ConfigureRuntime(new List<ItemDefinition> { i1, i2, i3, i4, i5, i6 });
            }
            else
            {
                _items.Warm();
            }

            var merge = GameContentLoader.LoadMergeRulesConfig() ?? ScriptableObject.CreateInstance<MergeRulesConfig>();
            var spawn = GameContentLoader.LoadSpawnConfig() ?? ScriptableObject.CreateInstance<SpawnConfig>();
            _combatConfig = GameContentLoader.LoadCombatConfig();
            if (_combatConfig == null)
            {
                _combatConfig = ScriptableObject.CreateInstance<CombatConfig>();
                _combatConfig.SetRuntimeBaseEnemyStrength(28);
            }
            var economy = GameContentLoader.LoadEconomyTables();
            if (economy == null)
            {
                economy = ScriptableObject.CreateInstance<EconomyTables>();
                economy.SetRuntimeSpawnUpgradeBaseCost(50);
            }
            _enemyCatalog = GameContentLoader.LoadEnemyCatalog();
            if (_enemyCatalog == null)
            {
                _enemyCatalog = ScriptableObject.CreateInstance<EnemyCatalog>();
                _enemyCatalog.ConfigureRuntime(new List<EnemyArchetypeDefinition>
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
                });
            }
            _boardCatalog = GameContentLoader.LoadBoardCatalog();
            if (_boardCatalog == null)
            {
                _boardCatalog = ScriptableObject.CreateInstance<BoardCatalog>();
                _boardCatalog.ConfigureRuntime(new List<BoardDefinition>
                {
                    new() { Id = "board_garden", DisplayName = "Garden Path", DifficultyLabel = "Easy", EnemyMultiplier = 1f, UnlockCostResource = 0, SpawnCapacityBonus = 0, MergeRewardMultiplier = 1f, EnemyArchetypeId = "grunt" },
                    new() { Id = "board_city", DisplayName = "City Crossing", DifficultyLabel = "Normal", EnemyMultiplier = 1.2f, UnlockCostResource = 4, SpawnCapacityBonus = 1, MergeRewardMultiplier = 1.1f, EnemyArchetypeId = "shield" },
                    new() { Id = "board_castle", DisplayName = "Castle Siege", DifficultyLabel = "Hard", EnemyMultiplier = 1.4f, UnlockCostResource = 7, SpawnCapacityBonus = 2, MergeRewardMultiplier = 1.25f, EnemyArchetypeId = "berserk" },
                    new() { Id = "board_ruins", DisplayName = "Haunted Ruins", DifficultyLabel = "Hard", EnemyMultiplier = 1.5f, UnlockCostResource = 10, UnlockObjectiveWave = 5, SpawnCapacityBonus = 1, MergeRewardMultiplier = 1.2f, EnemyArchetypeId = "berserk" },
                    new() { Id = "board_arena", DisplayName = "Champion Arena", DifficultyLabel = "Extreme", EnemyMultiplier = 1.7f, UnlockCostResource = 15, SpawnCapacityBonus = 3, MergeRewardMultiplier = 1.4f, EnemyArchetypeId = "shield" }
                });
            }

            var save = new LocalSaveService();
            save.TryLoad("merge_survivor_account_v1", out AccountData account);
            _inventory = new InventoryService(account ?? new AccountData());
            _progression = new ProgressionService(save, economy);

            var platformConfig = PlatformServiceFactory.GetConfig();
            var ads = PlatformServiceFactory.CreateAds(platformConfig);
            ads.Initialize();
            var analytics = PlatformServiceFactory.CreateAnalytics(platformConfig);
            analytics.Initialize();
            var store = PlatformServiceFactory.CreateStore(platformConfig);
            store.Initialize();
            var remoteConfig = PlatformServiceFactory.CreateRemoteConfig(platformConfig);
            remoteConfig.Initialize();
            PlatformServiceFactory.CreateCloudSave().Initialize();

            var simulation = GameContentLoader.LoadRemoteConfigSimulation();
            var simulationOverrides = simulation != null ? simulation.ToOverrideDictionary() : null;
            BalanceRemoteConfigApplier.Apply(remoteConfig, _combatConfig, economy, simulationOverrides);

            _session = new GameSession(
                new BoardState(4, 4),
                _boardCatalog,
                _enemyCatalog,
                _items,
                merge,
                spawn,
                _combatConfig,
                economy,
                _inventory,
                _progression,
                ads,
                analytics);
        }

        private void BuildUi()
        {
            EnsureFallbackCamera();
            EnsureEventSystem();
            var canvas = EnsureCanvas();
            var root = Ui("Root", canvas.transform);
            Stretch(root.GetComponent<RectTransform>());

            var hudBar = Ui("HudBar", root.transform);
            var hudBarRt = hudBar.GetComponent<RectTransform>();
            hudBarRt.anchorMin = new Vector2(0, 1);
            hudBarRt.anchorMax = new Vector2(1, 1);
            hudBarRt.pivot = new Vector2(0.5f, 1f);
            hudBarRt.sizeDelta = new Vector2(0, 90);
            hudBarRt.anchoredPosition = Vector2.zero;
            var hudBarBg = hudBar.AddComponent<Image>();
            hudBarBg.color = DesertTheme.PanelDark;
            hudBarBg.raycastTarget = false;

            var currencyRow = Ui("CurrencyRow", hudBar.transform);
            var crRt = currencyRow.GetComponent<RectTransform>();
            crRt.anchorMin = new Vector2(0, 0.55f);
            crRt.anchorMax = new Vector2(1, 1f);
            crRt.offsetMin = new Vector2(16, 4);
            crRt.offsetMax = new Vector2(-16, -6);
            var crLayout = currencyRow.AddComponent<HorizontalLayoutGroup>();
            crLayout.spacing = 24;
            crLayout.childAlignment = TextAnchor.MiddleCenter;
            crLayout.childControlWidth = false;
            crLayout.childControlHeight = true;
            crLayout.childForceExpandWidth = false;
            crLayout.childForceExpandHeight = true;

            _waveLabel = Label("WaveLabel", currencyRow.transform, Vector2.zero, Vector2.one, Vector2.zero, DesertTheme.FontSizeBody);
            _waveLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 0);
            _waveLabel.color = DesertTheme.AccentGold;
            var softSlot = Ui("SoftSlot", currencyRow.transform);
            var softSlotRt = softSlot.GetComponent<RectTransform>();
            softSlotRt.sizeDelta = new Vector2(100, 0);
            var softSlotLayout = softSlot.AddComponent<HorizontalLayoutGroup>();
            softSlotLayout.spacing = 4;
            softSlotLayout.childAlignment = TextAnchor.MiddleCenter;
            softSlotLayout.childControlWidth = false;
            softSlotLayout.childControlHeight = true;
            softSlotLayout.childForceExpandWidth = false;
            softSlotLayout.childForceExpandHeight = true;
            var softIcon = softIconSprite != null ? softIconSprite : Resources.Load<Sprite>("Icons/currency_soft");
            if (softIcon != null)
            {
                var softIconGo = Ui("SoftIcon", softSlot.transform);
                softIconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(28, 28);
                var softIconImg = softIconGo.AddComponent<Image>();
                softIconImg.sprite = softIcon;
                softIconImg.preserveAspect = true;
                softIconImg.raycastTarget = false;
            }
            _softLabel = Label("SoftLabel", softSlot.transform, Vector2.zero, Vector2.one, Vector2.zero, DesertTheme.FontSizeCaption);
            _softLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 0);
            var premiumSlot = Ui("PremiumSlot", currencyRow.transform);
            var premiumSlotRt = premiumSlot.GetComponent<RectTransform>();
            premiumSlotRt.sizeDelta = new Vector2(100, 0);
            var premiumSlotLayout = premiumSlot.AddComponent<HorizontalLayoutGroup>();
            premiumSlotLayout.spacing = 4;
            premiumSlotLayout.childAlignment = TextAnchor.MiddleCenter;
            premiumSlotLayout.childControlWidth = false;
            premiumSlotLayout.childControlHeight = true;
            premiumSlotLayout.childForceExpandWidth = false;
            premiumSlotLayout.childForceExpandHeight = true;
            var premiumIcon = premiumIconSprite != null ? premiumIconSprite : Resources.Load<Sprite>("Icons/gems_diamond");
            if (premiumIcon != null)
            {
                var premiumIconGo = Ui("PremiumIcon", premiumSlot.transform);
                var premiumIconRt = premiumIconGo.GetComponent<RectTransform>();
                premiumIconRt.sizeDelta = new Vector2(28, 28);
                var premiumIconImg = premiumIconGo.AddComponent<Image>();
                premiumIconImg.sprite = premiumIcon;
                premiumIconImg.preserveAspect = true;
                premiumIconImg.raycastTarget = false;
            }
            _premiumLabel = Label("PremiumLabel", premiumSlot.transform, Vector2.zero, Vector2.one, Vector2.zero, DesertTheme.FontSizeCaption);
            _premiumLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 0);
            _premiumLabel.color = DesertTheme.AccentTurquoise;
            var resourceSlot = Ui("ResourceSlot", currencyRow.transform);
            var resourceSlotRt = resourceSlot.GetComponent<RectTransform>();
            resourceSlotRt.sizeDelta = new Vector2(100, 0);
            var resourceSlotLayout = resourceSlot.AddComponent<HorizontalLayoutGroup>();
            resourceSlotLayout.spacing = 4;
            resourceSlotLayout.childAlignment = TextAnchor.MiddleCenter;
            resourceSlotLayout.childControlWidth = false;
            resourceSlotLayout.childControlHeight = true;
            resourceSlotLayout.childForceExpandWidth = false;
            resourceSlotLayout.childForceExpandHeight = true;
            var resourceIcon = resourceIconSprite != null ? resourceIconSprite : Resources.Load<Sprite>("Icons/currency_resource");
            if (resourceIcon != null)
            {
                var resourceIconGo = Ui("ResourceIcon", resourceSlot.transform);
                resourceIconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(28, 28);
                var resourceIconImg = resourceIconGo.AddComponent<Image>();
                resourceIconImg.sprite = resourceIcon;
                resourceIconImg.preserveAspect = true;
                resourceIconImg.raycastTarget = false;
            }
            _resourceLabel = Label("ResourceLabel", resourceSlot.transform, Vector2.zero, Vector2.one, Vector2.zero, DesertTheme.FontSizeCaption);
            _resourceLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 0);
            _resourceLabel.color = new Color(0.85f, 0.65f, 0.35f, 1f);

            _hud = Label("HUD", hudBar.transform, new Vector2(0, 0), new Vector2(1, 0.55f), Vector2.zero, DesertTheme.FontSizeCaption);
            var hudRt = _hud.GetComponent<RectTransform>();
            hudRt.offsetMin = new Vector2(16, 2);
            hudRt.offsetMax = new Vector2(-16, 0);
            _hud.color = DesertTheme.TextSecondary;

            _status = Label("Status", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -96), DesertTheme.FontSizeCaption);
            _status.color = DesertTheme.TextSecondary;
            var statusRt = _status.GetComponent<RectTransform>();
            statusRt.sizeDelta = new Vector2(-32, 22);

            _boardHud = Label("BoardHUD", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -116), DesertTheme.FontSizeCaption);
            _boardHud.color = DesertTheme.TextSecondary;

            var boardContainer = Ui("BoardContainer", root.transform);
            var containerRt = boardContainer.GetComponent<RectTransform>();
            containerRt.anchorMin = containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.sizeDelta = new Vector2(DesertTheme.GridColumns * (DesertTheme.GridCellSize + DesertTheme.GridSpacing), DesertTheme.GridColumns * (DesertTheme.GridCellSize + DesertTheme.GridSpacing));
            containerRt.anchoredPosition = new Vector2(0, 10);

            var boardBg = Ui("BoardBg", boardContainer.transform);
            Stretch(boardBg.GetComponent<RectTransform>());
            _boardBackgroundImage = boardBg.AddComponent<Image>();
            _boardBackgroundImage.color = DesertTheme.BgSecondary;
            _boardBackgroundImage.raycastTarget = false;

            var board = Ui("Board", boardContainer.transform);
            var br = board.GetComponent<RectTransform>();
            Stretch(br);

            var grid = board.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = DesertTheme.GridColumns;
            grid.cellSize = new Vector2(DesertTheme.GridCellSize, DesertTheme.GridCellSize);
            grid.spacing = new Vector2(DesertTheme.GridSpacing, DesertTheme.GridSpacing);

            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
            {
                var co = Ui($"Cell_{x}_{y}", board.transform);
                var image = co.AddComponent<Image>();
                image.color = DesertTheme.GridSlotEmpty;
                var pieceGo = Ui("Piece", co.transform);
                var pieceRt = pieceGo.GetComponent<RectTransform>();
                pieceRt.anchorMin = new Vector2(0.1f, 0.1f);
                pieceRt.anchorMax = new Vector2(0.9f, 0.9f);
                pieceRt.offsetMin = pieceRt.offsetMax = Vector2.zero;
                var pieceImage = pieceGo.AddComponent<Image>();
                pieceImage.color = PlaceholderArt.Tier1;
                pieceImage.preserveAspect = true;
                pieceImage.enabled = false;
                var txt = Label("L", co.transform, Vector2.zero, Vector2.one, Vector2.zero, 11);
                txt.transform.SetAsLastSibling();
                var cv = co.AddComponent<CellView>();
                cv.Bind(x, y, txt, pieceImage, itemVisualCatalog, mergeVfxPrefab);
                cv.OnDragStart += StartDrag;
                cv.OnDropOn += DropOn;
                cv.OnTap += TapCell;
                _cells.Add(cv);
            }

            _startRunButton = ButtonGo("Start Run", root.transform, new Vector2(0, -200), OnStartRun, panelButtonSprite);
            _startRunButton.GetComponent<Image>().color = DesertTheme.AccentGold;
            _startRunButton.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
            _endRunButton = ButtonGo("End Run", root.transform, new Vector2(0, -200), OnEndRun, panelButtonSprite);
            _endRunButton.GetComponent<Image>().color = DesertTheme.BtnSecondary;
            _spawnButton = ButtonGo("Spawn Item", root.transform, new Vector2(-150, -200), OnSpawn, panelButtonSprite);
            _spawnButton.GetComponent<Image>().color = DesertTheme.BtnSecondary;
            _fightButton = ButtonGo("Fight", root.transform, new Vector2(0, -200), OnFight, panelButtonSprite);
            _fightButton.GetComponent<Image>().color = DesertTheme.BtnDanger;
            var metaBtn = ButtonGo("Meta Hub", root.transform, new Vector2(150, -200), OpenMeta, panelButtonSprite);
            metaBtn.GetComponent<Image>().color = DesertTheme.AccentTurquoise;
            metaBtn.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
            _nextBoardButton = ButtonGo("Next Board", root.transform, new Vector2(-110, -260), OnNextBoard, panelButtonSprite);
            _nextBoardButton.GetComponent<Image>().color = DesertTheme.BtnSecondary;
            _boardSelectButton = ButtonGo("Board Select", root.transform, new Vector2(110, -260), () => OpenBoardSelect(forStartRun: false), panelButtonSprite);
            _boardSelectButton.GetComponent<Image>().color = DesertTheme.BtnSecondary;
            BuildMeta(root.transform);
            BuildBoardSelectPanel(root.transform);
            BuildFightResultPanel(root.transform);
            BuildOnboardingOverlay(root.transform);
            BuildTutorialOverlay(root.transform);
        }

        private void BuildOnboardingOverlay(Transform root)
        {
            _onboardingOverlay = Ui("OnboardingOverlay", root);
            var bg = _onboardingOverlay.AddComponent<Image>();
            bg.color = DesertTheme.BgPrimary;
            var r = _onboardingOverlay.GetComponent<RectTransform>();
            Stretch(r);

            var title = Label("Title", _onboardingOverlay.transform, new Vector2(0.5f, 0.80f), new Vector2(0.5f, 0.80f), Vector2.zero, 30);
            title.text = "Mergefall";
            title.color = DesertTheme.AccentGold;
            var subtitle = Label("Subtitle", _onboardingOverlay.transform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), Vector2.zero, DesertTheme.FontSizeBody);
            subtitle.text = "Board Quest";
            subtitle.color = DesertTheme.TextSecondary;
            var body = Label("Body", _onboardingOverlay.transform, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), Vector2.zero, DesertTheme.FontSizeCaption);
            body.text = "\u2726 Merge 3 identical crystals to forge a stronger one\n\u2694 Grow your caravan power, then tap Fight\n\u26FA Visit the Merchant Tent to upgrade";
            body.GetComponent<RectTransform>().sizeDelta = new Vector2(460, 110);
            body.color = DesertTheme.TextPrimary;
            var playBtn = ButtonGo("Begin Journey", _onboardingOverlay.transform, new Vector2(0, -80), () =>
            {
                PlayerPrefs.SetInt(OnboardingDoneKey, 1);
                PlayerPrefs.Save();
                _onboardingOverlay.SetActive(false);
            }, panelButtonSprite);
            playBtn.GetComponent<Image>().color = DesertTheme.AccentGold;
            playBtn.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
            _onboardingOverlay.SetActive(false);
        }

        private void ShowOnboardingIfFirstTime()
        {
            if (PlayerPrefs.GetInt(OnboardingDoneKey, 0) != 0) return;
            _onboardingOverlay.SetActive(true);
        }

        private void BuildTutorialOverlay(Transform root)
        {
            _tutorialOverlay = Ui("TutorialOverlay", root);
            Stretch(_tutorialOverlay.GetComponent<RectTransform>());

            // Dimmer: darkens the scene and absorbs stray taps during the coached steps.
            var dimmer = Ui("TutorialDimmer", _tutorialOverlay.transform);
            Stretch(dimmer.GetComponent<RectTransform>());
            var dimImg = dimmer.AddComponent<Image>();
            dimImg.color = new Color(0.04f, 0.03f, 0.02f, 0.62f);
            dimImg.raycastTarget = true; // block board interaction; advance is via the Next button

            // Highlight holder sits above the dimmer so coach marks are clearly visible.
            _tutorialHighlightHolder = Ui("TutorialHighlights", _tutorialOverlay.transform);
            Stretch(_tutorialHighlightHolder.GetComponent<RectTransform>());
            _tutorialHighlightHolder.GetComponent<RectTransform>().SetAsLastSibling();

            // Bottom banner with instruction text + Next / Skip.
            var banner = Ui("TutorialBanner", _tutorialOverlay.transform);
            var bannerRt = banner.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.05f, 0.05f);
            bannerRt.anchorMax = new Vector2(0.95f, 0.27f);
            bannerRt.offsetMin = bannerRt.offsetMax = Vector2.zero;
            var bannerBg = banner.AddComponent<Image>();
            if (panelBackgroundSprite != null) { bannerBg.sprite = panelBackgroundSprite; bannerBg.type = Image.Type.Sliced; bannerBg.color = Color.white; }
            else bannerBg.color = DesertTheme.PanelMid;

            _tutorialStepLabel = Label("TutStep", banner.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -22), DesertTheme.FontSizeCaption);
            _tutorialStepLabel.color = DesertTheme.AccentGold;

            _tutorialText = Label("TutText", banner.transform, new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.86f), Vector2.zero, DesertTheme.FontSizeBody, TextAlignmentOptions.Center);
            _tutorialText.color = DesertTheme.TextPrimary;
            _tutorialText.overflowMode = TextOverflowModes.Overflow;

            var nextBtn = ButtonGo("Next", banner.transform, new Vector2(80, 24), () => _tutorialNext = true, panelButtonSprite);
            var nextRt = nextBtn.GetComponent<RectTransform>();
            nextRt.anchorMin = nextRt.anchorMax = new Vector2(0.5f, 0f);
            nextRt.sizeDelta = new Vector2(150, DesertTheme.ButtonHeight);
            nextRt.anchoredPosition = new Vector2(90, 30);
            nextBtn.GetComponent<Image>().color = DesertTheme.AccentGold;
            nextBtn.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;

            var skipBtn = ButtonGo("Skip", banner.transform, new Vector2(-90, 30), () => _tutorialSkip = true, panelButtonSprite);
            var skipRt = skipBtn.GetComponent<RectTransform>();
            skipRt.anchorMin = skipRt.anchorMax = new Vector2(0.5f, 0f);
            skipRt.sizeDelta = new Vector2(120, DesertTheme.ButtonHeight);
            skipRt.anchoredPosition = new Vector2(-90, 30);
            skipBtn.GetComponent<Image>().color = DesertTheme.BtnSecondary;

            _tutorialOverlay.SetActive(false);
        }

        private IEnumerator RunTutorial()
        {
            _tutorialActive = true;
            _tutorialSkip = false;
            _tutorialOverlay.SetActive(true);
            _tutorialOverlay.transform.SetAsLastSibling();

            var steps = new (string label, string text, System.Func<List<RectTransform>> targets)[]
            {
                ("Step 1 of 3", "Tap a piece, then tap a matching piece to swap them. Bring identical crystals together!", GetMergePairTargets),
                ("Step 2 of 3", "Line up 3 identical crystals to merge them into a stronger tier — that grows your squad power.", GetBoardTargets),
                ("Step 3 of 3", "When your squad is ready, tap the FIGHT button to battle the incoming wave!", GetFightButtonTargets),
            };

            foreach (var step in steps)
            {
                if (_tutorialSkip) break;
                _tutorialStepLabel.text = step.label;
                _tutorialText.text = step.text;
                SetTutorialHighlights(step.targets());
                _tutorialNext = false;
                while (!_tutorialNext && !_tutorialSkip)
                {
                    PulseTutorialHighlights();
                    yield return null;
                }
            }

            EndTutorial();
        }

        private void EndTutorial()
        {
            ClearTutorialHighlights();
            if (_tutorialOverlay != null) _tutorialOverlay.SetActive(false);
            _tutorialActive = false;
            PlayerPrefs.SetInt(TutorialDoneKey, 1);
            PlayerPrefs.Save();
        }

        private List<RectTransform> GetFightButtonTargets()
        {
            var list = new List<RectTransform>();
            if (_fightButton != null) list.Add(_fightButton.GetComponent<RectTransform>());
            return list;
        }

        private List<RectTransform> GetBoardTargets()
        {
            var list = new List<RectTransform>();
            var container = _cells.Count > 0 ? _cells[0].transform.parent.parent : null;
            if (container is RectTransform rt) list.Add(rt);
            return list;
        }

        // Finds two board cells holding identical pieces (best teaching example), with fallbacks.
        private List<RectTransform> GetMergePairTargets()
        {
            var list = new List<RectTransform>();
            var byId = new Dictionary<string, CellView>();
            foreach (var c in _cells)
            {
                var id = _session.Board.Get(c.X, c.Y);
                if (string.IsNullOrEmpty(id)) continue;
                if (byId.TryGetValue(id, out var first))
                {
                    list.Add(first.GetComponent<RectTransform>());
                    list.Add(c.GetComponent<RectTransform>());
                    return list;
                }
                byId[id] = c;
            }
            // Fallback: highlight the first couple of filled cells, else the whole board.
            foreach (var c in _cells)
            {
                if (string.IsNullOrEmpty(_session.Board.Get(c.X, c.Y))) continue;
                list.Add(c.GetComponent<RectTransform>());
                if (list.Count == 2) return list;
            }
            return list.Count > 0 ? list : GetBoardTargets();
        }

        private void SetTutorialHighlights(List<RectTransform> targets)
        {
            ClearTutorialHighlights();
            if (targets == null) return;
            foreach (var target in targets)
            {
                if (target == null) continue;
                var go = Ui("Highlight", _tutorialHighlightHolder.transform);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = target.rect.size + new Vector2(18f, 18f);
                rt.position = target.position;
                var img = go.AddComponent<Image>();
                if (panelButtonSprite != null) { img.sprite = panelButtonSprite; img.type = Image.Type.Sliced; }
                img.color = new Color(DesertTheme.AccentGold.r, DesertTheme.AccentGold.g, DesertTheme.AccentGold.b, 0.35f);
                img.raycastTarget = false;
                _tutorialHighlights.Add(img);
            }
        }

        private void PulseTutorialHighlights()
        {
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            foreach (var img in _tutorialHighlights)
            {
                if (img == null) continue;
                var a = Mathf.Lerp(0.22f, 0.5f, pulse);
                var c = img.color; c.a = a; img.color = c;
                var s = Mathf.Lerp(1.0f, 1.08f, pulse);
                img.rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }

        private void ClearTutorialHighlights()
        {
            foreach (var img in _tutorialHighlights)
                if (img != null) Destroy(img.gameObject);
            _tutorialHighlights.Clear();
        }

        private void BuildFightResultPanel(Transform parent)
        {
            _fightResultPanel = Ui("FightResultPanel", parent);
            var bg = _fightResultPanel.AddComponent<Image>();
            if (panelBackgroundSprite != null) { bg.sprite = panelBackgroundSprite; bg.type = Image.Type.Sliced; bg.color = Color.white; }
            else bg.color = DesertTheme.PanelDark;
            var r = _fightResultPanel.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.08f, 0.22f);
            r.anchorMax = new Vector2(0.92f, 0.78f);
            r.offsetMin = r.offsetMax = Vector2.zero;

            _fightResultTitle = Label("ResultTitle", _fightResultPanel.transform, new Vector2(0.5f, 0.90f), new Vector2(0.5f, 0.90f), Vector2.zero, 32);
            var enemyPortraitGo = Ui("EnemyPortrait", _fightResultPanel.transform);
            var enemyPortraitRt = enemyPortraitGo.GetComponent<RectTransform>();
            enemyPortraitRt.anchorMin = enemyPortraitRt.anchorMax = new Vector2(0.5f, 0.74f);
            enemyPortraitRt.sizeDelta = new Vector2(86, 86);
            enemyPortraitRt.anchoredPosition = Vector2.zero;
            _enemyPortraitImage = enemyPortraitGo.AddComponent<Image>();
            _enemyPortraitImage.preserveAspect = true;
            _enemyPortraitImage.raycastTarget = false;
            _enemyPortraitImage.enabled = false;

            // Power-race bars: "Your Squad" (gold) vs the wave (red).
            (_playerBarFill, _playerPowerLabel) = MakePowerBar(_fightResultPanel.transform, 0.55f, "Your Squad", DesertTheme.AccentGold);
            (_enemyBarFill, _enemyPowerLabel) = MakePowerBar(_fightResultPanel.transform, 0.42f, "The Wave", DesertTheme.BtnDanger);

            _fightResultSubtitle = Label("ResultSubtitle", _fightResultPanel.transform, new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.30f), Vector2.zero, DesertTheme.FontSizeHeading);
            _fightResultRewards = Label("ResultRewards", _fightResultPanel.transform, new Vector2(0.5f, 0.19f), new Vector2(0.5f, 0.19f), Vector2.zero, DesertTheme.FontSizeBody);
            _fightResultRewards.color = DesertTheme.AccentGold;
            _fightResultRewards.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 32);

            _fightContinueButton = ButtonGo("Continue", _fightResultPanel.transform, new Vector2(0, -78), () =>
            {
                if (_resolvingFight) return; // ignore taps while the sequence is animating
                _fightResultPanel.SetActive(false);
                if (_lastFightWasLoss)
                {
                    _lastFightWasLoss = false;
                    EndRun();
                }
            }, panelButtonSprite);
            _fightContinueButton.GetComponent<Image>().color = DesertTheme.AccentGold;
            _fightContinueButton.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
            _fightResultPanel.SetActive(false);
        }

        // Builds a labeled horizontal power bar; returns the fill Image and the value label.
        private (Image fill, TMP_Text value) MakePowerBar(Transform parent, float anchorY, string caption, Color fillColor)
        {
            var row = Ui($"PowerBar_{caption}", parent);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.10f, anchorY);
            rowRt.anchorMax = new Vector2(0.90f, anchorY);
            rowRt.sizeDelta = new Vector2(0, 46);
            rowRt.anchoredPosition = Vector2.zero;

            var caps = Label("Caption", row.transform, new Vector2(0, 0.55f), new Vector2(1, 1f), Vector2.zero, DesertTheme.FontSizeCaption, TextAlignmentOptions.Left);
            caps.text = caption;
            caps.color = DesertTheme.TextSecondary;
            caps.GetComponent<RectTransform>().offsetMin = new Vector2(2, 0);

            var track = Ui("Track", row.transform);
            var trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0, 0f);
            trackRt.anchorMax = new Vector2(0.78f, 0.5f);
            trackRt.offsetMin = new Vector2(2, 2);
            trackRt.offsetMax = new Vector2(0, -2);
            var trackImg = track.AddComponent<Image>();
            trackImg.color = new Color(0f, 0f, 0f, 0.45f);
            trackImg.raycastTarget = false;

            var fillGo = Ui("Fill", track.transform);
            Stretch(fillGo.GetComponent<RectTransform>());
            var fill = fillGo.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            var value = Label("Value", row.transform, new Vector2(0.80f, 0f), new Vector2(1f, 0.5f), Vector2.zero, DesertTheme.FontSizeBody, TextAlignmentOptions.Right);
            value.text = "0";
            value.color = fillColor;
            return (fill, value);
        }

        private void BuildMeta(Transform parent)
        {
            _metaPanel = Ui("MetaPanel", parent);
            var bg = _metaPanel.AddComponent<Image>();
            if (panelBackgroundSprite != null) { bg.sprite = panelBackgroundSprite; bg.type = Image.Type.Sliced; bg.color = Color.white; }
            else bg.color = DesertTheme.PanelMid;
            var r = _metaPanel.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.06f, 0.15f);
            r.anchorMax = new Vector2(0.94f, 0.85f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            var metaTitle = Label("MetaTitle", _metaPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), DesertTheme.FontSizeTitle);
            metaTitle.text = "Merchant Tent";
            metaTitle.color = DesertTheme.AccentGold;
            var metaSubtitle = Label("MetaSubtitle", _metaPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -54), DesertTheme.FontSizeCaption);
            metaSubtitle.text = "Upgrade your caravan for the journey ahead";
            metaSubtitle.color = DesertTheme.TextSecondary;
            Button("Upgrade Spawn Capacity", _metaPanel.transform, new Vector2(0, 24), UpgradeSpawn, panelButtonSprite);
            Button("Upgrade Starting Chance", _metaPanel.transform, new Vector2(0, -30), UpgradeChance, panelButtonSprite);
            Button("Unlock Next Board", _metaPanel.transform, new Vector2(0, -84), UnlockNextBoard, panelButtonSprite);
            Button("Caravan Routes", _metaPanel.transform, new Vector2(0, -138), () => { CloseMeta(); OpenBoardSelect(); }, panelButtonSprite);
            var returnBtn = ButtonGo("Return", _metaPanel.transform, new Vector2(0, -192), CloseMeta, panelButtonSprite);
            returnBtn.GetComponent<Image>().color = DesertTheme.BtnSecondary;
            _metaPanel.SetActive(false);
        }

        private void BuildBoardSelectPanel(Transform parent)
        {
            _boardSelectPanel = Ui("BoardSelectPanel", parent);
            var bg = _boardSelectPanel.AddComponent<Image>();
            if (panelBackgroundSprite != null) { bg.sprite = panelBackgroundSprite; bg.type = Image.Type.Sliced; bg.color = Color.white; }
            else bg.color = DesertTheme.PanelBoardSelect;
            var r = _boardSelectPanel.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.08f, 0.12f);
            r.anchorMax = new Vector2(0.92f, 0.88f);
            r.offsetMin = r.offsetMax = Vector2.zero;

            var title = Label("BoardSelectTitle", _boardSelectPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), DesertTheme.FontSizeHeading);
            title.text = "Caravan Routes";
            title.color = DesertTheme.AccentGold;

            var viewport = Ui("BoardListViewport", _boardSelectPanel.transform);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.03f, 0.10f);
            viewportRect.anchorMax = new Vector2(0.97f, 0.92f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var scrollContent = Ui("BoardListContent", viewport.transform);
            var contentRect = scrollContent.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);
            var layout = scrollContent.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(4, 4, 4, 4);
            var contentSizeFitter = scrollContent.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scrollRect = _boardSelectPanel.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;

            for (var i = 0; i < _boardCatalog.Count; i++)
            {
                var index = i;
                var board = _boardCatalog.Get(i);
                if (board == null) continue;

                var entry = Ui($"BoardEntry_{i}", scrollContent.transform);
                var entryBg = entry.AddComponent<Image>();
                var isUnlocked = index < _session.UnlockedBoardCount;
                var isCurrent = index == _session.CurrentBoardIndex;
                entryBg.color = isCurrent ? DesertTheme.BoardEntryCurrent : (isUnlocked ? DesertTheme.BoardEntryUnlocked : DesertTheme.BoardEntryLocked);
                var entryRect = entry.GetComponent<RectTransform>();
                entryRect.sizeDelta = new Vector2(0, 56);

                var nameLabel = Label("Name", entry.transform, new Vector2(0, 0.55f), new Vector2(1, 1), new Vector2(0, 0), DesertTheme.FontSizeCaption, TextAlignmentOptions.TopLeft);
                nameLabel.text = $"{board.DisplayName}  <color=#D4A545>[{board.DifficultyLabel}]</color>";
                nameLabel.GetComponent<RectTransform>().offsetMin = new Vector2(14, 4);

                if (isUnlocked)
                {
                    var statusText = isCurrent ? "Current" : "Select";
                    var btn = ButtonGo(statusText, entry.transform, new Vector2(0, -26), () =>
                    {
                        if (_session.SelectBoard(index))
                        {
                            SetStatus($"Playing: {board.DisplayName}");
                            _boardSelectPanel.SetActive(false);
                            if (_boardSelectForStartRun)
                            {
                                _boardSelectForStartRun = false;
                                StartRunInternal();
                            }
                            else
                                RefreshAll();
                        }
                    }, panelButtonSprite);
                    var btnRect = btn.GetComponent<RectTransform>();
                    btnRect.anchorMin = new Vector2(0.82f, 0.5f);
                    btnRect.anchorMax = new Vector2(0.98f, 0.5f);
                    btnRect.anchoredPosition = new Vector2(0, -26);
                    btnRect.sizeDelta = new Vector2(100, 30);
                }
                else
                {
                    var unlockHint = GetUnlockHint(board);
                    var hintLabel = Label("UnlockHint", entry.transform, new Vector2(0, 0.1f), new Vector2(1, 0.45f), new Vector2(0, 0), 11, TextAlignmentOptions.TopLeft);
                    hintLabel.text = unlockHint;
                    hintLabel.color = DesertTheme.BoardEntryLockedText;
                    hintLabel.GetComponent<RectTransform>().offsetMin = new Vector2(14, 2);
                    var unlockBtn = ButtonGo("Unlock", entry.transform, new Vector2(0, -26), () => UnlockBoardAtIndex(index), panelButtonSprite);
                    var unlockBtnRect = unlockBtn.GetComponent<RectTransform>();
                    unlockBtnRect.anchorMin = new Vector2(0.72f, 0.5f);
                    unlockBtnRect.anchorMax = new Vector2(0.98f, 0.5f);
                    unlockBtnRect.anchoredPosition = new Vector2(0, -26);
                    unlockBtnRect.sizeDelta = new Vector2(110, 30);
                    unlockBtn.name = "UnlockBtn";
                }
            }

            Button("Close", _boardSelectPanel.transform, new Vector2(0, -30), () => { _boardSelectPanel.SetActive(false); RefreshAll(); }, panelButtonSprite);
            _boardSelectPanel.SetActive(false);
        }

        private string GetUnlockHint(BoardDefinition board)
        {
            if (board.UnlockObjectiveWave > 0 && _progression.Data.HighestWave >= board.UnlockObjectiveWave)
                return "Unlock (objective complete)";
            if (board.UnlockCostResource > 0)
                return $"Unlock: {board.UnlockCostResource} resource";
            return "Locked";
        }

        private void UnlockBoardAtIndex(int index)
        {
            if (index != _session.UnlockedBoardCount)
            {
                SetStatus("Unlock previous boards first.");
                return;
            }
            var nextBoard = _boardCatalog.Get(index);
            if (nextBoard == null) return;
            var canUnlockByObjective = nextBoard.UnlockObjectiveWave > 0 && _progression.Data.HighestWave >= nextBoard.UnlockObjectiveWave;
            if (!canUnlockByObjective && !_inventory.Spend(CurrencyType.ProgressionResource, nextBoard.UnlockCostResource))
            {
                SetStatus($"Need {nextBoard.UnlockCostResource} resource.");
                return;
            }
            if (_session.TryUnlockNextBoard())
            {
                SaveAccount();
                _progression.Save();
                SetStatus(canUnlockByObjective ? $"Unlocked {nextBoard.DisplayName}!" : $"Unlocked {nextBoard.DisplayName}");
                RefreshBoardSelectEntries();
                RefreshAll();
            }
        }

        private void OpenBoardSelect(bool forStartRun = false)
        {
            _boardSelectForStartRun = forStartRun;
            RefreshBoardSelectEntries();
            _boardSelectPanel.SetActive(true);
        }

        private void OnStartRun()
        {
            OpenBoardSelect(forStartRun: true);
        }

        private void OnEndRun()
        {
            EndRun();
        }

        private void StartRunInternal()
        {
            _session.ResetRun();
            for (var i = 0; i < 6; i++) _session.TrySpawn();
            _isInRun = true;
            _audio.PlayMusic(AudioManager.MusicGameplay);
            SetStatus($"Run started: {_session.CurrentBoard?.DisplayName}. Merge & fight!");
            RefreshAll();
            RefreshRunStateUI();
            if (!_tutorialActive && PlayerPrefs.GetInt(TutorialDoneKey, 0) == 0)
                StartCoroutine(RunTutorial());
        }

        private void EndRun()
        {
            _session.ResetRun();
            _isInRun = false;
            _audio.PlayMusic(AudioManager.MusicHub);
            SetStatus("Returned to hub. Start a new run when ready.");
            SaveAccount();
            RefreshAll();
            RefreshRunStateUI();
        }

        private void RefreshRunStateUI()
        {
            if (_startRunButton != null) _startRunButton.SetActive(!_isInRun);
            if (_endRunButton != null) _endRunButton.SetActive(_isInRun);
            if (_spawnButton != null) _spawnButton.SetActive(_isInRun);
            if (_fightButton != null) _fightButton.SetActive(_isInRun);
            if (_nextBoardButton != null) _nextBoardButton.SetActive(_isInRun && _session.UnlockedBoardCount > 1);
            if (_boardSelectButton != null) _boardSelectButton.SetActive(_isInRun);
        }

        private void RefreshBoardSelectEntries()
        {
            if (_boardSelectPanel == null) return;
            var content = _boardSelectPanel.transform.Find("BoardListViewport/BoardListContent");
            if (content == null) return;
            for (var i = 0; i < content.childCount; i++)
            {
                var entry = content.GetChild(i);
                var bg = entry.GetComponent<Image>();
                if (bg == null) continue;
                var isUnlocked = i < _session.UnlockedBoardCount;
                var isCurrent = i == _session.CurrentBoardIndex;
                bg.color = isCurrent ? DesertTheme.BoardEntryCurrent : (isUnlocked ? DesertTheme.BoardEntryUnlocked : DesertTheme.BoardEntryLocked);
                var selectBtn = entry.GetComponentInChildren<Button>();
                if (selectBtn != null)
                {
                    var txt = selectBtn.GetComponentInChildren<TMP_Text>();
                    if (txt != null)
                    {
                        if (isUnlocked)
                            txt.text = isCurrent ? "Current" : "Select";
                        else
                        {
                            var board = _boardCatalog.Get(i);
                            var canUnlockByObjective = board != null && board.UnlockObjectiveWave > 0 && _progression.Data.HighestWave >= board.UnlockObjectiveWave;
                            selectBtn.interactable = i == _session.UnlockedBoardCount;
                            txt.text = i == _session.UnlockedBoardCount ? (canUnlockByObjective ? "Unlock (free)" : $"Unlock ({board.UnlockCostResource})") : "\u2014";
                        }
                    }
                }
            }
        }

        private void OnSpawn()
        {
            SetStatus(_session.TrySpawn() ? "Spawned." : "Spawn failed.");
            RefreshAll();
        }

        private void OnFight()
        {
            if (_resolvingFight) return;
            _audio.PlaySfx(AudioManager.SfxFightStart);
            var r = _session.Fight();
            _lastFightWasLoss = !r.Won;
            SaveAccount();
            RefreshAll();
            ShowFightResult(r);
        }

        private void ShowFightResult(CombatResult r)
        {
            // Set up the static parts, then hand off to the animated resolution.
            if (_enemyPortraitImage != null)
            {
                var portrait = GetEnemyPortraitSprite(_session.CurrentEnemyArchetype?.Id);
                _enemyPortraitImage.sprite = portrait;
                _enemyPortraitImage.enabled = portrait != null;
            }
            StartCoroutine(AnimateFightResolution(r));
        }

        // 2.5s suspenseful power-race: bars + numbers count up, brief clash, then the verdict.
        private IEnumerator AnimateFightResolution(CombatResult r)
        {
            _resolvingFight = true;
            var playerPower = Mathf.Max(0, r.PlayerPower);
            var enemyPower = Mathf.Max(0, r.EnemyPower);
            var maxPower = Mathf.Max(1, Mathf.Max(playerPower, enemyPower));

            // Reset to a neutral "battle starting" state.
            _fightResultTitle.text = "Battle!";
            _fightResultTitle.color = DesertTheme.AccentGold;
            _fightResultTitle.rectTransform.localScale = Vector3.one;
            _fightResultSubtitle.text = "The clash begins\u2026";
            _fightResultSubtitle.color = DesertTheme.TextSecondary;
            _fightResultRewards.text = string.Empty;
            if (_playerBarFill != null) _playerBarFill.fillAmount = 0f;
            if (_enemyBarFill != null) _enemyBarFill.fillAmount = 0f;
            if (_playerPowerLabel != null) _playerPowerLabel.text = "0";
            if (_enemyPowerLabel != null) _enemyPowerLabel.text = "0";
            if (_fightContinueButton != null) _fightContinueButton.SetActive(false);
            _fightResultPanel.SetActive(true);

            yield return new WaitForSecondsRealtime(0.25f);

            // Count-up race (eased), bars normalized to the stronger side.
            const float raceTime = 1.4f;
            var elapsed = 0f;
            while (elapsed < raceTime)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / raceTime);
                var e = 1f - Mathf.Pow(1f - t, 3f); // easeOutCubic
                if (_playerBarFill != null) _playerBarFill.fillAmount = (playerPower / (float)maxPower) * e;
                if (_enemyBarFill != null) _enemyBarFill.fillAmount = (enemyPower / (float)maxPower) * e;
                if (_playerPowerLabel != null) _playerPowerLabel.text = Mathf.RoundToInt(playerPower * e).ToString();
                if (_enemyPowerLabel != null) _enemyPowerLabel.text = Mathf.RoundToInt(enemyPower * e).ToString();
                yield return null;
            }
            if (_playerPowerLabel != null) _playerPowerLabel.text = playerPower.ToString();
            if (_enemyPowerLabel != null) _enemyPowerLabel.text = enemyPower.ToString();

            // Suspense beat with a flash on the winning bar.
            var winnerBar = r.Won ? _playerBarFill : _enemyBarFill;
            var winnerColor = r.Won ? DesertTheme.AccentGold : DesertTheme.BtnDanger;
            for (var f = 0; f < 3; f++)
            {
                if (winnerBar != null) winnerBar.color = Color.white;
                yield return new WaitForSecondsRealtime(0.08f);
                if (winnerBar != null) winnerBar.color = winnerColor;
                yield return new WaitForSecondsRealtime(0.08f);
            }

            // Verdict: title pop + color, rewards, audio, continue button.
            _audio.PlaySfx(r.Won ? AudioManager.SfxWin : AudioManager.SfxLoss, r.Won ? sfxFightWin : sfxFightLoss);
            _fightResultTitle.text = r.Won ? "Victory!" : "Defeat";
            _fightResultTitle.color = r.Won ? DesertTheme.TextVictory : DesertTheme.TextDefeat;
            _fightResultSubtitle.text = r.Won ? "Your caravan prevails!" : "The wave overwhelms you\u2026";
            _fightResultSubtitle.color = DesertTheme.TextPrimary;
            if (r.Won && _combatConfig != null)
                _fightResultRewards.text = $"+{_combatConfig.WinSoft} <color=#E5BA42>\u2726</color>  +{_combatConfig.WinResource} <color=#D9A050>\u2B22</color>";
            else
                _fightResultRewards.text = r.Won ? "+rewards" : "Merge more pieces, then Fight again.";
            _fightResultRewards.color = r.Won ? DesertTheme.AccentGold : DesertTheme.TextSecondary;

            // Title bounce.
            var pop = 0f;
            const float popTime = 0.3f;
            while (pop < popTime)
            {
                pop += Time.unscaledDeltaTime;
                var s = 1f + 0.35f * (1f - Mathf.Clamp01(pop / popTime));
                _fightResultTitle.rectTransform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _fightResultTitle.rectTransform.localScale = Vector3.one;

            if (_fightContinueButton != null) _fightContinueButton.SetActive(true);
            _resolvingFight = false;
        }

        private Sprite GetEnemyPortraitSprite(string archetypeId)
        {
            if (string.IsNullOrEmpty(archetypeId)) return null;
            switch (archetypeId)
            {
                case "grunt":
                    return enemyPortraitGrunt != null ? enemyPortraitGrunt : Resources.Load<Sprite>("Enemies/bandit");
                case "shield":
                    return enemyPortraitShield != null ? enemyPortraitShield : Resources.Load<Sprite>("Enemies/gold-scarab");
                case "berserk":
                    return enemyPortraitBerserk != null ? enemyPortraitBerserk : Resources.Load<Sprite>("Enemies/barbarian");
                default:
                    return null;
            }
        }

        private void OpenMeta() => _metaPanel.SetActive(true);
        private void CloseMeta() => _metaPanel.SetActive(false);

        private void OnNextBoard()
        {
            if (_session.SelectNextUnlockedBoard())
            {
                SetStatus($"Board switched to {_session.CurrentBoard?.DisplayName}.");
            }
            else
            {
                SetStatus("Unlock another board first.");
            }

            RefreshAll();
        }

        private void UpgradeSpawn()
        {
            var cost = _progression.SpawnUpgradeCost();
            if (_inventory.Spend(CurrencyType.Soft, cost))
            {
                _progression.UpgradeSpawnCapacity();
                _progression.Save();
                SaveAccount();
                SetStatus($"Spawn upgraded ({cost}).");
            }
            else SetStatus($"Need {cost} soft.");
            RefreshAll();
        }

        private void UpgradeChance()
        {
            var cost = _progression.ChanceUpgradeCost();
            if (_inventory.Spend(CurrencyType.Soft, cost))
            {
                _progression.UpgradeStartingChance();
                _progression.Save();
                SaveAccount();
                SetStatus($"Chance upgraded ({cost}).");
            }
            else SetStatus($"Need {cost} soft.");
            RefreshAll();
        }

        private void UnlockNextBoard()
        {
            var nextBoard = GetNextBoardToUnlock();
            if (nextBoard == null)
            {
                SetStatus("All boards already unlocked.");
                return;
            }

            var canUnlockByObjective = nextBoard.UnlockObjectiveWave > 0 && _progression.Data.HighestWave >= nextBoard.UnlockObjectiveWave;
            if (!canUnlockByObjective)
            {
                var cost = nextBoard.UnlockCostResource;
                if (!_inventory.Spend(CurrencyType.ProgressionResource, cost))
                {
                    SetStatus($"Need {cost} resource to unlock next board.");
                    RefreshAll();
                    return;
                }
            }

            if (_session.TryUnlockNextBoard())
            {
                SaveAccount();
                _progression.Save();
                SetStatus(canUnlockByObjective ? $"Unlocked {nextBoard.DisplayName} (objective complete!)" : $"Unlocked board: {nextBoard.DisplayName}");
            }
            else
            {
                SetStatus("Board unlock failed.");
            }

            RefreshAll();
        }

        private void StartDrag(int x, int y)
        {
            if (_session.Board.IsEmpty(x, y)) return;
            _selected = (x, y);
            _audio.PlaySfx(AudioManager.SfxPickup);
        }

        private void TapCell(int x, int y)
        {
            if (_selected == null)
            {
                if (_session.Board.IsEmpty(x, y)) return;
                _selected = (x, y);
                _audio.PlaySfx(AudioManager.SfxPickup);
                return;
            }
            DropOn(x, y);
        }

        private void DropOn(int x, int y)
        {
            if (_selected == null) return;
            var src = _selected.Value;
            _selected = null;
            if (src.x == x && src.y == y) return;
            _session.Board.Swap(src, (x, y));
            var m = _session.TryMergeAt(x, y);
            SetStatus(m.Success ? $"Merged to T{m.UpgradedTier}" : "Moved.");
            if (m.Success)
                StartCoroutine(PlayMergeFeedback(x, y));
            SaveAccount();
            RefreshAll();
        }

        private IEnumerator PlayMergeFeedback(int x, int y)
        {
            _audio.PlaySfx(AudioManager.SfxMerge, sfxMerge);
            var cv = _cells.Find(c => c.X == x && c.Y == y);
            if (cv == null) yield break;
            yield return cv.PlayMergeHighlight();
        }

        private void RefreshAll()
        {
            foreach (var c in _cells)
            {
                var id = _session.Board.Get(c.X, c.Y);
                if (string.IsNullOrEmpty(id))
                {
                    c.Set("-");
                    c.SetPiece(null);
                }
                else
                {
                    var d = _items.GetById(id);
                    var tier = d?.Tier ?? 1;
                    c.Set(d == null ? id : $"{d.DisplayName}\nT{tier}");
                    c.SetPiece(tier, id);
                }
            }

            _waveLabel.text = $"Wave {_session.CurrentWave}";
            _softLabel.text = _inventory.Get(CurrencyType.Soft).ToString();
            _premiumLabel.text = _inventory.Get(CurrencyType.Premium).ToString();
            _resourceLabel.text = _inventory.Get(CurrencyType.ProgressionResource).ToString();
            _hud.text = $"Wave {_session.CurrentWave}  |  Soft {_inventory.Get(CurrencyType.Soft)}  Premium {_inventory.Get(CurrencyType.Premium)}  Resource {_inventory.Get(CurrencyType.ProgressionResource)}";
            if (_boardBackgroundImage != null && boardBackgroundCatalog != null)
            {
                var bgSprite = boardBackgroundCatalog.GetBackground(_session.CurrentBoardIndex);
                if (bgSprite != null) { _boardBackgroundImage.sprite = bgSprite; _boardBackgroundImage.color = Color.white; }
                else { _boardBackgroundImage.sprite = null; _boardBackgroundImage.color = DesertTheme.BgSecondary; }
            }
            var currentBoard = _session.CurrentBoard;
            _boardHud.text = currentBoard == null
                ? "No board configured."
                : $"Board {(_session.CurrentBoardIndex + 1)}/{_session.UnlockedBoardCount}: {currentBoard.DisplayName} ({_session.CurrentEnemyArchetype?.DisplayName ?? "No Archetype"})";
            RefreshRunStateUI();
        }

        private void SaveAccount()
        {
            PlayerPrefs.SetString("merge_survivor_account_v1", JsonUtility.ToJson(_inventory.Data));
            PlayerPrefs.Save();
        }

        private void SetStatus(string msg) => _status.text = msg;

        private BoardDefinition GetNextBoardToUnlock()
        {
            if (_boardCatalog == null || _boardCatalog.Count == 0)
            {
                return null;
            }

            if (_session.UnlockedBoardCount >= _boardCatalog.Count)
            {
                return null;
            }

            return _boardCatalog.Get(_session.UnlockedBoardCount);
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            var legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Destroy(legacy);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        private static void EnsureFallbackCamera()
        {
            if (Camera.main != null || FindFirstObjectByType<Camera>() != null)
            {
                return;
            }

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = DesertTheme.CameraBg;
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static Canvas EnsureCanvas()
        {
            var found = FindFirstObjectByType<Canvas>();
            if (found != null) return found;
            var go = new GameObject("Canvas");
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        private static GameObject Ui(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        private static TMP_Text Label(string name, Transform parent, Vector2 amin, Vector2 amax, Vector2 apos, float size, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = Ui(name, parent);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = amin;
            r.anchorMax = amax;
            r.anchoredPosition = apos;
            r.sizeDelta = new Vector2(0, 28);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = align;
            t.color = DesertTheme.TextPrimary;
            t.enableAutoSizing = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.richText = true;
            t.raycastTarget = false;
            return t;
        }

        private static void Button(string title, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction click, Sprite buttonSprite = null)
        {
            ButtonGo(title, parent, pos, click, buttonSprite);
        }

        private static GameObject ButtonGo(string title, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction click, Sprite buttonSprite = null)
        {
            var go = Ui($"Btn_{title}", parent);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(DesertTheme.ButtonWidth, DesertTheme.ButtonHeight);
            r.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            if (buttonSprite != null)
            {
                img.sprite = buttonSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = DesertTheme.BtnPrimary;
            var b = go.AddComponent<Button>();
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            b.colors = colors;
            b.onClick.AddListener(click);
            var label = Label("Text", go.transform, Vector2.zero, Vector2.one, Vector2.zero, DesertTheme.FontSizeCaption);
            label.text = title;
            label.alignment = TextAlignmentOptions.Center;
            return go;
        }
    }

    public sealed class CellView : MonoBehaviour, IBeginDragHandler, IDropHandler, IPointerClickHandler
    {
        private TMP_Text _label;
        private Image _pieceImage;
        private RectTransform _pieceRect;
        private ItemVisualCatalog _visualCatalog;
        private GameObject _mergeVfxPrefab;
        public int X { get; private set; }
        public int Y { get; private set; }
        public System.Action<int, int> OnDragStart;
        public System.Action<int, int> OnDropOn;
        public System.Action<int, int> OnTap;

        public void Bind(int x, int y, TMP_Text label, Image pieceImage = null, ItemVisualCatalog catalog = null, GameObject mergeVfxPrefab = null)
        {
            X = x; Y = y; _label = label;
            _pieceImage = pieceImage;
            _pieceRect = pieceImage != null ? pieceImage.GetComponent<RectTransform>() : null;
            _visualCatalog = catalog;
            _mergeVfxPrefab = mergeVfxPrefab;
        }

        public void Set(string txt) => _label.text = txt;

        public void SetPiece(int? tier, string itemId = null)
        {
            if (_pieceImage == null) return;
            if (!tier.HasValue)
            {
                _pieceImage.enabled = false;
                _pieceImage.sprite = null;
                return;
            }
            _pieceImage.enabled = true;

            if (_visualCatalog != null && _visualCatalog.TryGetSprite(itemId, out var sprite, out var tint))
            {
                _pieceImage.sprite = sprite;
                _pieceImage.color = tint;
                _pieceImage.preserveAspect = true;
            }
            else if (_visualCatalog != null && _visualCatalog.TryGetTierFallback(tier.Value, out var fbSprite, out var fbTint))
            {
                _pieceImage.sprite = fbSprite;
                _pieceImage.color = fbTint;
                _pieceImage.preserveAspect = true;
            }
            else
            {
                _pieceImage.sprite = null;
                _pieceImage.color = DesertTheme.TierColor(tier.Value);
            }
        }

        public void OnBeginDrag(PointerEventData eventData) => OnDragStart?.Invoke(X, Y);
        public void OnDrop(PointerEventData eventData) => OnDropOn?.Invoke(X, Y);
        public void OnPointerClick(PointerEventData eventData) => OnTap?.Invoke(X, Y);

        public IEnumerator PlayMergeHighlight()
        {
            if (_pieceImage == null || _pieceRect == null) yield break;
            if (_mergeVfxPrefab != null)
            {
                var vfx = Instantiate(_mergeVfxPrefab, transform);
                vfx.transform.localPosition = Vector3.zero;
                vfx.transform.localScale = Vector3.one * 45f;
                var ps = vfx.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.playOnAwake = false;
                    var psr = vfx.GetComponent<ParticleSystemRenderer>();
                    if (psr != null) psr.sortingOrder = 32000;
                    ps.Clear();
                    ps.Play();
                    var startLt = main.startLifetime;
                    var maxLife = Mathf.Max(startLt.constantMax, startLt.constant, startLt.constantMin);
                    if (maxLife <= 0f) maxLife = 0.5f;
                    Destroy(vfx, main.duration + maxLife + 0.2f);
                }
                else Destroy(vfx, 1f);
            }
            var origColor = _pieceImage.color;
            var origScale = _pieceRect.localScale;

            _pieceImage.color = Color.white;
            _pieceRect.localScale = origScale * 1.45f;
            yield return null;

            var elapsed = 0f;
            const float phase1 = 0.12f;
            while (elapsed < phase1)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / phase1;
                _pieceImage.color = Color.Lerp(Color.white, DesertTheme.AccentGold, t);
                _pieceRect.localScale = Vector3.Lerp(origScale * 1.45f, origScale * 0.88f, t);
                yield return null;
            }

            elapsed = 0f;
            const float phase2 = 0.18f;
            while (elapsed < phase2)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / phase2;
                var bounce = EaseOutBack(t);
                _pieceImage.color = Color.Lerp(DesertTheme.AccentGold, origColor, t);
                _pieceRect.localScale = Vector3.Lerp(origScale * 0.88f, origScale, bounce);
                yield return null;
            }

            _pieceImage.color = origColor;
            _pieceRect.localScale = origScale;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
