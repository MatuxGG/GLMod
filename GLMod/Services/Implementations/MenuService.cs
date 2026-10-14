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
        private bool _isOpen;
        private bool _enabled = true;

        public bool IsOpen => _isOpen && IsVisible;
        public IReadOnlyList<IMenuTab> Tabs => _tabs;
        public int SelectedIndex => _tabs.Count == 0 ? -1 : _selectedIndex;
        public IMenuTab SelectedTab => _tabs.Count == 0 ? null : _tabs[_selectedIndex];

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (!value)
                {
                    _isOpen = false;
                }
            }
        }

        public bool IsVisible => _enabled && _tabs.Count > 0;

        public bool RegisterTab(IMenuTab tab) => InsertTab(_tabs.Count, tab);

        public IMenuTab RegisterTab(string id, string title, Action draw, Action onShown = null)
        {
            var tab = new MenuTab(id, title, draw, onShown);
            return RegisterTab(tab) ? tab : null;
        }

        public bool InsertTab(int index, IMenuTab tab)
        {
            if (tab == null || string.IsNullOrWhiteSpace(tab.Id) || _tabs.Contains(tab) || FindTab(tab.Id) != null)
            {
                return false;
            }

            index = Math.Max(0, Math.Min(index, _tabs.Count));
            _tabs.Insert(index, tab);

            // Keep the same tab selected when a tab is inserted before it
            if (_tabs.Count > 1 && index <= _selectedIndex)
            {
                _selectedIndex++;
            }
            return true;
        }

        public IMenuTab FindTab(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return _tabs.Find(tab => tab.Id == id);
        }

        public bool UnregisterTab(string id) => UnregisterTab(FindTab(id));

        public bool UnregisterTab(IMenuTab tab)
        {
            int index = tab == null ? -1 : _tabs.IndexOf(tab);
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
            if (_tabs.Count == 0)
            {
                _isOpen = false;
            }
            return true;
        }

        public bool Select(IMenuTab tab)
        {
            int index = tab == null ? -1 : _tabs.IndexOf(tab);
            if (index < 0)
            {
                return false;
            }

            _selectedIndex = index;
            return true;
        }

        public bool Select(string id) => Select(FindTab(id));

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

        public void Open()
        {
            if (IsVisible)
            {
                _isOpen = true;
            }
        }

        public void Close() => _isOpen = false;

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }
    }
}
