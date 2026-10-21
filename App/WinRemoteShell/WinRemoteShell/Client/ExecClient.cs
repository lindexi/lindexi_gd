using System.Net.Http.Json;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Client;

public static class ExecClient
{
    public static Task ExecuteAsync(
        Uri server,
        IReadOnlyList<string> arguments,
        int? timeoutSeconds,
        TextWriter output,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(server, arguments, timeoutSeconds, output, null, cancellationToken);

    public static async Task ExecuteAsync(
        Uri server,
        IReadOnlyList<string> arguments,
        int? timeoutSeconds,
        TextWriter output,
        string? workingDirectory,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient
        {
            BaseAddress = server,
            Timeout = Timeout.InfiniteTimeSpan
        };
        var request = new ExecRequest(arguments, timeoutSeconds, workingDirectory);
        using var content = JsonContent.Create(request, AppJsonSerializerContext.Default.ExecRequest);
        using var message = new HttpRequestMessage(HttpMethod.Post, "exec") { Content = content };

        try
        {
            using var response = await client.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            await response.EnsureSuccessWithDetailsAsync(cancellationToken);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                await output.WriteLineAsync(line.AsMemory(), cancellationToken);
                await output.FlushAsync(cancellationToken);
            }
        }
        catch (HttpRequestException exception)
        {
            var executable = arguments.Count > 0 ? arguments[0] : "<missing>";
            throw new HttpRequestException(
                $"Failed to execute '{executable}' on remote server '{server}': {exception.Message}",
                exception,
                exception.StatusCode);
        }
    }
}
