using AgentLib.Model;

using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class TemporaryImageViewerTests
{
    [TestMethod]
    public void WhenSavingPngThenTemporaryFileUsesPngExtension()
    {
        var image = new CopilotChatImageItem(BinaryData.FromBytes([1, 2, 3]), "image/png");

        string filePath = TemporaryImageViewer.SaveToTemporaryFile(image);
        TestContext.WriteLine(filePath);

        Assert.AreEqual(".png", Path.GetExtension(filePath));
    }

    [TestMethod]
    public void WhenSavingImageThenTemporaryFileContainsImageData()
    {
        byte[] expectedData = [1, 2, 3];
        var image = new CopilotChatImageItem(BinaryData.FromBytes(expectedData), "image/png");

        string filePath = TemporaryImageViewer.SaveToTemporaryFile(image);
        TestContext.WriteLine(filePath);

        CollectionAssert.AreEqual(expectedData, File.ReadAllBytes(filePath));
    }

    public TestContext TestContext { get; set; } = null!;
}
