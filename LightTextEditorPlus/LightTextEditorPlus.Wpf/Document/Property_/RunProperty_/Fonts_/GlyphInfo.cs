﻿using System.Windows.Media;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Document
{
    /// <summary>
    /// 存储Glyph相关状态信息的对象
    /// </summary>
    class GlyphInfo
    {
        /// <summary>
        /// 创建<see cref="GlyphInfo"/>对象
        /// </summary>
        /// <param name="typeface"></param>
        /// <param name="renderGlyphTypeface"></param>
        /// <param name="originGlyphTypeface"></param>
        /// <param name="unicodeChar"></param>
        /// <param name="glyphIndex"></param>
        public GlyphInfo(Typeface typeface, GlyphTypeface renderGlyphTypeface, GlyphTypeface originGlyphTypeface,
            Utf32CodePoint unicodeChar, ushort glyphIndex)
        {
            Typeface = typeface;
            RenderGlyphTypeface = renderGlyphTypeface;
            OriginGlyphTypeface = originGlyphTypeface;
            UnicodeChar = unicodeChar;
            GlyphIndex = glyphIndex;
        }

        /// <summary>
        /// 渲染字符
        /// </summary>
        public Utf32CodePoint UnicodeChar { get; }

        /// <summary>
        /// 渲染字符的目标<see cref="Typeface"/>
        /// </summary>
        public Typeface Typeface { get; }

        /// <summary>
        /// 渲染字符的目标<see cref="GlyphTypeface"/>应该仅用于渲染
        /// </summary>
        public GlyphTypeface RenderGlyphTypeface { get; }

        /// <summary>
        /// 渲染字符的目标<see cref="GlyphTypeface"/>用于计算行高等数据，避免单个fallback字符引起偏移
        /// </summary>
        public GlyphTypeface OriginGlyphTypeface { get; }

        /// <summary>
        /// 渲染字符的目标<see cref="GlyphIndex"/>
        /// </summary>
        /// OpenType® Specification Version 1.9.1 https://learn.microsoft.com/en-us/typography/opentype/spec/
        /// [字体65,535的限制什么时候能修正？ - 知乎](https://www.zhihu.com/question/526308089 )
        /// 规范中规定了 GlyphIndex 是 UInt16 类型，因此最大值为 65535 个
        public ushort GlyphIndex { get; }
    }
}
