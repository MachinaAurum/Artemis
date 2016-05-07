using MachinaAurum.Artemis.Http;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MachinaAurum.Artemis.Tests
{
    public class HttpRequestTests
    {
        [Fact]
        public async Task HttpRequestMustParseRequestFirstLineFromStream()
        {
            MemoryStream stream = GetRequest();

            var httpRequest = new HttpRequest(stream);
            await httpRequest.ReadFirstLineAsync();

            Assert.Equal("GET", httpRequest.Method);
            Assert.Equal("/api/someresource", httpRequest.Uri);
            Assert.Equal("HTTP/1.1", httpRequest.Version);
        }

        [Fact]
        public async Task HttpRequestMustParseRequestHostFromStream()
        {
            MemoryStream stream = GetRequest();

            var httpRequest = new HttpRequest(stream);
            await httpRequest.ReadHeadersAsync();

            Assert.Equal("api.service.com", httpRequest.Host);
            Assert.NotEqual(stream.Position, stream.Length);
        }

        [Fact]
        public async Task HttpRequestMustStreamAllRequest()
        {
            MemoryStream stream = GetRequest();
            MemoryStream stream2 = new MemoryStream();

            var httpRequest = new HttpRequest(stream);
            await httpRequest.ReadHeadersAsync();
            await httpRequest.RedirectToAsync(stream2);
            
            Assert.Equal(stream.Position, stream.Length);
            Assert.Equal(stream2.Length, stream.Length);
        }

        [Fact]
        public async Task HttpRequestMustStreamAppendedHeaders()
        {
            MemoryStream stream = GetRequest();
            MemoryStream stream2 = new MemoryStream();

            var httpRequest = new HttpRequest(stream);
            await httpRequest.ReadHeadersAsync();
            httpRequest.AddHeader("SOMEHEADER", "VALUE");

            await httpRequest.RedirectToAsync(stream2);

            Assert.Equal(stream.Position, stream.Length);
            //SOMEHEADER: VALUE\r\n = 19bytes
            Assert.Equal(stream2.Length, stream.Length + 19);
        }

        private static MemoryStream GetRequest()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine("GET /api/someresource HTTP/1.1");
            writer.WriteLine("Host: api.service.com");
            writer.WriteLine("Content-Length: 4");
            writer.WriteLine();
            writer.WriteLine("!!!!");
            writer.Flush();

            stream.Position = 0;

            return stream;
        }
    }
}
