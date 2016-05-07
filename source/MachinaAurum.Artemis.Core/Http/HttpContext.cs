using System;
using System.Collections.Generic;

namespace MachinaAurum.Artemis.Http
{
    public class HttpContext : IDisposable
    {
        public Guid Correlation { get; set; }

        public IDictionary<string, object> Properties { get; set; }

        public IDisposable Connection { get; set; }
        public bool Found { get; set; }
        public HttpRequest Request { get; set; }
        public HttpResponse Response { get; set; }

        public HttpContext()
        {
            Correlation = Guid.NewGuid();
            Properties = new Dictionary<string, object>();
        }

        public void Dispose()
        {
            using (Connection) { }
        }
    }
}
