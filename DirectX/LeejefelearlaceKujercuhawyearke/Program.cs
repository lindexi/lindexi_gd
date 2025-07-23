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

    IDWriteFontFace* fontFace;
    hr = factory.Handle->CreateFontFace(fontFaceType, numberOfFonts, &dWriteFontFile, 0, FontSimulations.None,
        &fontFace);
    hr.Throw();

    IDWriteFontFaceReference* fontFaceReference;
    factory.Handle->CreateFontFaceReference(fontFile, null, (uint) 0, FontSimulations.None, &fontFaceReference);

    fontSetBuilder->AddFontFaceReference(fontFaceReference);

    IDWriteFontFace3* fontFace3;
    fontFaceReference->CreateFontFace(&fontFace3);

    FontMetrics fontMetrics = default;
    fontFace3->GetMetrics(&fontMetrics);

    var ascent = fontMetrics.Ascent;
}
