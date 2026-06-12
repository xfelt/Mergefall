using System.Collections.Generic;
using MergeSurvivor.Core;
using MergeSurvivor.Data;
using MergeSurvivor.Economy;
using MergeSurvivor.Meta;
using MergeSurvivor.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeSurvivor.UI
{
    /// <summary>
    /// Monetization + retention layer for the runtime UI: storefront (coin packs), Lucky Chest gacha,
    /// character collection, daily rewards, prestige, and the rewarded-ad surfaces (double-reward,
    /// free gems, free spin, revive). All built on the existing programmatic-UI helpers.
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        // --- Services / catalogs (constructed in SetupMonetizationServices) ---
        private IStoreService _store;
        private IAdsService _ads;
        private IAnalyticsService _analytics;
        private ICollectionService _collection;
        private IGachaService _gacha;
        private IDailyRewardService _daily;
        private IPrestigeService _prestige;
        private IStorefrontService _storefront;
        private CharacterCatalog _characterCatalog;
        private CoinPackCatalog _coinPackCatalog;
        private GachaConfig _gachaConfig;
        private DailyRewardTable _dailyTable;
        private PrestigeConfig _prestigeConfig;

        // --- Panels ---
        private GameObject _shopPanel;
        private GameObject _collectionPanel;
        private Transform _collectionContent;
        private GameObject _gachaPanel;
        private GameObject _dailyPanel;
        private Transform _dailyContent;
        private GameObject _prestigePanel;

        // --- Dynamic labels / buttons refreshed by RefreshMonetizationUI ---
        private TMP_Text _gachaPityLabel;
        private TMP_Text _gachaResultLabel;
        private TMP_Text _freeGemsLabel;
        private TMP_Text _prestigeInfoLabel;
        private Button _prestigeButton;

        // --- Fight-result rewarded-ad actions ---
        private GameObject _doubleRewardButton;
        private GameObject _reviveButton;
        private bool _doubleRewardClaimed;
        private int _revivesUsedThisRun;

        // --- Tuning ---
        private const int ReviveCapPerRun = 3;
        private const int FreeGemsDailyCap = 5;
        private const int FreeSpinDailyCap = 3;
        private const int FreeGemsPerAd = 25;
        private const int GemConvertCost = 10;     // spend this many gems...
        private const int GemConvertGold = 1000;   // ...to get this much gold

        // ----------------------------------------------------------------------------------
        // Service setup
        // ----------------------------------------------------------------------------------

        private void SetupMonetizationServices(LocalSaveService save, IAdsService ads, IStoreService store, IAnalyticsService analytics)
        {
            _ads = ads;
            _store = store;
            _analytics = analytics;

            _characterCatalog = GameContentLoader.LoadCharacterCatalog();
            if (_characterCatalog == null) _characterCatalog = BuildFallbackCharacterCatalog();
            else _characterCatalog.Warm();

            _coinPackCatalog = GameContentLoader.LoadCoinPackCatalog() ?? BuildFallbackCoinPackCatalog();
            _gachaConfig = GameContentLoader.LoadGachaConfig() ?? ScriptableObject.CreateInstance<GachaConfig>();
            _dailyTable = GameContentLoader.LoadDailyRewardTable() ?? BuildFallbackDailyTable();
            _prestigeConfig = GameContentLoader.LoadPrestigeConfig() ?? ScriptableObject.CreateInstance<PrestigeConfig>();

            _collection = new CollectionService(save, _characterCatalog);
            _gacha = new GachaService(_characterCatalog, _gachaConfig, _collection);
            _daily = new DailyRewardService(save, _dailyTable, _inventory);
            _prestige = new PrestigeService(save, _prestigeConfig);
            _storefront = new StorefrontService(_coinPackCatalog, _inventory, _collection);

            _store.RegisterProducts(_storefront.ProductIds());
            _store.PurchaseCompleted += OnPurchaseCompleted;
        }

        private void OnPurchaseCompleted(string productId)
        {
            var grant = _storefront.GrantPurchase(productId);
            if (!grant.Success)
            {
                SetStatus($"Purchase could not be granted: {productId}");
                return;
            }
            SaveAccount();
            _collection.Save();
            _analytics?.TrackEvent("iap_purchase", new Dictionary<string, object>
            {
                { "product_id", productId }, { "gems", grant.GemsGranted }
            });
            var bundle = string.IsNullOrEmpty(grant.BundleCharacterId) ? "" : " + a hero!";
            SetStatus($"Purchased! +{grant.GemsGranted} Gems{bundle}");
            RefreshAll();
        }

        // ----------------------------------------------------------------------------------
        // Fallback content (used when no authored Resources assets exist)
        // ----------------------------------------------------------------------------------

        private static CharacterDefinition MakeCharacter(string id, string name, CharacterRarity rarity, CharacterBonusType bonus, float value, int costGems)
        {
            var c = ScriptableObject.CreateInstance<CharacterDefinition>();
            c.ConfigureRuntime(id, name, rarity, bonus, value, costGems);
            return c;
        }

        private CharacterCatalog BuildFallbackCharacterCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            catalog.ConfigureRuntime(new List<CharacterDefinition>
            {
                // Common (6)
                MakeCharacter("hero_dust_wanderer", "Dust Wanderer", CharacterRarity.Common, CharacterBonusType.SquadPowerPercent, 0.05f, 150),
                MakeCharacter("hero_coin_tinker", "Coin Tinker", CharacterRarity.Common, CharacterBonusType.GoldGainPercent, 0.05f, 150),
                MakeCharacter("hero_merge_apprentice", "Merge Apprentice", CharacterRarity.Common, CharacterBonusType.MergeRewardPercent, 0.06f, 150),
                MakeCharacter("hero_pack_mule", "Pack Mule", CharacterRarity.Common, CharacterBonusType.ExtraSpawnCapacity, 1f, 150),
                MakeCharacter("hero_lucky_urchin", "Lucky Urchin", CharacterRarity.Common, CharacterBonusType.GachaLuckPercent, 0.03f, 150),
                MakeCharacter("hero_caravan_scout", "Caravan Scout", CharacterRarity.Common, CharacterBonusType.StartingGoldFlat, 50f, 150),
                // Rare (5)
                MakeCharacter("hero_sand_reaver", "Sand Reaver", CharacterRarity.Rare, CharacterBonusType.SquadPowerPercent, 0.12f, 450),
                MakeCharacter("hero_gold_vizier", "Gold Vizier", CharacterRarity.Rare, CharacterBonusType.GoldGainPercent, 0.12f, 450),
                MakeCharacter("hero_fusion_adept", "Fusion Adept", CharacterRarity.Rare, CharacterBonusType.MergeRewardPercent, 0.14f, 450),
                MakeCharacter("hero_beast_tamer", "Beast Tamer", CharacterRarity.Rare, CharacterBonusType.ExtraSpawnCapacity, 2f, 450),
                MakeCharacter("hero_fortune_seer", "Fortune Seer", CharacterRarity.Rare, CharacterBonusType.GachaLuckPercent, 0.06f, 450),
                // Epic (3)
                MakeCharacter("hero_dune_colossus", "Dune Colossus", CharacterRarity.Epic, CharacterBonusType.SquadPowerPercent, 0.22f, 1000),
                MakeCharacter("hero_mint_pasha", "Mint Pasha", CharacterRarity.Epic, CharacterBonusType.GoldGainPercent, 0.22f, 1000),
                MakeCharacter("hero_arcane_merger", "Arcane Merger", CharacterRarity.Epic, CharacterBonusType.MergeRewardPercent, 0.25f, 1000),
                // Legendary (2)
                MakeCharacter("hero_sun_pharaoh", "Sun Pharaoh", CharacterRarity.Legendary, CharacterBonusType.SquadPowerPercent, 0.40f, 2500),
                MakeCharacter("hero_genie_of_plenty", "Genie of Plenty", CharacterRarity.Legendary, CharacterBonusType.GoldGainPercent, 0.40f, 2500),
            });
            return catalog;
        }

        private CoinPackCatalog BuildFallbackCoinPackCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<CoinPackCatalog>();
            var packs = new List<CoinPack>();
            void Add(string id, string name, int gems, float bonus, string price, bool best = false, bool oneTime = false, string bundle = "")
            {
                var p = new CoinPack();
                p.ConfigureRuntime(id, name, gems, bonus, price, best, oneTime, bundle);
                packs.Add(p);
            }
            Add("gems_pouch", "Pouch of Gems", 100, 0f, "$0.99");
            Add("gems_stack", "Stack of Gems", 550, 0.10f, "$4.99");
            Add("gems_chest", "Chest of Gems", 1200, 0.20f, "$9.99");
            Add("gems_vault", "Vault of Gems", 2600, 0.30f, "$19.99", best: true);
            Add("gems_hoard", "Dragon's Hoard", 7000, 0.40f, "$49.99");
            Add("starter_bundle", "Starter Bundle", 500, 0f, "$2.99", oneTime: true, bundle: "hero_dune_colossus");
            catalog.ConfigureRuntime(packs);
            return catalog;
        }

        private DailyRewardTable BuildFallbackDailyTable()
        {
            var table = ScriptableObject.CreateInstance<DailyRewardTable>();
            table.ConfigureRuntime(new List<DailyRewardEntry>
            {
                new(100, 0, false),
                new(150, 0, false),
                new(200, 20, false),
                new(300, 0, false),
                new(400, 0, true),
                new(500, 40, false),
                new(1000, 100, true),
            });
            return table;
        }

        // ----------------------------------------------------------------------------------
        // Shared panel helpers
        // ----------------------------------------------------------------------------------

        private GameObject MakeModalPanel(string name, Transform parent, string title, out TMP_Text info)
        {
            var panel = Ui(name, parent);
            var bg = panel.AddComponent<Image>();
            if (panelBackgroundSprite != null) { bg.sprite = panelBackgroundSprite; bg.type = Image.Type.Sliced; bg.color = Color.white; }
            else bg.color = DesertTheme.PanelMid;
            var r = panel.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.06f, 0.10f);
            r.anchorMax = new Vector2(0.94f, 0.90f);
            r.offsetMin = r.offsetMax = Vector2.zero;

            var titleLabel = Label($"{name}Title", panel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), DesertTheme.FontSizeHeading);
            titleLabel.text = title;
            titleLabel.color = DesertTheme.AccentGold;

            info = Label($"{name}Info", panel.transform, new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.94f), Vector2.zero, DesertTheme.FontSizeCaption);
            info.color = DesertTheme.TextSecondary;

            var close = ButtonGo("Close", panel.transform, Vector2.zero, () => { panel.SetActive(false); RefreshAll(); }, panelButtonSprite);
            var closeRt = close.GetComponent<RectTransform>();
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0.045f);
            closeRt.sizeDelta = new Vector2(320, 68);
            closeRt.anchoredPosition = Vector2.zero;
            close.GetComponent<Image>().color = DesertTheme.BtnSecondary;

            panel.SetActive(false);
            return panel;
        }

        // Scrollable content area sized to sit between the info strip and the close button.
        private Transform MakeScrollContent(GameObject panel, float topAnchor = 0.84f)
        {
            var viewport = Ui("Viewport", panel.transform);
            var vr = viewport.GetComponent<RectTransform>();
            vr.anchorMin = new Vector2(0.03f, 0.13f);
            vr.anchorMax = new Vector2(0.97f, topAnchor);
            vr.offsetMin = vr.offsetMax = Vector2.zero;
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var content = Ui("Content", viewport.transform);
            var cr = content.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1);
            cr.anchorMax = new Vector2(1, 1);
            cr.pivot = new Vector2(0.5f, 1f);
            cr.offsetMin = cr.offsetMax = Vector2.zero;
            cr.sizeDelta = Vector2.zero;
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(4, 4, 4, 4);
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = panel.AddComponent<ScrollRect>();
            scroll.content = cr;
            scroll.viewport = vr;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 20f;
            return content.transform;
        }

        private GameObject MakeEntry(Transform content, float height, Color color)
        {
            var entry = Ui("Entry", content);
            entry.AddComponent<Image>().color = color;
            entry.GetComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            return entry;
        }

        private GameObject EntryButton(string label, Transform entry, float xMin, float xMax, UnityEngine.Events.UnityAction click, Color color)
        {
            var btn = ButtonGo(label, entry, Vector2.zero, click, panelButtonSprite);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0.18f);
            rt.anchorMax = new Vector2(xMax, 0.82f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            btn.GetComponent<Image>().color = color;
            return btn;
        }

        private void CloseAllMetaPanels()
        {
            if (_metaPanel != null) _metaPanel.SetActive(false);
            if (_boardSelectPanel != null) _boardSelectPanel.SetActive(false);
            if (_shopPanel != null) _shopPanel.SetActive(false);
            if (_collectionPanel != null) _collectionPanel.SetActive(false);
            if (_gachaPanel != null) _gachaPanel.SetActive(false);
            if (_dailyPanel != null) _dailyPanel.SetActive(false);
            if (_prestigePanel != null) _prestigePanel.SetActive(false);
        }

        // ----------------------------------------------------------------------------------
        // Shop (coin packs + rewarded free gems + gem->gold convert)
        // ----------------------------------------------------------------------------------

        private void BuildShopPanel(Transform parent)
        {
            _shopPanel = MakeModalPanel("ShopPanel", parent, "Gem Store", out var info);
            info.text = "Buy Gems with real money, watch an ad for free Gems, or convert Gems to Gold.";
            var content = MakeScrollContent(_shopPanel, 0.84f);

            foreach (var pack in _storefront.Packs)
            {
                var entry = MakeEntry(content, 76, DesertTheme.PanelLight);
                var name = Label("Name", entry.transform, new Vector2(0, 0.5f), new Vector2(0.6f, 1f), Vector2.zero, DesertTheme.FontSizeCaption, TextAlignmentOptions.Left);
                name.text = pack.IsBestValue ? $"{pack.DisplayName}  <color=#39C0B7>BEST VALUE</color>" : pack.DisplayName;
                name.GetComponent<RectTransform>().offsetMin = new Vector2(14, 0);
                var detail = Label("Detail", entry.transform, new Vector2(0, 0f), new Vector2(0.6f, 0.5f), Vector2.zero, 16, TextAlignmentOptions.Left);
                var bonusTxt = pack.BonusPercent > 0 ? $"  (+{Mathf.RoundToInt(pack.BonusPercent * 100)}%)" : "";
                var bundleTxt = string.IsNullOrEmpty(pack.BundleCharacterId) ? "" : "  + Hero";
                detail.text = $"<color=#39C0B7>{pack.TotalGems} Gems</color>{bonusTxt}{bundleTxt}";
                detail.color = DesertTheme.TextSecondary;
                detail.GetComponent<RectTransform>().offsetMin = new Vector2(14, 0);

                var productId = pack.ProductId;
                EntryButton(pack.PriceLabel, entry.transform, 0.64f, 0.97f, () => _store.Purchase(productId), DesertTheme.AccentGold)
                    .GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
            }

            // Free gems via rewarded ad.
            var freeEntry = MakeEntry(content, 70, DesertTheme.BoardEntryUnlocked);
            _freeGemsLabel = Label("FreeGems", freeEntry.transform, new Vector2(0, 0f), new Vector2(0.6f, 1f), Vector2.zero, DesertTheme.FontSizeCaption, TextAlignmentOptions.Left);
            _freeGemsLabel.GetComponent<RectTransform>().offsetMin = new Vector2(14, 0);
            EntryButton("Watch Ad", freeEntry.transform, 0.64f, 0.97f, OnFreeGemsClicked, DesertTheme.AccentTurquoise)
                .GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;

            // Convert gems -> gold.
            var convEntry = MakeEntry(content, 70, DesertTheme.BoardEntryUnlocked);
            var convLabel = Label("Convert", convEntry.transform, new Vector2(0, 0f), new Vector2(0.6f, 1f), Vector2.zero, DesertTheme.FontSizeCaption, TextAlignmentOptions.Left);
            convLabel.text = $"Convert {GemConvertCost} Gems -> {GemConvertGold} Gold";
            convLabel.GetComponent<RectTransform>().offsetMin = new Vector2(14, 0);
            EntryButton("Convert", convEntry.transform, 0.64f, 0.97f, OnConvertGemsClicked, DesertTheme.BtnSecondary);
        }

        private void OpenShop()
        {
            CloseAllMetaPanels();
            _shopPanel.SetActive(true);
            RefreshAll();
        }

        private void OnFreeGemsClicked()
        {
            if (_daily.RemainingAdGrants(AdPlacements.FreeGems, FreeGemsDailyCap) <= 0)
            {
                SetStatus("No more free Gems today. Come back tomorrow!");
                return;
            }
            _ads.ShowRewarded(AdPlacements.FreeGems, ok =>
            {
                if (!ok) { SetStatus("Ad not ready. Try again shortly."); return; }
                _inventory.Add(new GameReward { PremiumCurrency = FreeGemsPerAd });
                _daily.RegisterAdView(AdPlacements.FreeGems);
                _analytics?.TrackEvent("ad_reward", new Dictionary<string, object> { { "placement", AdPlacements.FreeGems } });
                SetStatus($"+{FreeGemsPerAd} Gems!");
                SaveAccount();
                RefreshAll();
            });
        }

        private void OnConvertGemsClicked()
        {
            if (_inventory.Spend(CurrencyType.Premium, GemConvertCost))
            {
                _inventory.Add(new GameReward { SoftCurrency = GemConvertGold });
                SetStatus($"Converted {GemConvertCost} Gems to {GemConvertGold} Gold.");
                SaveAccount();
                RefreshAll();
            }
            else SetStatus($"Need {GemConvertCost} Gems.");
        }

        // ----------------------------------------------------------------------------------
        // Collection (heroes): equip, direct buy with gems, craft with shards
        // ----------------------------------------------------------------------------------

        private void BuildCollectionPanel(Transform parent)
        {
            _collectionPanel = MakeModalPanel("CollectionPanel", parent, "Heroes", out var info);
            info.color = DesertTheme.TextSecondary;
            _collectionContent = MakeScrollContent(_collectionPanel, 0.84f);
        }

        private void OpenCollection()
        {
            CloseAllMetaPanels();
            _collectionPanel.SetActive(true);
            BuildCollectionEntries();
            _collectionPanel.SetActive(true);
            RefreshAll();
        }

        private void BuildCollectionEntries()
        {
            if (_collectionContent == null) return;
            for (var i = _collectionContent.childCount - 1; i >= 0; i--)
                Destroy(_collectionContent.GetChild(i).gameObject);

            var infoLabel = _collectionPanel.transform.Find("CollectionPanelInfo")?.GetComponent<TMP_Text>();
            if (infoLabel != null)
                infoLabel.text = $"Shards  C:{_collection.GetShards(CharacterRarity.Common)}  R:{_collection.GetShards(CharacterRarity.Rare)}  E:{_collection.GetShards(CharacterRarity.Epic)}  L:{_collection.GetShards(CharacterRarity.Legendary)}";

            foreach (var rarity in new[] { CharacterRarity.Legendary, CharacterRarity.Epic, CharacterRarity.Rare, CharacterRarity.Common })
            {
                foreach (var c in _characterCatalog.ByRarity(rarity))
                {
                    var owned = _collection.IsOwned(c.Id);
                    var isActive = _collection.ActiveId == c.Id;
                    var entry = MakeEntry(_collectionContent, 78, owned ? DesertTheme.BoardEntryUnlocked : DesertTheme.BoardEntryLocked);

                    var name = Label("Name", entry.transform, new Vector2(0, 0.5f), new Vector2(0.62f, 1f), Vector2.zero, DesertTheme.FontSizeCaption, TextAlignmentOptions.Left);
                    var col = RarityColor(c.Rarity);
                    name.text = $"<color=#{ColorUtility.ToHtmlStringRGB(col)}>{c.DisplayName}</color>  [{c.Rarity}]";
                    name.GetComponent<RectTransform>().offsetMin = new Vector2(14, 0);

                    var bonus = Label("Bonus", entry.transform, new Vector2(0, 0f), new Vector2(0.62f, 0.5f), Vector2.zero, 16, TextAlignmentOptions.Left);
                    bonus.text = BonusText(c);
                    bonus.color = DesertTheme.TextSecondary;
                    bonus.GetComponent<RectTransform>().offsetMin = new Vector2(14, 0);

                    var character = c;
                    if (owned)
                    {
                        if (isActive)
                        {
                            var lbl = Label("Equipped", entry.transform, new Vector2(0.64f, 0.2f), new Vector2(0.97f, 0.8f), Vector2.zero, DesertTheme.FontSizeCaption, TextAlignmentOptions.Center);
                            lbl.text = "<color=#39C0B7>EQUIPPED</color>";
                        }
                        else
                        {
                            EntryButton("Equip", entry.transform, 0.66f, 0.97f, () =>
                            {
                                _collection.SetActive(character.Id);
                                _collection.Save();
                                SetStatus($"Equipped {character.DisplayName}.");
                                BuildCollectionEntries();
                                RefreshAll();
                            }, DesertTheme.BtnPrimary);
                        }
                    }
                    else
                    {
                        EntryButton($"{character.DirectUnlockCostGems} Gems", entry.transform, 0.50f, 0.73f, () => BuyCharacter(character), DesertTheme.AccentGold)
                            .GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
                        var craftCost = _gachaConfig.CraftCost(character.Rarity);
                        EntryButton($"{craftCost} Shards", entry.transform, 0.75f, 0.97f, () => CraftCharacter(character), DesertTheme.BtnSecondary);
                    }
                }
            }
        }

        private void BuyCharacter(CharacterDefinition c)
        {
            if (_collection.IsOwned(c.Id)) return;
            if (_inventory.Spend(CurrencyType.Premium, c.DirectUnlockCostGems))
            {
                _collection.Unlock(c.Id);
                _collection.Save();
                _analytics?.TrackEvent("character_unlock", new Dictionary<string, object> { { "id", c.Id }, { "method", "gems" } });
                SetStatus($"Unlocked {c.DisplayName}!");
                SaveAccount();
                BuildCollectionEntries();
                RefreshAll();
            }
            else SetStatus($"Need {c.DirectUnlockCostGems} Gems.");
        }

        private void CraftCharacter(CharacterDefinition c)
        {
            var cost = _gachaConfig.CraftCost(c.Rarity);
            if (_collection.TryCraft(c, cost))
            {
                _collection.Save();
                _analytics?.TrackEvent("character_unlock", new Dictionary<string, object> { { "id", c.Id }, { "method", "shards" } });
                SetStatus($"Crafted {c.DisplayName} from shards!");
                BuildCollectionEntries();
                RefreshAll();
            }
            else SetStatus($"Need {cost} {c.Rarity} shards.");
        }

        // ----------------------------------------------------------------------------------
        // Lucky Chest (gacha)
        // ----------------------------------------------------------------------------------

        private void BuildGachaPanel(Transform parent)
        {
            _gachaPanel = MakeModalPanel("GachaPanel", parent, "Lucky Chest", out var info);
            info.text = OddsText();

            _gachaPityLabel = Label("Pity", _gachaPanel.transform, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.85f), Vector2.zero, 16);
            _gachaPityLabel.color = DesertTheme.TextSecondary;

            _gachaResultLabel = Label("Result", _gachaPanel.transform, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.76f), Vector2.zero, DesertTheme.FontSizeCaption, TextAlignmentOptions.Top);
            _gachaResultLabel.color = DesertTheme.TextPrimary;
            _gachaResultLabel.text = "Open the chest to collect heroes!";

            var pull1 = ButtonGo($"Pull x1\n{_gacha.SingleCostGems} Gems", _gachaPanel.transform, Vector2.zero, () => TryPull(1), panelButtonSprite);
            PlaceBottom(pull1, 0.06f, 0.36f, 0.135f, DesertTheme.AccentGold);
            pull1.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;

            var pull10 = ButtonGo($"Pull x10\n{_gacha.TenCostGems} Gems", _gachaPanel.transform, Vector2.zero, () => TryPull(10), panelButtonSprite);
            PlaceBottom(pull10, 0.37f, 0.66f, 0.135f, DesertTheme.AccentGold);
            pull10.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;

            var freeSpin = ButtonGo("Free Spin\n(Watch Ad)", _gachaPanel.transform, Vector2.zero, OnFreeSpinClicked, panelButtonSprite);
            PlaceBottom(freeSpin, 0.67f, 0.97f, 0.135f, DesertTheme.AccentTurquoise);
            freeSpin.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
        }

        private void PlaceBottom(GameObject btn, float xMin, float xMax, float yCenter, Color color)
        {
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yCenter - 0.05f);
            rt.anchorMax = new Vector2(xMax, yCenter + 0.05f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            btn.GetComponent<Image>().color = color;
        }

        private string OddsText()
        {
            return $"Odds  Common {Pct(CharacterRarity.Common)}  Rare {Pct(CharacterRarity.Rare)}  Epic {Pct(CharacterRarity.Epic)}  Legendary {Pct(CharacterRarity.Legendary)}";
            string Pct(CharacterRarity r) => $"{Mathf.RoundToInt(_gacha.Probability(r) * 100)}%";
        }

        private void OpenGacha()
        {
            CloseAllMetaPanels();
            _gachaPanel.SetActive(true);
            RefreshAll();
        }

        private void TryPull(int count)
        {
            var cost = count == 1 ? _gacha.SingleCostGems : _gacha.TenCostGems;
            if (!_inventory.Spend(CurrencyType.Premium, cost))
            {
                SetStatus($"Need {cost} Gems for x{count}.");
                return;
            }
            var results = _gacha.Pull(count);
            _analytics?.TrackEvent("gacha_pull", new Dictionary<string, object> { { "count", count }, { "cost_gems", cost } });
            ShowGachaResults(results);
            SaveAccount();
            RefreshAll();
        }

        private void OnFreeSpinClicked()
        {
            if (_daily.RemainingAdGrants(AdPlacements.FreeSpin, FreeSpinDailyCap) <= 0)
            {
                SetStatus("No more free spins today. Come back tomorrow!");
                return;
            }
            _ads.ShowRewarded(AdPlacements.FreeSpin, ok =>
            {
                if (!ok) { SetStatus("Ad not ready. Try again shortly."); return; }
                _daily.RegisterAdView(AdPlacements.FreeSpin);
                var results = _gacha.Pull(1);
                _analytics?.TrackEvent("gacha_pull", new Dictionary<string, object> { { "count", 1 }, { "cost_gems", 0 }, { "free", true } });
                ShowGachaResults(results);
                SaveAccount();
                RefreshAll();
            });
        }

        private void ShowGachaResults(IReadOnlyList<GachaResult> results)
        {
            if (results == null || results.Count == 0) { SetStatus("The chest was empty..."); return; }
            var sb = new System.Text.StringBuilder();
            foreach (var res in results)
            {
                var c = _characterCatalog.GetById(res.CharacterId);
                var nameTxt = c != null ? c.DisplayName : res.CharacterId;
                var col = ColorUtility.ToHtmlStringRGB(RarityColor(res.Rarity));
                if (res.IsNew)
                    sb.AppendLine($"<color=#{col}>NEW! {nameTxt} ({res.Rarity})</color>");
                else
                    sb.AppendLine($"<color=#{col}>{nameTxt}</color> dupe  +{res.ShardsAwarded} shards");
            }
            if (_gachaResultLabel != null) _gachaResultLabel.text = sb.ToString();
            SetStatus("Chest opened!");
        }

        // ----------------------------------------------------------------------------------
        // Daily reward
        // ----------------------------------------------------------------------------------

        private void BuildDailyRewardPanel(Transform parent)
        {
            _dailyPanel = MakeModalPanel("DailyPanel", parent, "Daily Reward", out var info);
            info.text = "Log in every day for escalating rewards. Miss a day and the streak resets!";
            _dailyContent = MakeScrollContent(_dailyPanel, 0.84f);

            var claim = ButtonGo("Claim", _dailyPanel.transform, Vector2.zero, OnClaimDaily, panelButtonSprite);
            var rt = claim.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.135f);
            rt.sizeDelta = new Vector2(360, 64);
            rt.anchoredPosition = Vector2.zero;
            claim.GetComponent<Image>().color = DesertTheme.AccentGold;
            claim.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
            claim.name = "ClaimButton";
        }

        private void OpenDaily()
        {
            CloseAllMetaPanels();
            _dailyPanel.SetActive(true);
            BuildDailyEntries();
            RefreshAll();
        }

        private void BuildDailyEntries()
        {
            if (_dailyContent == null) return;
            for (var i = _dailyContent.childCount - 1; i >= 0; i--)
                Destroy(_dailyContent.GetChild(i).gameObject);

            var nextIndex = _daily.NextDayIndex();
            var canClaim = _daily.CanClaim();
            for (var day = 0; day < _dailyTable.Count; day++)
            {
                var e = _dailyTable.Get(day);
                var isToday = day == nextIndex && canClaim;
                var entry = MakeEntry(_dailyContent, 56, isToday ? DesertTheme.BoardEntryCurrent : DesertTheme.BoardEntryUnlocked);
                var lbl = Label("Day", entry.transform, Vector2.zero, Vector2.one, Vector2.zero, DesertTheme.FontSizeCaption);
                var rewards = $"+{e.Gold} Gold";
                if (e.Gems > 0) rewards += $"  +{e.Gems} Gems";
                if (e.FreeChest) rewards += "  + Free Chest";
                var marker = isToday ? "  <color=#E5BA42>(TODAY)</color>" : "";
                lbl.text = $"Day {day + 1}:  {rewards}{marker}";
            }

            var claimBtn = _dailyPanel.transform.Find("ClaimButton")?.GetComponent<Button>();
            if (claimBtn != null)
            {
                claimBtn.interactable = canClaim;
                var t = claimBtn.GetComponentInChildren<TMP_Text>();
                if (t != null) t.text = canClaim ? $"Claim Day {nextIndex + 1}" : "Claimed Today";
            }
        }

        private void OnClaimDaily()
        {
            var result = _daily.Claim();
            if (!result.Success) { SetStatus("Already claimed today. Come back tomorrow!"); BuildDailyEntries(); return; }

            var msg = $"Daily claimed: +{result.Entry.Gold} Gold";
            if (result.Entry.Gems > 0) msg += $", +{result.Entry.Gems} Gems";
            if (result.Entry.FreeChest)
            {
                var pulls = _gacha.Pull(1);
                ShowGachaResults(pulls);
                msg += ", + Free Chest";
            }
            _analytics?.TrackEvent("daily_claim", new Dictionary<string, object> { { "day", result.DayIndex } });
            SetStatus(msg);
            SaveAccount();
            BuildDailyEntries();
            RefreshAll();
        }

        private void MaybeShowDailyReward()
        {
            // Don't interrupt the very first session's onboarding; show from the next launch onward.
            if (PlayerPrefs.GetInt(OnboardingDoneKey, 0) == 0) return;
            if (_daily != null && _daily.CanClaim()) OpenDaily();
        }

        // ----------------------------------------------------------------------------------
        // Prestige
        // ----------------------------------------------------------------------------------

        private void BuildPrestigePanel(Transform parent)
        {
            _prestigePanel = MakeModalPanel("PrestigePanel", parent, "Prestige", out var info);
            info.text = "Reborn stronger: reset your run for permanent power that compounds forever.";

            _prestigeInfoLabel = Label("PrestigeBody", _prestigePanel.transform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.82f), Vector2.zero, DesertTheme.FontSizeBody, TextAlignmentOptions.Top);
            _prestigeInfoLabel.color = DesertTheme.TextPrimary;

            var btn = ButtonGo("Prestige Now", _prestigePanel.transform, Vector2.zero, OnPrestigeClicked, panelButtonSprite);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.18f);
            rt.sizeDelta = new Vector2(420, 76);
            rt.anchoredPosition = Vector2.zero;
            btn.GetComponent<Image>().color = DesertTheme.AccentGold;
            btn.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
            _prestigeButton = btn.GetComponent<Button>();
        }

        private void OpenPrestige()
        {
            CloseAllMetaPanels();
            _prestigePanel.SetActive(true);
            RefreshPrestigeInfo();
            RefreshAll();
        }

        private void RefreshPrestigeInfo()
        {
            if (_prestigeInfoLabel == null) return;
            var highest = _progression.Data.HighestWave;
            var gain = _prestige.PreviewGain(highest);
            var canPrestige = _prestige.CanPrestige(highest);
            _prestigeInfoLabel.text =
                $"Prestige Level: {_prestige.Data.Level}\n" +
                $"Current Multiplier: x{_prestige.Multiplier():0.00}\n" +
                $"Prestige Points: {_prestige.Data.Points}\n\n" +
                $"Highest Wave: {highest}\n" +
                (canPrestige
                    ? $"Prestige now for <color=#E5BA42>+{gain} points</color> (resets your run)."
                    : $"Reach wave {_prestige.UnlockWave} to prestige.");
            if (_prestigeButton != null) _prestigeButton.interactable = canPrestige;
        }

        private void OnPrestigeClicked()
        {
            var highest = _progression.Data.HighestWave;
            var gained = _prestige.Prestige(highest);
            if (gained <= 0) { SetStatus($"Reach wave {_prestige.UnlockWave} to prestige."); return; }
            _analytics?.TrackEvent("prestige", new Dictionary<string, object> { { "points", gained }, { "level", _prestige.Data.Level } });
            _prestigePanel.SetActive(false);
            EndRun();
            SetStatus($"Prestiged! +{gained} points. Multiplier now x{_prestige.Multiplier():0.00}.");
            RefreshAll();
        }

        // ----------------------------------------------------------------------------------
        // Fight-result rewarded-ad actions (double reward / revive)
        // ----------------------------------------------------------------------------------

        private void AddFightResultActions(Transform card)
        {
            _doubleRewardButton = ButtonGo("Double Reward (Watch Ad)", card, Vector2.zero, OnDoubleRewardClicked, panelButtonSprite);
            var dr = _doubleRewardButton.GetComponent<RectTransform>();
            dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0.205f);
            dr.sizeDelta = new Vector2(540, 62);
            dr.anchoredPosition = Vector2.zero;
            _doubleRewardButton.GetComponent<Image>().color = DesertTheme.AccentTurquoise;
            _doubleRewardButton.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
            _doubleRewardButton.SetActive(false);

            _reviveButton = ButtonGo("Revive (Watch Ad)", card, Vector2.zero, OnReviveClicked, panelButtonSprite);
            var rv = _reviveButton.GetComponent<RectTransform>();
            rv.anchorMin = rv.anchorMax = new Vector2(0.5f, 0.205f);
            rv.sizeDelta = new Vector2(540, 62);
            rv.anchoredPosition = Vector2.zero;
            _reviveButton.GetComponent<Image>().color = DesertTheme.AccentTurquoise;
            _reviveButton.GetComponentInChildren<TMP_Text>().color = DesertTheme.BgPrimary;
            _reviveButton.SetActive(false);
        }

        private void HideFightResultActions()
        {
            _doubleRewardClaimed = false;
            if (_doubleRewardButton != null) _doubleRewardButton.SetActive(false);
            if (_reviveButton != null) _reviveButton.SetActive(false);
        }

        private void ConfigureFightResultActions(bool won)
        {
            if (won)
            {
                if (_doubleRewardButton != null) _doubleRewardButton.SetActive(!_doubleRewardClaimed);
            }
            else
            {
                if (_reviveButton != null) _reviveButton.SetActive(_revivesUsedThisRun < ReviveCapPerRun);
            }
        }

        private void OnDoubleRewardClicked()
        {
            _ads.ShowRewarded(AdPlacements.DoubleWinReward, ok =>
            {
                if (!ok) { SetStatus("Ad not ready. Try again shortly."); return; }
                _session.GrantBonusReward(_session.LastWinReward);
                _doubleRewardClaimed = true;
                if (_doubleRewardButton != null) _doubleRewardButton.SetActive(false);
                _analytics?.TrackEvent("ad_reward", new Dictionary<string, object> { { "placement", AdPlacements.DoubleWinReward } });
                SetStatus("Reward doubled!");
                SaveAccount();
                RefreshAll();
            });
        }

        private void OnReviveClicked()
        {
            _ads.ShowRewarded(AdPlacements.Revive, ok =>
            {
                if (!ok) { SetStatus("Ad not ready. Try again shortly."); return; }
                var spawned = _session.Revive();
                _revivesUsedThisRun++;
                _lastFightWasLoss = false;
                if (_reviveButton != null) _reviveButton.SetActive(false);
                if (_fightResultPanel != null) _fightResultPanel.SetActive(false);
                _analytics?.TrackEvent("ad_reward", new Dictionary<string, object> { { "placement", AdPlacements.Revive } });
                SetStatus($"Revived! +{spawned} gems on the board. Merge and fight on!");
                SaveAccount();
                RefreshAll();
            });
        }

        // ----------------------------------------------------------------------------------
        // Refresh + small helpers
        // ----------------------------------------------------------------------------------

        private void RefreshMonetizationUI()
        {
            if (_freeGemsLabel != null)
            {
                var left = _daily != null ? _daily.RemainingAdGrants(AdPlacements.FreeGems, FreeGemsDailyCap) : 0;
                _freeGemsLabel.text = $"Free Gems: +{FreeGemsPerAd}  ({left} left today)";
            }
            if (_gachaPityLabel != null && _gacha != null)
            {
                _gachaPityLabel.text = _gacha.PityCount > 0
                    ? $"Guaranteed Epic+ in {Mathf.Max(0, _gacha.PityCount - _gacha.PityCounter)} pulls"
                    : "";
            }
            if (_collectionPanel != null && _collectionPanel.activeSelf) BuildCollectionEntries();
            if (_dailyPanel != null && _dailyPanel.activeSelf) BuildDailyEntries();
            if (_prestigePanel != null && _prestigePanel.activeSelf) RefreshPrestigeInfo();
        }

        private static string BonusText(CharacterDefinition c)
        {
            return c.BonusType switch
            {
                CharacterBonusType.SquadPowerPercent => $"+{Mathf.RoundToInt(c.BonusValue * 100)}% Squad Power",
                CharacterBonusType.GoldGainPercent => $"+{Mathf.RoundToInt(c.BonusValue * 100)}% Gold",
                CharacterBonusType.MergeRewardPercent => $"+{Mathf.RoundToInt(c.BonusValue * 100)}% Merge Reward",
                CharacterBonusType.ExtraSpawnCapacity => $"+{Mathf.RoundToInt(c.BonusValue)} Spawn Slots",
                CharacterBonusType.GachaLuckPercent => $"+{Mathf.RoundToInt(c.BonusValue * 100)}% Chest Luck",
                CharacterBonusType.StartingGoldFlat => $"+{Mathf.RoundToInt(c.BonusValue)} Starting Gold",
                _ => ""
            };
        }

        private static Color RarityColor(CharacterRarity rarity)
        {
            return rarity switch
            {
                CharacterRarity.Common => new Color(0.78f, 0.76f, 0.70f, 1f),
                CharacterRarity.Rare => DesertTheme.Tier2,
                CharacterRarity.Epic => DesertTheme.Tier3,
                CharacterRarity.Legendary => DesertTheme.AccentGold,
                _ => DesertTheme.TextPrimary
            };
        }
    }
}
