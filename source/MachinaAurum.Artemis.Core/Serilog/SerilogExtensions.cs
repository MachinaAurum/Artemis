using MachinaAurum.Artemis.Http;
using MachinaAurum.Artemis.NEsper;

namespace Serilog
{
    public static class SerilogExtensions
    {
        public static LoggerConfiguration UseArtemis(this LoggerConfiguration config)
        {
            return config.Destructure.ByTransforming<HttpRequest>(x => new
                {
                    Method = x.Method,
                    Uri = x.Uri,
                    Host = x.Host
                })
               .Destructure.ByTransforming<TcpSource>(x => new
               {
                   Name = "TCPSource",
                   Post = x.Port
               })
               .Destructure.ByTransforming<HostFilter>(x => new
               {
                   Name = "HostFilter",
                   Host = x.Host
               })
               .Destructure.ByTransforming<CatalogRedirectSink>(x => new
               {
                   Name = "ConsulRedirectSink",
                   Service = x.Service
               });
        }
    }
}
