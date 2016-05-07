using com.espertech.esper.client;
using Consul;
using MachinaAurum.Artemis.Core.Services;
using MachinaAurum.Artemis.Http;
using MachinaAurum.Artemis.NEsper;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using Owin;
using Serilog;
using System;
using System.Diagnostics;
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
CatalogRedirectSink (enrichedrequests) { UseHostHeader: true }");

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
                            using (var client = new ConsulClient())
                            {
                                await UpdateCatalogFromConsul(client);
                            }
                        }

                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                    catch(Exception e)
                    {
                        Logger.Error(e, "ERROR");
                    }
                }
            });
        }

        private async Task UpdateCatalogFromConsul(ConsulClient client)
        {
            var catalog = new ServiceCatalog();

            var services = await client.Catalog.Services();

            foreach (var service in services.Response)
            {
                var serviceInfo = await client.Catalog.Service(service.Key);

                foreach (var endpoints in serviceInfo.Response)
                {
                    catalog.AddServiceInfo("Consul", endpoints.ServiceName, endpoints.ServiceAddress, endpoints.ServicePort);
                }
            }

            Catalog = catalog;
        }

        public bool Stop()
        {
            return true;
        }
    }

}
