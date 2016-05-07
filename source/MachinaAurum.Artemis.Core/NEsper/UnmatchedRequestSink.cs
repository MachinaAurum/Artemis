using com.espertech.esper.dataflow.annotations;
using com.espertech.esper.dataflow.interfaces;
using MachinaAurum.Artemis.Http;
using System;

namespace MachinaAurum.Artemis.NEsper
{

    [DataFlowOperator]
    public class UnmatchedRequestSink : DataFlowOpLifecycle
    {
        [DataFlowOpParameter]
        private int Status;

        public UnmatchedRequestSink()
        {
            Status = 404;
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
            var context = (HttpContext)theEvent;

            if (context.Found == false)
            {
                global::Serilog.Log.Warning("No service found to request");

                using (context)
                {
                    context.Response.Send(Status);
                }
            }
            //using (context.Response)
            //{
            //    if (Status.HasValue)
            //    {
            //        context.Response.StatusCode = Status.Value;
            //    }
            //}
        }
    }

}
