using System.IO.Compression;
using System.Runtime.InteropServices;

namespace WinRemoteShell.Server;

internal static partial class ScreenshotCapture
{
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;
    private const uint Srccopy = 0x00CC0020;
    private const uint DibRgbColors = 0;
    private static ReadOnlySpan<byte> PngSignature => [137, 80, 78, 71, 13, 10, 26, 10];

    public static async Task CaptureAsync(Stream output, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Screen capture is supported only on Windows.");
        }

        var x = GetSystemMetrics(SmXVirtualScreen);
        var y = GetSystemMetrics(SmYVirtualScreen);
        var width = GetSystemMetrics(SmCxVirtualScreen);
        var height = GetSystemMetrics(SmCyVirtualScreen);
        var screenDc = GetDC(IntPtr.Zero);
        var memoryDc = CreateCompatibleDC(screenDc);
        var bitmap = CreateCompatibleBitmap(screenDc, width, height);
        var previous = SelectObject(memoryDc, bitmap);

        try
        {
            if (!BitBlt(memoryDc, 0, 0, width, height, screenDc, x, y, Srccopy))
            {
                throw new InvalidOperationException("Unable to capture the desktop.");
            }

            var bitmapInfo = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = width,
                    Height = -height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = 0
                }
            };
            var pixels = new byte[checked(width * height * 4)];
            if (GetDIBits(memoryDc, bitmap, 0, (uint)height, pixels, ref bitmapInfo, DibRgbColors) == 0)
            {
                throw new InvalidOperationException("Unable to read captured desktop pixels.");
            }

            await WritePngAsync(output, pixels, width, height, cancellationToken);
        }
        finally
        {
            SelectObject(memoryDc, previous);
            DeleteObject(bitmap);
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static async Task WritePngAsync(Stream output, byte[] pixels, int width, int height, CancellationToken cancellationToken)
    {
        await output.WriteAsync(PngSignature.ToArray(), cancellationToken);
        var header = new byte[13];
        WriteUInt32(header, 0, (uint)width);
        WriteUInt32(header, 4, (uint)height);
        header[8] = 8;
        header[9] = 6;
        await WriteChunkAsync(output, "IHDR"u8.ToArray(), header, cancellationToken);

        using var compressed = new MemoryStream();
        await using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, true))
        {
            var scanline = new byte[checked(width * 4 + 1)];
            for (var row = 0; row < height; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                scanline[0] = 0;
                var sourceOffset = row * width * 4;
                for (var column = 0; column < width; column++)
                {
                    var source = sourceOffset + column * 4;
                    var target = 1 + column * 4;
                    scanline[target] = pixels[source + 2];
                    scanline[target + 1] = pixels[source + 1];
                    scanline[target + 2] = pixels[source];
                    scanline[target + 3] = 255;
                }

                await zlib.WriteAsync(scanline, cancellationToken);
            }
        }

        await WriteChunkAsync(output, "IDAT"u8.ToArray(), compressed.ToArray(), cancellationToken);
        await WriteChunkAsync(output, "IEND"u8.ToArray(), [], cancellationToken);
    }

    private static async Task WriteChunkAsync(Stream output, byte[] type, byte[] data, CancellationToken cancellationToken)
    {
        var length = new byte[4];
        WriteUInt32(length, 0, (uint)data.Length);
        await output.WriteAsync(length, cancellationToken);
        await output.WriteAsync(type, cancellationToken);
        await output.WriteAsync(data, cancellationToken);

        var crcInput = new byte[type.Length + data.Length];
        type.CopyTo(crcInput, 0);
        data.CopyTo(crcInput, type.Length);
        var crc = new byte[4];
        WriteUInt32(crc, 0, ComputeCrc32(crcInput));
        await output.WriteAsync(crc, cancellationToken);
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> data)
    {
        var crc = uint.MaxValue;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0u : 0xEDB88320u);
            }
        }

        return ~crc;
    }

    private static void WriteUInt32(Span<byte> destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetDC(IntPtr window);

    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(IntPtr window, IntPtr deviceContext);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleDC(IntPtr deviceContext);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleBitmap(IntPtr deviceContext, int width, int height);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr SelectObject(IntPtr deviceContext, IntPtr value);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint operation);

    [LibraryImport("gdi32.dll")]
    private static partial int GetDIBits(IntPtr deviceContext, IntPtr bitmap, uint start, uint lines, [Out] byte[] bits, ref BitmapInfo bitmapInfo, uint usage);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(IntPtr value);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteDC(IntPtr deviceContext);
}
