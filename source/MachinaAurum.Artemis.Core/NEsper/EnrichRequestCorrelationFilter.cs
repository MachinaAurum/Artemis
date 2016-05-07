using com.espertech.esper.dataflow.annotations;
using com.espertech.esper.dataflow.interfaces;
using MachinaAurum.Artemis.Http;
using System;

namespace MachinaAurum.Artemis.NEsper
{
    [DataFlowOperator]
    public class EnrichRequestCorrelationFilter : DataFlowOpLifecycle
    {
        [DataFlowContext]
        private EPDataFlowEmitter Emitter;


        [DataFlowOpParameter]
        public string Header;

        public EnrichRequestCorrelationFilter()
        {
            Emitter = null;
            Header = "X-Correlation";
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

            request.AddHeader(Header, context.Correlation.ToString());

            Emitter.Submit(context);
        }
    }
}
