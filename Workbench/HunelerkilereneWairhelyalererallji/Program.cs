using System.Reflection;
using System.Xml.Linq;
using SkiaSharp;

using Svg;
using Svg.Skia;

namespace HunelerkilereneWairhelyalererallji;

internal class Program
{
    static void Main(string[] args)
    {
        //var stream = Assembly.GetExecutingAssembly().GetFile("1.svg");
        //Console.WriteLine($"GetFile={stream is not null}");

        //foreach (var manifestResourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
        //{
        //    Console.WriteLine($"ManifestResourceName={manifestResourceName}");
        //}

        Console.WriteLine(AppContext.BaseDirectory);

        var file = "1.svg";
        file = Path.GetFullPath(file);

        if (!File.Exists(file))
        {
            file = Path.Join(AppContext.BaseDirectory, "1.svg");
        }

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
