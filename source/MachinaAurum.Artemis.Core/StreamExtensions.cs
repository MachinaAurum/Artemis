using System.Threading.Tasks;

namespace System.IO
{
    public static class StreamExtensions
    {
        public static async Task CopyToAsync(this Stream input, Stream output, long size)
        {
            int read = 0;            
            do
            {
                var buffer = new byte[1024];
                read = await input.ReadAsync(buffer, 0, 1024);
                size -= read;

                await output.WriteAsync(buffer, 0, read);
            } while (size > 0);
        }
    }
}
