using Serilog;
using System;
using Topshelf;

namespace MachinaAurum.Artemis
{
    class Program
    {
        static int Main()
        {
            int? port = null;
            string tag = null;

            var logger = new LoggerConfiguration()
               .UseArtemis()
               .ReadFrom.AppSettings()
               .CreateLogger();
            global::Serilog.Log.Logger = logger;

            return (int)HostFactory.Run(x =>
            {
                x.UseAssemblyInfoForServiceInfo();
                x.UseSerilog(logger);
                x.Service<ArtemisService>(s =>
                {
                    s.ConstructUsing(() => new ArtemisService(port, logger, tag));
                    s.WhenStarted(v => v.Start());
                    s.WhenStopped(v => v.Stop());
                });

                x.SetStartTimeout(TimeSpan.FromSeconds(10));
                x.SetStopTimeout(TimeSpan.FromSeconds(10));

                x.AddCommandLineDefinition("uiport", v => port = int.Parse(v));
                x.AddCommandLineDefinition("tag", v => tag = v);
            });
        }
    }
}
