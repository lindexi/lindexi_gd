using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using WinRemoteShell.Shared;
using WinRemoteShell.Shared.Transmissions;

namespace WinRemoteShell.Server;

public static class ServerHost
{
    public static WebApplication Create(int port)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Host.UseWindowsService(options => options.ServiceName = WindowsServiceInstaller.ServiceName);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = null;
            options.ListenAnyIP(port);
        });
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
        builder.Services.AddSingleton<CmdProcess>();
        builder.Services.AddSingleton<DirectProcessExecutor>();
        builder.Services.AddSingleton<RemoteProcessManager>();

        var app = builder.Build();
        app.UseWebSockets();
        MapExec(app);
        MapList(app);
        MapChangeDirectory(app);
        MapShell(app);
        MapPush(app);
        MapPull(app);
        MapScreenshot(app);
        MapProcesses(app);
        MapKillProcesses(app);
        return app;
    }

    public static async Task RunAsync(int port, CancellationToken cancellationToken = default)
    {
        await using var app = Create(port);
        await app.RunAsync(cancellationToken);
    }

    private static void MapExec(WebApplication app)
    {
        app.MapPost("/exec", async (
            ExecRequest request,
            DirectProcessExecutor executor,
            CmdProcess cmd,
            HttpContext context) =>
        {
            if (request.Arguments.Count == 0 || string.IsNullOrWhiteSpace(request.Arguments[0]))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("An executable file name is required.", context.RequestAborted);
                return;
            }

            context.Response.ContentType = "text/plain; charset=utf-8";

            try
            {
                using var timeoutSource = request.TimeoutSeconds is not null
                    ? CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted)
                    : null;
                timeoutSource?.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds!.Value));
                var cancellationToken = timeoutSource?.Token ?? context.RequestAborted;
                var currentWorkingDirectory = await cmd.GetWorkingDirectoryAsync(context.RequestAborted);
                var workingDirectory = string.IsNullOrWhiteSpace(request.WorkingDirectory)
                    ? currentWorkingDirectory
                    : Path.GetFullPath(request.WorkingDirectory, currentWorkingDirectory);

                await foreach (var line in executor.ExecuteAsync(request.Arguments, workingDirectory, cancellationToken))
                {
                    await context.Response.WriteAsync(line + Environment.NewLine, context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                }
            }
            catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
            {
            }
            catch (Exception exception) when (!context.RequestAborted.IsCancellationRequested)
            {
                await context.Response.WriteAsync(exception.ToString(), context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        });
    }

    private static void MapList(WebApplication app)
    {
        app.MapGet("/ls", async (string? path, CmdProcess cmd, CancellationToken cancellationToken) =>
        {
            var workingDirectory = await cmd.GetWorkingDirectoryAsync(cancellationToken);
            var resolvedPath = string.IsNullOrWhiteSpace(path)
                ? workingDirectory
                : Path.GetFullPath(path, workingDirectory);
            var entries = Directory.EnumerateFileSystemEntries(resolvedPath)
                .Select(entryPath =>
                {
                    var isDirectory = Directory.Exists(entryPath);
                    FileSystemInfo info = isDirectory
                        ? new DirectoryInfo(entryPath)
                        : new FileInfo(entryPath);
                    return new RemoteDirectoryEntry(
                        info.Name,
                        info.FullName,
                        isDirectory,
                        isDirectory ? null : ((FileInfo) info).Length,
                        info.CreationTimeUtc,
                        info.LastWriteTimeUtc);
                })
                .ToArray();
            return new DirectoryListingResponse(resolvedPath, entries);
        });
    }

    private static void MapProcesses(WebApplication app)
    {
        app.MapGet("/ps", (bool details, RemoteProcessManager processManager) => processManager.List(details));
    }

    private static void MapKillProcesses(WebApplication app)
    {
        app.MapPost("/kill", (KillProcessesRequest request, RemoteProcessManager processManager) =>
        {
            var hasProcessId = request.ProcessId is not null;
            var hasProcessName = !string.IsNullOrWhiteSpace(request.ProcessName);
            if (hasProcessId == hasProcessName)
            {
                return Results.BadRequest("Specify exactly one process identifier: processId or processName.");
            }

            if (request.ProcessId <= 0)
            {
                return Results.BadRequest("The processId must be greater than zero.");
            }

            return Results.Ok(processManager.Kill(request));
        });
    }

    private static void MapChangeDirectory(WebApplication app)
    {
        app.MapPost("/cd", async (ChangeDirectoryRequest request, CmdProcess cmd, CancellationToken cancellationToken) =>
        {
            await foreach (var _ in cmd.ExecuteAsync(["cd", "/d", $"\"{request.Path}\""], cancellationToken))
            {
            }

            return new WorkingDirectoryResponse(await cmd.GetWorkingDirectoryAsync(cancellationToken));
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
            var target = Decode(context.Request.Headers["X-WinRS-Target"].ToString());
            var modeValue = context.Request.Headers["X-WinRS-Push-Mode"].ToString();
            var mode = PushMode.Merge;
            if (!string.IsNullOrWhiteSpace(modeValue) &&
                (!Enum.TryParse(modeValue, true, out mode) || !Enum.IsDefined(mode)))
            {
                return Results.BadRequest("The push mode is invalid.");
            }

            var deleteTargetValue = context.Request.Headers["X-WinRS-Delete-Target"].ToString();
            if (!string.IsNullOrWhiteSpace(deleteTargetValue) &&
                !bool.TryParse(deleteTargetValue, out _))
            {
                return Results.BadRequest("The delete-target value is invalid.");
            }

            var deleteTarget = bool.TryParse(deleteTargetValue, out var parsedDeleteTarget) && parsedDeleteTarget;
            if (deleteTarget && mode != PushMode.Replace)
            {
                return Results.BadRequest("Deleting the push target requires Replace mode.");
            }

            var targetExists = File.Exists(target) || Directory.Exists(target);
            if (mode == PushMode.FailIfExists && targetExists)
            {
                return Results.Conflict("The push target already exists.");
            }

            if (mode == PushMode.Replace && targetExists)
            {
                if (Directory.Exists(target))
                {
                    Directory.Delete(target, true);
                }
                else
                {
                    File.Delete(target);
                }
            }

            if (deleteTarget)
            {
                return Results.Ok();
            }

            await TransferStream.ReceiveAsync(
                context.Request.Body,
                target,
                placeFileInExistingDirectory: false,
                context.RequestAborted);
            return Results.Ok();
        });
    }

    private static void MapPull(WebApplication app)
    {
        app.MapGet("/pull", async (HttpContext context) =>
        {
            var source = Decode(context.Request.Query["source"].ToString());
            context.Response.ContentType = "application/vnd.winremoteshell.transfer-v1";
            await TransferStream.WriteAsync(
                context.Response.Body,
                TransferManifest.Create(source),
                context.RequestAborted);
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

    private static async Task ReceiveShellInputAsync(WebSocket webSocket, CmdProcess cmd, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
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
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task SendShellOutputAsync(WebSocket webSocket, CmdProcess cmd, CancellationToken cancellationToken)
    {
        await foreach (var line in cmd.ReadOutputAsync(cancellationToken))
        {
            var contentText = line + Environment.NewLine;
            var byteCount = Encoding.UTF8.GetByteCount(contentText);
            var content = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                var bytesWritten = Encoding.UTF8.GetBytes(contentText, content);
                await webSocket.SendAsync(content.AsMemory(0, bytesWritten), WebSocketMessageType.Text, true, cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(content);
            }
        }
    }

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));
}
