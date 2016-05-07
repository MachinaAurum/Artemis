using System.IO;
using System.Threading.Tasks;

namespace MachinaAurum.Artemis.Http
{
    public class HttpResponse
    {
        Stream Output;

        public HttpResponse(Stream output)
        {
            Output = output;
        }

        public async Task RedirectFromAsync(Stream input)
        {
            await HttpParser.StreamResponse(input, Output);
        }

        public void Send(int status)
        {
            string message = null;

            if(status == 404)
            {
                message = "Not Found";
            }
            else if(status == 500)
            {
                message = "Internal Server Error";
            }

            var writer = new StreamWriter(Output);
            writer.WriteLine($"HTTP/1.1 {status} {message}");
            writer.WriteLine("Content-Length: 0");
            writer.WriteLine();            
            writer.Flush();
        }
    }
}
