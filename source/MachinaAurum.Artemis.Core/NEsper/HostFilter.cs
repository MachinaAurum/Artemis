using com.espertech.esper.dataflow.annotations;
using com.espertech.esper.dataflow.interfaces;
using MachinaAurum.Artemis.Http;
using Serilog;
using System;

namespace MachinaAurum.Artemis.NEsper
{
    [DataFlowOperator]
    public class HostFilter : DataFlowOpLifecycle
    {
        [DataFlowContext]
        private EPDataFlowEmitter Emitter;

        [DataFlowOpParameter]
        public string Host;

        public HostFilter()
        {
            Emitter = null;
            Host = null;
        }

        public void Close(DataFlowOpCloseContext closeContext)
        {
        }

        public DataFlowOpInitializeResult Initialize(DataFlowOpInitializateContext initContext)
        {
            var httpContext = initContext.ServicesContext.EventAdapterService.GetEventTypeByName("HttpContext");

            if (initContext.OutputPorts.Count == 1)
            {
                Host = initContext.OutputPorts[0].StreamName;
            }

            return new DataFlowOpInitializeResult(new[] { new com.espertech.esper.dataflow.util.GraphTypeDesc(false, false, httpContext) });
        }

        public void Open(DataFlowOpOpenContext openContext)
        {
        }

        public void OnInput(int port, Object theEvent)
        {
            var context = (HttpContext)theEvent;
            var request = context.Request;
            var host = request.Host;

            var log = context.Properties["Log"] as ILogger;
            log.Verbose("{@Block} {@Request}", this, request);

            if (host == Host)
            {
                log.Information("Request Host matched {Host}", Host);

                context.Found = true;
                Emitter.Submit(context);
            }
            else
            {
                log.Information("Request Host not matched {Host}", Host);
            }            
        }
    }
}
