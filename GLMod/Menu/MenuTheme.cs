using GLMod.Class;
using GLMod.Constants;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Look of the menu: brand (name, logo, site) and palette. Good Loss by default; Among Us Ranked when the
    /// detected mod is Among Us Ranked (palette of amongusranked.orianagames.com).
    /// </summary>
    public sealed class MenuTheme
    {
        /// <summary>Default theme: Good Loss site palette (navy + violet).</summary>
        public static readonly MenuTheme GoodLoss = new MenuTheme(
            id: "goodloss",
            brandName: "GOOD LOSS",
            brandInitials: "GL",
            siteLabel: "goodloss.fr",
            logoResource: GameConstants.LOGO_RESOURCE_NAME,
            accent: 0xA78BFA, accentStrong: 0x7C3AED, accentLight: 0xC4B5FD, onAccent: 0xFFFFFF,
            text: 0xEEF2F7, mutedText: 0x8B95A5, dimText: 0x5B6577,
            panelTop: 0x171C29, panelBottom: 0x0A0D13, surface: 0x161C28, surfaceHover: 0x1F2738, border: 0x283143);

        /// <summary>Among Us Ranked theme: Among Us Ranked brand and logo, stone + amber palette of amongusranked.orianagames.com.</summary>
        public static readonly MenuTheme Ranked = new MenuTheme(
            id: "ranked",
            brandName: "AMONG US RANKED",
            brandInitials: "AUR",
            siteLabel: "orianagames.com",
            logoResource: GameConstants.RANKED_LOGO_RESOURCE_NAME,
            accent: 0xF59E0B, accentStrong: 0xD97706, accentLight: 0xFCD34D, onAccent: 0x1C1917,
            text: 0xF5F5F4, mutedText: 0xA8A29E, dimText: 0x8C857F,
            panelTop: 0x1C1917, panelBottom: 0x141210, surface: 0x211E1B, surfaceHover: 0x2E2A27, border: 0x322D2A);

        public string Id { get; }

        /// <summary>Name shown on the resting tab and in the header (upper case).</summary>
        public string BrandName { get; }

        /// <summary>Badge text used when the logo cannot be loaded.</summary>
        public string BrandInitials { get; }

        /// <summary>Site shown in the footer.</summary>
        public string SiteLabel { get; }

        /// <summary>Embedded PNG of the logo.</summary>
        public string LogoResource { get; }

        public Color Accent { get; }
        public Color AccentStrong { get; }
        public Color AccentLight { get; }

        /// <summary>Text drawn on accent backgrounds (buttons, badges).</summary>
        public Color OnAccent { get; }

        public Color Text { get; }
        public Color MutedText { get; }
        public Color DimText { get; }
        public Color PanelTop { get; }
        public Color PanelBottom { get; }
        public Color Surface { get; }
        public Color SurfaceHover { get; }
        public Color Border { get; }

        private MenuTheme(string id, string brandName, string brandInitials, string siteLabel, string logoResource,
            int accent, int accentStrong, int accentLight, int onAccent, int text, int mutedText, int dimText,
            int panelTop, int panelBottom, int surface, int surfaceHover, int border)
        {
            Id = id;
            BrandName = brandName;
            BrandInitials = brandInitials;
            SiteLabel = siteLabel;
            LogoResource = logoResource;
            Accent = Rgb(accent);
            AccentStrong = Rgb(accentStrong);
            AccentLight = Rgb(accentLight);
            OnAccent = Rgb(onAccent);
            Text = Rgb(text);
            MutedText = Rgb(mutedText);
            DimText = Rgb(dimText);
            PanelTop = Rgb(panelTop);
            PanelBottom = Rgb(panelBottom);
            Surface = Rgb(surface);
            SurfaceHover = Rgb(surfaceHover);
            Border = Rgb(border);
        }

        /// <summary>
        /// Theme of a detected mod: Ranked for "Among Us Ranked" (and its variants, e.g. "Among Us Ranked Roles"),
        /// Good Loss otherwise. Pure.
        /// </summary>
        public static MenuTheme ForMod(string modName) => RankedMod.Matches(modName) ? Ranked : GoodLoss;

        private static Color Rgb(int rgb)
        {
            // Fields set directly: no IL2CPP call needed
            return new Color
            {
                r = ((rgb >> 16) & 0xFF) / 255f,
                g = ((rgb >> 8) & 0xFF) / 255f,
                b = (rgb & 0xFF) / 255f,
                a = 1f,
            };
        }
    }
}
