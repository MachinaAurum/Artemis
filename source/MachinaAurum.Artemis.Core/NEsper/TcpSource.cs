using com.espertech.esper.dataflow.annotations;
using com.espertech.esper.dataflow.interfaces;
using MachinaAurum.Artemis.Http;
using MachinaAurum.Artemis.Serilog;
using Serilog;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MachinaAurum.Artemis.NEsper
{
    [DataFlowOperator]
    public class TcpSource : DataFlowSourceOperator
    {
        [DataFlowContext]
        private EPDataFlowEmitter Emitter;

        [DataFlowOpParameter]
        public int? Port;

        public TcpSource()
        {
            Emitter = null;
            Port = null;
        }

        public void Close(DataFlowOpCloseContext closeContext)
        {
            throw new NotImplementedException();
        }

        public DataFlowOpInitializeResult Initialize(DataFlowOpInitializateContext initContext)
        {
            var dataflowContext = initContext.DataflowInstanceUserObject as DataflowContext;
            var log = dataflowContext.Resolve<ILogger>();

            var port = ChoosePort(Port, new[] { 80, 8080 });

            if(port.HasValue == false)
            {
                log.Error("Unable to bind to port {Port}", Port);
            }

            var tcpListener = new TcpListener(IPAddress.Any, port.Value);
            tcpListener.Start(100);

            log.Information("Port open {Endpoint}", tcpListener.LocalEndpoint.ToString());

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    var stream = tcpClient.GetStream();

                    var context = new HttpContext()
                    {
                        Connection = tcpClient,
                        Request = new HttpRequest(stream),
                        Response = new HttpResponse(stream)
                    };
                    await context.Request.ReadHeadersAsync();

                    log = log.ForContext(new[] {
                        new CorrelationEnricher(context.Correlation.ToString())
                        });
                    context.Properties.Add("Log", log);

                    log.Information("Request Accepted {Correlation}", context.Correlation);
                    log.Verbose("{@Block} {@Request}", this, context.Request);

                    Emitter.Submit(context);
                }
            });

            var httpContext = initContext.ServicesContext.EventAdapterService.GetEventTypeByName("HttpContext");
            return new DataFlowOpInitializeResult(new[] { new com.espertech.esper.dataflow.util.GraphTypeDesc(false, false, httpContext) });
        }

        private int? ChoosePort(int? choosen, int[] options)
        {
            if (choosen.HasValue)
            {
                if (IsPortAvailable(choosen))
                {
                    return choosen;
                }
                else
                {
                    return null;
                }
            }

            foreach (var port in options)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }

            return 0;
        }

        private bool IsPortAvailable(int? port)
        {
            if (port.HasValue == false)
            {
                return false;
            }

            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (var tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == port)
                {
                    return false;
                }
            }

            return true;
        }

        public void Next()
        {
        }

        public void Open(DataFlowOpOpenContext openContext)
        {

        }
    }
}
