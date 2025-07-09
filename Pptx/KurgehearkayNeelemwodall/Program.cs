using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.CustomXmlSchemaReferences;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Flatten.ElementConverters;
using DocumentFormat.OpenXml.Flatten.ElementConverters.Primitive;
using DocumentFormat.OpenXml.Flatten.Utils;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using ColorMap = DocumentFormat.OpenXml.Presentation.ColorMap;
using Path = System.IO.Path;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using TextBody = DocumentFormat.OpenXml.Presentation.TextBody;

var pptxFile = Path.Join(AppContext.BaseDirectory, "Test.pptx");
if (args.Length == 1)
{
    pptxFile = args[0];
}

using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(pptxFile, true);
var presentationPart = presentationDocument.PresentationPart;
var slideIndex = 0;
var slideIdList = presentationPart.Presentation.SlideIdList;
foreach (var slideId in slideIdList.OfType<SlideId>())
{
    Console.WriteLine($"SlideIndex={slideIndex} SlideId={slideId.Id}");
    slideIndex++;

    var slidePart = (SlidePart) presentationPart.GetPartById(slideId.RelationshipId);
    var slide = slidePart.Slide;

    var (_, slideLayoutPart, slideMasterPart) = GetParts(slide);

    Background? background = null;
    OpenXmlPart? backgroundRootPart = null;

    if (slide.CommonSlideData?.Background != null)
    {
        background = slide.CommonSlideData.Background;
        backgroundRootPart = slidePart;
    }
    else if (slideLayoutPart?.SlideLayout.CommonSlideData?.Background != null)
    {
        background = slideLayoutPart?.SlideLayout.CommonSlideData?.Background;
        backgroundRootPart = slideLayoutPart;
    }
    else if (slideMasterPart?.SlideMaster.CommonSlideData?.Background != null)
    {
        background = slideMasterPart?.SlideMaster.CommonSlideData?.Background;
        backgroundRootPart = slideMasterPart;
    }

    if (background is not null)
    {
        Debug.Assert(backgroundRootPart is not null);

        var colorMap = GetColorMap(slide);
        var colorScheme = GetColorScheme(slide);

        if (background.BackgroundProperties != null)
        {
            var childElements = background.BackgroundProperties.ChildElements;

        }

        if (background.BackgroundStyleReference is { } backgroundStyleReference)
        {
            GetSlideBackground();

            void GetSlideBackground()
            {
                if (backgroundStyleReference.SchemeColor is { } color)
                {
                    // https://github.com/dotnet/Open-XML-SDK/issues/1953
                    var schemeColorValues = color.Val.Value;
                    /*
                    Unhandled exception. System.TypeInitializationException: A type initializer threw an exception. To determine which type, inspect the InnerException's StackTrace property.
                     ---> System.NotSupportedException: 'DocumentFormat.OpenXml.Drawing.SchemeColorValues[]' is missing native code or metadata. This can happen for code that is not compatible with trimming or AOT. Inspect and fix trimming and AOT related warnings that were generated when the app was published. For more information see https://aka.ms/nativeaot-compatibility
                       at System.Reflection.Runtime.General.TypeUnifier.WithVerifiedTypeHandle(RuntimeArrayTypeInfo, RuntimeTypeInfo) + 0x51
                       at System.Array.InternalCreate(RuntimeType, Int32, Int32*, Int32*) + 0x15c
                       at System.Array.CreateInstance(Type, Int32) + 0x3e
                       at System.RuntimeType.GetEnumValues() + 0x5e
                       at System.Enum.GetValues(Type) + 0xf
                       at DocumentFormat.OpenXml.EnumInfoLookup`1.EnumStringLookupImpl..ctor() + 0x4a
                       at DocumentFormat.OpenXml.EnumInfoLookup`1..cctor() + 0x14
                       at System.Runtime.CompilerServices.ClassConstructorRunner.EnsureClassConstructorRun(StaticClassConstructionContext*) + 0x8b
                       --- End of inner exception stack trace ---
                       at System.Runtime.CompilerServices.ClassConstructorRunner.EnsureClassConstructorRun(StaticClassConstructionContext*) + 0xf3
                       at System.Runtime.CompilerServices.ClassConstructorRunner.CheckStaticClassConstructionReturnGCStaticBase(StaticClassConstructionContext*, Object) + 0x8
                       at DocumentFormat.OpenXml.EnumValue`1.Parse(String) + 0x3d
                       at DocumentFormat.OpenXml.OpenXmlSimpleValue`1.get_Value() + 0x35
                       at Program.<<Main>$>g__GetSlideBackground|0_3(Program.<>c__DisplayClass0_0&) + 0x4d
                       at Program.<Main>$(String[] args) + 0x6ab
                     */

                    var aRgbColor = ColorHelper.ToColor(color, colorScheme, colorMap, null);
                    Console.WriteLine($"SlideBackground={aRgbColor} Value={schemeColorValues}");
                }
            }
        }
    }
}



static (SlidePart? slidePart, SlideLayoutPart? slideLayoutPart, SlideMasterPart? slideMasterPart) GetParts(OpenXmlPartRootElement root)
{
    SlidePart? slidePart = null;
    SlideLayoutPart? slideLayoutPart = null;
    SlideMasterPart? slideMasterPart = null;
    if (root is Slide slide) slidePart = slide.SlidePart;

    if (slidePart != null)
        slideLayoutPart = slidePart.SlideLayoutPart;
    else if (root is SlideLayout slideLayout) slideLayoutPart = slideLayout.SlideLayoutPart;

    if (slideLayoutPart != null)
        slideMasterPart = slideLayoutPart.SlideMasterPart;
    else if (root is SlideMaster slideMaster) slideMasterPart = slideMaster.SlideMasterPart;

    return (slidePart, slideLayoutPart, slideMasterPart);
}

static ColorMap? GetColorMap(OpenXmlPartRootElement root)
{
    var (slidePart, slideLayoutPart, slideMasterPart) = GetParts(root);

    var masterColorMap = slideMasterPart?.SlideMaster.ColorMap;

    //从当前Slide获取ColorMap
    if (slidePart?.Slide.ColorMapOverride != null)
    {
        if (slidePart.Slide.ColorMapOverride.MasterColorMapping != null) return masterColorMap;

        if (slidePart.Slide.ColorMapOverride.OverrideColorMapping != null)
            return slidePart.Slide.ColorMapOverride.OverrideColorMapping.ToColorMap();
    }

    //从SlideLayout获取ColorMap
    if (slideLayoutPart?.SlideLayout?.ColorMapOverride != null)
    {
        if (slideLayoutPart.SlideLayout.ColorMapOverride.MasterColorMapping != null) return masterColorMap;

        if (slideLayoutPart.SlideLayout.ColorMapOverride.OverrideColorMapping != null)
            return slideLayoutPart.SlideLayout.ColorMapOverride.OverrideColorMapping.ToColorMap();
    }

    //从SlideMaster获取ColorMap
    return masterColorMap;
}

static ColorScheme? GetColorScheme(OpenXmlPartRootElement root)
{
    var (slidePart, slideLayoutPart, slideMasterPart) = GetParts(root);

    //从当前Slide获取theme
    if (slidePart?.ThemeOverridePart?.ThemeOverride?.ColorScheme != null)
        return slidePart.ThemeOverridePart.ThemeOverride.ColorScheme;

    //从SlideLayout获取theme
    if (slideLayoutPart?.ThemeOverridePart?.ThemeOverride?.ColorScheme != null)
        return slideLayoutPart.ThemeOverridePart.ThemeOverride.ColorScheme;

    //从SlideMaster获取theme
    return slideMasterPart?.ThemePart?.Theme?.ThemeElements?.ColorScheme;
}