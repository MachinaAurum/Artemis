using Serilog.Core;
using Serilog.Events;

namespace MachinaAurum.Artemis.Serilog
{
    public class CorrelationEnricher : ILogEventEnricher
    {
        string Correlation;

        public CorrelationEnricher(string correlation)
        {
            Correlation = correlation;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var property = propertyFactory.CreateProperty("Correlation", Correlation);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
