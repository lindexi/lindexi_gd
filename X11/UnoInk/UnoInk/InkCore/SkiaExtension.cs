using System.Runtime.CompilerServices;
using SkiaSharp;

namespace UnoInk.InkCore;

static class SkiaExtension
{
    /// <summary>
    /// 从 <paramref name="sourceBitmap"/> 拷贝所有像素覆盖原本的像素
    /// </summary>
    /// <param name="destinationBitmap"></param>
    /// <param name="sourceBitmap"></param>
    /// <returns></returns>
    public static unsafe bool ReplacePixels(this SKBitmap destinationBitmap, SKBitmap sourceBitmap)
    {
        var destinationPixelPtr = (byte*) destinationBitmap.GetPixels(out var length).ToPointer();
        var sourcePixelPtr = (byte*) sourceBitmap.GetPixels().ToPointer();

        Unsafe.CopyBlockUnaligned(destinationPixelPtr, sourcePixelPtr, (uint) length);
        return true;
    }

    /// <summary>
    /// 从 <paramref name="sourceBitmap"/> 拷贝指定范围 <paramref name="rect"/> 像素过来覆盖指定范围 <paramref name="rect"/> 的像素
    /// </summary>
    /// <param name="destinationBitmap"></param>
    /// <param name="sourceBitmap"></param>
    /// <param name="rect"></param>
    public static unsafe bool ReplacePixels(this SKBitmap destinationBitmap, SKBitmap sourceBitmap, SKRectI rect)
    {
        uint* basePtr = (uint*) destinationBitmap.GetPixels().ToPointer();
        uint* sourcePtr = (uint*) sourceBitmap.GetPixels().ToPointer();
        //Console.WriteLine($"ReplacePixels Rect={rect.Left},{rect.Top},{rect.Right},{rect.Bottom} wh={rect.Width},{rect.Height} BitmapWH={destinationBitmap.Width},{destinationBitmap.Height} D={destinationBitmap.RowBytes == (destinationBitmap.Width * sizeof(uint))}");

        for (int row = rect.Top; row < rect.Bottom; row++)
        {
            if (row >= destinationBitmap.Height)
            {
                return false;
            }

            var col = rect.Left;
            uint* destinationPixelPtr = basePtr + destinationBitmap.Width * row + col;
            uint* sourcePixelPtr = sourcePtr + sourceBitmap.Width * row + col;

            var length = rect.Width;

            if (col + length > destinationBitmap.Width)
            {
                return false;
            }

            var byteCount = (uint) length * sizeof(uint);
            Unsafe.CopyBlockUnaligned(destinationPixelPtr, sourcePixelPtr, byteCount);
        }

        return true;
    }

    /// <summary>
    /// 清理指定范围
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="rect"></param>
    public static unsafe void ClearBounds(this SKBitmap bitmap, SKRectI rect)
    {
        uint* basePtr = (uint*) bitmap.GetPixels().ToPointer();
        // Loop through the rows
        //var stopwatch = Stopwatch.StartNew();
        //for (int row = 0; row < bitmap.Height; row++)
        //{
        //    for (int col = 0; col < bitmap.Width; col++)
        //    {
        //        uint* ptr = basePtr + bitmap.Width * row + col;
        //        *ptr = unchecked((uint)(0xFF << 24 + ((byte)col) <<
        //                                16 + (byte) row));
        //    }
        //}

        for (int row = rect.Top; row < rect.Bottom; row++)
        {
            var col = rect.Left;
            uint* ptr = basePtr + bitmap.Width * row + col;

            var length = rect.Width;
            Unsafe.InitBlock(ptr, 0, (uint) length * sizeof(uint));
            //var span = new Span<uint>(ptr, length);
            //span.Clear();
        }

        //Console.WriteLine($"耗时 {stopwatch.ElapsedMilliseconds}"); // 差不多一秒
    }
}
