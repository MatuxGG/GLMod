using GLMod.Menu;
using System;
using System.Collections.Generic;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for the Good Loss menu state: its tabs, the selected tab and whether it is open.
    /// Must be used from the Unity main thread.
    /// </summary>
    public interface IMenuService
    {
        /// <summary>
        /// Gets whether the menu is open
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
        /// <returns>False when the tab is null or already registered</returns>
        bool RegisterTab(IMenuTab tab);

        /// <summary>
        /// Adds a tab drawn by a delegate at the end of the tab bar
        /// </summary>
        /// <param name="title">Title shown in the tab bar</param>
        /// <param name="draw">Draws the content with the MenuUI widgets</param>
        /// <param name="onShown">Optional, called when the tab becomes visible</param>
        /// <returns>The created tab (keep it to unregister or select it)</returns>
        IMenuTab RegisterTab(string title, Action draw, Action onShown = null);

        /// <summary>
        /// Removes a tab
        /// </summary>
        /// <param name="tab">Tab to remove</param>
        /// <returns>False when the tab was not registered</returns>
        bool UnregisterTab(IMenuTab tab);

        /// <summary>
        /// Selects a tab
        /// </summary>
        /// <param name="tab">Tab to select</param>
        /// <returns>False when the tab is not registered</returns>
        bool Select(IMenuTab tab);

        /// <summary>
        /// Selects the next tab (wraps around)
        /// </summary>
        void SelectNext();

        /// <summary>
        /// Selects the previous tab (wraps around)
        /// </summary>
        void SelectPrevious();

        /// <summary>
        /// Opens the menu
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
