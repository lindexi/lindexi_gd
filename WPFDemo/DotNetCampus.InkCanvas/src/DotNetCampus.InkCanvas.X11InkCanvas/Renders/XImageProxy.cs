using SkiaSharp;
using X11ApplicationFramework.Natives;

namespace DotNetCampus.InkCanvas.X11InkCanvas.Renders;

readonly struct XImageProxy : IDisposable
{
    public XImageProxy(SKBitmap skBitmap)
    {
        const int bytePerPixelCount = 4; // RGBA 一共4个 byte 长度
        var bitPerByte = 8;

        var bitmapWidth = skBitmap.Width;
        var bitmapHeight = skBitmap.Height;

        var img = new XImage();
        int bitsPerPixel = bytePerPixelCount * bitPerByte;
        img.width = bitmapWidth;
        img.height = bitmapHeight;
        img.format = 2; //ZPixmap;
        img.data = skBitmap.GetPixels();
        img.byte_order = 0; // LSBFirst;
        img.bitmap_unit = bitsPerPixel;
        img.bitmap_bit_order = 0; // LSBFirst;
        img.bitmap_pad = bitsPerPixel;
        img.depth = bitsPerPixel;
        img.bytes_per_line = bitmapWidth * bytePerPixelCount;
        img.bits_per_pixel = bitsPerPixel;
        XLib.XInitImage(ref img);
        Image = img;
    }

    public XImage Image { get; }

    public void Dispose()
    {
        var image = Image;
        // 以下代码会导致闪退
        // 错误信息： free(): invalid pointer
        //XLib.XDestroyImage(ref image);
    }
}

