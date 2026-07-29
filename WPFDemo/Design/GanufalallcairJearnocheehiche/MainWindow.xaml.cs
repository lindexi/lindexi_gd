using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GanufalallcairJearnocheehiche;

public partial class MainWindow : Window
{
    private const int ArtworkSize = 512;
    private static readonly int[] PngSizes = [1024, 512, 256, 128, 64, 48, 32, 16];
    private static readonly int[] IcoSizes = [256, 128, 64, 48, 32, 16];
    private readonly string _outputDirectory = AppContext.BaseDirectory;

    public MainWindow()
    {
        InitializeComponent();
        OutputPathTextBlock.Text = _outputDirectory;
    }

    private void ExportPngButton_Click(object sender, RoutedEventArgs e)
    {
        RunExport(
            () => ExportPngFiles(_outputDirectory),
            (string)FindResource("PngExportCompleted"));
    }

    private void ExportIcoButton_Click(object sender, RoutedEventArgs e)
    {
        RunExport(
            () => ExportIcoFile(_outputDirectory),
            (string)FindResource("IcoExportCompleted"));
    }

    private void ExportAllButton_Click(object sender, RoutedEventArgs e)
    {
        RunExport(
            () =>
            {
                ExportPngFiles(_outputDirectory);
                ExportIcoFile(_outputDirectory);
            },
            (string)FindResource("AllExportCompleted"));
    }

    private void RunExport(Action exportAction, string successMessage)
    {
        ArgumentNullException.ThrowIfNull(exportAction);

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
            File.WriteAllBytes(Path.Combine(directory, $"CodeConversationIcon-{size}x{size}.png"), png);
        }
    }

    private void ExportIcoFile(string directory)
    {
        byte[][] images = IcoSizes.Select(RenderPng).ToArray();
        string iconPath = Path.Combine(directory, "CodeConversationIcon.ico");

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

        IconArtwork.Measure(new Size(ArtworkSize, ArtworkSize));
        IconArtwork.Arrange(new Rect(0, 0, ArtworkSize, ArtworkSize));
        IconArtwork.UpdateLayout();

        RenderTargetBitmap bitmap = new(size, size, 96, 96, PixelFormats.Pbgra32);
        DrawingVisual visual = new();

        using (DrawingContext drawingContext = visual.RenderOpen())
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, size, size));
            drawingContext.PushTransform(new ScaleTransform(size / (double)ArtworkSize, size / (double)ArtworkSize));
            drawingContext.DrawRectangle(new VisualBrush(IconArtwork), null, new Rect(0, 0, ArtworkSize, ArtworkSize));
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
        string title = (string)FindResource("ExportFailed");
        StatusTextBlock.Text = $"{title}：{details}";
        MessageBox.Show(this, details, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
