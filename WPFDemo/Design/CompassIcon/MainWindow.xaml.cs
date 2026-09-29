using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompassIcon;

public partial class MainWindow : Window
{
    private static readonly int[] PngSizes = [16, 24, 32, 48, 64, 128, 256, 512, 1024];
    private static readonly int[] IcoSizes = [16, 24, 32, 48, 64, 128, 256];
    private readonly string _outputDirectory = AppContext.BaseDirectory;
    private bool _hasExportedOnStartup;

    /// <summary>
    /// 初始化矢量预览及输出目录。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        OutputPathTextBox.Text = _outputDirectory;
    }

    private void Window_ContentRendered(object? sender, EventArgs e)
    {
        if (_hasExportedOnStartup)
        {
            return;
        }

        _hasExportedOnStartup = true;
        ExportAll();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        ExportAll();
    }

    private void ExportAll()
    {
        ExportButton.IsEnabled = false;
        try
        {
            DrawingImage artwork = (DrawingImage)FindResource("CompassArtwork");
            Dictionary<int, byte[]> images = [];
            foreach (int size in PngSizes)
            {
                byte[] png = RenderPng(artwork, size);
                images.Add(size, png);
                File.WriteAllBytes(Path.Combine(_outputDirectory, $"CompassIcon-{size}x{size}.png"), png);
            }

            ExportIco(images);
            StatusTextBlock.Foreground = Brushes.DarkGreen;
            StatusTextBlock.Text = (string)FindResource("ExportCompleted");
        }
        catch (IOException exception)
        {
            ShowExportError(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            ShowExportError(exception.Message);
        }
        finally
        {
            ExportButton.IsEnabled = true;
        }
    }

    private static byte[] RenderPng(DrawingImage artwork, int size)
    {
        DrawingVisual visual = new();
        using (DrawingContext context = visual.RenderOpen())
        {
            context.DrawImage(artwork, new Rect(0, 0, size, size));
        }

        RenderTargetBitmap bitmap = new(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using MemoryStream stream = new();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private void ExportIco(IReadOnlyDictionary<int, byte[]> images)
    {
        using FileStream stream = File.Create(Path.Combine(_outputDirectory, "CompassIcon.ico"));
        using BinaryWriter writer = new(stream);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)IcoSizes.Length);

        int offset = 6 + 16 * IcoSizes.Length;
        foreach (int size in IcoSizes)
        {
            byte[] image = images[size];
            // ICO 目录以 0 表示 256 像素，帧数据使用支持透明通道的 PNG。
            writer.Write((byte)(size == 256 ? 0 : size));
            writer.Write((byte)(size == 256 ? 0 : size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)image.Length);
            writer.Write((uint)offset);
            offset += image.Length;
        }

        foreach (int size in IcoSizes)
        {
            writer.Write(images[size]);
        }
    }

    private void ShowExportError(string message)
    {
        StatusTextBlock.Foreground = Brushes.Firebrick;
        StatusTextBlock.Text = $"{FindResource("ExportFailed")}{message}";
    }
}
