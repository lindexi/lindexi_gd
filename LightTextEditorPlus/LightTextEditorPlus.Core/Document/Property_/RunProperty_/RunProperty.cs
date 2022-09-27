using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Document
{
    internal interface IReadOnlyRunProperty
    {
        double FontSize { get; }
        FontName FontFamily { get; }

        /// <summary>
        /// 斜体表示，默认值为Normal
        /// </summary>
        FontStyle FontStyle { get; }

        /// <summary>
        /// 字的粗细度，默认值为Normal
        /// </summary>
        FontWeight FontWeight { get; }

        bool TryGetProperty(string propertyName, [NotNullWhen(true)] out object value);

        /// <summary>
        /// 构建新的属性
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IReadOnlyRunProperty BuildNewProperty(Action<RunProperty> action);
    }

    class RunProperty : IReadOnlyRunProperty
    {
        public RunProperty()
        {
        }

        private RunProperty(RunProperty? styleRunProperty)
        {
            StyleRunProperty = styleRunProperty;
        }

        public double FontSize
        {
            set
            {
                var valueToSet = value.CoerceValue(1, 65536);
                _fontSize = valueToSet;
                RaiseOnTextRunPropertyChanged();
            }
            get => _fontSize ?? StyleRunProperty?.FontSize ?? DefaultFontSize;
        }

        private double? _fontSize;

        private const double DefaultFontSize = 15;

        public FontName FontFamily
        {
            set
            {
                _fontFamily = value;
                RaiseOnTextRunPropertyChanged();
            }
            get => _fontFamily ?? StyleRunProperty?.FontFamily ?? FontName.DefaultNotDefineFontName;
        }

        private FontName? _fontFamily;

        /// <summary>
        /// 斜体表示，默认值为Normal
        /// </summary>
        public FontStyle FontStyle
        {
            set
            {
                _fontStyle = value;
                RaiseOnTextRunPropertyChanged();
            }
            get => _fontStyle ?? StyleRunProperty?.FontStyle ?? FontStyle.DefaultNotDefine;
        }

        private FontStyle? _fontStyle;

        /// <summary>
        /// 字的粗细度，默认值为Normal
        /// </summary>
        public FontWeight FontWeight
        {
            set
            {
                _fontWeight = value;
                RaiseOnTextRunPropertyChanged();
            }
            get => _fontWeight ?? StyleRunProperty?.FontWeight ?? FontWeight.DefaultNotDefine;
        }

        private FontWeight? _fontWeight;

        #region 附加属性

        /// <summary>
        /// 推荐放入的对象应该是不可变对象
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public void SetProperty(string propertyName, object value)
        {
            AdditionalPropertyDictionary ??= new Dictionary<string, object>();

            AdditionalPropertyDictionary[propertyName] = value;
        }

        public bool TryGetProperty(string propertyName, [NotNullWhen(true)] out object value)
        {
            if (AdditionalPropertyDictionary?.TryGetValue(propertyName, out value!) is true)
            {
                return true;
            }

            if (StyleRunProperty?.TryGetProperty(propertyName, out value) is true)
            {
                return true;
            }

            value = default!;
            return false;
        }

        private Dictionary<string, object>? AdditionalPropertyDictionary { set; get; }

        #endregion

        #region 框架

        /// <summary>
        /// 继承样式里的属性
        /// </summary>
        private RunProperty? StyleRunProperty { get; }

        /// <summary>
        /// 构建新的属性
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IReadOnlyRunProperty BuildNewProperty(Action<RunProperty> action)
        {
            var runProperty = new RunProperty(this);
            action(runProperty);
            return runProperty;
        }

        private void RaiseOnTextRunPropertyChanged()
        {

        }
        #endregion
    }


}
