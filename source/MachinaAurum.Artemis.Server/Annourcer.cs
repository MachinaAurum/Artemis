using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MachinaAurum.Artemis.Server
{
    public static class Annourcer
    {
        public static void UseConsul(string serviceName, int port, int checkPort, string[] tags)
        {
            var li = new HttpListener();
            li.Prefixes.Add($"http://+:{checkPort}/check/");
            li.Start();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var ctx = li.GetContext();
                    using (ctx.Response)
                    {
                        ctx.Response.StatusCode = 200;
                        ctx.Response.SendChunked = false;
                    }
                }
            });

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        using (var client = new HttpClient())
                        {
                            var localIp = "127.0.0.1";
                            var check = $"http://127.0.0.1:{checkPort}/check/";
                            string contentStr = $@"{{""Name"":""{serviceName}"",""Address"":""{localIp}"",""Port"":{port},""Tags"": [{string.Join(",", tags.Select(x => $"\"{x}\"" ))}],""Check"":{{""HTTP"":""{check}"",""Interval"":""10s""}}}}";
                            var content = new StringContent(contentStr);

                            var registerUrl = $"http://{"127.0.0.1"}:{8500}/v1/agent/service/register";

                            var response = await client.PostAsync(registerUrl, content);
                        }
                    }
                    catch (Exception)
                    {
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            });
        }
    }
}
