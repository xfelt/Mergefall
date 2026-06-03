using System.Collections.Generic;
using MergeSurvivor.Core;
using MergeSurvivor.Data;
using MergeSurvivor.Economy;

namespace MergeSurvivor.Meta
{
    public struct PurchaseGrant
    {
        public bool Success;
        public string ProductId;
        public int GemsGranted;
        public string BundleCharacterId; // empty when none
        public bool BundleCharacterIsNew;
    }

    public interface IStorefrontService
    {
        IReadOnlyList<CoinPack> Packs { get; }
        IEnumerable<string> ProductIds();
        /// <summary>Apply a completed purchase: grant gems (+bonus%) and any bundled character. Idempotent guard is the caller's job (store fires once).</summary>
        PurchaseGrant GrantPurchase(string productId);
    }

    /// <summary>
    /// Maps a completed IAP product to the in-game grant (premium currency + optional bundle character).
    /// Platform-agnostic: the UI subscribes to IStoreService.PurchaseCompleted and calls GrantPurchase.
    /// </summary>
    public sealed class StorefrontService : IStorefrontService
    {
        private readonly CoinPackCatalog _catalog;
        private readonly IInventoryService _inventory;
        private readonly ICollectionService _collection;

        public StorefrontService(CoinPackCatalog catalog, IInventoryService inventory, ICollectionService collection)
        {
            _catalog = catalog;
            _inventory = inventory;
            _collection = collection;
        }

        public IReadOnlyList<CoinPack> Packs => _catalog != null ? _catalog.Packs : new List<CoinPack>();

        public IEnumerable<string> ProductIds()
            => _catalog != null ? _catalog.ProductIds() : new List<string>();

        public PurchaseGrant GrantPurchase(string productId)
        {
            var pack = _catalog?.GetByProductId(productId);
            if (pack == null) return new PurchaseGrant { Success = false, ProductId = productId };

            var gems = pack.TotalGems;
            _inventory?.Add(new GameReward { PremiumCurrency = gems });

            var bundleNew = false;
            if (!string.IsNullOrEmpty(pack.BundleCharacterId) && _collection != null)
            {
                bundleNew = _collection.Unlock(pack.BundleCharacterId);
                _collection.Save();
            }

            return new PurchaseGrant
            {
                Success = true,
                ProductId = productId,
                GemsGranted = gems,
                BundleCharacterId = pack.BundleCharacterId,
                BundleCharacterIsNew = bundleNew
            };
        }
    }
}
