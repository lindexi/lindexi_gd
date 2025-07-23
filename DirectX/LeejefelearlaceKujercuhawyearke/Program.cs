// See https://aka.ms/new-console-template for more information

using Silk.NET.Core.Native;
using Silk.NET.DirectWrite;

DWrite dWrite = DWrite.GetApi();
ComPtr<IDWriteFactory6> factory = dWrite.DWriteCreateFactory<IDWriteFactory6>(FactoryType.Shared);

var fontFile = @"C:\windows\fonts\simsun.ttc";

unsafe
{
    IDWriteInMemoryFontFileLoader* dWriteInMemoryFontFileLoader;
    factory.Handle->CreateInMemoryFontFileLoader(&dWriteInMemoryFontFileLoader);

    ComPtr<IDWriteFontFileLoader> dWriteFontFileLoader =
        dWriteInMemoryFontFileLoader->QueryInterface<IDWriteFontFileLoader>();
    factory.Handle->RegisterFontFileLoader(dWriteFontFileLoader.Handle);

    HResult hr = 0;

    IDWriteFontFile* dWriteFontFile;
    fixed (char* pFontFile = fontFile)
    {
        factory.Handle->CreateFontFileReference(pFontFile, null, &dWriteFontFile);
    }

    // isSupported, &fileType, &fontFaceType, &numberOfFonts
    int isSupported = 0;
    FontFileType fileType = FontFileType.Unknown;
    FontFaceType fontFaceType = FontFaceType.Unknown;
    uint numberOfFonts = 0;
    hr = dWriteFontFile->Analyze(&isSupported, &fileType, &fontFaceType, &numberOfFonts);
    hr.Throw();

    IDWriteFontSetBuilder* fontSetBuilder;
    factory.Handle->CreateFontSetBuilder(&fontSetBuilder);

    //IDWriteFontFace* fontFace;
    //hr = factory.Handle->CreateFontFace(fontFaceType, numberOfFonts, &dWriteFontFile, 0, FontSimulations.None,
    //    &fontFace);
    //hr.Throw();

    IDWriteFontFaceReference* fontFaceReference;

    fixed (char* pFontFile = fontFile)
    {
        hr = factory.Handle->CreateFontFaceReference(pFontFile, null, (uint)0, FontSimulations.None,
            &fontFaceReference);
        hr.Throw();
    }

    fontSetBuilder->AddFontFaceReference(fontFaceReference);

    IDWriteFontFace3* fontFace3;
    fontFaceReference->CreateFontFace(&fontFace3);

    FontMetrics fontMetrics = default;
    fontFace3->GetMetrics(&fontMetrics);

    IDWriteLocalizedStrings* dWriteLocalizedStrings;
    //fontFace3->GetFaceNames(&dWriteLocalizedStrings);
    //PrintLocalizedStrings(dWriteLocalizedStrings);

    fontFace3->GetFamilyNames(&dWriteLocalizedStrings);
    PrintLocalizedStrings(dWriteLocalizedStrings);

    Console.WriteLine($"Ascent: {fontMetrics.Ascent}");
    Console.WriteLine($"Descent: {fontMetrics.Descent}");
    Console.WriteLine($"LineGap: {fontMetrics.LineGap}");
    Console.WriteLine($"CapHeight: {fontMetrics.CapHeight}");
    Console.WriteLine($"XHeight: {fontMetrics.XHeight}");
    Console.WriteLine($"DesignUnitsPerEm: {fontMetrics.DesignUnitsPerEm}");

    var lineSpacing = fontMetrics.Ascent + fontMetrics.Descent + fontMetrics.LineGap;
    Console.WriteLine($"LineSpacing: {lineSpacing} {lineSpacing / (double)fontMetrics.DesignUnitsPerEm}");
}

unsafe void PrintLocalizedStrings(IDWriteLocalizedStrings* dWriteLocalizedStrings)
{
    uint count = dWriteLocalizedStrings->GetCount();
    List<(string LocaleName, string Name)> list = new((int)count);

    for (uint i = 0; i < count; i++)
    {
        uint length = 0;
        dWriteLocalizedStrings->GetLocaleNameLength(i, &length);

        // 加一解决 \0 的问题
        length += 1;
        char* localeNameBuffer = stackalloc char[(int)length];
        dWriteLocalizedStrings->GetLocaleName(i, localeNameBuffer, length);

        // zh-cn 等输出
        string localeName = new string(localeNameBuffer, 0, (int)length - 1);

        dWriteLocalizedStrings->GetStringLength(i, &length);
        length += 1;
        char* nameBuffer = stackalloc char[(int)length];
        dWriteLocalizedStrings->GetString(i, nameBuffer, length);
        string name = new string(nameBuffer, 0, (int)length - 1);

        list.Add((localeName, name));
    }

    foreach (var (localeName, name) in list)
    {
        if (localeName == "zh-cn")
        {
            Console.WriteLine($"FontName: {name}");
            return;
        }
    }

    Console.WriteLine(list.FirstOrDefault().Name);
}