using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace ImageViewer.Services;

internal sealed class ImageFilePreparationService
{
    private static readonly HashSet<string> AvaloniaSupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".gif"
    };

    public async Task<PreparedImageFile> PrepareAsync(string filePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (AvaloniaSupportedExtensions.Contains(Path.GetExtension(filePath)))
        {
            return new PreparedImageFile(filePath);
        }

        using var image = await Image.LoadAsync(filePath, cancellationToken).ConfigureAwait(false);
        var temporaryFilePath = Path.Join(Path.GetTempPath(), $"ImageViewer-{Guid.NewGuid():N}.png");

        try
        {
            await image.SaveAsPngAsync(temporaryFilePath, cancellationToken).ConfigureAwait(false);
            return new PreparedImageFile(temporaryFilePath, deleteOnDispose: true);
        }
        catch
        {
            File.Delete(temporaryFilePath);
            throw;
        }
    }
}

internal sealed class PreparedImageFile : IDisposable
{
    private readonly bool _deleteOnDispose;

    public PreparedImageFile(string filePath, bool deleteOnDispose = false)
    {
        FilePath = filePath;
        _deleteOnDispose = deleteOnDispose;
    }

    public string FilePath { get; }

    public void Dispose()
    {
        if (_deleteOnDispose)
        {
            File.Delete(FilePath);
        }
    }
}
