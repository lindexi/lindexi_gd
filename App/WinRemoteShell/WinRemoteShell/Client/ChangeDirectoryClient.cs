using System.Net.Http.Json;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Client;

public static class ChangeDirectoryClient
{
    public static async Task<WorkingDirectoryResponse> ChangeAsync(
        Uri server,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(server);

        using var client = new HttpClient { BaseAddress = server };
        var request = new ChangeDirectoryRequest(path);
        using var content = JsonContent.Create(request, AppJsonSerializerContext.Default.ChangeDirectoryRequest);
        using var response = await client.PostAsync("cd", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(
            AppJsonSerializerContext.Default.WorkingDirectoryResponse,
            cancellationToken) ?? throw new InvalidDataException("The server returned an empty working directory.");
    }
}
