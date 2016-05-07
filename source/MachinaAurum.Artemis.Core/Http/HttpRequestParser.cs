using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MachinaAurum.Artemis.Http
{
    public static class HttpParser
    {
        public class FirstLine
        {
            public string Method;
            public string Uri;
            public string Version;
        }

        public class HeaderLine
        {
            public string Name;
            public string Value;
        }

        public static async Task ParseRequestFirstLine(Stream input, Stream output, HttpRequestData data)
        {
            data.Method = await ReadUntil(input, output, ' ', true, 20);
            data.Uri = await ReadUntil(input, output, ' ', true, 1000 * 10);
            data.Version = await ReadUntilLineEnd(input, output, true, 100);
        }

        public static async Task ParseHeaders(Stream input, Stream output, HttpRequestData data)
        {
            var @continue = false;
            do
            {
                @continue = await ParseHeaderLine(input, output, data);
            } while (@continue);
        }

        public static async Task<bool> ParseHeaderLine(Stream input, Stream output, HttpRequestData data)
        {
            var name = await ReadUntil(input, output, ' ', ':', '\r', '\n', false, 100);

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            await ReadWhile(input, output, ' ', true, 100);

            var value = await ReadUntilLineEnd(input, output, false, 1024);

            if (name == "Host")
            {
                data.Host = value;
            }
            else if (name == "Content-Length")
            {
                data.ContentLength = long.Parse(value);
            }

            return true;
        }

        public static async Task<bool> StreamHeaderLine(Stream input, Stream output)
        {
            var name = await ReadUntil(input, output, ' ', ':', '\r', '\n', false, 100);

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            await ReadWhile(input, output, ' ', true, 100);
            await ReadUntilLineEnd(input, output, false, 1024);

            return true;
        }

        public static async Task StreamResponse(Stream input, Stream output)
        {
            await ReadUntil(input, output, ' ', true, 20);
            await ReadUntil(input, output, ' ', true, 10);
            await ReadUntilLineEnd(input, output, true, 100);

            HttpRequestData data = new HttpRequestData();

            await ParseHeaders(input, output, data);
            await input.CopyToAsync(output, data.ContentLength.Value);
        }

        static async Task<string> ReadUntilLineEnd(Stream input, Stream output, bool zerostr = true, int limit = 100)
        {
            await Task.Yield();

            var builder = new StringBuilder();

            while (ReadCharLineBreak(input, output, builder))
            {
                limit--;
                if (limit <= 0)
                {
                    throw new Exception();
                }
            }

            return builder.ToString();
        }

        static bool ReadCharLineBreak(Stream input, Stream output, StringBuilder builder)
        {
            var b = (byte)input.ReadByte();
            output.WriteByte(b);
            char bc = (char)b;

            if (bc == '\n')
            {
                return false;
            }
            else if (bc == '\r')
            {
                b = (byte)input.ReadByte(); //read \n
                output.WriteByte(b);
                return false;
            }
            else
            {
                builder.Append(bc);
                return true;
            }
        }

        static async Task<string> ReadUntil(Stream input, Stream output, char c, bool zerostr = true, int limit = 100)
        {
            var builder = new StringBuilder();

            while (await ReadChar(input, output, builder, c))
            {
                limit--;
                if (limit <= 0)
                {
                    throw new Exception();
                }
            }

            return builder.ToString();
        }

        static async Task<string> ReadUntil(Stream input, Stream output, char c1, char c2, bool zerostr = true, int limit = 100)
        {
            var builder = new StringBuilder();

            while (await ReadChar(input, output, builder, c1, c2))
            {
                limit--;
                if (limit <= 0)
                {
                    throw new Exception();
                }
            }

            return builder.ToString();
        }

        static async Task<string> ReadUntil(Stream input, Stream output, char c1, char c2, char c3, char c4, bool zerostr = true, int limit = 100)
        {
            var builder = new StringBuilder();

            while (await ReadChar(input, output, builder, c1, c2, c3, c4))
            {
                limit--;
                if (limit <= 0)
                {
                    throw new Exception();
                }
            }

            return builder.ToString();
        }

        static async Task<bool> ReadChar(Stream input, Stream output, StringBuilder builder, char c)
        {
            await Task.Yield();
            var b = (byte)input.ReadByte();
            output.WriteByte(b);
            char bc = (char)b;

            if (bc != c)
            {
                builder.Append(bc);
                return true;
            }
            else
            {
                return false;
            }
        }

        static async Task<bool> ReadChar(Stream input, Stream output, StringBuilder builder, char c1, char c2)
        {
            await Task.Yield();
            var b = (byte)input.ReadByte();
            output.WriteByte(b);
            char bc = (char)b;

            if (bc == c1 || bc == c2)
            {
                return false;
            }
            else
            {
                builder.Append(bc);
                return true;
            }
        }

        static async Task<bool> ReadChar(Stream input, Stream output, StringBuilder builder, char c1, char c2, char c3, char c4)
        {
            await Task.Yield();
            var b = (byte)input.ReadByte();
            output.WriteByte(b);
            char bc = (char)b;

            if (bc == c1 || bc == c2 || bc == c3 || bc == c4)
            {
                return false;
            }
            else
            {
                builder.Append(bc);
                return true;
            }
        }

        static async Task ReadWhile(Stream input, Stream output, char c, bool zerostr = true, int limit = 100)
        {
            var builder = new StringBuilder();
            while (await ReadChar(input, output, builder, c))
            {
                limit--;
                if (limit <= 0)
                {
                    throw new Exception();
                }
            }
        }
    }
}
