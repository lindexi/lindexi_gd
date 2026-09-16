using System.Net;
using WinRemoteShell.Shared;
using WinRemoteShell.Shared.Transmissions;

namespace WinRemoteShell.Client;

public static class PushClient
{
    /// <summary>
    /// Pushes a local file or directory to the remote server.
    /// </summary>
    public static async Task PushAsync(
        Uri server,
        string source,
        string target,
        PushMode mode = PushMode.Merge,
        TextWriter? output = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(server);
        output ??= TextWriter.Null;
        ArgumentNullException.ThrowIfNull(source);
        var deleteTarget = source.Length == 0;
        if (!deleteTarget && string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("The source path is required.", nameof(source));
        }

        if (deleteTarget && mode != PushMode.Replace)
        {
            throw new ArgumentException("An empty source path can only be used with Replace mode.", nameof(mode));
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("The target path is required.", nameof(target));
        }

        using var client = new HttpClient { BaseAddress = server };
        using var request = new HttpRequestMessage(HttpMethod.Post, "push");
        if (deleteTarget)
        {
            request.Headers.Add("X-WinRS-Delete-Target", "true");
        }
        else
        {
            request.Content = new TransferContent(TransferManifest.Create(source));
        }

        request.Headers.Add("X-WinRS-Target", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(target)));
        request.Headers.Add("X-WinRS-Push-Mode", mode.ToString());

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream);
        var responseText = new System.Text.StringBuilder();
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            responseText.AppendLine(line);
            await output.WriteLineAsync(line.AsMemory(), cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var serverError = responseText.ToString().TrimEnd();
            var message = string.IsNullOrWhiteSpace(serverError)
                ? $"The remote server returned {(int) response.StatusCode} ({response.ReasonPhrase}) without error details. The server may be an older WinRemoteShell version."
                : $"The remote server returned {(int) response.StatusCode} ({response.ReasonPhrase}):{Environment.NewLine}{serverError}";
            throw new HttpRequestException(message, null, response.StatusCode);
        }
    }

    private sealed class TransferContent(TransferDefinition definition) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            TransferStream.WriteAsync(stream, definition, CancellationToken.None);

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context,
            CancellationToken cancellationToken) =>
            TransferStream.WriteAsync(stream, definition, cancellationToken);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
