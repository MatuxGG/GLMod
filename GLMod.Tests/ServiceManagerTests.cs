using GLMod.Enums;
using GLMod.Services.Implementations;
using Xunit;

namespace GLMod.Tests
{
    public class ServiceManagerTests
    {
        [Fact]
        public void NewManager_HasNoEnabledServices()
        {
            var manager = new ServiceManager();
            Assert.Empty(manager.EnabledServices);
        }

        [Fact]
        public void EnableService_ByEnum_RegistersIt()
        {
            var manager = new ServiceManager();

            manager.EnableService(ServiceType.Kills);

            Assert.True(manager.ExistsService(ServiceType.Kills));
            Assert.True(manager.ExistsService("Kills"));
        }

        [Fact]
        public void EnableService_ByString_RegistersIt()
        {
            var manager = new ServiceManager();

            manager.EnableService("CustomThing");

            Assert.True(manager.ExistsService("CustomThing"));
        }

        [Fact]
        public void EnableService_Twice_IsIdempotent()
        {
            var manager = new ServiceManager();

            manager.EnableService(ServiceType.Tasks);
            manager.EnableService(ServiceType.Tasks);
            manager.EnableService("Tasks");

            Assert.Single(manager.EnabledServices);
        }

        [Fact]
        public void DisableService_RemovesIt()
        {
            var manager = new ServiceManager();
            manager.EnableService(ServiceType.Votes);

            manager.DisableService(ServiceType.Votes);

            Assert.False(manager.ExistsService(ServiceType.Votes));
        }

        [Fact]
        public void DisableService_NotPresent_IsNoOp()
        {
            var manager = new ServiceManager();

            manager.DisableService(ServiceType.Roles);

            Assert.Empty(manager.EnabledServices);
        }

        [Fact]
        public void DisableAllServices_ClearsList()
        {
            var manager = new ServiceManager();
            manager.EnableService(ServiceType.StartGame);
            manager.EnableService(ServiceType.EndGame);
            manager.EnableService(ServiceType.Kills);

            manager.DisableAllServices();

            Assert.Empty(manager.EnabledServices);
        }

        [Fact]
        public void ExistsService_UnknownService_ReturnsFalse()
        {
            var manager = new ServiceManager();

            Assert.False(manager.ExistsService("NeverEnabled"));
            Assert.False(manager.ExistsService(ServiceType.Shield));
        }
    }
}
