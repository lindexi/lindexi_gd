using System.Net.Http.Json;
using WinRemoteShell.Shared;
using WinRemoteShell.Shared.Transmissions;

namespace WinRemoteShell.Client;

public static class UpdateClient
{
    /// <summary>
    /// Updates the remote application from the files in the local application directory.
    /// </summary>
    public static async Task UpdateAsync(
        Uri server,
        bool force,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(output);

        using var client = new HttpClient
        {
            BaseAddress = server,
            Timeout = Timeout.InfiniteTimeSpan
        };
        var remoteVersion = await client.GetFromJsonAsync(
            "update/version",
            AppJsonSerializerContext.Default.UpdateVersionResponse,
            cancellationToken) ?? throw new InvalidDataException("The remote update version response is empty.");

        var localVersion = ApplicationVersion.Current;
        await output.WriteLineAsync($"Local version: {localVersion}".AsMemory(), cancellationToken);
        await output.WriteLineAsync($"Remote version: {remoteVersion.Version}".AsMemory(), cancellationToken);
        await output.FlushAsync(cancellationToken);

        if (!force && ApplicationVersion.Compare(localVersion, remoteVersion.Version) <= 0)
        {
            await output.WriteLineAsync("The remote version is already up to date. Use --force to update anyway.".AsMemory(), cancellationToken);
            await output.FlushAsync(cancellationToken);
            return;
        }

        var source = AppContext.BaseDirectory;
        using var request = new HttpRequestMessage(HttpMethod.Post, "update")
        {
            Content = new TransferContent(TransferManifest.Create(source))
        };
        request.Headers.Add("X-WinRS-Version", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(localVersion)));
        request.Headers.Add("X-WinRS-Force", force.ToString());

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            await output.WriteLineAsync(line.AsMemory(), cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        response.EnsureSuccessStatusCode();
    }

    private sealed class TransferContent(TransferDefinition definition) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context) =>
            TransferStream.WriteAsync(stream, definition, CancellationToken.None);

        protected override Task SerializeToStreamAsync(
            Stream stream,
            System.Net.TransportContext? context,
            CancellationToken cancellationToken) =>
            TransferStream.WriteAsync(stream, definition, cancellationToken);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
