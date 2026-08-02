using System.IO.Compression;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Server;

public static class ServerHost
{
    public static WebApplication Create(int port)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Host.UseWindowsService(options => options.ServiceName = WindowsServiceInstaller.ServiceName);
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(port));
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
        builder.Services.AddSingleton<CmdProcess>();

        var app = builder.Build();
        app.UseWebSockets();
        MapExec(app);
        MapShell(app);
        MapPush(app);
        MapPull(app);
        MapScreenshot(app);
        return app;
    }

    public static async Task RunAsync(int port, CancellationToken cancellationToken = default)
    {
        await using var app = Create(port);
        await app.RunAsync(cancellationToken);
    }

    private static void MapExec(WebApplication app)
    {
        app.MapPost("/exec", async (ExecRequest request, CmdProcess cmd, HttpContext context) =>
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            using var timeoutSource = request.TimeoutSeconds is { } seconds
                ? CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted)
                : null;
            timeoutSource?.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds!.Value));
            var cancellationToken = timeoutSource?.Token ?? context.RequestAborted;

            try
            {
                await foreach (var line in cmd.ExecuteAsync(request.Arguments, cancellationToken))
                {
                    await context.Response.WriteAsync(line + Environment.NewLine, context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                }
            }
            catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
            {
                await cmd.InterruptOrRestartAsync(context.RequestAborted);
            }
        });
    }

    private static void MapShell(WebApplication app)
    {
        app.Map("/shell", async (HttpContext context, CmdProcess cmd) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            var receiveTask = ReceiveShellInputAsync(webSocket, cmd, cancellationSource.Token);
            var sendTask = SendShellOutputAsync(webSocket, cmd, cancellationSource.Token);

            await Task.WhenAny(receiveTask, sendTask);
            await cancellationSource.CancelAsync();

            try
            {
                await Task.WhenAll(receiveTask, sendTask);
            }
            catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
            {
            }

            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            }
        });
    }

    private static void MapPush(WebApplication app)
    {
        app.MapPost("/push", async (HttpContext context) =>
        {
            var maxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (maxRequestBodySizeFeature is { IsReadOnly: false })
            {
                maxRequestBodySizeFeature.MaxRequestBodySize = null;
            }

            var target = Decode(context.Request.Headers["X-WinRS-Target"].ToString());
            if (context.Request.Headers["X-WinRS-Type"] == "directory")
            {
                Directory.CreateDirectory(target);
                var archivePath = Path.Join(Path.GetTempPath(), $"WinRemoteShell_Push_{Guid.NewGuid():N}.zip");
                try
                {
                    await using (var archiveStream = new FileStream(
                        archivePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        4096,
                        FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        await context.Request.Body.CopyToAsync(archiveStream, context.RequestAborted);
                    }

                    using var archive = ZipFile.OpenRead(archivePath);
                    archive.ExtractToDirectory(target, true);
                }
                finally
                {
                    File.Delete(archivePath);
                }
            }
            else
            {
                await using var file = File.Create(target);
                await context.Request.Body.CopyToAsync(file, context.RequestAborted);
            }
        });
    }

    private static void MapPull(WebApplication app)
    {
        app.MapGet("/pull", async (HttpContext context) =>
        {
            var source = Decode(context.Request.Query["source"].ToString());
            if ((File.GetAttributes(source) & FileAttributes.Directory) != 0)
            {
                context.Response.Headers["X-WinRS-Type"] = "directory";
                context.Response.ContentType = "application/zip";
                await WriteDirectoryArchiveAsync(source, context.Response, context.RequestAborted);
            }
            else
            {
                context.Response.Headers["X-WinRS-Type"] = "file";
                context.Response.Headers["X-WinRS-FileName"] = Encode(Path.GetFileName(source));
                context.Response.ContentType = "application/octet-stream";
                await using var file = File.OpenRead(source);
                context.Response.ContentLength = file.Length;
                await file.CopyToAsync(context.Response.Body, context.RequestAborted);
            }
        });
    }

    private static void MapScreenshot(WebApplication app)
    {
        app.MapGet("/screenshot", async (HttpContext context) =>
        {
            context.Response.ContentType = "image/png";
            await ScreenshotCapture.CaptureAsync(context.Response.Body, context.RequestAborted);
        });
    }

    private static async Task WriteDirectoryArchiveAsync(
        string source,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        // ZipArchive 会在写入条目元数据和释放时同步写入中央目录，而 Kestrel 禁止对响应 Body 执行同步 I/O。
        // 此适配器将同步写入限制在 PipeWriter 缓冲区内，再通过异步刷新发送响应，从而保持流式压缩和背压。
        using var responseStream = new PipeWriterStream(response.BodyWriter);
        using (var archive = new ZipArchive(responseStream, ZipArchiveMode.Create, true))
        {
            foreach (var directoryPath in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                archive.CreateEntry(Path.GetRelativePath(source, directoryPath).Replace('\\', '/') + "/");
            }

            foreach (var filePath in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = archive.CreateEntry(
                    Path.GetRelativePath(source, filePath).Replace('\\', '/'),
                    CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await using var file = File.OpenRead(filePath);
                await file.CopyToAsync(entryStream, cancellationToken);
            }
        }

        await responseStream.FlushAsync(cancellationToken);
    }

    private sealed class PipeWriterStream(PipeWriter writer) : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => FlushCoreAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            Write(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            buffer.CopyTo(writer.GetSpan(buffer.Length));
            writer.Advance(buffer.Length);
        }

        public override Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            Write(buffer.Span);
            await writer.FlushAsync(cancellationToken);
        }

        private async Task FlushCoreAsync(CancellationToken cancellationToken)
        {
            await writer.FlushAsync(cancellationToken);
        }
    }

    private static async Task ReceiveShellInputAsync(WebSocket webSocket, CmdProcess cmd, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var message = new MemoryStream();
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            await message.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
            if (!result.EndOfMessage)
            {
                continue;
            }

            var line = Encoding.UTF8.GetString(message.GetBuffer(), 0, checked((int)message.Length));
            message.SetLength(0);
            if (string.Equals(line.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await cmd.WriteLineAsync(line, cancellationToken);
        }
    }

    private static async Task SendShellOutputAsync(WebSocket webSocket, CmdProcess cmd, CancellationToken cancellationToken)
    {
        await foreach (var line in cmd.ReadOutputAsync(cancellationToken))
        {
            var content = Encoding.UTF8.GetBytes(line + Environment.NewLine);
            await webSocket.SendAsync(content, WebSocketMessageType.Text, true, cancellationToken);
        }
    }

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));
}
