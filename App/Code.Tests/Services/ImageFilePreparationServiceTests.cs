using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageViewer.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ImageViewer.Tests.Services;

public sealed class ImageFilePreparationServiceTests
{
    private readonly ImageFilePreparationService _service = new();

    [Fact(Timeout = 10_000)]
    public async Task WhenAvaloniaSupportsImageThenOriginalFileIsUsed()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"ImageViewerTests-{Guid.NewGuid():N}.png");

        using var preparedImage = await _service.PrepareAsync(filePath, CancellationToken.None);

        Assert.Equal(filePath, preparedImage.FilePath);
    }

    [Fact(Timeout = 10_000)]
    public async Task WhenImageRequiresConversionThenTemporaryPngIsCreated()
    {
        var sourceFilePath = Path.Combine(Path.GetTempPath(), $"ImageViewerTests-{Guid.NewGuid():N}.webp");
        using (var image = new Image<Rgba32>(2, 3))
        {
            image.SaveAsWebp(sourceFilePath);
        }

        string temporaryFilePath;
        try
        {
            using (var preparedImage = await _service.PrepareAsync(sourceFilePath, CancellationToken.None))
            {
                temporaryFilePath = preparedImage.FilePath;
                Assert.EndsWith(".png", temporaryFilePath, StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            File.Delete(sourceFilePath);
        }
    }

    [Fact(Timeout = 10_000)]
    public async Task WhenConvertedImageIsDisposedThenTemporaryPngIsDeleted()
    {
        var sourceFilePath = Path.Combine(Path.GetTempPath(), $"ImageViewerTests-{Guid.NewGuid():N}.webp");
        using (var image = new Image<Rgba32>(2, 3))
        {
            image.SaveAsWebp(sourceFilePath);
        }

        string temporaryFilePath;
        try
        {
            using (var preparedImage = await _service.PrepareAsync(sourceFilePath, CancellationToken.None))
            {
                temporaryFilePath = preparedImage.FilePath;
            }

            Assert.False(File.Exists(temporaryFilePath));
        }
        finally
        {
            File.Delete(sourceFilePath);
        }
    }
}
