// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

using Image<Rgba32> image = new Image<Rgba32>(20000, 20000);

var stopwatch = new Stopwatch();
stopwatch.Start();
Parallel.For(0, image.Height, rowIndex =>
{
    // ReSharper disable once AccessToDisposedClosure
    Memory<Rgba32> rowMemory = image.DangerousGetPixelRowMemory(rowIndex);

    var span = rowMemory.Span;

    for (int i = 0; i < rowMemory.Length; i++)
    {
        ref var pixel = ref span[i];
        pixel.A = 0xFF;
        pixel.R = (byte) rowIndex;
        pixel.G = (byte) (rowIndex + 1);
        pixel.B = (byte) (rowIndex + 2);
    }
});
stopwatch.Stop();
image.SaveAsPng("1.png");

Console.WriteLine("Hello, World!");
