using System.Text.RegularExpressions;

var file = @"F:\temp\表格样式HerchakallhuKallbudellaw\File.txt";

var outputFolder = Path.GetDirectoryName(file);
var testFile = Path.Combine(outputFolder, "Test.txt");

while (true)
{
    try
    {
        var text = File.ReadAllText(file);

        string styleId = "";

        var styleIdRegex = new Regex(@"StyleId = ""(\S+)"", ");
        var styleIdMatch = styleIdRegex.Match(text);
        if (styleIdMatch.Success)
        {
            styleId = styleIdMatch.Groups[1].Value;
        }
        else
        {
            Console.WriteLine($"匹配失败");
        }

        string styleName = "";

        var styleNameRegex = new Regex(@"StyleName = ""([\S\s]+)"" };");
        var styleNameMatch = styleNameRegex.Match(text);
        if (styleNameMatch.Success)
        {
            styleName = styleNameMatch.Groups[1].Value;
        }
        else
        {
            Console.WriteLine($"匹配失败");
        }

        var outputFileName = $"{styleName}.cs";

        var outputFile = Path.Combine(outputFolder, outputFileName);

        var className = styleId.Replace("{", "").Replace("}", "").Replace("-", "_");
        text = text.Replace("public class GeneratedClass", $"public static class _{className}").Replace("public TableStyleEntry GenerateTableStyleEntry()", "public static TableStyleEntry GenerateTableStyleEntry()");
        File.WriteAllText(outputFile,text);

        File.AppendAllLines(testFile,new string[]
        {
            $"\"{styleId}\" => _{className}.GenerateTableStyleEntry(),"
        });

        Console.WriteLine($"生成 {styleName}");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }

    Console.ReadLine();
}