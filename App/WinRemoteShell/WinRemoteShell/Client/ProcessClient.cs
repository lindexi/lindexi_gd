using System.Net.Http.Json;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Client;

public static class ProcessClient
{
    public static async Task<ProcessListResponse> ListAsync(Uri server, bool includeDetails, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(server);

        using var client = new HttpClient { BaseAddress = server };
        using var response = await client.GetAsync($"ps?details={includeDetails.ToString().ToLowerInvariant()}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(
            AppJsonSerializerContext.Default.ProcessListResponse,
            cancellationToken) ?? throw new InvalidDataException("The server returned an empty process list.");
    }
}
