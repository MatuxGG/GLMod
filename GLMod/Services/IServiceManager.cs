using GLMod.Enums;
using System.Collections.Generic;

namespace GLMod.Services
{
    /// <summary>
    /// Interface for managing enabled services
    /// </summary>
    public interface IServiceManager
    {
        /// <summary>
        /// Gets the list of enabled services
        /// </summary>
        List<string> EnabledServices { get; }

        /// <summary>
        /// Enables a service by type
        /// </summary>
        /// <param name="service">Service type to enable</param>
        void EnableService(ServiceType service);

        /// <summary>
        /// Enables a service by name
        /// </summary>
        /// <param name="service">Service name to enable</param>
        void EnableService(string service);

        /// <summary>
        /// Disables a service by type
        /// </summary>
        /// <param name="service">Service type to disable</param>
        void DisableService(ServiceType service);

        /// <summary>
        /// Disables a service by name
        /// </summary>
        /// <param name="service">Service name to disable</param>
        void DisableService(string service);

        /// <summary>
        /// Disables all services
        /// </summary>
        void DisableAllServices();

        /// <summary>
        /// Checks if a service is enabled by type
        /// </summary>
        /// <param name="service">Service type to check</param>
        /// <returns>True if enabled, false otherwise</returns>
        bool ExistsService(ServiceType service);

        /// <summary>
        /// Checks if a service is enabled by name
        /// </summary>
        /// <param name="service">Service name to check</param>
        /// <returns>True if enabled, false otherwise</returns>
        bool ExistsService(string service);
    }
}
