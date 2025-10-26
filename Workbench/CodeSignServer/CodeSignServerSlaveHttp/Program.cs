using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
            Task.Run(async () =>
            {
                var httpClient = new HttpClient();
                var multipartFormDataContent = new MultipartFormDataContent();
                multipartFormDataContent.Add(new StringContent("foo", Encoding.UTF8));
                var memoryStream = new FakeStream();
                multipartFormDataContent.Add(new StreamContent(memoryStream), "f", "f1");
                multipartFormDataContent.Add(new StringContent("fooasd", Encoding.UTF8), "asd");

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
                //var multipartReader = new MultipartReader();
                var headersContentType = request.Headers.ContentType;
                string? contentType = headersContentType[0];
                if (contentType is null)
                {
                    return;
                }

                // multipart/form-data; boundary="0b8697bf-8e34-4d73-b71b-8b3606995409"
                var l = "multipart/form-data; boundary=\"".Length;
                var boundary = contentType.Substring(l, contentType.Length - l - 1);

                var multipartReader = new MultipartReader(boundary, request.Body, 1024);

                while (true)
                {
                    MultipartSection? multipartSection = await multipartReader.ReadNextSectionAsync();
                    if (multipartSection == null)
                    {
                        break;
                    }

                    var multipartSectionContentType = multipartSection.ContentType;
                    if (multipartSectionContentType?.StartsWith("text/plain") is true)
                    {
                        var text = await multipartSection.ReadAsStringAsync();
                    }
                    else if (multipartSection.ContentDisposition is not null)
                    {
                        var fileMultipartSection = multipartSection.AsFileSection();
                        if (fileMultipartSection != null)
                        {
                            var fileStream = fileMultipartSection.FileStream;
                            if (fileStream is null)
                            {
                                continue;
                            }

                            byte[] buffer = new byte[1024 * 1024];
                            while (true)
                            {
                                var readCount = await fileStream.ReadAsync(buffer);
                                if (readCount == buffer.Length)
                                {

                                }
                                else if (readCount == 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

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
    public override long Length => long.MaxValue;
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