using GLMod.Menu;
using System;
using System.Collections.Generic;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for the Good Loss menu state: its tabs, the selected tab and whether it is open or hidden.
    /// Third-party mods use it (from their Load() or later) to hide the menu, remove built-in tabs or add their own.
    /// Must be used from the Unity main thread.
    /// </summary>
    public interface IMenuService
    {
        /// <summary>
        /// Gets or sets whether the menu exists for the player. False hides it completely (no "GOOD LOSS" tab,
        /// no panel, F2 / arrows / clicks are left to the game) and closes it. True by default.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Gets whether the menu is shown: enabled and with at least one tab
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Gets whether the menu is open (always false while it is not visible)
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Gets the tabs in display order (built-in tabs first, then third-party tabs in registration order)
        /// </summary>
        IReadOnlyList<IMenuTab> Tabs { get; }

        /// <summary>
        /// Gets the index of the selected tab, -1 when there is no tab
        /// </summary>
        int SelectedIndex { get; }

        /// <summary>
        /// Gets the selected tab, null when there is no tab
        /// </summary>
        IMenuTab SelectedTab { get; }

        /// <summary>
        /// Adds a tab at the end of the tab bar
        /// </summary>
        /// <param name="tab">Tab to add</param>
        /// <returns>False when the tab is null, has no id, or its id is already used</returns>
        bool RegisterTab(IMenuTab tab);

        /// <summary>
        /// Adds a tab drawn by a delegate at the end of the tab bar
        /// </summary>
        /// <param name="id">Unique id, e.g. "mymod.rules"</param>
        /// <param name="title">Title shown in the tab bar</param>
        /// <param name="draw">Draws the content with the MenuUI widgets</param>
        /// <param name="onShown">Optional, called when the tab becomes visible</param>
        /// <returns>The created tab (keep it to unregister or select it), null when the id is already used</returns>
        IMenuTab RegisterTab(string id, string title, Action draw, Action onShown = null);

        /// <summary>
        /// Adds a tab at a position of the tab bar (0 = first; clamped to the number of tabs)
        /// </summary>
        /// <param name="index">Position</param>
        /// <param name="tab">Tab to add</param>
        /// <returns>False when the tab is null, has no id, or its id is already used</returns>
        bool InsertTab(int index, IMenuTab tab);

        /// <summary>
        /// Finds a registered tab by id (built-in ids: GameConstants.MENU_TAB_*)
        /// </summary>
        /// <param name="id">Tab id</param>
        /// <returns>The tab, or null</returns>
        IMenuTab FindTab(string id);

        /// <summary>
        /// Removes a tab
        /// </summary>
        /// <param name="tab">Tab to remove</param>
        /// <returns>False when the tab was not registered</returns>
        bool UnregisterTab(IMenuTab tab);

        /// <summary>
        /// Removes a tab by id, e.g. a built-in tab: UnregisterTab(GameConstants.MENU_TAB_SESSIONS)
        /// </summary>
        /// <param name="id">Tab id</param>
        /// <returns>False when no tab has this id</returns>
        bool UnregisterTab(string id);

        /// <summary>
        /// Selects a tab
        /// </summary>
        /// <param name="tab">Tab to select</param>
        /// <returns>False when the tab is not registered</returns>
        bool Select(IMenuTab tab);

        /// <summary>
        /// Selects a tab by id
        /// </summary>
        /// <param name="id">Tab id</param>
        /// <returns>False when no tab has this id</returns>
        bool Select(string id);

        /// <summary>
        /// Selects the next tab (wraps around)
        /// </summary>
        void SelectNext();

        /// <summary>
        /// Selects the previous tab (wraps around)
        /// </summary>
        void SelectPrevious();

        /// <summary>
        /// Opens the menu (no effect while it is not visible)
        /// </summary>
        void Open();

        /// <summary>
        /// Closes the menu
        /// </summary>
        void Close();

        /// <summary>
        /// Opens the menu when closed, closes it when open
        /// </summary>
        void Toggle();
    }
}
