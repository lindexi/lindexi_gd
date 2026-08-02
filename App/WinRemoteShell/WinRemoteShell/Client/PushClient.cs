using System.IO.Compression;
using System.Text;

namespace WinRemoteShell.Client;

public static class PushClient
{
    public static async Task PushAsync(Uri server, string source, string target, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient { BaseAddress = server };
        using var request = new HttpRequestMessage(HttpMethod.Post, "push");
        request.Headers.Add("X-WinRS-Target", Convert.ToBase64String(Encoding.UTF8.GetBytes(target)));

        if ((File.GetAttributes(source) & FileAttributes.Directory) != 0)
        {
            request.Headers.Add("X-WinRS-Type", "directory");
            var archiveStream = new MemoryStream();
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
            {
                foreach (var directoryPath in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
                {
                    archive.CreateEntry(Path.GetRelativePath(source, directoryPath).Replace('\\', '/') + "/");
                }

                foreach (var filePath in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
                {
                    var entry = archive.CreateEntry(Path.GetRelativePath(source, filePath), CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    await using var file = File.OpenRead(filePath);
                    await file.CopyToAsync(entryStream, cancellationToken);
                }
            }

            archiveStream.Position = 0;
            request.Content = new StreamContent(archiveStream);
        }
        else
        {
            request.Headers.Add("X-WinRS-Type", "file");
            request.Content = new StreamContent(File.OpenRead(source));
        }

        using (request.Content)
        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
        }
    }
}
