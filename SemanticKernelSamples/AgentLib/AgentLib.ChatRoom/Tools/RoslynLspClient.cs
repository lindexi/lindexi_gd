using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentLib.ChatRoom.Tools;

internal sealed class RoslynLspClient : IAsyncDisposable
{
    private const int MaximumMessageLength = 16 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Process _process;
    private readonly Stream _input;
    private readonly Stream _output;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonNode?>> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, byte> _openDocuments = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _readerTask;
    private readonly Task _errorReaderTask;
    private Exception? _connectionFailure;
    private int _disposeStarted;
    private long _nextRequestId;

    private RoslynLspClient(Process process)
    {
        _process = process;
        _input = process.StandardInput.BaseStream;
        _output = process.StandardOutput.BaseStream;
        _readerTask = ReadMessagesAsync(_shutdown.Token);
        _errorReaderTask = CopyErrorsAsync(process.StandardError);
    }

    public static async Task<RoslynLspClient> StartAsync(
        string workspacePath,
        string command,
        CancellationToken cancellationToken)
    {
        ThrowIfNullOrWhiteSpace(workspacePath, nameof(workspacePath));
        ThrowIfNullOrWhiteSpace(command, nameof(command));

        workspacePath = Path.GetFullPath(workspacePath);
        if (!Directory.Exists(workspacePath))
        {
            throw new DirectoryNotFoundException("指定的工作区不存在。");
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = command,
            Arguments = "--stdio --autoLoadProjects --logLevel Warning",
            WorkingDirectory = workspacePath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"无法启动 {command}。");
        RoslynLspClient client = new(process);

        try
        {
            Uri workspaceUri = new(workspacePath.EndsWith(Path.DirectorySeparatorChar)
                ? workspacePath
                : workspacePath + Path.DirectorySeparatorChar);
            JsonNode? initializeResult = await client.RequestAsync(
                "initialize",
                new
                {
                    processId = Process.GetCurrentProcess().Id,
                    rootUri = workspaceUri.AbsoluteUri,
                    capabilities = new
                    {
                        window = new { workDoneProgress = false },
                        textDocument = new
                        {
                            definition = new { linkSupport = false },
                            typeDefinition = new { linkSupport = false },
                            implementation = new { linkSupport = false },
                            documentSymbol = new { hierarchicalDocumentSymbolSupport = true }
                        },
                        workspace = new
                        {
                            workspaceFolders = true,
                            symbol = new { dynamicRegistration = false }
                        }
                    },
                    workspaceFolders = new[]
                    {
                        new { uri = workspaceUri.AbsoluteUri, name = Path.GetFileName(workspacePath) }
                    }
                },
                cancellationToken).ConfigureAwait(false);

            if (initializeResult is null)
            {
                throw new InvalidOperationException("Roslyn LSP initialize 返回了空结果。");
            }

            await client.NotifyAsync("initialized", new { }, cancellationToken).ConfigureAwait(false);
            return client;
        }
        catch
        {
            await client.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task<JsonNode?> RequestAsync(
        string method,
        object? parameters,
        CancellationToken cancellationToken)
    {
        ThrowIfNullOrWhiteSpace(method, nameof(method));
        ThrowIfConnectionUnavailable();

        long id = Interlocked.Increment(ref _nextRequestId);
        TaskCompletionSource<JsonNode?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(id, completion))
        {
            throw new InvalidOperationException($"重复的 LSP 请求 ID：{id}");
        }

        try
        {
            await WriteMessageAsync(new { jsonrpc = "2.0", id, method, @params = parameters }, cancellationToken)
                .ConfigureAwait(false);
            return await WaitWithCancellationAsync(completion.Task, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _pendingRequests.TryRemove(id, out _);
        }
    }

    public Task NotifyAsync(string method, object? parameters, CancellationToken cancellationToken)
    {
        ThrowIfNullOrWhiteSpace(method, nameof(method));
        ThrowIfConnectionUnavailable();
        return WriteMessageAsync(new { jsonrpc = "2.0", method, @params = parameters }, cancellationToken);
    }

    public async Task OpenDocumentAsync(string filePath, CancellationToken cancellationToken)
    {
        ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

        filePath = Path.GetFullPath(filePath);
        if (!_openDocuments.TryAdd(filePath, 0))
        {
            return;
        }

        try
        {
            string text = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            await NotifyAsync(
                "textDocument/didOpen",
                new
                {
                    textDocument = new
                    {
                        uri = new Uri(filePath).AbsoluteUri,
                        languageId = GetLanguageId(filePath),
                        version = 1,
                        text
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _openDocuments.TryRemove(filePath, out _);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        if (!_process.HasExited)
        {
            using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await RequestAsync("shutdown", null, shutdownTimeout.Token).ConfigureAwait(false);
                await NotifyAsync("exit", null, shutdownTimeout.Token).ConfigureAwait(false);
                await WaitForProcessExitAsync(_process, shutdownTimeout.Token).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is OperationCanceledException or IOException or InvalidOperationException)
            {
                Debug.WriteLine($"Roslyn LSP 正常关闭失败，将终止进程：{exception.Message}");
            }
        }

        _shutdown.Cancel();
        _input.Dispose();
        _output.Dispose();

        if (!_process.HasExited)
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        }

        await ObserveBackgroundTaskAsync(_readerTask).ConfigureAwait(false);
        await ObserveBackgroundTaskAsync(_errorReaderTask).ConfigureAwait(false);

        _process.Dispose();
        _shutdown.Dispose();
        _writeLock.Dispose();
    }

    private async Task WriteMessageAsync(object message, CancellationToken cancellationToken)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        byte[] header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _input.WriteAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
            await _input.WriteAsync(body, 0, body.Length, cancellationToken).ConfigureAwait(false);
            await _input.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ReadMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int contentLength = await ReadContentLengthAsync(cancellationToken).ConfigureAwait(false);
                byte[] body = new byte[contentLength];
                await ReadExactlyAsync(_output, body, cancellationToken).ConfigureAwait(false);
                JsonNode? message = JsonNode.Parse(body);
                HandleMessage(message);
            }
        }
        catch (Exception exception)
        {
            var connectionException = new IOException("Roslyn LSP 连接已关闭。", exception);
            Interlocked.CompareExchange(ref _connectionFailure, connectionException, null);
            foreach (TaskCompletionSource<JsonNode?> request in _pendingRequests.Values)
            {
                request.TrySetException(connectionException);
            }
        }
    }

    private async Task<int> ReadContentLengthAsync(CancellationToken cancellationToken)
    {
        int? contentLength = null;
        while (true)
        {
            string line = await ReadAsciiLineAsync(cancellationToken).ConfigureAwait(false);
            if (line.Length == 0)
            {
                int length = contentLength ?? throw new InvalidDataException("LSP 消息缺少 Content-Length。");
                if (length <= 0 || length > MaximumMessageLength)
                {
                    throw new InvalidDataException("LSP 消息的 Content-Length 超出允许范围。");
                }

                return length;
            }

            const string prefix = "Content-Length:";
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(line[prefix.Length..].Trim(), out int parsedLength))
            {
                contentLength = parsedLength;
            }
        }
    }

    private async Task<string> ReadAsciiLineAsync(CancellationToken cancellationToken)
    {
        List<byte> bytes = [];
        byte[] buffer = new byte[1];
        while (true)
        {
            int count = await _output.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                throw new EndOfStreamException();
            }

            if (buffer[0] == (byte)'\n')
            {
                if (bytes.Count > 0 && bytes[^1] == (byte)'\r')
                {
                    bytes.RemoveAt(bytes.Count - 1);
                }

                return Encoding.ASCII.GetString(bytes.ToArray());
            }

            bytes.Add(buffer[0]);
        }
    }

    private void HandleMessage(JsonNode? message)
    {
        if (message?["id"] is not JsonValue idNode
            || !idNode.TryGetValue<long>(out long id)
            || !_pendingRequests.TryGetValue(id, out TaskCompletionSource<JsonNode?>? completion))
        {
            return;
        }

        if (message["error"] is JsonNode error)
        {
            Debug.WriteLine($"LSP 请求失败：{error.ToJsonString()}");
            completion.TrySetException(new InvalidOperationException("LSP 请求失败。"));
        }
        else
        {
            completion.TrySetResult(message["result"]?.DeepClone());
        }
    }

    private static string GetLanguageId(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".vb" => "visualbasic",
        ".razor" or ".cshtml" => "razor",
        _ => "csharp"
    };

    private static async Task CopyErrorsAsync(StreamReader errorReader)
    {
        while (true)
        {
            string? line = await errorReader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                return;
            }

            Debug.WriteLine($"[roslyn-language-server] {line}");
        }
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int count = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken)
                .ConfigureAwait(false);
            if (count == 0)
            {
                throw new EndOfStreamException();
            }

            offset += count;
        }
    }

    private static async Task<T> WaitWithCancellationAsync<T>(Task<T> task, CancellationToken cancellationToken)
    {
        if (task.IsCompleted || !cancellationToken.CanBeCanceled)
        {
            return await task.ConfigureAwait(false);
        }

        var cancellationCompletion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration = cancellationToken.Register(
            static state => ((TaskCompletionSource<object?>)state!).TrySetResult(null),
            cancellationCompletion);

        if (task != await Task.WhenAny(task, cancellationCompletion.Task).ConfigureAwait(false))
        {
            throw new OperationCanceledException(cancellationToken);
        }

        return await task.ConfigureAwait(false);
    }

    private static async Task ObserveBackgroundTaskAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is OperationCanceledException or EndOfStreamException or IOException or ObjectDisposedException)
        {
            Debug.WriteLine($"Roslyn LSP 后台读取已结束：{exception.Message}");
        }
    }

    private static async Task WaitForProcessExitAsync(Process process, CancellationToken cancellationToken)
    {
        while (!process.HasExited)
        {
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("参数不能为空。", parameterName);
        }
    }

    private void ThrowIfConnectionUnavailable()
    {
        if (Volatile.Read(ref _disposeStarted) != 0)
        {
            throw new ObjectDisposedException(nameof(RoslynLspClient));
        }

        Exception? connectionFailure = Volatile.Read(ref _connectionFailure);
        if (connectionFailure is not null)
        {
            throw new IOException("Roslyn LSP 连接不可用。", connectionFailure);
        }
    }
}
