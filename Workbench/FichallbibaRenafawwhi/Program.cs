// See https://aka.ms/new-console-template for more information

using System.Globalization;
using Icu;

Icu.Wrapper.Init();

Console.WriteLine($"分词测试：");

Span<string> testTextSpan = ["大学生活", "大学生活动", "大学生命"];

foreach (var testText in testTextSpan)
{
    Console.WriteLine($"对 '{testText}' 进行分词：");
    foreach (var boundary in Icu.BreakIterator.GetBoundaries(BreakIterator.UBreakIteratorType.WORD,
                 Locale.GetLocaleForLCID(CultureInfo.CurrentCulture.LCID), testText))
    {
        var subText = testText.AsSpan().Slice(boundary.Start, boundary.End - boundary.Start);
        Console.WriteLine($" - \"{subText.ToString()}\"");
    }
}

Console.WriteLine();
Console.WriteLine($"分行测试：");

var text = "asd fx, aasa “说话大学生上课”\nasd sadf";
var boundaries = Icu.BreakIterator.GetBoundaries(BreakIterator.UBreakIteratorType.LINE,
    Locale.GetLocaleForLCID(CultureInfo.CurrentCulture.LCID), text);
foreach (Boundary boundary in boundaries)
{
    var subText = text.AsSpan().Slice(boundary.Start, boundary.End - boundary.Start);
    Console.WriteLine($" - \"{subText.ToString()}\"");
}

Console.WriteLine();
Console.WriteLine($"转义测试：");

// Will output "NFC form of XA\u0308bc is XÄbc"
// 有些控制台输出不了 Ä 字符哦
Console.WriteLine($"NFC form of XA\\u0308bc is {Icu.Normalizer.Normalize("XA\u0308bc",
    Icu.Normalizer.UNormalizationMode.UNORM_NFC)}");
Icu.Wrapper.Cleanup();