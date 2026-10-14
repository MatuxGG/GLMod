using System;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Widgets to draw the content of a menu tab, laid out from top to bottom in the tab area
    /// (IMGUI, 1080p reference units). Only valid inside IMenuTab.Draw.
    /// </summary>
    public static class MenuUI
    {
        // Good Loss site palette
        public static readonly Color Accent = Rgb(0xA78BFA);
        public static readonly Color Text = Rgb(0xEEF2F7);
        public static readonly Color MutedText = Rgb(0x8B95A5);
        public static readonly Color Success = Rgb(0x22C55E);
        public static readonly Color Danger = Rgb(0xEF4444);

        internal static readonly Color PanelBackground = Rgb(0x11161E, 0.96f);
        internal static readonly Color Raised = Rgb(0x1A212C);
        internal static readonly Color RaisedHover = Rgb(0x252E3B);
        internal static readonly Color AccentHover = Rgb(0x8B5CF6);
        internal static readonly Color OnAccent = Rgb(0x0A0D12);

        private const float Spacing = 6f;
        private const float ButtonHeight = 30f;
        private const float RowButtonWidth = 72f;
        private const float RowHeight = 28f;
        internal const float CornerRadius = 6f;
        internal const float LargeRadius = 10f;

        internal static GUIStyle BrandStyle;
        internal static GUIStyle HintStyle;
        internal static GUIStyle TabStyle;
        private static GUIStyle _heading;
        private static GUIStyle _highlight;
        private static GUIStyle _text;
        private static GUIStyle _muted;
        private static GUIStyle _fieldName;
        private static GUIStyle _primaryButton;
        private static GUIStyle _secondaryButton;
        private static GUIStyle _fillSquare;
        private static GUIStyle _fillRounded;
        private static GUIStyle _fillLarge;
        private static GUIContent _empty;

        private static Rect _area;
        private static float _y;

        /// <summary>
        /// Width available for the content of the tab
        /// </summary>
        public static float Width => _area.width;

        /// <summary>
        /// Height drawn so far in the current tab (used to scroll long tabs)
        /// </summary>
        internal static float ContentHeight => _y - _area.y;

        /// <summary>
        /// Section title
        /// </summary>
        public static void Heading(string text) => DrawText(text, _heading, Text);

        /// <summary>
        /// Large accent text (e.g. a rank name)
        /// </summary>
        public static void Highlight(string text) => DrawText(text, _highlight, Accent);

        /// <summary>
        /// Body text, wrapped
        /// </summary>
        public static void Label(string text) => DrawText(text, _text, Text);

        /// <summary>
        /// Body text in a given color, wrapped
        /// </summary>
        public static void Label(string text, Color color) => DrawText(text, _text, color);

        /// <summary>
        /// Secondary text (hints, explanations), wrapped
        /// </summary>
        public static void Muted(string text) => DrawText(text, _muted, MutedText);

        /// <summary>
        /// "name  value" line
        /// </summary>
        public static void Field(string name, string value) => Field(name, value, Text);

        /// <summary>
        /// "name  value" line with a colored value
        /// </summary>
        public static void Field(string name, string value, Color valueColor)
        {
            float nameWidth = Mathf.Round(_area.width * 0.34f);
            float valueWidth = _area.width - nameWidth;
            var nameContent = new GUIContent(name ?? "");
            var valueContent = new GUIContent(string.IsNullOrEmpty(value) ? "-" : value);

            _text.normal.textColor = valueColor;
            float height = Math.Max(_fieldName.CalcHeight(nameContent, nameWidth), _text.CalcHeight(valueContent, valueWidth));
            GUI.Label(new Rect(_area.x, _y, nameWidth, height), nameContent, _fieldName);
            GUI.Label(new Rect(_area.x + nameWidth, _y, valueWidth, height), valueContent, _text);
            _y += height + Spacing;
        }

        /// <summary>
        /// Accent button sized to its text
        /// </summary>
        /// <returns>True when clicked</returns>
        public static bool Button(string text)
        {
            float width = Math.Min(_area.width, _primaryButton.CalcSize(new GUIContent(text ?? "")).x + 32f);
            var rect = new Rect(_area.x, _y, width, ButtonHeight);
            _y += ButtonHeight + Spacing;
            return DrawButton(rect, text, true);
        }

        /// <summary>
        /// One-line row: text on the left, optional small button on the right
        /// </summary>
        /// <param name="text">Row text (cut when too long)</param>
        /// <param name="buttonText">Button text, or null for no button</param>
        /// <param name="textColor">Color of the row text</param>
        /// <returns>True when the button is clicked</returns>
        public static bool Row(string text, string buttonText, Color textColor)
        {
            var rowRect = new Rect(_area.x, _y, _area.width, RowHeight);
            DrawRect(rowRect, Raised, CornerRadius);

            float textWidth = _area.width - 20f - (buttonText != null ? RowButtonWidth + 4f : 0f);
            _text.normal.textColor = textColor;
            _text.wordWrap = false;
            GUI.Label(new Rect(rowRect.x + 10f, rowRect.y, textWidth, RowHeight), text ?? "", _text);
            _text.wordWrap = true;

            bool clicked = false;
            if (buttonText != null)
            {
                var buttonRect = new Rect(rowRect.xMax - RowButtonWidth - 3f, rowRect.y + 3f, RowButtonWidth, RowHeight - 6f);
                clicked = DrawButton(buttonRect, buttonText, false);
            }

            _y += RowHeight + 4f;
            return clicked;
        }

        /// <summary>
        /// Vertical gap
        /// </summary>
        public static void Space(float height = 10f) => _y += height;

        /// <summary>
        /// Thin horizontal line
        /// </summary>
        public static void Separator()
        {
            DrawRect(new Rect(_area.x, _y + 4f, _area.width, 1f), Raised, 0f);
            _y += 9f;
        }

        internal static void BeginContent(Rect area)
        {
            _area = area;
            _y = area.y;
        }

        internal static void EnsureStyles()
        {
            if (_text != null)
            {
                return;
            }

            BrandStyle = NewStyle(19, FontStyle.Bold, Accent, false);
            HintStyle = NewStyle(12, FontStyle.Normal, MutedText, false);
            TabStyle = NewStyle(14, FontStyle.Bold, MutedText, false);
            TabStyle.alignment = TextAnchor.MiddleCenter;
            _heading = NewStyle(16, FontStyle.Bold, Text, true);
            _highlight = NewStyle(26, FontStyle.Bold, Accent, true);
            _text = NewStyle(15, FontStyle.Normal, Text, true);
            _text.alignment = TextAnchor.MiddleLeft;
            _muted = NewStyle(13, FontStyle.Normal, MutedText, true);
            _fieldName = NewStyle(14, FontStyle.Normal, MutedText, true);
            _fieldName.alignment = TextAnchor.MiddleLeft;
            _primaryButton = NewStyle(14, FontStyle.Bold, OnAccent, false);
            _primaryButton.alignment = TextAnchor.MiddleCenter;
            _secondaryButton = NewStyle(13, FontStyle.Bold, Accent, false);
            _secondaryButton.alignment = TextAnchor.MiddleCenter;

            _empty = new GUIContent("");
            _fillSquare = FillStyle(Texture2D.whiteTexture, 0);
            _fillRounded = RoundedFillStyle((int)CornerRadius);
            _fillLarge = RoundedFillStyle((int)LargeRadius);
        }

        private static GUIStyle FillStyle(Texture2D texture, int border)
        {
            var style = new GUIStyle();
            style.normal.background = texture;
            if (border > 0)
            {
                var offset = new RectOffset();
                offset.left = border;
                offset.right = border;
                offset.top = border;
                offset.bottom = border;
                style.border = offset;
            }
            return style;
        }

        private static GUIStyle RoundedFillStyle(int radius)
        {
            try
            {
                // White texture with anti-aliased rounded corners, stretched as a 9-slice
                int size = radius * 2 + 2;
                var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float px = x + 0.5f;
                        float py = y + 0.5f;
                        float cx = Mathf.Clamp(px, radius, size - radius);
                        float cy = Mathf.Clamp(py, radius, size - radius);
                        float distance = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                        float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                        texture.SetPixel(x, y, new Color { r = 1f, g = 1f, b = 1f, a = alpha });
                    }
                }
                texture.Apply();
                return FillStyle(texture, radius);
            }
            catch (Exception e)
            {
                GLMod.log("[Menu] Rounded corners unavailable, using square corners: " + e.Message);
                return _fillSquare;
            }
        }

        /// <summary>
        /// Filled rectangle, with rounded corners when radius > 0.
        /// GUI.DrawTexture is stripped from the game: a GUI.Box with a white background tinted by
        /// GUI.backgroundColor is used instead.
        /// </summary>
        internal static void DrawRect(Rect rect, Color color, float radius)
        {
            GUIStyle style = radius >= LargeRadius ? _fillLarge : radius > 0f ? _fillRounded : _fillSquare;
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUI.Box(rect, _empty, style);
            GUI.backgroundColor = previous;
        }

        /// <summary>
        /// Button with a hover background: accent (primary) or raised (secondary)
        /// </summary>
        internal static bool DrawButton(Rect rect, string text, bool primary)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            Color background = primary
                ? (hover ? AccentHover : Accent)
                : (hover ? RaisedHover : PanelBackground);
            DrawRect(rect, background, CornerRadius);
            return GUI.Button(rect, text ?? "", primary ? _primaryButton : _secondaryButton);
        }

        private static void DrawText(string text, GUIStyle style, Color color)
        {
            var content = new GUIContent(text ?? "");
            style.normal.textColor = color;
            float height = style.CalcHeight(content, _area.width);
            GUI.Label(new Rect(_area.x, _y, _area.width, height), content, style);
            _y += height + Spacing;
        }

        private static GUIStyle NewStyle(int fontSize, FontStyle fontStyle, Color color, bool wordWrap)
        {
            var style = new GUIStyle();
            style.fontSize = fontSize;
            style.fontStyle = fontStyle;
            style.wordWrap = wordWrap;
            style.richText = false;
            style.normal.textColor = color;
            return style;
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
