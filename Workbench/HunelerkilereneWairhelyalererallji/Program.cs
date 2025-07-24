using SkiaSharp;

using Svg;
using Svg.Skia;

namespace HunelerkilereneWairhelyalererallji;

internal class Program
{
    static void Main(string[] args)
    {
        var file = "1.svg";
        file = Path.GetFullPath(file);

        var skSvg = new SKSvg();
        skSvg.Load(file);

        skSvg.Save("1.png", SKColors.Transparent);
    }
}
