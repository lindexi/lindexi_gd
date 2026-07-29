using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QeechegayyalbeaheJerhernawwulear;

public partial class MainWindow : Window
{
    private static readonly int[] PngSizes = [512, 64, 32];
    private static readonly int[] IcoSizes = [256, 64, 32];
    private readonly string _outputDirectory = AppContext.BaseDirectory;

    public MainWindow()
    {
        InitializeComponent();
        OutputPathTextBlock.Text = _outputDirectory;
    }

    private void ExportPngButton_Click(object sender, RoutedEventArgs e)
    {
        RunExport(() => ExportPngFiles(_outputDirectory), "PNG 图片导出完成");
    }

    private void ExportIcoButton_Click(object sender, RoutedEventArgs e)
    {
        RunExport(() => ExportIcoFile(_outputDirectory), "ICO 图标导出完成");
    }

    private void ExportAllButton_Click(object sender, RoutedEventArgs e)
    {
        RunExport(
            () =>
            {
                ExportPngFiles(_outputDirectory);
                ExportIcoFile(_outputDirectory);
            },
            "全部图片导出完成");
    }

    private void RunExport(Action exportAction, string successMessage)
    {
        SetButtonsEnabled(false);

        try
        {
            Directory.CreateDirectory(_outputDirectory);
            exportAction();
            StatusTextBlock.Text = $"{successMessage}：{_outputDirectory}";
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
            SetButtonsEnabled(true);
        }
    }

    private void ExportPngFiles(string directory)
    {
        foreach (int size in PngSizes)
        {
            byte[] png = RenderPng(size);
            File.WriteAllBytes(Path.Combine(directory, $"WpfCaptureIcon-{size}x{size}.png"), png);
        }
    }

    private void ExportIcoFile(string directory)
    {
        byte[][] images = IcoSizes.Select(RenderPng).ToArray();
        string iconPath = Path.Combine(directory, "WpfCaptureIcon.ico");

        using FileStream stream = File.Create(iconPath);
        using BinaryWriter writer = new(stream);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)images.Length);

        int imageOffset = 6 + (16 * images.Length);
        for (int index = 0; index < images.Length; index++)
        {
            int size = IcoSizes[index];
            byte[] image = images[index];

            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)image.Length);
            writer.Write((uint)imageOffset);

            imageOffset += image.Length;
        }

        foreach (byte[] image in images)
        {
            writer.Write(image);
        }
    }

    private byte[] RenderPng(int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        IconArtwork.Measure(new Size(512, 512));
        IconArtwork.Arrange(new Rect(0, 0, 512, 512));
        IconArtwork.UpdateLayout();

        RenderTargetBitmap bitmap = new(size, size, 96, 96, PixelFormats.Pbgra32);
        DrawingVisual visual = new();

        using (DrawingContext drawingContext = visual.RenderOpen())
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, size, size));
            drawingContext.PushTransform(new ScaleTransform(size / 512d, size / 512d));
            drawingContext.DrawRectangle(new VisualBrush(IconArtwork), null, new Rect(0, 0, 512, 512));
            drawingContext.Pop();
        }

        bitmap.Render(visual);
        bitmap.Freeze();

        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using MemoryStream stream = new();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private void SetButtonsEnabled(bool isEnabled)
    {
        ExportPngButton.IsEnabled = isEnabled;
        ExportIcoButton.IsEnabled = isEnabled;
        ExportAllButton.IsEnabled = isEnabled;
    }

    private void ShowExportError(string details)
    {
        StatusTextBlock.Text = $"导出失败：{details}";
        MessageBox.Show(this, details, "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
