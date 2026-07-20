using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HuracaijelkairWeqabefakaja;

internal sealed class IconExportService
{
    private const string FileNamePrefix = "OrbitIcon";

    private static readonly int[] PngSizes = [512, 64, 32, 16];
    private static readonly int[] IcoSizes = [16, 32, 64, 256];

    internal static string DefaultOutputDirectory => Path.Join(AppContext.BaseDirectory, "GeneratedIcons");

    internal async Task ExportAsync(
        Drawing drawing,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(drawing);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        var files = new List<EncodedFile>(PngSizes.Length + 1);
        var icoFrames = new List<IconFrame>(IcoSizes.Length);

        foreach (int size in PngSizes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] pngBytes = EncodePng(drawing, size);
            files.Add(new EncodedFile($"{FileNamePrefix}-{size}x{size}.png", pngBytes));
        }

        foreach (int size in IcoSizes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] pngBytes = EncodePng(drawing, size);
            icoFrames.Add(new IconFrame(size, pngBytes));
        }

        files.Add(new EncodedFile($"{FileNamePrefix}.ico", EncodeIco(icoFrames)));
        Directory.CreateDirectory(outputDirectory);

        foreach (EncodedFile file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string filePath = Path.Join(outputDirectory, file.FileName);
            await WriteAtomicallyAsync(filePath, file.Content, cancellationToken).ConfigureAwait(false);
        }
    }

    private static byte[] EncodePng(Drawing drawing, int pixelSize)
    {
        var renderTarget = new RenderTargetBitmap(
            pixelSize,
            pixelSize,
            96,
            96,
            PixelFormats.Pbgra32);

        var drawingVisual = new DrawingVisual();
        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        {
            double scale = pixelSize / (double)OrbitArtworkRenderer.DesignSize;
            drawingContext.PushTransform(new ScaleTransform(scale, scale));
            drawingContext.DrawDrawing(drawing);
            drawingContext.Pop();
        }

        renderTarget.Render(drawingVisual);
        renderTarget.Freeze();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static byte[] EncodeIco(IReadOnlyList<IconFrame> frames)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write(checked((ushort)frames.Count));

        uint imageOffset = checked((uint)(6 + frames.Count * 16));
        foreach (IconFrame frame in frames)
        {
            writer.Write(frame.Size == 256 ? (byte)0 : checked((byte)frame.Size));
            writer.Write(frame.Size == 256 ? (byte)0 : checked((byte)frame.Size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(checked((uint)frame.PngBytes.Length));
            writer.Write(imageOffset);
            imageOffset = checked(imageOffset + (uint)frame.PngBytes.Length);
        }

        foreach (IconFrame frame in frames)
        {
            writer.Write(frame.PngBytes);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static async Task WriteAtomicallyAsync(
        string filePath,
        byte[] content,
        CancellationToken cancellationToken)
    {
        string temporaryFilePath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await File.WriteAllBytesAsync(temporaryFilePath, content, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryFilePath, filePath, true);
        }
        finally
        {
            if (File.Exists(temporaryFilePath))
            {
                File.Delete(temporaryFilePath);
            }
        }
    }

    private sealed record EncodedFile(string FileName, byte[] Content);

    private sealed record IconFrame(int Size, byte[] PngBytes);
}
