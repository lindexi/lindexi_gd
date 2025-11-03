using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebUtilities;

using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;

var port = GetAvailablePort(IPAddress.Loopback);
var url = $"http://127.0.0.1:{port}";

Task.Run(async () =>
{
    using var httpClient = new HttpClient();

    using var multipartFormDataContent = new MultipartFormDataContent();
    using var fakeLongStream = new FakeLongStream();
    multipartFormDataContent.Add(new StreamContent(fakeLongStream), "TheFile", "FileName.zip");
    multipartFormDataContent.Add(new StringContent("Value1"), "Field1");
    var response = await httpClient.PostAsync($"{url}/PostMultipartForm", multipartFormDataContent);
    response.EnsureSuccessStatusCode();
});

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrel(options =>
{
    // 无限制请求体大小
    // Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException:“Request body too large. The max request body size is 30000000 bytes.”
    options.Limits.MaxRequestBodySize = null;
});

var app = builder.Build();
app.Urls.Add(url);

app.MapPost("/PostMultipartForm", async (Microsoft.AspNetCore.Http.HttpContext context) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    var request = context.Request;
    var response = context.Response;

    var headersContentType = request.Headers.ContentType;
    string? contentType = headersContentType[0];
    if (contentType is null)
    {
        return;
    }

    var type = new ContentType(contentType);
    var contentTypeBoundary = type.Boundary;
    if (contentTypeBoundary is null)
    {
        return;
    }

    var multipartReader = new MultipartReader(contentTypeBoundary, request.Body, 1024);

    while (true)
    {
        MultipartSection? multipartSection = await multipartReader.ReadNextSectionAsync();
        if (multipartSection == null)
        {
            // 读取完成了
            break;
        }

        if (multipartSection.ContentDisposition is null)
        {
            continue;
        }

        // ContentType=application/octet-stream
        // form-data; name="file"; filename="Input.zip"

        var contentDisposition = new ContentDisposition(multipartSection.ContentDisposition);

        if (contentDisposition.FileName is not null)
        {
            // 文件
            var fileName = contentDisposition.FileName;
            fileName = GetSafeFileName(fileName);
            // 处理文件上传逻辑，例如保存文件
            // 这里简单地将文件保存到临时目录。小心，生产环境中请确保文件名安全，小心被攻击
            var filePath = Path.Join(Path.GetTempPath(), $"Uploaded_{fileName}");
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read,
                10240,
                // 确保文件在关闭后被删除，以防止临时文件堆积。此仅仅为演示需求，避免临时文件太多。请根据你的需求决定是否使用此选项
                FileOptions.DeleteOnClose);
            await multipartSection.Body.CopyToAsync(fileStream);

            // 完成文件写入之后，可以通过以下代码，直接读取文件的内容
            fileStream.Position = 0;
            // 此时就可以立刻读取 FileStream 的内容了
            logger.LogInformation($"Received file '{fileName}', saved to '{filePath}'");
        }
        else
        {
            // 普通表单字段
            var formMultipartSection = multipartSection.AsFormDataSection();
            if (formMultipartSection is null)
            {
                continue;
            }

            var name = formMultipartSection.Name;
            var value = await formMultipartSection.GetValueAsync();

            logger.LogInformation($"Received form field '{name}': {value}");
        }
    }

    await response.StartAsync();

    await response.CompleteAsync();
});

app.Run();

static string GetSafeFileName(string arbitraryString)
{
    var invalidChars = System.IO.Path.GetInvalidFileNameChars();
    var replaceIndex = arbitraryString.IndexOfAny(invalidChars, 0);
    if (replaceIndex == -1) return arbitraryString;

    var r = new StringBuilder();
    var i = 0;

    do
    {
        r.Append(arbitraryString, i, replaceIndex - i);

        switch (arbitraryString[replaceIndex])
        {
            case '"':
                r.Append("''");
                break;
            case '<':
                r.Append('\u02c2'); // '˂' (modifier letter left arrowhead)
                break;
            case '>':
                r.Append('\u02c3'); // '˃' (modifier letter right arrowhead)
                break;
            case '|':
                r.Append('\u2223'); // '∣' (divides)
                break;
            case ':':
                r.Append('-');
                break;
            case '*':
                r.Append('\u2217'); // '∗' (asterisk operator)
                break;
            case '\\':
            case '/':
                r.Append('\u2044'); // '⁄' (fraction slash)
                break;
            case '\0':
            case '\f':
            case '?':
                break;
            case '\t':
            case '\n':
            case '\r':
            case '\v':
                r.Append(' ');
                break;
            default:
                r.Append('_');
                break;
        }

        i = replaceIndex + 1;
        replaceIndex = arbitraryString.IndexOfAny(invalidChars, i);
    } while (replaceIndex != -1);

    r.Append(arbitraryString, i, arbitraryString.Length - i);

    return r.ToString();
}

static int GetAvailablePort(IPAddress ip)
{
    using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
    socket.Bind(new IPEndPoint(ip, 0));
    socket.Listen(1);
    var ipEndPoint = (IPEndPoint) socket.LocalEndPoint!;
    var port = ipEndPoint.Port;
    return port;
}

class FakeLongStream : Stream
{
    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (Position == Length)
        {
            return 0;
        }

        Position += count;

        Random.Shared.NextBytes(buffer.AsSpan(offset, count));

        if (Position < Length)
        {
            return count;
        }

        var result = (int) (Length - (Position - count));
        Position = Length;
        return result;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => int.MaxValue / 2;
    public override long Position { get; set; }
}