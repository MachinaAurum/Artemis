using com.espertech.esper.dataflow.annotations;
using com.espertech.esper.dataflow.interfaces;
using MachinaAurum.Artemis.Http;
using Serilog;
using System;

namespace MachinaAurum.Artemis.NEsper
{
    [DataFlowOperator]
    public class PathFilter : DataFlowOpLifecycle
    {
        [DataFlowContext]
        private EPDataFlowEmitter Emitter;

        [DataFlowOpParameter]
        public string Path;

        public PathFilter()
        {
            Emitter = null;
            Path = null;
        }

        public void Close(DataFlowOpCloseContext closeContext)
        {
        }

        public DataFlowOpInitializeResult Initialize(DataFlowOpInitializateContext initContext)
        {
            var httpContext = initContext.ServicesContext.EventAdapterService.GetEventTypeByName("HttpContext");
            return new DataFlowOpInitializeResult(new[] { new com.espertech.esper.dataflow.util.GraphTypeDesc(false, false, httpContext) });
        }

        public void Open(DataFlowOpOpenContext openContext)
        {
        }

        public void OnInput(int port, Object theEvent)
        {
            var context = (HttpContext)theEvent;
            var request = context.Request;
            
            var log = context.Properties["Log"] as ILogger;
            log.Verbose("{@Block} {@Request}", this, request);

            if (request.Uri.StartsWith(Path))
            {
                log.Information("Request Path matched {Path}", Path);

                context.Found = true;
                Emitter.Submit(context);
            }
            else
            {
                log.Information("Request Path not matched {Path}", Path);
            }            
        }
    }
}
