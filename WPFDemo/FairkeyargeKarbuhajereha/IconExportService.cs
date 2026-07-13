using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FairkeyargeKarbuhajereha;

internal sealed class IconExportService
{
    private static readonly int[] IconSizes = [16, 24, 32, 256];

    public async Task<string> ExportAsync(DrawingImage drawingImage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(drawingImage);

        var outputDirectory = Path.Join(AppContext.BaseDirectory, "Icons");
        Directory.CreateDirectory(outputDirectory);

        var pngFrames = new List<byte[]>(IconSizes.Length);
        foreach (var size in IconSizes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pngBytes = RenderPng(drawingImage.Drawing, size);
            pngFrames.Add(pngBytes);

            var pngPath = Path.Join(outputDirectory, $"Png{size}x{size}IconFile.png");
            await File.WriteAllBytesAsync(pngPath, pngBytes, cancellationToken).ConfigureAwait(false);
        }

        var iconPath = Path.Join(outputDirectory, "AppIcon.ico");
        await WriteIconAsync(iconPath, pngFrames, cancellationToken).ConfigureAwait(false);

        return outputDirectory;
    }

    private static byte[] RenderPng(Drawing drawing, int size)
    {
        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            var bounds = drawing.Bounds;
            var scale = Math.Min(size / bounds.Width, size / bounds.Height);
            var offsetX = ((size - (bounds.Width * scale)) / 2) - (bounds.X * scale);
            var offsetY = ((size - (bounds.Height * scale)) / 2) - (bounds.Y * scale);

            drawingContext.PushTransform(new TranslateTransform(offsetX, offsetY));
            drawingContext.PushTransform(new ScaleTransform(scale, scale));
            drawingContext.DrawDrawing(drawing);
            drawingContext.Pop();
            drawingContext.Pop();
        }

        var renderTarget = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(drawingVisual);
        renderTarget.Freeze();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static async Task WriteIconAsync(
        string iconPath,
        IReadOnlyList<byte[]> pngFrames,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            iconPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)pngFrames.Count);

        var imageOffset = 6 + (16 * pngFrames.Count);
        for (var index = 0; index < pngFrames.Count; index++)
        {
            var size = IconSizes[index];
            writer.Write((byte)(size == 256 ? 0 : size));
            writer.Write((byte)(size == 256 ? 0 : size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)pngFrames[index].Length);
            writer.Write((uint)imageOffset);
            imageOffset += pngFrames[index].Length;
        }

        writer.Flush();
        foreach (var pngFrame in pngFrames)
        {
            await stream.WriteAsync(pngFrame, cancellationToken).ConfigureAwait(false);
        }
    }
}
