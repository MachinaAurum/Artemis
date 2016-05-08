using com.espertech.esper.client;
using MachinaAurum.Artemis.Core.Services;
using MachinaAurum.Artemis.Http;
using MachinaAurum.Artemis.NEsper;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using Newtonsoft.Json.Linq;
using Owin;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using WebApiContrib.Formatting.Razor;
using WebApiContrib.IoC.Unity;

namespace MachinaAurum.Artemis
{
    public class ArtemisService
    {
        EPServiceProvider Provider;

        int? Port;
        ILogger Logger;
        ServiceCatalog Catalog = new ServiceCatalog();

        public ArtemisService(int? port, ILogger logger)
        {
            Port = port;
            Logger = logger;
        }

        public bool Start()
        {
            StartConsul();

            ConfigureNEsper().ContinueWith(x =>
            {
                ConfigureAdminUI();
            });

            return true;
        }

        private void ConfigureAdminUI()
        {
            if (Port.HasValue)
            {
                var unity = new UnityContainer();
                unity.RegisterInstance<EPServiceProvider>(Provider);

                var app = WebApp.Start($"http://+:{Port}/ui", x =>
                {
                    var webApiConfiguration = new HttpConfiguration();
                    webApiConfiguration.DependencyResolver = new UnityResolver(unity);
                    webApiConfiguration.Formatters.Add(new FormUrlEncodedMediaTypeFormatter());
                    webApiConfiguration.Formatters.Add(new RazorViewFormatter());
                    webApiConfiguration.MapHttpAttributeRoutes();

                    x.UseWebApi(webApiConfiguration);
                });
            }
        }

        private Task ConfigureNEsper()
        {
            return Task.Factory.StartNew(() =>
            {
                Provider = EPServiceProviderManager.GetDefaultProvider();
                Provider.EPAdministrator.Configuration.AddImport<TcpSource>();

                Provider.EPAdministrator.Configuration.AddImport<HostFilter>();
                Provider.EPAdministrator.Configuration.AddImport<EnrichRequestCorrelationFilter>();

                Provider.EPAdministrator.Configuration.AddImport<UnmatchedRequestSink>();
                Provider.EPAdministrator.Configuration.AddImport<RedirectSink>();
                Provider.EPAdministrator.Configuration.AddImport<CatalogRedirectSink>();
                Provider.EPAdministrator.Configuration.AddEventType<HttpContext>("HttpContext");

                var elp1 = Provider.EPAdministrator.CreateEPL(@"create dataflow ArtemisRouting
TcpSource -> requests { Port: 9090 }
EnrichRequestCorrelationFilter (requests) -> enrichedrequests {}
CatalogRedirectSink (sinks) { Use: ""Path"" }");

                var unity = new UnityContainer();
                unity.RegisterType<IServiceFinder>(new InjectionFactory(x => Catalog));
                unity.RegisterInstance<ILogger>(Logger);
                var dataflowContext = new DataflowContext(unity);

                var init = new com.espertech.esper.client.dataflow.EPDataFlowInstantiationOptions();
                init.SetDataFlowInstanceUserObject(dataflowContext);
                var instance = Provider.EPRuntime.DataFlowRuntime.Instantiate("ArtemisRouting", init);
                instance.Start();
            });
        }

        private void StartConsul()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        var consul = Process.GetProcessesByName("consul");
                        if (consul.Length == 0)
                        {
                            Logger.Warning("Consul not found");
                        }
                        else
                        {
                            await UpdateCatalogFromConsul();
                        }

                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "ERROR");
                    }
                }
            });
        }

        private async Task UpdateCatalogFromConsul()
        {
            var catalog = new ServiceCatalog();

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("http://localhost:8500/v1/agent/services");
                var responseDictionary = await response.Content.ReadAsAsync<Dictionary<string, object>>();

                foreach (var item in responseDictionary)
                {
                    try
                    {
                        var serviceName = item.Key;
                        var service = (item.Value as JObject).ToObject<ConsulService>();
                        catalog.AddServiceInfo("Consul", service.Service, service.Address, service.Port);
                    }
                    catch
                    {
                        //LOG SOMETHING!
                    }
                }
            }

            Catalog = catalog;
        }

        public class ConsulService
        {
            public string Id { get; set; }
            public string Service { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
        }

        public bool Stop()
        {
            return true;
        }
    }

}
