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
            public FakeTab(string id) => Id = id;
            public string Id { get; }
            public string Title => Id.ToUpperInvariant();
            public void OnShown() { }
            public void Draw() { }
        }

        private static (MenuService menu, FakeTab a, FakeTab b, FakeTab c) MenuWithThreeTabs()
        {
            var menu = new MenuService();
            var a = new FakeTab("a");
            var b = new FakeTab("b");
            var c = new FakeTab("c");
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
            Assert.True(menu.Enabled);
            Assert.False(menu.IsVisible);
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
            Assert.True(menu.IsVisible);
        }

        [Fact]
        public void RegisterTab_RejectsNullDuplicatesAndMissingIds()
        {
            var menu = new MenuService();
            var tab = new FakeTab("a");

            Assert.True(menu.RegisterTab(tab));
            Assert.False(menu.RegisterTab(tab));
            Assert.False(menu.RegisterTab(new FakeTab("a")));
            Assert.False(menu.RegisterTab(new FakeTab(" ")));
            Assert.False(menu.RegisterTab((IMenuTab)null));
            Assert.Single(menu.Tabs);
        }

        [Fact]
        public void RegisterTab_WithDelegates_CreatesTab()
        {
            var menu = new MenuService();
            int drawn = 0;
            int shown = 0;

            IMenuTab tab = menu.RegisterTab("mymod.rules", "Rules", () => drawn++, () => shown++);
            tab.Draw();
            tab.OnShown();

            Assert.Same(tab, menu.SelectedTab);
            Assert.Equal("mymod.rules", tab.Id);
            Assert.Equal("Rules", tab.Title);
            Assert.Equal(1, drawn);
            Assert.Equal(1, shown);
            Assert.Null(menu.RegisterTab("mymod.rules", "Again", () => { }));
        }

        [Fact]
        public void RegisterTab_WithDelegates_RequiresIdTitleAndDraw()
        {
            var menu = new MenuService();

            Assert.Throws<ArgumentException>(() => menu.RegisterTab("", "Title", () => { }));
            Assert.Throws<ArgumentException>(() => menu.RegisterTab("id", " ", () => { }));
            Assert.Throws<ArgumentNullException>(() => menu.RegisterTab("id", "Title", null));
        }

        [Fact]
        public void FindTab_ById()
        {
            var (menu, _, b, _) = MenuWithThreeTabs();

            Assert.Same(b, menu.FindTab("b"));
            Assert.Null(menu.FindTab("unknown"));
            Assert.Null(menu.FindTab(null));
        }

        [Fact]
        public void InsertTab_AtPosition_KeepsSelectedTab()
        {
            var (menu, a, b, c) = MenuWithThreeTabs();
            menu.Select(b);
            var first = new FakeTab("first");
            var last = new FakeTab("last");

            Assert.True(menu.InsertTab(0, first));
            Assert.True(menu.InsertTab(99, last));

            Assert.Equal(new IMenuTab[] { first, a, b, c, last }, menu.Tabs);
            Assert.Same(b, menu.SelectedTab);
            Assert.False(menu.InsertTab(1, new FakeTab("a")));
        }

        [Fact]
        public void InsertTab_IntoEmptyMenu_SelectsIt()
        {
            var menu = new MenuService();
            var tab = new FakeTab("only");

            Assert.True(menu.InsertTab(-5, tab));

            Assert.Same(tab, menu.SelectedTab);
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
        public void Select_ByIdOrTab()
        {
            var (menu, _, b, c) = MenuWithThreeTabs();

            Assert.True(menu.Select("c"));
            Assert.Same(c, menu.SelectedTab);
            Assert.True(menu.Select(b));
            Assert.False(menu.Select(new FakeTab("x")));
            Assert.False(menu.Select("x"));
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
        public void UnregisterTab_ById()
        {
            var (menu, a, _, c) = MenuWithThreeTabs();

            Assert.True(menu.UnregisterTab("b"));
            Assert.False(menu.UnregisterTab("b"));
            Assert.False(menu.UnregisterTab((string)null));

            Assert.Equal(new IMenuTab[] { a, c }, menu.Tabs);
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
        public void UnregisterTab_AllTabs_HidesAndClosesTheMenu()
        {
            var (menu, a, b, c) = MenuWithThreeTabs();
            menu.Open();

            menu.UnregisterTab(a);
            menu.UnregisterTab(b);
            menu.UnregisterTab(c);

            Assert.False(menu.UnregisterTab(c));
            Assert.Equal(-1, menu.SelectedIndex);
            Assert.False(menu.IsVisible);
            Assert.False(menu.IsOpen);
            menu.Open();
            Assert.False(menu.IsOpen);
        }

        [Fact]
        public void OpenCloseToggle()
        {
            var (menu, _, _, _) = MenuWithThreeTabs();

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

        [Fact]
        public void Disabled_HidesClosesAndIgnoresOpen()
        {
            var (menu, _, _, _) = MenuWithThreeTabs();
            menu.Open();

            menu.Enabled = false;

            Assert.False(menu.IsVisible);
            Assert.False(menu.IsOpen);
            menu.Open();
            menu.Toggle();
            Assert.False(menu.IsOpen);

            menu.Enabled = true;
            Assert.True(menu.IsVisible);
            Assert.False(menu.IsOpen);
            menu.Toggle();
            Assert.True(menu.IsOpen);
        }
    }
}
