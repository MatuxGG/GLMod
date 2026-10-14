using GLMod.Constants;
using GLMod.Services.Interfaces;
using BepInEx.Unity.IL2CPP;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Draws the Good Loss menu (IMGUI) and handles its keys. At rest it only shows a faint
    /// "Good Loss" label in the bottom-left corner; F2 or a click on it slides the panel in.
    /// Left / right arrows switch tabs, up / down arrows and the mouse wheel scroll the tab.
    /// </summary>
    public class MenuBehaviour : MonoBehaviour
    {
        // Layout in 1080p reference units, scaled to the screen height
        private const float ReferenceHeight = 1080f;
        private const float MinScale = 0.85f;
        private const float Margin = 14f;
        private const float PanelWidth = 420f;
        private const float PanelHeight = 440f;
        private const float PanelRadius = 10f;
        private const float Padding = 16f;
        private const float HeaderHeight = 44f;
        private const float TabBarHeight = 34f;
        private const float FooterHeight = 30f;
        private const float TabArrowWidth = 22f;
        private const float TabPadding = 22f;
        private const float SlideSeconds = 0.18f;
        private const float RestingAlpha = 0.4f;
        private const float RestingHoverAlpha = 0.9f;
        private const float WheelScrollStep = 18f;
        private const float ArrowScrollSpeed = 420f;

        private static MenuBehaviour _instance;
        private static KeyCode _toggleKey = KeyCode.F2;
        private static float _viewHeight = ReferenceHeight;
        private static float _slide;
        private static float _scroll;
        private static float _tabScroll;
        private static IMenuTab _shownTab;
        private static Rect _labelScreenRect;
        private static Rect _panelScreenRect;
        private static readonly HashSet<string> _loggedErrors = new HashSet<string>();

        public MenuBehaviour(IntPtr ptr) : base(ptr)
        {
        }

        /// <summary>
        /// True when the mouse is over the resting label or the panel: game buttons must ignore the click
        /// </summary>
        public static bool IsPointerOverMenu
        {
            get
            {
                Vector3 mouse = Input.mousePosition;
                var point = new Vector2(mouse.x, Screen.height - mouse.y);
                return (_slide < 1f && _labelScreenRect.Contains(point))
                    || (_slide > 0f && _panelScreenRect.Contains(point));
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
                        _scroll -= ArrowScrollSpeed * Time.unscaledDeltaTime;
                    }
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        _scroll += ArrowScrollSpeed * Time.unscaledDeltaTime;
                    }
                }

                _slide = Mathf.MoveTowards(_slide, menu.IsOpen ? 1f : 0f, Time.unscaledDeltaTime / SlideSeconds);
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

            Matrix4x4 previousMatrix = GUI.matrix;
            try
            {
                MenuUI.EnsureStyles();
                // Below 1080p the panel shrinks, but never so much that the text gets unreadable
                float scale = Mathf.Max(MinScale, Screen.height / ReferenceHeight);
                _viewHeight = Screen.height / scale;
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

                DrawRestingLabel(menu, scale);
                if (_slide > 0f)
                {
                    DrawPanel(menu, scale);
                }
            }
            catch (Exception e)
            {
                LogOnce("OnGUI", "[Menu] Drawing failed: " + e);
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
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

            try
            {
                visible.OnShown();
            }
            catch (Exception e)
            {
                LogOnce("OnShown:" + visible.Title, "[Menu] Tab '" + visible.Title + "' OnShown failed: " + e);
            }
        }

        private static void DrawRestingLabel(IMenuService menu, float scale)
        {
            float visibility = 1f - Ease(_slide);
            if (visibility <= 0.01f)
            {
                _labelScreenRect = Rect.zero;
                return;
            }

            const string text = "Good Loss";
            Vector2 size = MenuUI.BrandStyle.CalcSize(new GUIContent(text));
            var rect = new Rect(Margin, _viewHeight - Margin - size.y, size.x, size.y);
            _labelScreenRect = ToScreen(rect, scale);

            bool hover = rect.Contains(Event.current.mousePosition);
            Color previousColor = GUI.color;
            GUI.color = new Color { r = 1f, g = 1f, b = 1f, a = visibility * (hover ? RestingHoverAlpha : RestingAlpha) };
            if (GUI.Button(rect, text, MenuUI.BrandStyle))
            {
                menu.Open();
            }
            GUI.color = previousColor;
        }

        private static void DrawPanel(IMenuService menu, float scale)
        {
            float x = Mathf.Lerp(-PanelWidth - Margin, Margin, Ease(_slide));
            float y = Mathf.Max(Margin, _viewHeight - Margin - PanelHeight);
            var panel = new Rect(x, y, PanelWidth, PanelHeight);
            _panelScreenRect = ToScreen(panel, scale);
            MenuUI.DrawRect(panel, MenuUI.PanelBackground, PanelRadius);

            // Header: brand + close button
            GUI.Label(new Rect(x + Padding, y + 12f, 200f, 26f), "Good Loss", MenuUI.BrandStyle);
            var close = new Rect(x + PanelWidth - Padding - 26f, y + 10f, 26f, 26f);
            if (MenuUI.DrawButton(close, "×", false))
            {
                menu.Close();
            }

            var tabBar = new Rect(x + Padding, y + HeaderHeight, PanelWidth - 2f * Padding, TabBarHeight);
            DrawTabBar(menu, tabBar);
            MenuUI.DrawRect(new Rect(tabBar.x, tabBar.yMax, tabBar.width, 1f), MenuUI.Raised, 0f);

            float contentTop = tabBar.yMax + 12f;
            var content = new Rect(x + Padding, contentTop, PanelWidth - 2f * Padding, y + PanelHeight - FooterHeight - contentTop);
            DrawContent(menu.SelectedTab, content);

            GUI.Label(new Rect(x + Padding, y + PanelHeight - FooterHeight + 6f, PanelWidth - 2f * Padding, FooterHeight - 10f),
                "←  →  tabs      ↑  ↓  scroll      " + _toggleKey + "  close", MenuUI.HintStyle);
        }

        private static void DrawTabBar(IMenuService menu, Rect bar)
        {
            IReadOnlyList<IMenuTab> tabs = menu.Tabs;
            if (tabs.Count == 0)
            {
                return;
            }

            if (MenuUI.DrawButton(new Rect(bar.x, bar.y + 4f, TabArrowWidth, bar.height - 8f), "‹", false))
            {
                menu.SelectPrevious();
            }
            if (MenuUI.DrawButton(new Rect(bar.xMax - TabArrowWidth, bar.y + 4f, TabArrowWidth, bar.height - 8f), "›", false))
            {
                menu.SelectNext();
            }

            var strip = new Rect(bar.x + TabArrowWidth + 4f, bar.y, bar.width - 2f * (TabArrowWidth + 4f), bar.height);
            var widths = new float[tabs.Count];
            var starts = new float[tabs.Count];
            float total = 0f;
            for (int i = 0; i < tabs.Count; i++)
            {
                widths[i] = MenuUI.TabStyle.CalcSize(new GUIContent(tabs[i].Title)).x + TabPadding;
                starts[i] = total;
                total += widths[i];
            }

            // Keep the selected tab visible when the tabs are wider than the bar
            int selected = menu.SelectedIndex;
            if (starts[selected] < _tabScroll)
            {
                _tabScroll = starts[selected];
            }
            if (starts[selected] + widths[selected] > _tabScroll + strip.width)
            {
                _tabScroll = starts[selected] + widths[selected] - strip.width;
            }
            _tabScroll = Mathf.Clamp(_tabScroll, 0f, Mathf.Max(0f, total - strip.width));
            float offset = total < strip.width ? (strip.width - total) / 2f : -_tabScroll;

            GUI.BeginGroup(strip);
            try
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    var rect = new Rect(offset + starts[i], 0f, widths[i], strip.height);
                    bool isSelected = i == selected;
                    bool hover = rect.Contains(Event.current.mousePosition);
                    MenuUI.TabStyle.normal.textColor = isSelected ? MenuUI.Accent : (hover ? MenuUI.Text : MenuUI.MutedText);
                    if (isSelected)
                    {
                        MenuUI.DrawRect(new Rect(rect.x + 8f, rect.yMax - 3f, rect.width - 16f, 3f), MenuUI.Accent, 0f);
                    }
                    if (GUI.Button(rect, tabs[i].Title, MenuUI.TabStyle))
                    {
                        menu.Select(tabs[i]);
                    }
                }
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private static void DrawContent(IMenuTab tab, Rect content)
        {
            float contentHeight = 0f;
            GUI.BeginGroup(content);
            try
            {
                MenuUI.BeginContent(new Rect(0f, -_scroll, content.width, content.height));
                if (tab == null)
                {
                    MenuUI.Muted("No tab.");
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
                        MenuUI.Muted("This tab could not be displayed (see the BepInEx log).");
                    }
                }
                contentHeight = MenuUI.ContentHeight;
            }
            finally
            {
                GUI.EndGroup();
            }

            Event current = Event.current;
            if (current.type == EventType.ScrollWheel && content.Contains(current.mousePosition))
            {
                _scroll += current.delta.y * WheelScrollStep;
                current.Use();
            }

            float maxScroll = Mathf.Max(0f, contentHeight - content.height);
            _scroll = Mathf.Clamp(_scroll, 0f, maxScroll);
            if (maxScroll > 0f)
            {
                // Scroll indicator
                float barHeight = Mathf.Max(24f, content.height * content.height / contentHeight);
                float barY = content.y + (content.height - barHeight) * (_scroll / maxScroll);
                MenuUI.DrawRect(new Rect(content.xMax + 6f, barY, 3f, barHeight), MenuUI.MutedText, 0f);
            }
        }

        private static float Ease(float t)
        {
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        private static Rect ToScreen(Rect rect, float scale)
        {
            return new Rect(rect.x * scale, rect.y * scale, rect.width * scale, rect.height * scale);
        }

        private static void LogOnce(string key, string message)
        {
            if (_loggedErrors.Add(key))
            {
                GLMod.log(message);
            }
        }
    }
}
