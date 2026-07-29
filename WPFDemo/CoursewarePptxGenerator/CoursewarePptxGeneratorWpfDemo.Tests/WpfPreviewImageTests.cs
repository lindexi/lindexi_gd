using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PptxGenerator;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class WpfPreviewImageTests
{
    [TestMethod]
    public async Task Save_FromWorkerThread_UsesFrozenSnapshot()
    {
        var bitmapSource = BitmapSource.Create(
            1,
            1,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            new byte[] { 0, 0, 255, 255 },
            4);
        var previewImage = new WpfPreviewImage(bitmapSource);

        var bytes = await Task.Run(() =>
        {
            using var stream = new MemoryStream();
            previewImage.Save(stream);
            return stream.ToArray();
        });

        Assert.IsTrue(previewImage.Source.IsFrozen);
        Assert.IsGreaterThan(8, bytes.Length);
        CollectionAssert.AreEqual(
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 },
            bytes[..8]);
    }
}
