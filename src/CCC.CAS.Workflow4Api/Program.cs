using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using CCC.CAS.API.AspNetCommon.Extensions;

namespace CCC.CAS.Workflow4Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .AddSharedSettings()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    _ = webBuilder
                        .UseStartup<Startup>()
                        .UseCommonSerilog(typeof(Program).Assembly.GetName().Name ?? "");
                });
    }
}
