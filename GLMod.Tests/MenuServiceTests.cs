using System;
using GLMod.Menu;
using GLMod.Services.Implementations;
using Xunit;

namespace GLMod.Tests
{
    public class MenuServiceTests
    {
        private sealed class FakeTab : IMenuTab
        {
            public FakeTab(string title) => Title = title;
            public string Title { get; }
            public void OnShown() { }
            public void Draw() { }
        }

        private static (MenuService menu, FakeTab a, FakeTab b, FakeTab c) MenuWithThreeTabs()
        {
            var menu = new MenuService();
            var a = new FakeTab("A");
            var b = new FakeTab("B");
            var c = new FakeTab("C");
            menu.RegisterTab(a);
            menu.RegisterTab(b);
            menu.RegisterTab(c);
            return (menu, a, b, c);
        }

        [Fact]
        public void NewMenu_IsClosedWithoutTabs()
        {
            var menu = new MenuService();

            Assert.False(menu.IsOpen);
            Assert.Empty(menu.Tabs);
            Assert.Equal(-1, menu.SelectedIndex);
            Assert.Null(menu.SelectedTab);
        }

        [Fact]
        public void RegisterTab_KeepsOrderAndSelectsFirst()
        {
            var (menu, a, b, c) = MenuWithThreeTabs();

            Assert.Equal(new IMenuTab[] { a, b, c }, menu.Tabs);
            Assert.Same(a, menu.SelectedTab);
        }

        [Fact]
        public void RegisterTab_RejectsNullAndDuplicates()
        {
            var menu = new MenuService();
            var tab = new FakeTab("A");

            Assert.True(menu.RegisterTab(tab));
            Assert.False(menu.RegisterTab(tab));
            Assert.False(menu.RegisterTab((IMenuTab)null));
            Assert.Single(menu.Tabs);
        }

        [Fact]
        public void RegisterTab_WithDelegates_CreatesTab()
        {
            var menu = new MenuService();
            int drawn = 0;
            int shown = 0;

            IMenuTab tab = menu.RegisterTab("My mod", () => drawn++, () => shown++);
            tab.Draw();
            tab.OnShown();

            Assert.Same(tab, menu.SelectedTab);
            Assert.Equal("My mod", tab.Title);
            Assert.Equal(1, drawn);
            Assert.Equal(1, shown);
        }

        [Fact]
        public void RegisterTab_WithDelegates_RequiresTitleAndDraw()
        {
            var menu = new MenuService();

            Assert.Throws<ArgumentException>(() => menu.RegisterTab(" ", () => { }));
            Assert.Throws<ArgumentNullException>(() => menu.RegisterTab("Title", null));
        }

        [Fact]
        public void SelectNextAndPrevious_WrapAround()
        {
            var (menu, a, b, c) = MenuWithThreeTabs();

            menu.SelectPrevious();
            Assert.Same(c, menu.SelectedTab);
            menu.SelectNext();
            Assert.Same(a, menu.SelectedTab);
            menu.SelectNext();
            Assert.Same(b, menu.SelectedTab);
        }

        [Fact]
        public void SelectNext_WithoutTabs_DoesNothing()
        {
            var menu = new MenuService();

            menu.SelectNext();
            menu.SelectPrevious();

            Assert.Null(menu.SelectedTab);
        }

        [Fact]
        public void Select_UnknownTab_KeepsSelection()
        {
            var (menu, _, b, _) = MenuWithThreeTabs();
            menu.Select(b);

            Assert.False(menu.Select(new FakeTab("X")));
            Assert.Same(b, menu.SelectedTab);
        }

        [Fact]
        public void UnregisterTab_BeforeSelection_KeepsSelectedTab()
        {
            var (menu, a, _, c) = MenuWithThreeTabs();
            menu.Select(c);

            Assert.True(menu.UnregisterTab(a));

            Assert.Same(c, menu.SelectedTab);
        }

        [Fact]
        public void UnregisterTab_LastSelected_SelectsNewLast()
        {
            var (menu, _, b, c) = MenuWithThreeTabs();
            menu.Select(c);

            menu.UnregisterTab(c);

            Assert.Same(b, menu.SelectedTab);
        }

        [Fact]
        public void UnregisterTab_AllTabs_LeavesNoSelection()
        {
            var (menu, a, b, c) = MenuWithThreeTabs();

            menu.UnregisterTab(a);
            menu.UnregisterTab(b);
            menu.UnregisterTab(c);

            Assert.False(menu.UnregisterTab(c));
            Assert.Equal(-1, menu.SelectedIndex);
        }

        [Fact]
        public void OpenCloseToggle()
        {
            var menu = new MenuService();

            menu.Toggle();
            Assert.True(menu.IsOpen);
            menu.Toggle();
            Assert.False(menu.IsOpen);
            menu.Open();
            menu.Open();
            Assert.True(menu.IsOpen);
            menu.Close();
            Assert.False(menu.IsOpen);
        }
    }
}
