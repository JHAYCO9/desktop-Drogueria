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

        public static string ApiBaseUrl
        {
            get
            {
                var url = Configuration["ApiBaseUrl"] ?? "https://backend-drogueria.onrender.com/api/v1";
                // Garantiza la barra final: sin ella, HttpClient reemplaza
                // el último segmento de la ruta en vez de añadirle la ruta
                // relativa, causando 404 silenciosos (ej. pierde el "v1").
                return url.EndsWith("/") ? url : url + "/";
            }
        }
    }
}