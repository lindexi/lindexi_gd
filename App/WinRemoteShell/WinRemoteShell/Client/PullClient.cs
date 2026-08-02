using System.Text;
using WinRemoteShell.Shared;
using WinRemoteShell.Shared.Transmissions;

namespace WinRemoteShell.Client;

public static class PullClient
{
    public static async Task PullAsync(Uri server, string source, string output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(server);
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("The source path is required.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new ArgumentException("The output path is required.", nameof(output));
        }

        using var client = new HttpClient { BaseAddress = server };
        var encodedSource = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(source)));
        using var response = await client.GetAsync(
            $"pull?source={encodedSource}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await TransferStream.ReceiveAsync(
            responseStream,
            output,
            placeFileInExistingDirectory: true,
            cancellationToken);
    }
}
