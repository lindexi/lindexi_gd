using System.Net.Http.Json;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Client;

public static class KillClient
{
    public static async Task<KillProcessesResponse> KillAsync(
        Uri server,
        int? processId,
        string? processName,
        bool killTree,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(server);

        using var client = new HttpClient { BaseAddress = server };
        var request = new KillProcessesRequest(processId, processName, killTree);
        using var content = JsonContent.Create(request, AppJsonSerializerContext.Default.KillProcessesRequest);
        using var response = await client.PostAsync("kill", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(
            AppJsonSerializerContext.Default.KillProcessesResponse,
            cancellationToken) ?? throw new InvalidDataException("The server returned an empty kill response.");
    }
}
