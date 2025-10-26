using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace CodeSignServerMaster
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                var httpClient = new HttpClient();
                var multipartFormDataContent = new MultipartFormDataContent();
                multipartFormDataContent.Add(new StringContent("foo", Encoding.UTF8));
                var memoryStream = new FakeStream();
                multipartFormDataContent.Add(new StreamContent(memoryStream), "File", "f1");

                using var httpResponseMessage = await httpClient.PostAsync("http://127.0.0.1:57563/sign", multipartFormDataContent);
            });

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

            app.Map("/sign", async (Microsoft.AspNetCore.Http.HttpContext context) =>
            {
                var request = context.Request;
                await request.ReadFormAsync(new FormOptions()
                {
                    BufferBody = false,
                    MultipartBodyLengthLimit = long.MaxValue,
                });

                IFormCollection formCollection = request.Form;
                foreach (KeyValuePair<string, StringValues> keyValuePair in formCollection)
                {

                }

                foreach (IFormFile formCollectionFile in formCollection.Files)
                {
                    var stream = formCollectionFile.OpenReadStream();
                    byte[] buffer = new byte[1024 * 1024];
                    while (true)
                    {
                        var readCount = await stream.ReadAsync(buffer);
                        if (readCount == buffer.Length)
                        {

                        }
                        else
                        {
                            break;
                        }
                    }
                }
            });

            app.Run();
        }
    }
}

class FakeStream : Stream
{
    public override void Flush()
    {

    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        Position += count;
        if (Position < Length)
        {
            return count;
        }
        return (int) (Length - (Position - count));
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return Position;
    }

    public override void SetLength(long value)
    {
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => 1024 * 1024 * 1024;
    public override long Position { get; set; }
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