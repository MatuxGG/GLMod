using GLMod.Menu;
using Xunit;

namespace GLMod.Tests
{
    public class ShownTabTrackerTests
    {
        private sealed class FakeTab : IMenuTab
        {
            public FakeTab(string id) => Id = id;
            public string Id { get; }
            public string Title => Id;
            public void OnShown() { }
            public void Draw() { }
        }

        [Fact]
        public void NewTab_IsNotifiedOnce()
        {
            var tracker = new ShownTabTracker();
            var tab = new FakeTab("a");

            Assert.True(tracker.Update(tab, "", out bool changed));
            Assert.True(changed);
            Assert.Same(tab, tracker.Tab);
            Assert.False(tracker.Update(tab, "", out changed));
            Assert.False(changed);
        }

        [Fact]
        public void TabSwitch_NotifiesNewTab()
        {
            var tracker = new ShownTabTracker();
            tracker.Update(new FakeTab("a"), "bob", out _);

            Assert.True(tracker.Update(new FakeTab("b"), "bob", out bool changed));
            Assert.True(changed);
        }

        // k0024: a tab shown during the login stayed on "loading" once connected, OnShown was never called again
        [Fact]
        public void K0024_LoginWhileTabShown_NotifiesTabAgain()
        {
            var tracker = new ShownTabTracker();
            var stats = new FakeTab("glmod.stats");
            tracker.Update(stats, "", out _);

            Assert.True(tracker.Update(stats, "bob", out bool changed));
            Assert.False(changed);
            Assert.False(tracker.Update(stats, "bob", out _));
        }

        [Fact]
        public void LogoutOrAccountSwitchWhileTabShown_NotifiesTabAgain()
        {
            var tracker = new ShownTabTracker();
            var tab = new FakeTab("a");
            tracker.Update(tab, "bob", out _);

            Assert.True(tracker.Update(tab, "alice", out _));
            Assert.True(tracker.Update(tab, null, out _));
            Assert.False(tracker.Update(tab, "", out _));
        }

        [Fact]
        public void ClosedMenu_NotifiesNothing_ThenReopenNotifies()
        {
            var tracker = new ShownTabTracker();
            var tab = new FakeTab("a");
            tracker.Update(tab, "", out _);

            Assert.False(tracker.Update(null, "", out bool changed));
            Assert.True(changed);
            Assert.False(tracker.Update(null, "bob", out _));
            Assert.Null(tracker.Tab);
            Assert.True(tracker.Update(tab, "bob", out changed));
            Assert.True(changed);
        }
    }
}
