using GLMod.Class;
using GLMod.Enums;
using Xunit;

namespace GLMod.Tests
{
    public class BackgroundEventsTests
    {
        [Fact]
        public void SabotageForTask_CleanO2Filter_IsNotASabotage()
        {
            // Bughunt k0003: the regular "Clean O2 Filter" task was detected as an O2 sabotage,
            // so SAB_START_O2 was recorded without any sabotage.
            Assert.Null(BackgroundEvents.SabotageForTask(TaskTypes.CleanO2Filter));
        }

        [Fact]
        public void SabotageForTask_RestoreOxy_IsO2Sabotage()
        {
            // Bughunt k0003: the real O2 sabotage task (RestoreOxy) was never detected.
            Assert.Equal(SabotageType.O2, BackgroundEvents.SabotageForTask(TaskTypes.RestoreOxy));
        }

        [Theory]
        [InlineData(TaskTypes.ResetReactor, SabotageType.Reactor)]
        [InlineData(TaskTypes.ResetSeismic, SabotageType.Reactor)]
        [InlineData(TaskTypes.FixComms, SabotageType.Coms)]
        [InlineData(TaskTypes.FixLights, SabotageType.Lights)]
        public void SabotageForTask_OtherSabotageTasks(TaskTypes taskType, SabotageType expected)
        {
            Assert.Equal(expected, BackgroundEvents.SabotageForTask(taskType));
        }
    }
}
