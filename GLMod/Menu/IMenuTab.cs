namespace GLMod.Menu
{
    /// <summary>
    /// A tab of the Good Loss menu. Built-in tabs and third-party mod tabs implement it
    /// (or use MenuService.RegisterTab(id, title, draw)). Every member is called on the Unity main thread.
    /// </summary>
    public interface IMenuTab
    {
        /// <summary>
        /// Unique and stable id, used to find, select or remove the tab (e.g. "mymod.rules";
        /// built-in tabs: GameConstants.MENU_TAB_*)
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Title shown in the tab bar
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Called when the tab becomes visible (menu opened on it, or tab selected). Refresh data here.
        /// </summary>
        void OnShown();

        /// <summary>
        /// Draws the tab content with the MenuUI widgets. Called on every IMGUI event while visible:
        /// keep it cheap and never start requests from it.
        /// </summary>
        void Draw();
    }
}
