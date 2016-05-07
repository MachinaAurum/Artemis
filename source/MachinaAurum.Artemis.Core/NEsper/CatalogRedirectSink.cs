using com.espertech.esper.dataflow.annotations;
using com.espertech.esper.dataflow.interfaces;
using MachinaAurum.Artemis.Core.Services;
using MachinaAurum.Artemis.Http;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MachinaAurum.Artemis.NEsper
{
    [DataFlowOperator]
    class CatalogRedirectSink : DataFlowOpLifecycle
    {
        [DataFlowOpParameter]
        public string Service;

        [DataFlowOpParameter]
        public bool UseHostHeader;

        Func<IServiceFinder> FinderFactory;

        public CatalogRedirectSink()
        {
            Service = null;
            UseHostHeader = false;
        }

        public void Close(DataFlowOpCloseContext closeContext)
        {
        }

        public DataFlowOpInitializeResult Initialize(DataFlowOpInitializateContext initContext)
        {
            if (initContext.InputPorts.Count == 1 && initContext.InputPorts[0].StreamNames.Count == 1)
            {
                Service = initContext.InputPorts[0].StreamNames.First();
            }

            var dataflowContext = initContext.DataflowInstanceUserObject as DataflowContext;
            FinderFactory = dataflowContext.Resolve<Func<IServiceFinder>>();

            return null;
        }

        public void Open(DataFlowOpOpenContext openContext)
        {
        }

        public void OnInput(int port, Object theEvent)
        {
            var finder = FinderFactory();

            var context = (HttpContext)theEvent;
            var request = context.Request;

            var log = context.Properties["Log"] as ILogger;
            log.Verbose("{@Block} {@Request}", this, request);

            var serviceName = Service;

            if (UseHostHeader)
            {
                serviceName = context.Request.Host;
            }

            log.Information("Sending request to {Service}", serviceName);

            Task.Factory.StartNew(async () =>
            {
                var serviceInfo = finder.GetService(serviceName);

                if (serviceInfo != null)
                {
                    log.Information("Sending request to {Host}:{Port} by {Provider}...", serviceInfo.Address, serviceInfo.Port, serviceInfo.Provider);
                    await CallService(context, request, log, serviceInfo);
                }
                else
                {
                    log.Warning("Service not found!");
                    using (context)
                    {
                        context.Response.Send(404);
                    }
                }
            });
        }

        //private async Task<ServiceDto> CallConsul(HttpContext context, ILogger log, string serviceName)
        //{
        //    try
        //    {
        //        var data = new ServiceDto();
        //        log.Verbose("Searching service in Consul...");

        //        using (var client = new ConsulClient())
        //        {
        //            var consulCatalog = await client.Catalog.Service(serviceName);

        //            if (consulCatalog.Response.Length > 0)
        //            {
        //                var service = consulCatalog.Response[0];
        //                data.Address = IPAddress.Parse(service.Address);
        //                data.Port = service.ServicePort;

        //                log.Information("Sending request to {Host}:{port}...", service.Address, data.Port);
        //            }
        //            else
        //            {
        //                log.Warning("Service not found!");
        //                context.Response.Send(404);
        //            }
        //        }

        //        return data;
        //    }
        //    catch (Exception e)
        //    {
        //        log.Error(e, "Cannot call Consul");
        //        context.Response.Send(500);
        //        return null;
        //    }
        //}

        private static async Task CallService(HttpContext context, HttpRequest request, ILogger log, ServiceInfo serviceInfo)
        {
            IPAddress serviceAdress = IPAddress.Parse(serviceInfo.Address);
            int servicePort = serviceInfo.Port;

            using (context)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        client.Connect(serviceAdress, servicePort);

                        using (var realService = client.GetStream())
                        {
                            log.Verbose("Sending request...");

                            await request.RedirectToAsync(realService);

                            log.Verbose("Waiting response...");

                            await context.Response.RedirectFromAsync(realService);

                            log.Information("Finished!");
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error(e, "Cannot call service");
                    context.Response.Send(500);
                }
            }
        }
    }
}
