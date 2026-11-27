using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PptxGenerator.Chat.Tests;

[TestClass]
public sealed class ChatIconTests
{
    [STATestMethod]
    public void RenderChatIconAssets()
    {
        string directory = GetAssetsDirectory();
        Directory.CreateDirectory(directory);
        int[] sizes = [16, 24, 32, 48, 64, 128, 256];
        var frames = sizes.Select(RenderPng).ToArray();
        File.WriteAllBytes(Path.Combine(directory, "Chat.png"), frames[^1]);
        string path = Path.Combine(directory, "Chat.ico");
        WriteIcon(path, sizes, frames);

        using var stream = File.OpenRead(path);
        var icon = new IconBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        CollectionAssert.AreEqual(sizes, icon.Frames.Select(frame => frame.PixelWidth).Order().ToArray());
    }

    private static string GetAssetsDirectory([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "PptxGenerator.Chat", "Assets"));

    private static byte[] RenderPng(int size)
    {
        var icon = new DrawingVisual();
        using (var drawing = icon.RenderOpen())
        {
            drawing.PushTransform(new ScaleTransform(size / 64.0, size / 64.0));
            var baseColor = new LinearGradientBrush(Color.FromRgb(52, 69, 66), Color.FromRgb(28, 43, 41), 90);
            drawing.DrawRoundedRectangle(baseColor, null, new Rect(2, 2, 60, 60), 14, 14);

            drawing.DrawRoundedRectangle(Brushes.White, null, new Rect(11, 13, 41, 32), 5, 5);
            var slideInk = new SolidColorBrush(Color.FromRgb(72, 92, 86));
            drawing.DrawRoundedRectangle(slideInk, null, new Rect(17, 20, 25, 4), 2, 2);
            drawing.DrawRoundedRectangle(slideInk, null, new Rect(17, 28, 15, 3), 1.5, 1.5);
            drawing.DrawRoundedRectangle(slideInk, null, new Rect(17, 35, 10, 3), 1.5, 1.5);

            var bubble = Geometry.Parse("M34,31 L51,31 Q57,31 57,37 L57,45 Q57,51 51,51 L46,51 L39,57 L39,51 L34,51 Q28,51 28,45 L28,37 Q28,31 34,31 Z");
            drawing.DrawGeometry(new SolidColorBrush(Color.FromRgb(196, 235, 208)), new Pen(slideInk, 2), bubble);
            foreach (int x in new[] { 35, 42, 49 }) drawing.DrawEllipse(slideInk, null, new Point(x, 41), 1.8, 1.8);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(icon);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static void WriteIcon(string path, int[] sizes, byte[][] frames)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)sizes.Length);
        int offset = 6 + sizes.Length * 16;
        for (int i = 0; i < sizes.Length; i++)
        {
            byte dimension = (byte)(sizes[i] % 256); // ICO encodes 256 pixels as zero.
            writer.Write(dimension);
            writer.Write(dimension);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(frames[i].Length);
            writer.Write(offset);
            offset += frames[i].Length;
        }
        foreach (var frame in frames) writer.Write(frame);
    }
}
