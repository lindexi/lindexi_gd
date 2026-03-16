// See https://aka.ms/new-console-template for more information

using System.Text;

using Silk.NET.Core.Native;
using Silk.NET.DirectWrite;

DWrite dWrite = DWrite.GetApi();
ComPtr<IDWriteFactory6> factory = dWrite.DWriteCreateFactory<IDWriteFactory6>(FactoryType.Shared);

// 宋体字体
var fontFile = @"C:\windows\fonts\simsun.ttc";

unsafe
{
    HResult hr = 0;

    IDWriteFontFaceReference* fontFaceReference;

    fixed (char* pFontFile = fontFile)
    {
        hr = factory.Handle->CreateFontFaceReference(pFontFile, null, (uint) 0, FontSimulations.None,
            &fontFaceReference);
        hr.Throw();
    }

    IDWriteFontFace3* fontFace3;
    fontFaceReference->CreateFontFace(&fontFace3);

    uint rangeCount = 0;
    fontFace3->GetUnicodeRanges(0, null, ref rangeCount);
    var unicodeRanges = new UnicodeRange[rangeCount];

    fixed (UnicodeRange* p = unicodeRanges)
    {
        fontFace3->GetUnicodeRanges(rangeCount, p, ref rangeCount);
    }

    for (var i = 0; i < unicodeRanges.Length; i++)
    {
        var unicodeRange = unicodeRanges[i];
        var start = new Rune(unicodeRange.First);
        var end = new Rune(unicodeRange.Last);

        Console.WriteLine($"Range {i}: '{start.ToString()}'({start.Value}) - '{end.ToString()}'({end.Value}) Length={end.Value - start.Value + 1}");
    }

    // 判断某个字符是否在字体支持的范围内
    Span<char> testCharList = ['A', 'a', ',', '汉', ' '];
    foreach (var testChar in testCharList)
    {
        var rune = new Rune(testChar);
        var isSupported = false;
        for (var i = 0; i < unicodeRanges.Length; i++)
        {
            var unicodeRange = unicodeRanges[i];
            if (rune.Value >= unicodeRange.First && rune.Value <= unicodeRange.Last)
            {
                isSupported = true;
                break;
            }
        }

        Console.WriteLine($"Character '{testChar}'({rune.Value}) is supported: {isSupported}");
    }
}