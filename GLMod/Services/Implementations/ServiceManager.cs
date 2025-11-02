using GLMod.Enums;
using GLMod.Services.Interfaces;
using System.Collections.Generic;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Service responsible for managing enabled/disabled services
    /// </summary>
    public class ServiceManager : IServiceManager
    {
        private readonly List<string> _enabledServices;

        public List<string> EnabledServices => _enabledServices;

        public ServiceManager()
        {
            _enabledServices = new List<string>();
        }

        public void EnableService(ServiceType service)
        {
            string serviceName = service.ToString();
            EnableService(serviceName);
        }

        public void EnableService(string service)
        {
            if (!_enabledServices.Contains(service))
            {
                _enabledServices.Add(service);
            }
        }

        public void DisableService(ServiceType service)
        {
            string serviceName = service.ToString();
            DisableService(serviceName);
        }

        public void DisableService(string service)
        {
            if (_enabledServices.Contains(service))
            {
                _enabledServices.Remove(service);
            }
        }

        public void DisableAllServices()
        {
            _enabledServices.Clear();
        }

        public bool ExistsService(ServiceType service)
        {
            return _enabledServices.Contains(service.ToString());
        }

        public bool ExistsService(string service)
        {
            return _enabledServices.Contains(service);
        }
    }
}
