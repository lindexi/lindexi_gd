using System.Windows.Media;

namespace LightTextEditorPlus.TextEditorPlus.Render
{
    /// <summary>
    /// 包含Glyph相关的数据信息
    /// </summary>
    class TextCharGlyphData
    {
        /// <summary>
        /// 字符字号
        /// </summary>
        public double FontSize { get; set; }

        /// <summary>
        /// 字符
        /// </summary>
        public GlyphRun GlyphRun { get; set; }

        /// <summary>
        /// 字符信息
        /// </summary>
        public GlyphInfo GlyphInfo { get; set; }

        ///// <summary>
        ///// 字符进行旋转等变换的Matrix
        ///// </summary>
        //public Matrix Matrix { get; set; } = null;
    }
}