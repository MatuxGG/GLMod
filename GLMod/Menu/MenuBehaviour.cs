using BepInEx.Unity.IL2CPP;
using GLMod.Class;
using GLMod.Constants;
using GLMod.Services.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Draws the Good Loss menu (IMGUI) and handles its keys. At rest it only shows a small
    /// brand chip ("GOOD LOSS", or "AMONG US RANKED" with the Among Us Ranked theme) at the bottom edge; F2 or a click on it slides the panel in.
    /// Left / right arrows switch tabs, up / down arrows and the mouse wheel scroll the tab.
    /// Layout values are 1080p reference units, converted to screen pixels with MenuUI.U.
    /// </summary>
    public class MenuBehaviour : MonoBehaviour
    {
        private const float ReferenceHeight = 1080f;
        private const float MinScale = 0.85f;
        private const float Margin = 10f;
        private const float PanelWidth = 440f;
        private const float PanelHeight = 500f;
        private const float Padding = 20f;
        private const float HeaderHeight = 70f;
        private const float TabBarHeight = 40f;
        private const float FooterHeight = 42f;
        private const float ChipHeight = 22f;
        private const float ChipLeft = 12f;
        private const float OpenSeconds = 0.32f;
        private const float CloseSeconds = 0.2f;
        private const float ContentSeconds = 0.22f;
        private const float WheelScrollStep = 22f;
        private const float ArrowScrollSpeed = 520f;

        private static MenuBehaviour _instance;
        private static KeyCode _toggleKey = KeyCode.F2;
        private static float _slide;
        private static float _scroll;
        private static float _tabScroll;
        private static float _chipHover;
        private static float _contentProgress = 1f;
        private static int _contentDirection = 1;
        private static float _indicatorX = -1f;
        private static float _indicatorWidth;
        private static int _lastSelectedIndex = -1;
        private static IMenuTab _shownTab;
        private static Rect _chipRect;
        private static Rect _panelRect;
        private static readonly HashSet<string> _loggedErrors = new HashSet<string>();

        public MenuBehaviour(IntPtr ptr) : base(ptr)
        {
        }

        /// <summary>
        /// True when the mouse is over the resting chip or the panel: game buttons must ignore the click
        /// </summary>
        public static bool IsPointerOverMenu
        {
            get
            {
                if (GLMod.MenuService?.IsVisible != true)
                {
                    return false;
                }
                Vector3 mouse = Input.mousePosition;
                var point = new Vector2(mouse.x, Screen.height - mouse.y);
                return (_slide < 1f && _chipRect.Contains(point))
                    || (_slide > 0f && _panelRect.Contains(point));
            }
        }

        /// <summary>
        /// True while the open menu uses the arrow keys: the player must not move
        /// </summary>
        public static bool IsCapturingArrows =>
            GLMod.MenuService?.IsOpen == true
            && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)
                || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow));

        public static void Init(BasePlugin plugin)
        {
            if (_instance != null)
            {
                return;
            }

            if (Enum.TryParse(GameConstants.MENU_TOGGLE_KEY, out KeyCode key))
            {
                _toggleKey = key;
            }

            // BepInEx registers the type in IL2CPP and attaches it to its persistent manager object
            _instance = plugin.AddComponent<MenuBehaviour>();
        }

        public void Update()
        {
            IMenuService menu = GLMod.MenuService;
            if (menu == null)
            {
                return;
            }

            try
            {
                float dt = Time.unscaledDeltaTime;
                GLMod.TranslationService?.RefreshLanguage();
                if (Input.GetKeyDown(_toggleKey))
                {
                    menu.Toggle();
                }

                if (menu.IsOpen)
                {
                    if (Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        menu.SelectPrevious();
                    }
                    if (Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        menu.SelectNext();
                    }
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        _scroll -= ArrowScrollSpeed * MenuUI.Scale * dt;
                    }
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        _scroll += ArrowScrollSpeed * MenuUI.Scale * dt;
                    }
                }

                // Hidden by a mod (or no tab left): disappears at once, without the closing slide
                _slide = menu.IsVisible
                    ? Mathf.MoveTowards(_slide, menu.IsOpen ? 1f : 0f, dt / (menu.IsOpen ? OpenSeconds : CloseSeconds))
                    : 0f;
                _contentProgress = Mathf.MoveTowards(_contentProgress, 1f, dt / ContentSeconds);
                TrackSelection(menu);
                NotifyShownTab(menu);
            }
            catch (Exception e)
            {
                LogOnce("Update", "[Menu] Update failed: " + e.Message);
            }
        }

        public void OnGUI()
        {
            IMenuService menu = GLMod.MenuService;
            if (menu == null)
            {
                return;
            }
            if (!menu.IsVisible)
            {
                _chipRect = Rect.zero;
                _panelRect = Rect.zero;
                return;
            }

            Color previousColor = GUI.color;
            try
            {
                MenuUI.EnsureStyles(Mathf.Max(MinScale, Screen.height / ReferenceHeight));
                MenuUI.ApplyTheme(MenuTheme.ForMod(GLMod.ConfigService?.ModName));
                DrawChip(menu);
                if (_slide > 0f)
                {
                    DrawPanel(menu);
                }
            }
            catch (Exception e)
            {
                LogOnce("OnGUI", "[Menu] Drawing failed: " + e);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        // ------------------------------------------------------------------ state

        private static void TrackSelection(IMenuService menu)
        {
            int selected = menu.SelectedIndex;
            if (selected == _lastSelectedIndex)
            {
                return;
            }

            // Content slides in from the side of the newly selected tab
            _contentDirection = selected > _lastSelectedIndex ? 1 : -1;
            _lastSelectedIndex = selected;
            _contentProgress = 0f;
            _scroll = 0f;
        }

        private static void NotifyShownTab(IMenuService menu)
        {
            IMenuTab visible = menu.IsOpen ? menu.SelectedTab : null;
            if (visible == _shownTab)
            {
                return;
            }

            _shownTab = visible;
            _scroll = 0f;
            if (visible == null)
            {
                return;
            }

            _contentProgress = 0f;
            try
            {
                visible.OnShown();
            }
            catch (Exception e)
            {
                LogOnce("OnShown:" + visible.Title, "[Menu] Tab '" + visible.Title + "' OnShown failed: " + e);
            }
        }

        // ------------------------------------------------------------------ resting chip

        private static void DrawChip(IMenuService menu)
        {
            float visibility = 1f - Mathf.Clamp01(_slide * 2.5f);
            if (visibility <= 0.01f)
            {
                _chipRect = Rect.zero;
                return;
            }

            string text = MenuUI.Theme.BrandName;
            string key = _toggleKey.ToString();
            float u = MenuUI.U(1f);
            float textWidth = MenuUI.ChipStyle.CalcSize(new GUIContent(text)).x;
            float keyWidth = MenuUI.KeycapStyle.CalcSize(new GUIContent(key)).x + MenuUI.U(8f);
            float height = MenuUI.U(ChipHeight);
            float textX = MenuUI.U(16f);
            float baseWidth = textX + textWidth + MenuUI.U(9f);

            // Hover: the chip lights up and widens to reveal the key
            bool hover = _chipRect.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
            {
                _chipHover = Mathf.MoveTowards(_chipHover, hover ? 1f : 0f, Time.unscaledDeltaTime / 0.15f);
            }
            float width = baseWidth + (keyWidth + MenuUI.U(6f)) * Ease.OutCubic(_chipHover);

            // Flush with the bottom edge of the screen: the background goes below it, so only its top corners are rounded
            var rect = new Rect(MenuUI.U(ChipLeft), Screen.height - height, width, height);
            _chipRect = rect;
            var background = new Rect(rect.x, rect.y, rect.width, rect.height + MenuUI.U(8f));

            float alpha = visibility * (0.55f + 0.45f * _chipHover);
            GUI.color = new Color { r = 1f, g = 1f, b = 1f, a = alpha };
            if (_chipHover > 0.01f)
            {
                MenuUI.DrawGlow(background, MenuUI.WithAlpha(MenuUI.Accent, 0.25f * _chipHover));
            }
            MenuUI.DrawRect(background, MenuUI.WithAlpha(MenuUI.PanelBottom, 0.55f + 0.35f * _chipHover), MenuUI.Corner.Small);

            // Accent bar breathing slowly
            float breath = 0.65f + 0.35f * Mathf.Sin(Time.realtimeSinceStartup * 2.2f);
            var bar = new Rect(rect.x + MenuUI.U(7f), rect.y + MenuUI.U(6f), Mathf.Max(2f, MenuUI.U(3f)), rect.height - MenuUI.U(12f));
            MenuUI.DrawRect(bar, MenuUI.WithAlpha(MenuUI.Accent, 0.6f + 0.4f * breath), MenuUI.Corner.None);

            MenuUI.DrawText(new Rect(rect.x + textX, rect.y, textWidth + u, rect.height), text, MenuUI.ChipStyle, MenuUI.Text);
            if (_chipHover > 0.3f)
            {
                var keycap = new Rect(rect.x + textX + textWidth + MenuUI.U(6f), rect.y + MenuUI.U(4f), keyWidth, rect.height - MenuUI.U(8f));
                DrawKeycap(keycap, key);
            }

            if (GUI.Button(rect, "", MenuUI.InvisibleStyle))
            {
                menu.Open();
            }
        }

        // ------------------------------------------------------------------ panel

        private static void DrawPanel(IMenuService menu)
        {
            float progress = menu.IsOpen ? Ease.OutBack(_slide) : Ease.OutCubic(_slide);
            float width = MenuUI.U(PanelWidth);
            float height = Mathf.Min(MenuUI.U(PanelHeight), Screen.height - 2f * MenuUI.U(Margin));
            float hiddenX = -width - MenuUI.U(Margin) - MenuUI.U(40f);
            float x = Mathf.LerpUnclamped(hiddenX, MenuUI.U(Margin), progress);
            float y = Screen.height - MenuUI.U(Margin) - height;
            var panel = new Rect(x, y, width, height);
            _panelRect = panel;

            GUI.color = new Color { r = 1f, g = 1f, b = 1f, a = Mathf.Clamp01(_slide * 1.8f) };
            MenuUI.DrawGlow(panel, MenuUI.WithAlpha(MenuUI.AccentStrong, 0.35f));
            MenuUI.DrawOutline(panel, MenuUI.WithAlpha(MenuUI.Accent, 0.28f), MenuUI.PanelBottom, MenuUI.Corner.Large);
            float px = MenuUI.Px(1f);
            MenuUI.DrawPanelBackground(new Rect(panel.x + px, panel.y + px, panel.width - 2f * px, panel.height - 2f * px));
            // Accent line along the top edge
            MenuUI.DrawAccentGradient(new Rect(panel.x + MenuUI.U(14f), panel.y + px, panel.width - MenuUI.U(28f), MenuUI.Px(2f)), false);

            DrawHeader(menu, panel);

            var tabBar = new Rect(panel.x + MenuUI.U(Padding) - MenuUI.U(6f), panel.y + MenuUI.U(HeaderHeight),
                panel.width - 2f * MenuUI.U(Padding) + MenuUI.U(12f), MenuUI.U(TabBarHeight));
            DrawTabBar(menu, tabBar);
            MenuUI.DrawRect(new Rect(panel.x + px, tabBar.yMax, panel.width - 2f * px, px), MenuUI.Border, MenuUI.Corner.None);

            float contentTop = tabBar.yMax + MenuUI.U(16f);
            float contentBottom = panel.yMax - MenuUI.U(FooterHeight) - MenuUI.U(6f);
            var content = new Rect(panel.x + MenuUI.U(Padding), contentTop, panel.width - 2f * MenuUI.U(Padding), contentBottom - contentTop);
            DrawContent(menu.SelectedTab, content);

            DrawFooter(new Rect(panel.x, panel.yMax - MenuUI.U(FooterHeight), panel.width, MenuUI.U(FooterHeight)));
        }

        private static void DrawHeader(IMenuService menu, Rect panel)
        {
            float pad = MenuUI.U(Padding);
            float logoSize = MenuUI.U(36f);
            var logo = new Rect(panel.x + pad, panel.y + MenuUI.U(17f), Mathf.Round(MenuUI.U(40f) * MenuUI.LogoAspect), MenuUI.U(40f));
            if (!MenuUI.DrawLogo(logo))
            {
                // Fallback badge when the embedded logo cannot be loaded
                logo = new Rect(panel.x + pad, panel.y + MenuUI.U(18f), logoSize, logoSize);
                MenuUI.DrawGlow(logo, MenuUI.WithAlpha(MenuUI.Accent, 0.3f));
                MenuUI.DrawAccentGradient(logo, true);
                MenuUI.DrawText(logo, MenuUI.Theme.BrandInitials, MenuUI.LogoStyle, MenuUI.OnAccent);
            }

            float textX = logo.xMax + MenuUI.U(12f);
            MenuUI.DrawText(new Rect(textX, logo.y - MenuUI.U(2f), MenuUI.U(220f), MenuUI.U(24f)), MenuUI.Theme.BrandName, MenuUI.BrandStyle, MenuUI.Text);
            MenuUI.DrawText(new Rect(textX, logo.y + MenuUI.U(20f), MenuUI.U(220f), MenuUI.U(18f)), "GLMOD  v" + GLMod.Version, MenuUI.CaptionStyle, MenuUI.DimText);

            float closeSize = MenuUI.U(30f);
            DrawRoomCodes(panel.xMax - pad - closeSize - MenuUI.U(14f), logo.y);
            var close = new Rect(panel.xMax - pad - closeSize, panel.y + MenuUI.U(21f), closeSize, closeSize);
            bool hover = MenuUI.IsHover(close);
            MenuUI.DrawRect(close, hover ? MenuUI.WithAlpha(MenuUI.Danger, 0.85f) : MenuUI.Surface, MenuUI.Corner.Small);
            MenuUI.CloseStyle.normal.textColor = hover ? MenuUI.White : MenuUI.Text;
            if (GUI.Button(close, "×", MenuUI.CloseStyle))
            {
                menu.Close();
            }
        }

        /// <summary>
        /// Code of the room the player is in and, below it, the code of their account (the one their
        /// games get on the Good Loss servers when it is free): both differ when they join someone else.
        /// </summary>
        private static void DrawRoomCodes(float right, float top)
        {
            ITranslationService texts = GLMod.TranslationService;
            string room = PlayModeHelper.CurrentOnlineGameCode();
            if (room != null)
            {
                DrawRoomCodeLine(right, top, texts.Get("menu.room.current"), room);
            }

            if (GoodLossServers.IsCurrentRegion() && GLMod.AuthService.IsLoggedIn)
            {
                string mine = GLMod.RoomTicketService.RoomCode ?? texts.Get("menu.room.none");
                DrawRoomCodeLine(right, top + MenuUI.U(20f), texts.Get("menu.room.mine"), mine);
            }
        }

        private static void DrawRoomCodeLine(float right, float y, string label, string code)
        {
            float height = MenuUI.U(18f);
            float codeWidth = MenuUI.CaptionStyle.CalcSize(new GUIContent(code)).x;
            float labelWidth = MenuUI.CaptionStyle.CalcSize(new GUIContent(label)).x;
            float gap = MenuUI.U(8f);
            MenuUI.DrawText(new Rect(right - codeWidth, y, codeWidth, height), code, MenuUI.CaptionStyle, MenuUI.Accent);
            MenuUI.DrawText(new Rect(right - codeWidth - gap - labelWidth, y, labelWidth, height), label, MenuUI.CaptionStyle, MenuUI.DimText);
        }

        private static void DrawTabBar(IMenuService menu, Rect bar)
        {
            IReadOnlyList<IMenuTab> tabs = menu.Tabs;
            if (tabs.Count == 0)
            {
                return;
            }

            var widths = new float[tabs.Count];
            var starts = new float[tabs.Count];
            var titles = new string[tabs.Count];
            float total = 0f;
            for (int i = 0; i < tabs.Count; i++)
            {
                titles[i] = (tabs[i].Title ?? "").ToUpperInvariant();
                widths[i] = MenuUI.TabStyle.CalcSize(new GUIContent(titles[i])).x + MenuUI.U(24f);
                starts[i] = total;
                total += widths[i];
            }

            // Keep the selected tab visible when the tabs are wider than the bar
            int selected = menu.SelectedIndex;
            if (starts[selected] < _tabScroll)
            {
                _tabScroll = starts[selected];
            }
            if (starts[selected] + widths[selected] > _tabScroll + bar.width)
            {
                _tabScroll = starts[selected] + widths[selected] - bar.width;
            }
            _tabScroll = Mathf.Clamp(_tabScroll, 0f, Mathf.Max(0f, total - bar.width));
            float offset = total < bar.width ? 0f : -_tabScroll;

            // Indicator gliding to the selected tab
            float targetX = offset + starts[selected] + MenuUI.U(10f);
            float targetWidth = widths[selected] - MenuUI.U(20f);
            if (_indicatorX < 0f)
            {
                _indicatorX = targetX;
                _indicatorWidth = targetWidth;
            }
            else if (Event.current.type == EventType.Repaint)
            {
                float follow = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 16f);
                _indicatorX += (targetX - _indicatorX) * follow;
                _indicatorWidth += (targetWidth - _indicatorWidth) * follow;
            }

            GUI.BeginGroup(bar);
            try
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    var rect = new Rect(offset + starts[i], 0f, widths[i], bar.height);
                    bool isSelected = i == selected;
                    bool hover = rect.Contains(Event.current.mousePosition);
                    if (hover && !isSelected)
                    {
                        MenuUI.DrawRect(new Rect(rect.x + MenuUI.U(4f), rect.y + MenuUI.U(5f), rect.width - MenuUI.U(8f), rect.height - MenuUI.U(10f)),
                            MenuUI.Surface, MenuUI.Corner.Small);
                    }
                    MenuUI.TabStyle.normal.textColor = isSelected ? MenuUI.White : (hover ? MenuUI.Text : MenuUI.DimText);
                    if (GUI.Button(rect, titles[i], MenuUI.TabStyle))
                    {
                        menu.Select(tabs[i]);
                    }
                }

                var indicator = new Rect(_indicatorX, bar.height - MenuUI.Px(3f), _indicatorWidth, MenuUI.Px(3f));
                MenuUI.DrawGlow(indicator, MenuUI.WithAlpha(MenuUI.Accent, 0.5f));
                MenuUI.DrawAccentGradient(indicator, false);
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private static void DrawContent(IMenuTab tab, Rect content)
        {
            float eased = Ease.OutCubic(_contentProgress);
            float slideOffset = (1f - eased) * MenuUI.U(18f) * _contentDirection;
            Color panelColor = GUI.color;
            GUI.color = new Color { r = 1f, g = 1f, b = 1f, a = panelColor.a * eased };

            float contentHeight = 0f;
            GUI.BeginGroup(content);
            try
            {
                MenuUI.BeginContent(new Rect(slideOffset, -_scroll, content.width, content.height));
                if (tab == null)
                {
                    MenuUI.Muted(Text("menu.noTab"));
                }
                else
                {
                    try
                    {
                        tab.Draw();
                    }
                    catch (Exception e)
                    {
                        LogOnce("Draw:" + tab.Title, "[Menu] Tab '" + tab.Title + "' failed to draw: " + e);
                        MenuUI.Muted(Text("menu.tabError"));
                    }
                }
                contentHeight = MenuUI.ContentHeight;
            }
            finally
            {
                GUI.EndGroup();
                GUI.color = panelColor;
            }

            Event current = Event.current;
            if (current.type == EventType.ScrollWheel && content.Contains(current.mousePosition))
            {
                _scroll += current.delta.y * MenuUI.U(WheelScrollStep);
                current.Use();
            }

            float maxScroll = Mathf.Max(0f, contentHeight - content.height);
            _scroll = Mathf.Clamp(_scroll, 0f, maxScroll);
            if (maxScroll > 0f)
            {
                float trackX = content.xMax + MenuUI.U(8f);
                MenuUI.DrawRect(new Rect(trackX, content.y, MenuUI.Px(3f), content.height), MenuUI.Surface, MenuUI.Corner.None);
                float barHeight = Mathf.Max(MenuUI.U(28f), content.height * content.height / contentHeight);
                float barY = content.y + (content.height - barHeight) * (_scroll / maxScroll);
                MenuUI.DrawRect(new Rect(trackX, barY, MenuUI.Px(3f), barHeight), MenuUI.Accent, MenuUI.Corner.None);
            }
        }

        private static void DrawFooter(Rect footer)
        {
            float px = MenuUI.Px(1f);
            MenuUI.DrawRect(new Rect(footer.x + px, footer.y, footer.width - 2f * px, px), MenuUI.Border, MenuUI.Corner.None);

            float x = footer.x + MenuUI.U(Padding);
            float keyY = footer.y + MenuUI.U(11f);
            float keyHeight = MenuUI.U(20f);
            x = DrawHint(x, keyY, keyHeight, new[] { "←", "→" }, Text("menu.hint.tabs"));
            x = DrawHint(x + MenuUI.U(14f), keyY, keyHeight, new[] { "↑", "↓" }, Text("menu.hint.scroll"));
            DrawHint(x + MenuUI.U(14f), keyY, keyHeight, new[] { _toggleKey.ToString() }, Text("menu.hint.close"));

            MenuUI.DrawText(new Rect(footer.x, footer.y, footer.width - MenuUI.U(Padding), footer.height),
                MenuUI.Theme.SiteLabel, MenuUI.CaptionStyle, MenuUI.DimText, TextAnchor.MiddleRight);
        }

        private static float DrawHint(float x, float y, float height, string[] keys, string label)
        {
            foreach (string key in keys)
            {
                float width = Mathf.Max(height, MenuUI.KeycapStyle.CalcSize(new GUIContent(key)).x + MenuUI.U(10f));
                DrawKeycap(new Rect(x, y, width, height), key);
                x += width + MenuUI.U(4f);
            }

            float labelWidth = MenuUI.CaptionStyle.CalcSize(new GUIContent(label)).x;
            MenuUI.DrawText(new Rect(x + MenuUI.U(3f), y, labelWidth + MenuUI.U(2f), height), label, MenuUI.CaptionStyle, MenuUI.DimText);
            return x + MenuUI.U(3f) + labelWidth;
        }

        private static void DrawKeycap(Rect rect, string key)
        {
            MenuUI.DrawOutline(rect, MenuUI.Border, MenuUI.Surface, MenuUI.Corner.Small);
            MenuUI.DrawRect(new Rect(rect.x, rect.yMax - MenuUI.Px(2f), rect.width, MenuUI.Px(2f)), MenuUI.Border, MenuUI.Corner.None);
            MenuUI.DrawText(rect, key, MenuUI.KeycapStyle, MenuUI.Text);
        }

        private static string Text(string key) => GLMod.TranslationService?.Get(key) ?? key;

        private static void LogOnce(string key, string message)
        {
            if (_loggedErrors.Add(key))
            {
                GLMod.log(message);
            }
        }

        /// <summary>
        /// Easing curves of the menu animations
        /// </summary>
        private static class Ease
        {
            public static float OutCubic(float t)
            {
                float inverse = 1f - Mathf.Clamp01(t);
                return 1f - inverse * inverse * inverse;
            }

            // Overshoots slightly before settling, for the opening slide
            public static float OutBack(float t)
            {
                const float overshoot = 1.4f;
                float shifted = Mathf.Clamp01(t) - 1f;
                return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
            }
        }
    }
}
