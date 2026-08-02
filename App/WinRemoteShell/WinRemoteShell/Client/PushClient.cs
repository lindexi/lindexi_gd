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

        string? archivePath = null;
        try
        {
            if ((File.GetAttributes(source) & FileAttributes.Directory) != 0)
            {
                request.Headers.Add("X-WinRS-Type", "directory");
                archivePath = Path.Join(Path.GetTempPath(), $"WinRemoteShell_Push_{Guid.NewGuid():N}.zip");
                await CreateDirectoryArchiveAsync(source, archivePath, cancellationToken);
                request.Content = new StreamContent(File.OpenRead(archivePath));
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
        finally
        {
            if (archivePath is not null)
            {
                File.Delete(archivePath);
            }
        }
    }

    private static async Task CreateDirectoryArchiveAsync(
        string source,
        string archivePath,
        CancellationToken cancellationToken)
    {
        await using var archiveStream = new FileStream(
            archivePath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create);

        foreach (var directoryPath in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            archive.CreateEntry(Path.GetRelativePath(source, directoryPath).Replace('\\', '/') + "/");
        }

        foreach (var filePath in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = archive.CreateEntry(
                Path.GetRelativePath(source, filePath).Replace('\\', '/'),
                CompressionLevel.Fastest);
            await using var entryStream = entry.Open();
            await using var file = File.OpenRead(filePath);
            await file.CopyToAsync(entryStream, cancellationToken);
        }
    }
}
