using System;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Document
{
    /// <summary>
    /// 用于创建<see cref="GlyphRun"/>的辅助类
    /// </summary>
    static class GlyphRunCreator
    {
        /// <summary>
        /// 尝试从指定的<see cref="Typeface"/>对象获取<see cref="GlyphInfo"/>对象
        /// </summary>
        /// <param name="typeface"></param>
        /// <param name="unicodeChar"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool TryGetGlyphInfo(Typeface typeface, Utf32CodePoint unicodeChar, out GlyphInfo? info)
        {
            if (typeface is null)
            {
                throw new ArgumentNullException(nameof(typeface));
            }

            ushort glyphIndex = default;
            info = null;

            if (typeface.TryGetGlyphTypeface(out var glyph))
            {
                if (glyph.CharacterToGlyphMap.TryGetValue(unicodeChar.Value, out glyphIndex))
                {
                    info = new GlyphInfo(typeface, glyph, glyph, unicodeChar, glyphIndex);
                    return true;
                }
            }

            if (FallBackFontFamily.Instance.TryGetFallBackFontFamily(unicodeChar, out var familyName))
            {
                var fallbackTypeface = new Typeface(new FontFamily(familyName), typeface.Style, typeface.Weight,
                    typeface.Stretch);

                if (fallbackTypeface.TryGetGlyphTypeface(out var fallbackGlyph))
                {
                    if (fallbackGlyph.CharacterToGlyphMap.TryGetValue(unicodeChar.Value, out glyphIndex))
                    {
                        var originGlyph = glyph ?? fallbackGlyph;
                        info = new GlyphInfo(typeface, fallbackGlyph, originGlyph, unicodeChar, glyphIndex);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 获取指定大小的单个<see cref="GlyphRun"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="fontSize"></param>
        /// <returns></returns>
        public static GlyphRun BuildSingleGlyphRun(GlyphInfo info, double fontSize)
        {
            return BuildSingleGlyphRun(info, fontSize, new Point());
        }

        /// <summary>
        /// 获取指定大小,位置的单个<see cref="GlyphRun"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="fontSize"></param>
        /// <param name="baselineOrigin"></param>
        /// <returns></returns>
        public static GlyphRun BuildSingleGlyphRun(GlyphInfo info, double fontSize, Point baselineOrigin)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var width = info.RenderGlyphTypeface.AdvanceWidths[info.GlyphIndex] * fontSize;
            width = GlyphExtension.RefineValue(width);

#pragma warning disable 618 // 忽略调用废弃构造函数。因为这里无法传入 DPI 值
            var glyphRun = new GlyphRun(
#pragma warning restore 618
                info.RenderGlyphTypeface,
                0,
                false,
                fontSize,
                new[] { info.GlyphIndex },
                baselineOrigin,
                new[] { width },
                DefaultGlyphOffsetArray,
                info.UnicodeChar.ToCharArray(),
                null,
                null,
                null, DefaultXmlLanguage);

            return glyphRun;
        }

        private static readonly Point[] DefaultGlyphOffsetArray = new Point[] { new Point() };

        private static readonly XmlLanguage DefaultXmlLanguage =
            XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);
    }
}
