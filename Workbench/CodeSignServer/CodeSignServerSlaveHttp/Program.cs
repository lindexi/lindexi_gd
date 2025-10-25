using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace CodeSignServerMaster
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.WebHost.UseKestrel(options =>
            {
                // 无限制请求体大小
                options.Limits.MaxRequestBodySize = null;
            });

            var app = builder.Build();
            app.Urls.Add("http://0.0.0.0:57563");

            // Configure the HTTP request pipeline.

            app.MapGet("/alive", () => DateTime.Now.ToString(CultureInfo.InvariantCulture));

            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            app.Map("/fetch", async (Microsoft.AspNetCore.Http.HttpContext context) =>
            {
                
            });

            app.Run();
        }
    }
}


static class HttpHelper
{
    public static void RegisterForDispose(this Microsoft.AspNetCore.Http.HttpResponse response, Action disposeAction)
    {
        response.RegisterForDispose(new DelegateDisposable(disposeAction));
    }

    class DelegateDisposable(Action disposeAction) : IDisposable
    {
        public void Dispose()
        {
            disposeAction();
        }
    }
}