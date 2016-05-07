using com.espertech.esper.dataflow.annotations;
using com.espertech.esper.dataflow.interfaces;
using MachinaAurum.Artemis.Http;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MachinaAurum.Artemis.NEsper
{
    [DataFlowOperator]
    public class RedirectSink : DataFlowOpLifecycle
    {
        [DataFlowOpParameter]
        private string Host;

        [DataFlowOpParameter]
        private int Port;

        public RedirectSink()
        {
            Host = null;
            Port = 80;
        }

        public void Close(DataFlowOpCloseContext closeContext)
        {
        }

        public DataFlowOpInitializeResult Initialize(DataFlowOpInitializateContext initContext)
        {            
            return null;
        }

        public void Open(DataFlowOpOpenContext openContext)
        {
        }

        public void OnInput(int port, Object theEvent)
        {
            global::Serilog.Log.Information("Sending request to {Host}:{port}", Host, Port);

            Task.Factory.StartNew(async () =>
            {
                using (var context = (HttpContext)theEvent)
                {
                    var request = context.Request;

                    using (var client = new TcpClient())
                    {
                        global::Serilog.Log.Verbose("Connecting {Host}:{port}...", Host, Port);
                        client.Connect(Host, Port);
                        
                        using (var realService = client.GetStream())
                        {
                            global::Serilog.Log.Verbose("Send the original request");

                            await request.RedirectToAsync(realService);

                            global::Serilog.Log.Verbose("Waiting response...");

                            await context.Response.RedirectFromAsync(realService);

                            global::Serilog.Log.Verbose("Response sent to the original client");
                        }
                    }
                }
            });
        }
    }
}
