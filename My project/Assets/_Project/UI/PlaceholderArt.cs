using UnityEngine;

namespace MergeSurvivor.UI
{
    /// <summary>
    /// Deprecated: tier color fallback. All visuals now delegate to DesertTheme.
    /// Will be removed once ItemVisualCatalog sprites are fully wired.
    /// </summary>
    public static class PlaceholderArt
    {
        public static readonly Color Tier1 = DesertTheme.Tier1;
        public static readonly Color Tier2 = DesertTheme.Tier2;
        public static readonly Color Tier3 = DesertTheme.Tier3;
        public static readonly Color Tier4 = DesertTheme.Tier4;

        public static Color TierColor(int tier) => DesertTheme.TierColor(tier);
    }
}
