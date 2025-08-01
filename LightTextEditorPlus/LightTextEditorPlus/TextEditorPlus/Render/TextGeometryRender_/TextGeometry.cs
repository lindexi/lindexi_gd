using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using LightTextEditorPlus.TextEditorPlus.Document;

namespace LightTextEditorPlus.TextEditorPlus.Render
{
    class TextGeometry
    {
        static TextGeometryRenderResult BuildGeometry(RunProperty runProperty,
            string text)
        {
            var typeface = new Typeface(runProperty.FontFamily, runProperty.FontStyle, runProperty.FontWeight,
                FontStretches.Normal);

            //filter for special char https://referencesource.microsoft.com/#PresentationCore/Core/CSharp/MS/Internal/TextFormatting/SimpleTextLine.cs,df93c5e29b80fc17
            if (text.Length == 1
                && text[0] != '\r' && text[0] != '\n' && text[0] != '\t'
                && GlyphRunCreator.TryGetGlyphInfo(typeface, text[0], out var info))
            {
                var glyphData = new TextCharGlyphData
                {
                    FontSize = runProperty.FontSize,
                    GlyphInfo = info
                };

                var glyphRun = GlyphRunCreator.BuildSingleGlyphRun(info, runProperty.FontSize);
                glyphData.GlyphRun = glyphRun;

            }

            return default;
        }
    }
}
