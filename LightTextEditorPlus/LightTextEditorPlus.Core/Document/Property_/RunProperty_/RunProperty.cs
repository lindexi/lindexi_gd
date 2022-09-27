using System;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Document
{
    class RunProperty: IReadOnlyRunProperty
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


        private double? _fontSize ;

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

        //runProperty.FontStyle, runProperty.FontWeight,
       
        ///// <summary>
        ///// 斜体表示，默认值为Normal
        ///// </summary>
        //public FontStyle FontStyle
        //{
        //    set
        //    {
        //        _fontStyle = value;
        //        RaiseOnTextRunPropertyChanged();
        //    }
        //    get => _fontStyle ?? StyleRunProperty?.FontStyle ?? DefaultFontStyle;
        //}

        //private FontStyle? _fontStyle;
        //private static FontStyle DefaultFontStyle => FontStyles.Normal;

        ///// <summary>
        ///// 字的粗细度，默认值为Normal
        ///// </summary>
        //public FontWeight FontWeight
        //{
        //    set
        //    {
        //        _fontWeight = value;
        //        RaiseOnTextRunPropertyChanged();
        //    }
        //    get => _fontWeight ?? StyleRunProperty?.FontWeight ?? DefaultFontWeight;
        //}

        //private FontWeight? _fontWeight;
        //private static FontWeight DefaultFontWeight => FontWeights.Normal;

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
    }



    interface IReadOnlyRunProperty
    {
        IReadOnlyRunProperty BuildNewProperty(Action<RunProperty> action);
    }
}
