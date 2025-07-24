using System.Xml.Linq;
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

        var fileStream = File.OpenRead(file);
        var streamReader = new StreamReader(fileStream);

        var xDocument = XDocument.Load(streamReader, LoadOptions.SetLineInfo);
        xDocument.Save("2.svg");

        var skSvg = new SKSvg();
        skSvg.Load("2.svg");

        skSvg.Save("1.png", SKColors.Transparent);
    }
}
