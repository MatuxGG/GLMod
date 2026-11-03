using GLMod.Menu;
using Xunit;

namespace GLMod.Tests
{
    public class BannerStackTests
    {
        [Fact]
        public void Left_CentersTheBanner()
        {
            Assert.Equal(660f, BannerStack.Left(1920f, 600f));
        }

        [Fact]
        public void Top_TheFirstOneIsFlushWithTheBottomEdge()
        {
            Assert.Equal(1040f, BannerStack.Top(1080f, 40f, 6f, 0));
        }

        [Fact]
        public void Top_TheNextOnesStackUpWithAGap()
        {
            Assert.Equal(994f, BannerStack.Top(1080f, 40f, 6f, 1));
            Assert.Equal(948f, BannerStack.Top(1080f, 40f, 6f, 2));
        }
    }
}
