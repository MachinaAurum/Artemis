using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MachinaAurum.Artemis.Core.Services
{
    class ServiceCatalog : IServiceFinder
    {
        ConcurrentDictionary<string, Dictionary<string, ServiceInfo>> Services = new ConcurrentDictionary<string, Dictionary<string, ServiceInfo>>();
        Random Random = new Random();

        public ServiceInfo GetService(string name)
        {
            Dictionary<string, ServiceInfo> infos = null;
            if (Services.TryGetValue(name, out infos))
            {
                var skip = Random.Next(infos.Count) - 1;
                return infos.Values.Skip(skip).First();
            }
            else
            {
                return null;
            }
        }

        internal void AddServiceInfo(string provider, string serviceName, string serviceAddress, int servicePort)
        {
            var serviceInfo = new ServiceInfo()
            {
                Provider = provider,
                Address = serviceAddress,
                Port = servicePort
            };

            var list = new Dictionary<string, ServiceInfo>();
            list.Add(serviceAddress + servicePort, serviceInfo);

            Services.AddOrUpdate(serviceName, list, (key, value) =>
            {
                value.Remove(serviceAddress + servicePort);
                value.Add(serviceAddress + servicePort, serviceInfo);
                return value;
            });
        }
    }

    public interface IServiceFinder
    {
        ServiceInfo GetService(string name);
    }

    public class ServiceInfo
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string Provider { get; set; }
    }
}
