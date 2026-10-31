using Xunit;

namespace GLMod.Tests
{
    public class MurderPatchTests
    {
        [Fact]
        public void SucceededMurder_IsRecorded()
        {
            Assert.True(MurderPatch.IsSuccessfulMurder(MurderPatch.MURDER_SUCCEEDED_FLAG));
        }

        [Fact]
        public void SucceededMurderDecidedByHost_IsRecorded()
        {
            // Other flags (DecisionByHost...) next to Succeeded do not hide the kill
            Assert.True(MurderPatch.IsSuccessfulMurder(-1));
        }

        [Fact]
        public void FailedMurder_IsNotRecorded()
        {
            // Bughunt k0023: a murder blocked by a guardian angel (FailedProtected | DecisionByHost)
            // was recorded as a real "killed" action and key positions.
            Assert.False(MurderPatch.IsSuccessfulMurder(~MurderPatch.MURDER_SUCCEEDED_FLAG));
            Assert.False(MurderPatch.IsSuccessfulMurder(0));
        }
    }
}
