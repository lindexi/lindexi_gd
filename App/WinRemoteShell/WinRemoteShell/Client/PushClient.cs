using System.Net;
using WinRemoteShell.Shared;
using WinRemoteShell.Shared.Transmissions;

namespace WinRemoteShell.Client;

public static class PushClient
{
    public static Task PushAsync(
        Uri server,
        string source,
        string target,
        CancellationToken cancellationToken = default) =>
        PushAsync(server, source, target, PushMode.Merge, cancellationToken);

    public static async Task PushAsync(
        Uri server,
        string source,
        string target,
        PushMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(server);
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("The source path is required.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("The target path is required.", nameof(target));
        }

        using var content = new TransferContent(TransferManifest.Create(source));
        using var client = new HttpClient { BaseAddress = server };
        using var request = new HttpRequestMessage(HttpMethod.Post, "push")
        {
            Content = content
        };
        request.Headers.Add("X-WinRS-Target", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(target)));
        request.Headers.Add("X-WinRS-Push-Mode", mode.ToString());

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
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
