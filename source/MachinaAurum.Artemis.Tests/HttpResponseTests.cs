using MachinaAurum.Artemis.Http;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MachinaAurum.Artemis.Tests
{
    public class HttpResponseTests
    {
        [Fact]
        public async Task HttpRequestMustStreamAllRequest()
        {
            MemoryStream stream = GetResponse();
            MemoryStream stream2 = new MemoryStream();

            var response = new HttpResponse(stream2);
            await response.RedirectFromAsync(stream);

            Assert.Equal(stream.Position, stream.Length);
            Assert.Equal(stream2.Length, stream.Length);
        }

        private static MemoryStream GetResponse()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine("HTTP/1.1 200 OK");
            writer.WriteLine("Content-Length: 4");
            writer.WriteLine();
            writer.WriteLine("!!!!");
            writer.Flush();

            stream.Position = 0;

            return stream;
        }
    }
}
