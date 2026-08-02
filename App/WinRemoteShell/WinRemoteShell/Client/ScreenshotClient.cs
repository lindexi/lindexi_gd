namespace WinRemoteShell.Client;

public static class ScreenshotClient
{
    public static async Task<string> CaptureAsync(Uri server, string? output, CancellationToken cancellationToken = default)
    {
        var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var outputPath = string.IsNullOrWhiteSpace(output)
            ? Path.Combine(Environment.CurrentDirectory, fileName)
            : Directory.Exists(output) || Path.EndsInDirectorySeparator(output)
                ? Path.Combine(output, fileName)
                : output;

        using var client = new HttpClient { BaseAddress = server };
        using var response = await client.GetAsync("screenshot", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var file = File.Create(outputPath);
        await content.CopyToAsync(file, cancellationToken);
        return outputPath;
    }
}
