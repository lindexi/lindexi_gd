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
        using var client = new HttpClient { BaseAddress = server };
        var request = new ExecRequest(arguments, timeoutSeconds, workingDirectory);
        using var content = JsonContent.Create(request, AppJsonSerializerContext.Default.ExecRequest);
        using var message = new HttpRequestMessage(HttpMethod.Post, "exec") { Content = content };
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            await output.WriteLineAsync(line.AsMemory(), cancellationToken);
            await output.FlushAsync(cancellationToken);
        }
    }
}
