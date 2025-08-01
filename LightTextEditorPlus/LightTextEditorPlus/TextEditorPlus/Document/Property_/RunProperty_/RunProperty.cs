using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using LightTextEditorPlus.TextEditorPlus.Utils;

namespace LightTextEditorPlus.TextEditorPlus.Document
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

        public FontFamily FontFamily
        {
            set
            {
                _fontFamily = value;
                RaiseOnTextRunPropertyChanged();
            }
            get => _fontFamily ?? StyleRunProperty?.FontFamily ?? DefaultFontFamily;
        }

        private FontFamily? _fontFamily;
        private static FontFamily DefaultFontFamily => SystemFonts.CaptionFontFamily;

        //runProperty.FontStyle, runProperty.FontWeight,
       
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
            get => _fontStyle ?? StyleRunProperty?.FontStyle ?? DefaultFontStyle;
        }

        private FontStyle? _fontStyle;
        private static FontStyle DefaultFontStyle => FontStyles.Normal;

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
            get => _fontWeight ?? StyleRunProperty?.FontWeight ?? DefaultFontWeight;
        }

        private FontWeight? _fontWeight;
        private static FontWeight DefaultFontWeight => FontWeights.Normal;

        private RunProperty? StyleRunProperty { get; }

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
