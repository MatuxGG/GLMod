namespace GLMod.Menu
{
    /// <summary>
    /// Decides when the visible menu tab must be notified with IMenuTab.OnShown: when it becomes visible,
    /// and again when the connected account changes while it stays visible (login, logout or account switch),
    /// so a tab shown during the login loads its data once connected. Pure logic, used by MenuBehaviour.
    /// </summary>
    public sealed class ShownTabTracker
    {
        private string _account = "";

        /// <summary>
        /// Tab currently shown (null when the menu is closed)
        /// </summary>
        public IMenuTab Tab { get; private set; }

        /// <summary>
        /// Records the visible tab and the connected account ("" when not connected). Returns true when
        /// the visible tab must be notified; tabChanged tells whether it is a newly shown tab.
        /// </summary>
        public bool Update(IMenuTab visible, string account, out bool tabChanged)
        {
            account = account ?? "";
            tabChanged = visible != Tab;
            bool accountChanged = account != _account;
            Tab = visible;
            _account = account;
            return visible != null && (tabChanged || accountChanged);
        }
    }
}
