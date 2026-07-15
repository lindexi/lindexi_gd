using ImageViewer.Services;

namespace ImageViewer.Tests.Services;

public sealed class ImageDirectoryServiceTests
{
    [Fact]
    public void GetImagesInSameDirectoryReturnsOnlySupportedImagesInNaturalOrder()
    {
        using var tempDirectory = TempDirectory.Create();
        var directory = tempDirectory.Path;
        var img10 = CreateFile(directory, "img10.png");
        var img1 = CreateFile(directory, "img1.jpg");
        var txt = CreateFile(directory, "img2.txt");
        var img2 = CreateFile(directory, "img2.PNG");
        var service = new ImageDirectoryService();

        var images = service.GetImagesInSameDirectory(img10);

        Assert.Equal([img1, img2, img10], images);
        Assert.DoesNotContain(txt, images);
    }

    [Fact]
    public void GetImagesInSameDirectoryReturnsEmptyWhenDirectoryHasNoImages()
    {
        using var tempDirectory = TempDirectory.Create();
        var directory = tempDirectory.Path;
        var textFile = CreateFile(directory, "readme.txt");
        var service = new ImageDirectoryService();

        var images = service.GetImagesInSameDirectory(textFile);

        Assert.Empty(images);
    }

    [Fact]
    public void GetImagesInSameDirectoryReturnsEmptyForMissingUnsupportedFile()
    {
        using var tempDirectory = TempDirectory.Create();
        var missingFile = Path.Combine(tempDirectory.Path, "missing.txt");
        var service = new ImageDirectoryService();

        var images = service.GetImagesInSameDirectory(missingFile);

        Assert.Empty(images);
    }

    [Fact]
    public void GetImagesInSameDirectoryReturnsEmptyWhenDirectoryDoesNotExist()
    {
        var missingDirectoryPath = Path.Combine(Path.GetTempPath(), "ImageViewerTests", Guid.NewGuid().ToString("N"), "child.png");
        var service = new ImageDirectoryService();

        var images = service.GetImagesInSameDirectory(missingDirectoryPath);

        Assert.Empty(images);
    }

    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.JPEG", true)]
    [InlineData("photo.webp", true)]
    [InlineData("photo.svg", false)]
    [InlineData("photo", false)]
    public void IsSupportedImagePathReturnsExpectedResult(string fileName, bool expected)
    {
        var service = new ImageDirectoryService();

        var isSupported = service.IsSupportedImagePath(fileName);

        Assert.Equal(expected, isSupported);
    }

    private static string CreateFile(string directory, string fileName)
    {
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, fileName);
        File.WriteAllBytes(filePath, []);
        return filePath;
    }

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempDirectory Create()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ImageViewerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return new TempDirectory(directory);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
