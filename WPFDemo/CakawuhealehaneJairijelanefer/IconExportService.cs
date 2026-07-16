using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CakawuhealehaneJairijelanefer;

internal static class IconExportService
{
    private const string FileNamePrefix = "StackedCube";
    private static readonly int[] PngSizes = [16, 32, 64, 128, 256, 512];

    internal static IconExportResult Export(string outputDirectory, Drawing drawing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(drawing);

        Directory.CreateDirectory(outputDirectory);

        List<string> pngPaths = new(PngSizes.Length);
        List<IconImage> icoImages = new(PngSizes.Length - 1);

        foreach (int size in PngSizes)
        {
            byte[] pngData = RenderPng(drawing, size);
            string pngPath = Path.Join(outputDirectory, $"{FileNamePrefix}_{size}x{size}.png");
            File.WriteAllBytes(pngPath, pngData);
            pngPaths.Add(pngPath);

            if (size <= 256)
            {
                icoImages.Add(new IconImage(size, pngData));
            }
        }

        string icoPath = Path.Join(outputDirectory, $"{FileNamePrefix}.ico");
        WriteIcon(icoPath, icoImages);

        return new IconExportResult(outputDirectory, pngPaths, icoPath);
    }

    private static byte[] RenderPng(Drawing drawing, int size)
    {
        DrawingVisual visual = new();
        using (DrawingContext drawingContext = visual.RenderOpen())
        {
            double scale = size / IconDrawingFactory.LogicalSize;
            drawingContext.PushTransform(new ScaleTransform(scale, scale));
            drawingContext.DrawDrawing(drawing);
            drawingContext.Pop();
        }

        RenderTargetBitmap bitmap = new(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using MemoryStream stream = new();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static void WriteIcon(string icoPath, IReadOnlyList<IconImage> images)
    {
        const int iconDirectorySize = 6;
        const int iconDirectoryEntrySize = 16;

        using FileStream stream = File.Create(icoPath);
        using BinaryWriter writer = new(stream);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write(checked((ushort)images.Count));

        uint imageOffset = checked((uint)(iconDirectorySize + (iconDirectoryEntrySize * images.Count)));
        foreach (IconImage image in images)
        {
            writer.Write(image.Size == 256 ? (byte)0 : checked((byte)image.Size));
            writer.Write(image.Size == 256 ? (byte)0 : checked((byte)image.Size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(checked((uint)image.PngData.Length));
            writer.Write(imageOffset);
            imageOffset = checked(imageOffset + (uint)image.PngData.Length);
        }

        foreach (IconImage image in images)
        {
            writer.Write(image.PngData);
        }
    }

    private sealed record IconImage(int Size, byte[] PngData);
}

internal sealed record IconExportResult(
    string OutputDirectory,
    IReadOnlyList<string> PngPaths,
    string IcoPath);
