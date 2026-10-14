using GLMod.Constants;
using System;
using System.IO;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Widgets to draw the content of a menu tab, laid out from top to bottom in the tab area
    /// (IMGUI). Sizes are given in 1080p reference units and scaled to the screen.
    /// Only valid inside IMenuTab.Draw.
    /// </summary>
    public static class MenuUI
    {
        /// <summary>
        /// Current theme (Good Loss, or Among Us Ranked); chosen by MenuBehaviour from the detected mod
        /// </summary>
        public static MenuTheme Theme { get; private set; } = MenuTheme.GoodLoss;

        // Palette of the current theme
        public static Color Accent => Theme.Accent;
        public static Color AccentStrong => Theme.AccentStrong;
        public static Color Text => Theme.Text;
        public static Color MutedText => Theme.MutedText;
        public static readonly Color Success = Rgb(0x22C55E);
        public static readonly Color Danger = Rgb(0xEF4444);

        internal static Color AccentLight => Theme.AccentLight;
        internal static Color OnAccent => Theme.OnAccent;
        internal static Color DimText => Theme.DimText;
        internal static Color PanelTop => Theme.PanelTop;
        internal static Color PanelBottom => Theme.PanelBottom;
        internal static Color Surface => Theme.Surface;
        internal static Color SurfaceHover => Theme.SurfaceHover;
        internal static Color Border => Theme.Border;
        internal static readonly Color White = Rgb(0xFFFFFF);

        internal enum Corner
        {
            None,
            Small,
            Medium,
            Large,
        }

        private const int SmallRadius = 4;
        private const int MediumRadius = 7;
        private const int LargeRadius = 12;
        private const int GlowSpread = 18;
        private const int SmallGlowRadius = 1;
        private const int SmallGlowSpread = 8;
        private const float Spacing = 9f;

        // Windows fonts first (Bahnschrift is the condensed DIN-like face of Windows 10+), Arial elsewhere
        private static readonly string[] DisplayFontNames = { "Bahnschrift SemiBold", "Bahnschrift", "Segoe UI Semibold", "Arial" };
        private static readonly string[] BodyFontNames = { "Segoe UI", "Arial" };

        internal static float Scale { get; private set; } = 1f;

        // Display styles (titles, tabs, buttons)
        internal static GUIStyle BrandStyle;
        internal static GUIStyle LogoStyle;
        internal static GUIStyle TabStyle;
        internal static GUIStyle KeycapStyle;
        internal static GUIStyle ChipStyle;
        internal static GUIStyle SectionStyle;
        internal static GUIStyle HighlightStyle;
        internal static GUIStyle ResultStyle;
        internal static GUIStyle TileValueStyle;
        internal static GUIStyle ButtonStyle;
        internal static GUIStyle SmallButtonStyle;
        internal static GUIStyle PillStyle;
        internal static GUIStyle AvatarStyle;
        internal static GUIStyle NameStyle;
        internal static GUIStyle InvisibleStyle;
        internal static GUIStyle CloseStyle;

        // Body styles
        internal static GUIStyle TextStyle;
        internal static GUIStyle MutedStyle;
        internal static GUIStyle CaptionStyle;
        internal static GUIStyle FieldNameStyle;
        internal static GUIStyle FieldValueStyle;
        internal static GUIStyle RowStyle;

        private static Font _displayFont;
        private static Font _bodyFont;
        private static bool _fontsLoaded;
        private static float _stylesScale = -1f;

        private static GUIStyle _fillSquare;
        private static GUIStyle _fillSmall;
        private static GUIStyle _fillMedium;
        private static GUIStyle _fillLarge;
        private static GUIStyle _glow;
        private static GUIStyle _smallGlow;
        private static GUIStyle _circle;
        // Textures baked with theme colors (gradients, logo), built once per theme
        private sealed class ThemeAssets
        {
            public GUIStyle Panel;
            public GUIStyle Accent;
            public GUIStyle AccentSquare;
            public GUIStyle Logo;
            public float LogoAspect = 1f;
        }

        private static readonly System.Collections.Generic.Dictionary<string, ThemeAssets> _themeAssets =
            new System.Collections.Generic.Dictionary<string, ThemeAssets>();
        private static ThemeAssets _assets;
        private static GUIContent _empty;

        private static Rect _area;
        private static float _y;

        /// <summary>
        /// Width available for the content of the tab, in screen pixels
        /// </summary>
        public static float Width => _area.width;

        /// <summary>
        /// Height drawn so far in the current tab (used to scroll long tabs)
        /// </summary>
        internal static float ContentHeight => _y - _area.y;

        // ------------------------------------------------------------------ public widgets

        /// <summary>
        /// Section title: small accent capitals followed by a thin line
        /// </summary>
        public static void Heading(string text)
        {
            string title = (text ?? "").ToUpperInvariant();
            Rect rect = Reserve(20f);
            float textWidth = SectionStyle.CalcSize(new GUIContent(title)).x;
            DrawText(new Rect(rect.x, rect.y, textWidth, rect.height), title, SectionStyle, Accent);
            float lineX = rect.x + textWidth + U(10f);
            DrawRect(new Rect(lineX, rect.y + rect.height / 2f, rect.xMax - lineX, Px(1f)), Border, Corner.None);
            _y += U(2f);
        }

        /// <summary>
        /// Large display text (e.g. a rank name)
        /// </summary>
        public static void Highlight(string text) => DrawFlowText(text, HighlightStyle, AccentLight);

        /// <summary>
        /// Body text, wrapped
        /// </summary>
        public static void Label(string text) => DrawFlowText(text, TextStyle, Text);

        /// <summary>
        /// Body text in a given color, wrapped
        /// </summary>
        public static void Label(string text, Color color) => DrawFlowText(text, TextStyle, color);

        /// <summary>
        /// Secondary text (hints, explanations), wrapped
        /// </summary>
        public static void Muted(string text) => DrawFlowText(text, MutedStyle, MutedText);

        /// <summary>
        /// "name ........ value" line with a divider
        /// </summary>
        public static void Field(string name, string value) => Field(name, value, Text);

        /// <summary>
        /// "name ........ value" line with a divider and a colored value
        /// </summary>
        public static void Field(string name, string value, Color valueColor)
        {
            var nameContent = new GUIContent(name ?? "");
            var valueContent = new GUIContent(string.IsNullOrEmpty(value) ? "-" : value);
            float nameWidth = Mathf.Round(_area.width * 0.4f);
            float valueWidth = _area.width - nameWidth;
            float height = Math.Max(U(30f), FieldValueStyle.CalcHeight(valueContent, valueWidth) + U(8f));

            var rect = new Rect(_area.x, _y, _area.width, height);
            DrawText(new Rect(rect.x, rect.y, nameWidth, height), nameContent.text, FieldNameStyle, MutedText);
            DrawText(new Rect(rect.x + nameWidth, rect.y, valueWidth, height), valueContent.text, FieldValueStyle, valueColor);
            DrawRect(new Rect(rect.x, rect.yMax - Px(1f), rect.width, Px(1f)), Border, Corner.None);
            _y += height + U(4f);
        }

        /// <summary>
        /// Gradient call-to-action button, glowing on hover
        /// </summary>
        /// <returns>True when clicked</returns>
        public static bool Button(string text)
        {
            string label = (text ?? "").ToUpperInvariant();
            float width = Math.Min(_area.width, Math.Max(U(180f), ButtonStyle.CalcSize(new GUIContent(label)).x + U(56f)));
            Rect rect = ReserveWidth(width, 40f);
            return DrawPrimaryButton(rect, label);
        }

        /// <summary>
        /// One-line card: colored edge, text, optional small button on the right
        /// </summary>
        /// <param name="text">Row text (cut when too long)</param>
        /// <param name="buttonText">Button text, or null for no button</param>
        /// <param name="color">Edge and text color</param>
        /// <returns>True when the button is clicked</returns>
        public static bool Row(string text, string buttonText, Color color)
        {
            Rect rect = Reserve(42f, 6f);
            bool hover = IsHover(rect);
            DrawRect(rect, hover ? SurfaceHover : Surface, Corner.Medium);
            DrawRect(new Rect(rect.x, rect.y + U(8f), Px(3f), rect.height - U(16f)), color, Corner.None);

            float buttonWidth = buttonText != null ? U(84f) : 0f;
            var textRect = new Rect(rect.x + U(16f), rect.y, rect.width - U(28f) - buttonWidth, rect.height);
            DrawText(textRect, text ?? "", RowStyle, color);

            if (buttonText == null)
            {
                return false;
            }
            var buttonRect = new Rect(rect.xMax - buttonWidth - U(7f), rect.y + U(7f), buttonWidth, rect.height - U(14f));
            return DrawSecondaryButton(buttonRect, buttonText.ToUpperInvariant());
        }

        /// <summary>
        /// Labelled progress bar (fraction from 0 to 1)
        /// </summary>
        public static void ProgressBar(string label, float fraction, string valueText)
        {
            Rect header = Reserve(20f, 4f);
            DrawText(header, (label ?? "").ToUpperInvariant(), CaptionStyle, MutedText);
            DrawText(header, valueText ?? "", CaptionStyle, Text, TextAnchor.MiddleRight);

            Rect track = Reserve(8f);
            DrawProgress(track, fraction);
        }

        /// <summary>
        /// Row of stat tiles (label in small capitals, value below)
        /// </summary>
        public static void Tiles(params (string label, string value)[] tiles)
        {
            if (tiles == null || tiles.Length == 0)
            {
                return;
            }

            Rect row = Reserve(64f);
            float gap = U(8f);
            float width = (row.width - gap * (tiles.Length - 1)) / tiles.Length;
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = new Rect(row.x + i * (width + gap), row.y, width, row.height);
                DrawRect(tile, Surface, Corner.Medium);
                DrawText(new Rect(tile.x + U(12f), tile.y + U(9f), tile.width - U(24f), U(16f)),
                    (tiles[i].label ?? "").ToUpperInvariant(), CaptionStyle, MutedText);
                DrawText(new Rect(tile.x + U(12f), tile.y + U(27f), tile.width - U(24f), U(28f)),
                    string.IsNullOrEmpty(tiles[i].value) ? "-" : tiles[i].value, TileValueStyle, Text);
            }
        }

        /// <summary>
        /// Compact grid of stat tiles (small label, value), skipping null values; wraps every columns tiles
        /// </summary>
        public static void StatGrid(int columns, params (string label, string value)[] stats)
        {
            if (stats == null || columns <= 0)
            {
                return;
            }

            var visible = new System.Collections.Generic.List<(string label, string value)>();
            foreach (var stat in stats)
            {
                if (stat.value != null)
                {
                    visible.Add(stat);
                }
            }

            float gap = U(6f);
            for (int start = 0; start < visible.Count; start += columns)
            {
                Rect row = Reserve(48f, 6f);
                float width = (row.width - gap * (columns - 1)) / columns;
                for (int i = start; i < Math.Min(start + columns, visible.Count); i++)
                {
                    var tile = new Rect(row.x + (i - start) * (width + gap), row.y, width, row.height);
                    DrawRect(tile, Surface, Corner.Small);
                    DrawText(new Rect(tile.x + U(10f), tile.y + U(6f), tile.width - U(16f), U(14f)),
                        (visible[i].label ?? "").ToUpperInvariant(), CaptionStyle, MutedText);
                    DrawText(new Rect(tile.x + U(10f), tile.y + U(21f), tile.width - U(16f), U(22f)),
                        visible[i].value, TileValueStyle, Text);
                }
            }
            _y += U(3f);
        }

        /// <summary>
        /// Row of colored tags (pills), wrapping to the next line when full
        /// </summary>
        public static void Tags(params (string text, Color color)[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return;
            }

            float gap = U(6f);
            float lineHeight = U(22f);
            float x = _area.x;
            float y = _y;
            foreach (var tag in tags)
            {
                string text = (tag.text ?? "").ToUpperInvariant();
                float width = PillStyle.CalcSize(new GUIContent(text)).x + U(18f);
                if (x > _area.x && x + width > _area.xMax)
                {
                    x = _area.x;
                    y += lineHeight + gap;
                }
                DrawRect(new Rect(x, y, width, lineHeight), WithAlpha(tag.color, 0.18f), Corner.Small);
                DrawText(new Rect(x, y, width, lineHeight), text, PillStyle, tag.color);
                x += width + gap;
            }
            _y = y + lineHeight + U(Spacing);
        }

        /// <summary>
        /// Vertical gap (reference units)
        /// </summary>
        public static void Space(float height = 10f) => _y += U(height);

        /// <summary>
        /// Thin horizontal line
        /// </summary>
        public static void Separator()
        {
            Rect rect = Reserve(9f);
            DrawRect(new Rect(rect.x, rect.y + rect.height / 2f, rect.width, Px(1f)), Border, Corner.None);
        }

        // ------------------------------------------------------------------ layout (internal)

        internal static float U(float units) => Mathf.Round(units * Scale);

        // At least one physical pixel for hairlines
        internal static float Px(float units) => Mathf.Max(1f, Mathf.Round(units * Scale));

        internal static void BeginContent(Rect area)
        {
            _area = area;
            _y = area.y;
        }

        /// <summary>
        /// Full-width block of the given height (reference units) at the cursor; the cursor moves below it
        /// </summary>
        internal static Rect Reserve(float height, float spacingAfter = Spacing)
        {
            var rect = new Rect(_area.x, _y, _area.width, U(height));
            _y += rect.height + U(spacingAfter);
            return rect;
        }

        private static Rect ReserveWidth(float width, float height)
        {
            var rect = new Rect(_area.x, _y, width, U(height));
            _y += rect.height + U(Spacing);
            return rect;
        }

        internal static bool IsHover(Rect rect) => rect.Contains(Event.current.mousePosition);

        // ------------------------------------------------------------------ drawing (internal)

        /// <summary>
        /// Filled rectangle. GUI.DrawTexture is stripped from the game: a GUI.Box with a white
        /// background tinted by GUI.backgroundColor is used instead.
        /// </summary>
        internal static void DrawRect(Rect rect, Color color, Corner corner)
        {
            GUIStyle style = corner == Corner.Large ? _fillLarge
                : corner == Corner.Medium ? _fillMedium
                : corner == Corner.Small ? _fillSmall
                : _fillSquare;
            DrawBox(rect, style, color);
        }

        internal static void DrawPanelBackground(Rect rect) => DrawBox(rect, _assets.Panel, White);

        internal static void DrawAccentGradient(Rect rect, bool rounded, float alpha = 1f) =>
            DrawBox(rect, rounded ? _assets.Accent : _assets.AccentSquare, WithAlpha(White, alpha));

        /// <summary>
        /// Card with a centered title and explanation, for empty or offline states
        /// </summary>
        internal static void EmptyState(string title, string text)
        {
            var content = new GUIContent(text ?? "");
            float textHeight = MutedStyle.CalcHeight(content, _area.width - U(40f));
            Rect card = Reserve(0f, 0f);
            card.height = U(52f) + textHeight;
            _y = card.yMax + U(Spacing);

            DrawRect(card, Surface, Corner.Medium);
            DrawText(new Rect(card.x, card.y + U(14f), card.width, U(22f)), (title ?? "").ToUpperInvariant(), SectionStyle, Text, TextAnchor.MiddleCenter);
            TextAnchor previous = MutedStyle.alignment;
            MutedStyle.alignment = TextAnchor.UpperCenter;
            DrawText(new Rect(card.x + U(20f), card.y + U(40f), card.width - U(40f), textHeight), content.text, MutedStyle, MutedText);
            MutedStyle.alignment = previous;
        }

        internal static void DrawCircle(Rect rect, Color color) => DrawBox(rect, _circle, color);

        /// <summary>
        /// Width / height ratio of the Good Loss logo
        /// </summary>
        internal static float LogoAspect => _assets?.LogoAspect ?? 1f;

        /// <summary>
        /// Draws the Good Loss logo stretched to the rectangle; false when it could not be loaded
        /// </summary>
        internal static bool DrawLogo(Rect rect)
        {
            if (_assets?.Logo == null)
            {
                return false;
            }
            DrawBox(rect, _assets.Logo, White);
            return true;
        }

        /// <summary>
        /// Soft glow around a rectangle
        /// </summary>
        internal static void DrawGlow(Rect rect, Color color)
        {
            // A 9-slice border larger than the drawn rectangle distorts the halo into a block:
            // thin or small elements (bars, dots) use a tighter halo
            bool small = Math.Min(rect.width, rect.height) < 2f * LargeRadius;
            float spread = small ? SmallGlowSpread : GlowSpread;
            DrawBox(new Rect(rect.x - spread, rect.y - spread, rect.width + 2f * spread, rect.height + 2f * spread),
                small ? _smallGlow : _glow, color);
        }

        internal static void DrawOutline(Rect rect, Color border, Color fill, Corner corner)
        {
            DrawRect(rect, border, corner);
            float px = Px(1f);
            DrawRect(new Rect(rect.x + px, rect.y + px, rect.width - 2f * px, rect.height - 2f * px), fill, corner);
        }

        internal static void DrawProgress(Rect track, float fraction)
        {
            DrawRect(track, Border, Corner.Small);
            float width = track.width * Mathf.Clamp01(fraction);
            if (width < 1f)
            {
                return;
            }
            var fill = new Rect(track.x, track.y, Mathf.Max(width, track.height), track.height);
            DrawGlow(fill, WithAlpha(Accent, 0.25f));
            DrawAccentGradient(fill, true);
        }

        internal static void DrawText(Rect rect, string text, GUIStyle style, Color color, TextAnchor? alignment = null)
        {
            TextAnchor previous = style.alignment;
            if (alignment.HasValue)
            {
                style.alignment = alignment.Value;
            }
            style.normal.textColor = color;
            GUI.Label(rect, text ?? "", style);
            style.alignment = previous;
        }

        internal static bool DrawPrimaryButton(Rect rect, string text)
        {
            bool hover = IsHover(rect);
            if (hover)
            {
                DrawGlow(rect, WithAlpha(Accent, 0.45f));
            }
            DrawAccentGradient(rect, true);
            if (hover)
            {
                DrawRect(rect, WithAlpha(White, 0.08f), Corner.Medium);
            }
            ButtonStyle.normal.textColor = OnAccent;
            return GUI.Button(rect, text ?? "", ButtonStyle);
        }

        internal static bool DrawSecondaryButton(Rect rect, string text)
        {
            bool hover = IsHover(rect);
            DrawOutline(rect, WithAlpha(Accent, hover ? 0.9f : 0.45f), hover ? WithAlpha(AccentStrong, 0.35f) : PanelBottom, Corner.Small);
            SmallButtonStyle.normal.textColor = hover ? White : AccentLight;
            return GUI.Button(rect, text ?? "", SmallButtonStyle);
        }

        /// <summary>
        /// Small rounded label (map, role...) at a given position; returns its width
        /// </summary>
        internal static float DrawPill(float x, float y, string text, Color textColor, Color background)
        {
            var content = new GUIContent(text ?? "");
            float width = PillStyle.CalcSize(content).x + U(18f);
            var rect = new Rect(x, y, width, U(22f));
            DrawRect(rect, background, Corner.Small);
            DrawText(rect, content.text, PillStyle, textColor);
            return width;
        }

        internal static Color WithAlpha(Color color, float alpha)
        {
            return new Color { r = color.r, g = color.g, b = color.b, a = color.a * alpha };
        }

        private static void DrawBox(Rect rect, GUIStyle style, Color tint)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = tint;
            GUI.Box(rect, _empty, style);
            GUI.backgroundColor = previous;
        }

        private static void DrawFlowText(string text, GUIStyle style, Color color)
        {
            var content = new GUIContent(text ?? "");
            float height = style.CalcHeight(content, _area.width);
            DrawText(new Rect(_area.x, _y, _area.width, height), content.text, style, color);
            _y += height + U(6f);
        }

        // ------------------------------------------------------------------ styles and textures

        /// <summary>
        /// Builds the textures once and the styles whenever the UI scale changes
        /// </summary>
        internal static void EnsureStyles(float scale)
        {
            if (_empty == null)
            {
                LoadFonts();
                BuildTextures();
                _assets = GetAssets(Theme);
            }

            if (Math.Abs(scale - _stylesScale) < 0.01f)
            {
                return;
            }
            Scale = scale;
            _stylesScale = scale;

            BrandStyle = DisplayStyle(20, FontStyle.Bold, TextAnchor.MiddleLeft);
            LogoStyle = DisplayStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter);
            TabStyle = DisplayStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter);
            KeycapStyle = DisplayStyle(11, FontStyle.Bold, TextAnchor.MiddleCenter);
            ChipStyle = DisplayStyle(12, FontStyle.Bold, TextAnchor.MiddleLeft);
            SectionStyle = DisplayStyle(12, FontStyle.Bold, TextAnchor.MiddleLeft);
            HighlightStyle = DisplayStyle(32, FontStyle.Bold, TextAnchor.MiddleLeft);
            HighlightStyle.wordWrap = true;
            ResultStyle = DisplayStyle(28, FontStyle.Bold, TextAnchor.MiddleLeft);
            TileValueStyle = DisplayStyle(17, FontStyle.Bold, TextAnchor.MiddleLeft);
            ButtonStyle = DisplayStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter);
            SmallButtonStyle = DisplayStyle(12, FontStyle.Bold, TextAnchor.MiddleCenter);
            PillStyle = DisplayStyle(12, FontStyle.Bold, TextAnchor.MiddleCenter);
            AvatarStyle = DisplayStyle(24, FontStyle.Bold, TextAnchor.MiddleCenter);
            NameStyle = DisplayStyle(21, FontStyle.Bold, TextAnchor.MiddleLeft);
            CloseStyle = BodyStyle(20, FontStyle.Normal, TextAnchor.MiddleCenter, false);

            TextStyle = BodyStyle(14, FontStyle.Normal, TextAnchor.UpperLeft, true);
            MutedStyle = BodyStyle(13, FontStyle.Normal, TextAnchor.UpperLeft, true);
            CaptionStyle = DisplayStyle(11, FontStyle.Bold, TextAnchor.MiddleLeft);
            FieldNameStyle = BodyStyle(13, FontStyle.Normal, TextAnchor.MiddleLeft, false);
            FieldValueStyle = BodyStyle(14, FontStyle.Bold, TextAnchor.MiddleRight, true);
            RowStyle = BodyStyle(14, FontStyle.Normal, TextAnchor.MiddleLeft, false);
        }

        private static GUIStyle DisplayStyle(int size, FontStyle fontStyle, TextAnchor alignment)
        {
            return NewStyle(_displayFont, size, fontStyle, alignment, false);
        }

        private static GUIStyle BodyStyle(int size, FontStyle fontStyle, TextAnchor alignment, bool wordWrap)
        {
            return NewStyle(_bodyFont, size, fontStyle, alignment, wordWrap);
        }

        private static GUIStyle NewStyle(Font font, int size, FontStyle fontStyle, TextAnchor alignment, bool wordWrap)
        {
            var style = new GUIStyle();
            if (font != null)
            {
                style.font = font;
            }
            style.fontSize = Math.Max(8, Mathf.RoundToInt(size * Scale));
            style.fontStyle = fontStyle;
            style.alignment = alignment;
            style.wordWrap = wordWrap;
            style.clipping = TextClipping.Clip;
            style.richText = false;
            style.normal.textColor = Text;
            return style;
        }

        private static void LoadFonts()
        {
            if (_fontsLoaded)
            {
                return;
            }
            _fontsLoaded = true;
            _displayFont = LoadOsFont(DisplayFontNames);
            _bodyFont = LoadOsFont(BodyFontNames);
        }

        private static Font LoadOsFont(string[] names)
        {
            try
            {
                Font font = Font.CreateDynamicFontFromOSFont(names, 16);
                if (font != null)
                {
                    font.hideFlags = HideFlags.HideAndDontSave;
                }
                return font;
            }
            catch (Exception e)
            {
                GLMod.log("[Menu] System font unavailable (" + names[0] + "), using the default font: " + e.Message);
                return null;
            }
        }

        private static void BuildTextures()
        {
            _empty = new GUIContent("");
            InvisibleStyle = new GUIStyle();
            _fillSquare = FillStyle(Texture2D.whiteTexture, 0);
            _fillSmall = SafeStyle(() => FillStyle(RoundedTexture(SmallRadius, White, White, false, 0), SmallRadius));
            _fillMedium = SafeStyle(() => FillStyle(RoundedTexture(MediumRadius, White, White, false, 0), MediumRadius));
            _fillLarge = SafeStyle(() => FillStyle(RoundedTexture(LargeRadius, White, White, false, 0), LargeRadius));
            _circle = SafeStyle(() => FillStyle(CircleTexture(64), 0));
            _glow = SafeStyle(() => FillStyle(GlowTexture(LargeRadius, GlowSpread), LargeRadius + GlowSpread));
            _smallGlow = SafeStyle(() => FillStyle(GlowTexture(SmallGlowRadius, SmallGlowSpread), SmallGlowRadius + SmallGlowSpread));
        }

        /// <summary>
        /// Switches the theme; its textures are built on first use (EnsureStyles must have run once)
        /// </summary>
        internal static void ApplyTheme(MenuTheme theme)
        {
            theme ??= MenuTheme.GoodLoss;
            if (theme == Theme && _assets != null)
            {
                return;
            }

            Theme = theme;
            if (_empty != null)
            {
                _assets = GetAssets(theme);
            }
        }

        private static ThemeAssets GetAssets(MenuTheme theme)
        {
            if (_themeAssets.TryGetValue(theme.Id, out ThemeAssets assets))
            {
                return assets;
            }

            assets = new ThemeAssets
            {
                Panel = SafeStyle(() => FillStyle(RoundedTexture(LargeRadius, theme.PanelTop, theme.PanelBottom, false, 64), LargeRadius)),
                Accent = SafeStyle(() => FillStyle(RoundedTexture(SmallRadius, theme.AccentStrong, theme.Accent, true, 64), SmallRadius)),
                AccentSquare = SafeStyle(() => FillStyle(RoundedTexture(0, theme.AccentStrong, theme.Accent, true, 64), 0)),
            };
            assets.Logo = LoadLogo(theme.LogoResource, out assets.LogoAspect);
            _themeAssets[theme.Id] = assets;
            return assets;
        }

        /// <summary>
        /// Logo embedded in the DLL (PNG decoded by ImageConversion, as Among Us mods do for sprites)
        /// </summary>
        private static GUIStyle LoadLogo(string resource, out float aspect)
        {
            aspect = 1f;
            try
            {
                using Stream stream = typeof(MenuUI).Assembly.GetManifestResourceStream(resource);
                if (stream == null)
                {
                    GLMod.log("[Menu] Logo resource missing: " + resource);
                    return null;
                }

                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                var texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.wrapMode = TextureWrapMode.Clamp;
                if (!ImageConversion.LoadImage(texture, buffer.ToArray(), false))
                {
                    GLMod.log("[Menu] Logo could not be decoded: " + resource);
                    return null;
                }
                texture.filterMode = FilterMode.Trilinear;
                aspect = texture.width / (float)texture.height;
                return FillStyle(texture, 0);
            }
            catch (Exception e)
            {
                GLMod.log("[Menu] Logo unavailable, using the initials badge: " + e.Message);
                return null;
            }
        }

        private static GUIStyle SafeStyle(Func<GUIStyle> build)
        {
            try
            {
                return build();
            }
            catch (Exception e)
            {
                GLMod.log("[Menu] Texture generation failed, using flat shapes: " + e.Message);
                return _fillSquare;
            }
        }

        private static GUIStyle FillStyle(Texture2D texture, int border)
        {
            var style = new GUIStyle();
            style.normal.background = texture;
            if (border > 0)
            {
                // The 4-argument RectOffset constructor is stripped from the game
                var offset = new RectOffset();
                offset.left = border;
                offset.right = border;
                offset.top = border;
                offset.bottom = border;
                style.border = offset;
            }
            return style;
        }

        private static Texture2D NewTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        /// <summary>
        /// Rounded rectangle (anti-aliased corners) filled with a gradient from start to end,
        /// horizontal or vertical, with a stretchable middle of the given length (9-slice)
        /// </summary>
        private static Texture2D RoundedTexture(int radius, Color start, Color end, bool horizontal, int middle)
        {
            int width = 2 * radius + (horizontal ? Math.Max(2, middle) : 2);
            int height = 2 * radius + (horizontal ? 2 : Math.Max(2, middle));
            Texture2D texture = NewTexture(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = horizontal ? x / (float)(width - 1) : 1f - y / (float)(height - 1);
                    Color color = Lerp(start, end, t);
                    color.a *= CornerCoverage(x, y, width, height, radius);
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D CircleTexture(int size)
        {
            Texture2D texture = NewTexture(size, size);
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float alpha = Mathf.Clamp01(center - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    texture.SetPixel(x, y, new Color { r = 1f, g = 1f, b = 1f, a = alpha });
                }
            }
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Soft halo: opaque inside a rounded rectangle, fading over spread pixels around it
        /// </summary>
        private static Texture2D GlowTexture(int radius, int spread)
        {
            int size = 2 * (radius + spread) + 2;
            Texture2D texture = NewTexture(size, size);
            float inner = radius + spread;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float cx = Mathf.Clamp(px, inner, size - inner);
                    float cy = Mathf.Clamp(py, inner, size - inner);
                    float distance = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy)) - radius;
                    float falloff = 1f - Mathf.Clamp01(distance / spread);
                    texture.SetPixel(x, y, new Color { r = 1f, g = 1f, b = 1f, a = falloff * falloff * falloff });
                }
            }
            texture.Apply();
            return texture;
        }

        private static float CornerCoverage(int x, int y, int width, int height, int radius)
        {
            if (radius <= 0)
            {
                return 1f;
            }
            float px = x + 0.5f;
            float py = y + 0.5f;
            float cx = Mathf.Clamp(px, radius, width - radius);
            float cy = Mathf.Clamp(py, radius, height - radius);
            float distance = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
            return Mathf.Clamp01(radius - distance + 0.5f);
        }

        private static Color Lerp(Color a, Color b, float t)
        {
            return new Color
            {
                r = a.r + (b.r - a.r) * t,
                g = a.g + (b.g - a.g) * t,
                b = a.b + (b.b - a.b) * t,
                a = a.a + (b.a - a.a) * t,
            };
        }

        private static Color Rgb(int rgb, float alpha = 1f)
        {
            // Fields set directly: no IL2CPP call needed, so the palette can be built anywhere
            return new Color
            {
                r = ((rgb >> 16) & 0xFF) / 255f,
                g = ((rgb >> 8) & 0xFF) / 255f,
                b = (rgb & 0xFF) / 255f,
                a = alpha,
            };
        }
    }
}
