using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodingwhejeedenochaDeelemjuher;

public sealed class RoslynLspClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Process _process;
    private readonly Stream _input;
    private readonly Stream _output;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonNode?>> _pendingRequests = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _readerTask;
    private long _nextRequestId;

    private RoslynLspClient(Process process)
    {
        _process = process;
        _input = process.StandardInput.BaseStream;
        _output = process.StandardOutput.BaseStream;
        _readerTask = ReadMessagesAsync(_shutdown.Token);
        _ = CopyErrorsAsync(process.StandardError, _shutdown.Token);
    }

    public static async Task<RoslynLspClient> StartAsync(
        string workspacePath,
        string command = "roslyn-language-server",
        CancellationToken cancellationToken = default)
    {
        workspacePath = Path.GetFullPath(workspacePath);
        if (!Directory.Exists(workspacePath))
        {
            throw new DirectoryNotFoundException($"工作区不存在：{workspacePath}");
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
                    processId = Environment.ProcessId,
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
                cancellationToken);

            if (initializeResult is null)
            {
                throw new InvalidOperationException("Roslyn LSP initialize 返回了空结果。");
            }

            await client.NotifyAsync("initialized", new { }, cancellationToken);
            return client;
        }
        catch
        {
            await client.DisposeAsync();
            throw;
        }
    }

    public async Task<JsonNode?> RequestAsync(
        string method,
        object? parameters,
        CancellationToken cancellationToken = default)
    {
        long id = Interlocked.Increment(ref _nextRequestId);
        TaskCompletionSource<JsonNode?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(id, completion))
        {
            throw new InvalidOperationException($"重复的 LSP 请求 ID：{id}");
        }

        try
        {
            await WriteMessageAsync(new { jsonrpc = "2.0", id, method, @params = parameters }, cancellationToken);
            return await completion.Task.WaitAsync(cancellationToken);
        }
        finally
        {
            _pendingRequests.TryRemove(id, out _);
        }
    }

    public Task NotifyAsync(string method, object? parameters, CancellationToken cancellationToken = default) =>
        WriteMessageAsync(new { jsonrpc = "2.0", method, @params = parameters }, cancellationToken);

    public async Task OpenDocumentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        filePath = Path.GetFullPath(filePath);
        string text = await File.ReadAllTextAsync(filePath, cancellationToken);
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
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            try
            {
                await RequestAsync("shutdown", null).WaitAsync(TimeSpan.FromSeconds(5));
                await NotifyAsync("exit", null);
            }
            catch
            {
            }
        }

        _shutdown.Cancel();
        _input.Dispose();
        _output.Dispose();

        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
        }

        try
        {
            await _readerTask;
        }
        catch (OperationCanceledException)
        {
        }

        _process.Dispose();
        _shutdown.Dispose();
        _writeLock.Dispose();
    }

    private async Task WriteMessageAsync(object message, CancellationToken cancellationToken)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        byte[] header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _input.WriteAsync(header, cancellationToken);
            await _input.WriteAsync(body, cancellationToken);
            await _input.FlushAsync(cancellationToken);
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
                int contentLength = await ReadContentLengthAsync(cancellationToken);
                byte[] body = new byte[contentLength];
                await _output.ReadExactlyAsync(body, cancellationToken);
                JsonNode? message = JsonNode.Parse(body);
                HandleMessage(message);
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or EndOfStreamException or IOException)
        {
            foreach (TaskCompletionSource<JsonNode?> request in _pendingRequests.Values)
            {
                request.TrySetException(new IOException("Roslyn LSP 连接已关闭。", exception));
            }
        }
    }

    private async Task<int> ReadContentLengthAsync(CancellationToken cancellationToken)
    {
        int? contentLength = null;
        while (true)
        {
            string line = await ReadAsciiLineAsync(cancellationToken);
            if (line.Length == 0)
            {
                return contentLength ?? throw new InvalidDataException("LSP 消息缺少 Content-Length。");
            }

            const string prefix = "Content-Length:";
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(line[prefix.Length..].Trim(), out int parsedLength))
            {
                contentLength = parsedLength;
            }
        }
    }

    private async Task<string> ReadAsciiLineAsync(CancellationToken cancellationToken)
    {
        List<byte> bytes = [];
        while (true)
        {
            byte[] buffer = new byte[1];
            int count = await _output.ReadAsync(buffer, cancellationToken);
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

                return Encoding.ASCII.GetString([.. bytes]);
            }

            bytes.Add(buffer[0]);
        }
    }

    private void HandleMessage(JsonNode? message)
    {
        if (message?["id"] is not JsonValue idNode ||
            !idNode.TryGetValue<long>(out long id) ||
            !_pendingRequests.TryGetValue(id, out TaskCompletionSource<JsonNode?>? completion))
        {
            return;
        }

        if (message["error"] is JsonNode error)
        {
            completion.TrySetException(new InvalidOperationException($"LSP 请求失败：{error.ToJsonString()}"));
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

    private static async Task CopyErrorsAsync(StreamReader errorReader, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await errorReader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                return;
            }

            Console.Error.WriteLine($"[roslyn-language-server] {line}");
        }
    }
}
