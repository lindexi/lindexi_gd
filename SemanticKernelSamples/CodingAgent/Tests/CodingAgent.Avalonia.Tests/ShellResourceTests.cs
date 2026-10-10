using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ShellResourceTests
{
    [TestMethod]
    public void ShellAvatarShouldLoadFromPackagedResources()
    {
        var uri = new Uri("avares://CodingChatRoom.AvaloniaShell/Assets/Icons/CodingChatRoom_48x48.png");

        using var stream = AssetLoader.Open(uri);
        using var bitmap = new Bitmap(stream);

        Assert.AreEqual(new PixelSize(48, 48), bitmap.PixelSize);
    }

    [TestMethod]
    public void MainWindowShouldConstructWithItsResources()
    {
        var window = new MainWindow
        {
            DataContext = new MainViewModel(),
        };

        try
        {
            Assert.IsNotNull(window.Content);
        }
        finally
        {
            window.Close();
        }
    }
}
