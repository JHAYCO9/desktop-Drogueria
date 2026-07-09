using System.IO;
using Microsoft.Extensions.Configuration;

namespace DrogueriaMickey.Desktop.Services
{
    public static class AppConfig
    {
        public static IConfiguration Configuration { get; }

        static AppConfig()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string ApiBaseUrl => Configuration["ApiBaseUrl"] ?? "http://localhost:8080/api/v1/";
    }
}