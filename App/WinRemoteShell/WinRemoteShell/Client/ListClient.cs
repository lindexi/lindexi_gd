using System.Net.Http.Json;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Client;

public static class ListClient
{
    public static async Task<DirectoryListingResponse> ListAsync(
        Uri server,
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(server);

        using var client = new HttpClient { BaseAddress = server };
        var requestUri = string.IsNullOrWhiteSpace(path)
            ? "ls"
            : $"ls?path={Uri.EscapeDataString(path)}";
        using var response = await client.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(
            AppJsonSerializerContext.Default.DirectoryListingResponse,
            cancellationToken) ?? throw new InvalidDataException("The server returned an empty directory listing.");
    }
}
