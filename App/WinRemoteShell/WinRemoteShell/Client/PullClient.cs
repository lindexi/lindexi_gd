using System.IO.Compression;
using System.Text;

namespace WinRemoteShell.Client;

public static class PullClient
{
    public static async Task PullAsync(Uri server, string source, string output, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient { BaseAddress = server };
        var encodedSource = Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(source)));
        using var response = await client.GetAsync($"pull?source={encodedSource}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        if (response.Headers.GetValues("X-WinRS-Type").Single() == "directory")
        {
            Directory.CreateDirectory(output);
            using var archive = new ZipArchive(responseStream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(output, true);
            return;
        }

        var fileName = Encoding.UTF8.GetString(Convert.FromBase64String(response.Headers.GetValues("X-WinRS-FileName").Single()));
        var outputPath = Directory.Exists(output) ? Path.Combine(output, fileName) : output;
        await using var file = File.Create(outputPath);
        await responseStream.CopyToAsync(file, cancellationToken);
    }
}
