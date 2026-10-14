using GLMod.Menu;
using GLMod.Services.Interfaces;
using System;
using System.Collections.Generic;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// State of the Good Loss menu. Pure logic: drawing and input live in MenuBehaviour.
    /// </summary>
    public class MenuService : IMenuService
    {
        private readonly List<IMenuTab> _tabs = new List<IMenuTab>();
        private int _selectedIndex;

        public bool IsOpen { get; private set; }
        public IReadOnlyList<IMenuTab> Tabs => _tabs;
        public int SelectedIndex => _tabs.Count == 0 ? -1 : _selectedIndex;
        public IMenuTab SelectedTab => _tabs.Count == 0 ? null : _tabs[_selectedIndex];

        public bool RegisterTab(IMenuTab tab)
        {
            if (tab == null || _tabs.Contains(tab))
            {
                return false;
            }

            _tabs.Add(tab);
            return true;
        }

        public IMenuTab RegisterTab(string title, Action draw, Action onShown = null)
        {
            var tab = new MenuTab(title, draw, onShown);
            RegisterTab(tab);
            return tab;
        }

        public bool UnregisterTab(IMenuTab tab)
        {
            int index = _tabs.IndexOf(tab);
            if (index < 0)
            {
                return false;
            }

            _tabs.RemoveAt(index);

            // Keep the same tab selected when a tab before it is removed
            if (index < _selectedIndex)
            {
                _selectedIndex--;
            }
            if (_selectedIndex >= _tabs.Count)
            {
                _selectedIndex = Math.Max(0, _tabs.Count - 1);
            }
            return true;
        }

        public bool Select(IMenuTab tab)
        {
            int index = _tabs.IndexOf(tab);
            if (index < 0)
            {
                return false;
            }

            _selectedIndex = index;
            return true;
        }

        public void SelectNext()
        {
            if (_tabs.Count == 0)
            {
                return;
            }

            _selectedIndex = (_selectedIndex + 1) % _tabs.Count;
        }

        public void SelectPrevious()
        {
            if (_tabs.Count == 0)
            {
                return;
            }

            _selectedIndex = (_selectedIndex - 1 + _tabs.Count) % _tabs.Count;
        }

        public void Open() => IsOpen = true;

        public void Close() => IsOpen = false;

        public void Toggle() => IsOpen = !IsOpen;
    }
}
