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

        foreach (var xElement in xDocument.Descendants("text"))
        {
            var value = xElement.Value;
            if (!string.IsNullOrEmpty(value) && value.Length > 0 && value[0] is var c && c == 0xFFFD)
            {
                // 0xFFFFD 是 utf8 特殊字符
                // 画出来就是�符号，不如删掉
                xElement.Value = string.Empty;
            }
        }

        xDocument.Save("2.svg");

        var skSvg = new SKSvg();
        skSvg.Load("2.svg");

        skSvg.Save("1.png", SKColors.Transparent);
    }
}
