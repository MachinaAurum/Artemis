using com.espertech.esper.dataflow.annotations;
using com.espertech.esper.dataflow.interfaces;
using MachinaAurum.Artemis.Http;
using MachinaAurum.Artemis.Serilog;
using Serilog;
using System;
using System.Net;
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
        public int Port;

        public TcpSource()
        {
            Emitter = null;
            Port = 80;
        }

        public void Close(DataFlowOpCloseContext closeContext)
        {
            throw new NotImplementedException();
        }

        public DataFlowOpInitializeResult Initialize(DataFlowOpInitializateContext initContext)
        {
            var dataflowContext = initContext.DataflowInstanceUserObject as DataflowContext;
            var log = dataflowContext.Resolve<ILogger>();

            var tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();

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

        public void Next()
        {
        }

        public void Open(DataFlowOpOpenContext openContext)
        {

        }
    }
}
