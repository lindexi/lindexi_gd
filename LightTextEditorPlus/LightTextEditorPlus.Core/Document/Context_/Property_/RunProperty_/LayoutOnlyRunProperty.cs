using System;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Document
{
    /// <summary>
    /// 仅布局支持的文本字符属性
    /// </summary>
    // todo 考虑属性系统支持设置是否影响布局，不影响布局的，例如改个颜色，可以不重新布局
    // todo 考虑支持 行间注 `Usage of interlinear annotations` 作为 pronunciation 为汉字标注读音。其中 pronunciation 包括 pronunciation 注音符号 和 Romanization 罗马拼音（即 Hanyu Pinyin 汉语拼音）的支持
    // pronunciation 注音符号，多用于台湾，注音符号标注于相应汉字（基文）的右侧，能够在横排和竖排下使用，支持难度相对较高
    // Romanization 罗马拼音（即 Hanyu Pinyin 汉语拼音），因拉丁字母的特质，此类标音文本仅横排，支持难度相对较低
    public record LayoutOnlyRunProperty : IReadOnlyRunProperty
    {
        /// <summary>
        /// 创建仅布局支持的文本字符属性
        /// </summary>
        public LayoutOnlyRunProperty()
        {
        }

        /// <inheritdoc />
        public double FontSize
        {
            init
            {
                var valueToSet = value.CoerceValue(MinFontSize, MaxFontSize);
                _fontSize = valueToSet;
                //RaiseOnTextRunPropertyChanged();
            }
            get => _fontSize ?? DefaultFontSize;
        }

        /// <summary>
        /// 最大字体大小
        /// </summary>
        public const double MaxFontSize = 65536;

        /// <summary>
        /// 最小字体大小
        /// </summary>
        public const double MinFontSize = 1;

        private readonly double? _fontSize;

        private const double DefaultFontSize = 15;

        /// <inheritdoc />
        public virtual FontName FontName
        {
            init
            {
                _fontFamily = value;
                //RaiseOnTextRunPropertyChanged();
            }
            get => _fontFamily ?? FontName.DefaultNotDefineFontName;
        }

        private readonly FontName? _fontFamily;

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(FontSize, FontName);
        }

        /// <summary>
        /// 判断相等
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Equals(LayoutOnlyRunProperty? other)
        {
            if (other is null) return false;

            if
            (
                FontSize.Equals(other.FontSize)
                && FontName.Equals(other.FontName)
            )
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public virtual bool Equals(IReadOnlyRunProperty? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (other is LayoutOnlyRunProperty runProperty)
            {
                return Equals(runProperty);
            }
            else
            {
                return false;
            }
        }
    }
}
