using UnityEngine;

namespace MergeSurvivor.UI
{
    public static class DesertTheme
    {
        // === BACKGROUNDS (deep warm tones, nocturnal desert feel) ===
        public static readonly Color BgPrimary = new(0.14f, 0.09f, 0.07f, 1f);      // Rich mahogany
        public static readonly Color BgSecondary = new(0.18f, 0.13f, 0.10f, 1f);     // Cool sandstone shadow
        public static readonly Color BgTertiary = new(0.22f, 0.12f, 0.09f, 1f);      // Dark terracotta
        public static readonly Color BgOverlay = new(0.06f, 0.04f, 0.03f, 0.92f);    // Modal overlay

        // === BOARD / GRID (amber illuminated tapestry feel) ===
        public static readonly Color GridSlotEmpty = new(0.20f, 0.15f, 0.10f, 0.85f);
        public static readonly Color GridSlotHighlight = new(0.35f, 0.25f, 0.12f, 0.95f);
        public static readonly Color GridBorder = new(0.45f, 0.32f, 0.15f, 0.7f);

        // === INTERACTABLES (jewel tones + oasis colors) ===
        public static readonly Color BtnPrimary = new(0.12f, 0.55f, 0.38f, 1f);      // Emerald green
        public static readonly Color BtnPrimaryPressed = new(0.08f, 0.42f, 0.28f, 1f);
        public static readonly Color BtnSecondary = new(0.18f, 0.32f, 0.62f, 1f);    // Lapis lazuli
        public static readonly Color BtnSecondaryPressed = new(0.12f, 0.24f, 0.48f, 1f);
        public static readonly Color BtnDanger = new(0.72f, 0.22f, 0.18f, 1f);       // Desert red
        public static readonly Color BtnDangerPressed = new(0.55f, 0.15f, 0.12f, 1f);
        public static readonly Color BtnDisabled = new(0.28f, 0.24f, 0.22f, 0.7f);
        public static readonly Color AccentGold = new(0.90f, 0.72f, 0.20f, 1f);      // Glowing gold
        public static readonly Color AccentTurquoise = new(0.15f, 0.75f, 0.72f, 1f); // Bright turquoise

        // === TIER COLORS (crystal / golem progression) ===
        public static readonly Color Tier1 = new(0.55f, 0.78f, 0.42f, 1f);  // Raw green crystal
        public static readonly Color Tier2 = new(0.30f, 0.55f, 0.88f, 1f);  // Blue lapis shard
        public static readonly Color Tier3 = new(0.65f, 0.35f, 0.82f, 1f);  // Amethyst
        public static readonly Color Tier4 = new(0.92f, 0.75f, 0.22f, 1f);  // Polished gold jewel
        public static readonly Color Tier5 = new(0.86f, 0.20f, 0.30f, 1f);  // Ruby
        public static readonly Color Tier6 = new(0.85f, 0.92f, 1.00f, 1f);  // Radiant diamond

        // === TYPOGRAPHY ===
        public static readonly Color TextPrimary = new(1f, 0.97f, 0.90f, 1f);       // Pale cream
        public static readonly Color TextSecondary = new(0.78f, 0.72f, 0.62f, 1f);  // Warm sand
        public static readonly Color TextDisabled = new(0.50f, 0.45f, 0.40f, 1f);
        public static readonly Color TextShadow = new(0.30f, 0.08f, 0.06f, 0.8f);   // Deep maroon shadow
        public static readonly Color TextVictory = new(0.40f, 0.90f, 0.50f, 1f);
        public static readonly Color TextDefeat = new(0.95f, 0.40f, 0.35f, 1f);

        // === PANEL BACKGROUNDS ===
        public static readonly Color PanelDark = new(0.08f, 0.06f, 0.04f, 0.92f);
        public static readonly Color PanelMid = new(0.15f, 0.11f, 0.08f, 0.95f);
        public static readonly Color PanelLight = new(0.22f, 0.17f, 0.12f, 0.90f);
        public static readonly Color PanelBoardSelect = new(0.10f, 0.07f, 0.05f, 0.96f);

        // === BOARD ENTRY STATUS COLORS ===
        public static readonly Color BoardEntryCurrent = new(0.18f, 0.35f, 0.22f, 0.92f);
        public static readonly Color BoardEntryUnlocked = new(0.15f, 0.18f, 0.25f, 0.90f);
        public static readonly Color BoardEntryLocked = new(0.22f, 0.14f, 0.12f, 0.90f);
        public static readonly Color BoardEntryLockedText = new(0.75f, 0.60f, 0.55f, 1f);

        // === CAMERA ===
        public static readonly Color CameraBg = new(0.06f, 0.04f, 0.03f, 1f);

        // === LAYOUT CONSTANTS ===
        public const int FontSizeTitle = 44;
        public const int FontSizeHeading = 32;
        public const int FontSizeBody = 26;
        public const int FontSizeCaption = 20;
        public const int FontSizeHud = 26;
        public const float ButtonHeight = 64f;
        public const float ButtonWidth = 200f;
        // Board fills the width of a 1080-reference portrait canvas: 4*216 + 3*14 = 906.
        public const float GridCellSize = 216f;
        public const float GridSpacing = 14f;
        public const int GridColumns = 4;
        public const float PanelCornerRadius = 12f;

        public static Color TierColor(int tier)
        {
            return tier switch
            {
                1 => Tier1,
                2 => Tier2,
                3 => Tier3,
                4 => Tier4,
                5 => Tier5,
                _ => Tier6
            };
        }
    }
}
