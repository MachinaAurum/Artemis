using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MachinaAurum.Artemis.Http
{
    public class HttpRequest
    {
        Stream Input;
        Stream BufferFirstLine;
        Stream BufferHeaders;
        Stream BufferAppendHeaders;

        HttpRequestData Data;

        public string Method { get { return Data.Method; } }
        public string Uri { get { return Data.Uri; } }
        public string Version { get { return Data.Version; } }
        public string Host { get { return Data.Host; } }
        public long? ContentLength { get { return Data.ContentLength; } }

        public HttpRequest(Stream input)
        {
            Input = input;
            BufferFirstLine = new MemoryStream();
            BufferHeaders = new MemoryStream();
            BufferAppendHeaders = new MemoryStream();

            Data = new HttpRequestData();
        }

        public async Task ReadFirstLineAsync()
        {
            if (string.IsNullOrEmpty(Data.Method))
            {
                await HttpParser.ParseRequestFirstLine(Input, BufferFirstLine, Data);
            }
        }

        public async Task ReadHeadersAsync()
        {
            if (string.IsNullOrEmpty(Data.Host))
            {
                await ReadFirstLineAsync();
                await HttpParser.ParseHeaders(Input, BufferHeaders, Data);
            }
        }

        public void AddHeader(string header, string value)
        {
            var writer = new StreamWriter(BufferAppendHeaders, Encoding.ASCII, 1024, true);
            writer.Write($"{header}: {value}\r\n");
            writer.Flush();
        }

        public async Task RedirectToAsync(Stream output)
        {
            BufferFirstLine.Position = 0;
            await BufferFirstLine.CopyToAsync(output);

            BufferAppendHeaders.Position = 0;
            await BufferAppendHeaders.CopyToAsync(output);

            BufferHeaders.Position = 0;
            await BufferHeaders.CopyToAsync(output);

            await Input.CopyToAsync(output, ContentLength.Value);
        }
    }
}
