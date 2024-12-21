// See https://aka.ms/new-console-template for more information

using System.Globalization;

using Icu;

Icu.Wrapper.Init();

var text = "asd fx, aasa “说话大”";
var boundaries = Icu.BreakIterator.GetBoundaries(BreakIterator.UBreakIteratorType.WORD, Locale.GetLocaleForLCID(CultureInfo.CurrentCulture.LCID), text);
foreach (Boundary boundary in boundaries)
{
    var subText = text.AsSpan().Slice(boundary.Start, boundary.End - boundary.Start);
    Console.WriteLine(subText.ToString());
}

// Will output "NFC form of XA\u0308bc is XÄbc"
Console.WriteLine($"NFC form of XA\\u0308bc is {Icu.Normalizer.Normalize("XA\u0308bc",
    Icu.Normalizer.UNormalizationMode.UNORM_NFC)}");
Icu.Wrapper.Cleanup();
