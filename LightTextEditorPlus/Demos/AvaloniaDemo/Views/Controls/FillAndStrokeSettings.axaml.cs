using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Converters;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

using System.Globalization;

using System;

namespace LightTextEditorPlus.AvaloniaDemo.Views.Controls;

public partial class FillAndStrokeSettings : UserControl
{
    public FillAndStrokeSettings()
    {
        InitializeComponent();
    }

    public bool IsSettingEnable
    {
        get => IsSettingEnableCheckBox.IsChecked ?? true;
    }

    // 遇到 CheckBox 无法双向绑定问题
    //public static readonly DirectProperty<FillAndStrokeSettings, bool?>
    //    IsSettingEnableProperty = AvaloniaProperty.RegisterDirect<FillAndStrokeSettings, bool?>
    //    (
    //        nameof(IsSettingEnable),
    //        getter: settings => settings._isSettingEnable,
    //        setter: (settings, enable) => settings._isSettingEnable = enable ?? true,
    //        defaultBindingMode: BindingMode.TwoWay,
    //        unsetValue: true
    //    );

    public string HeaderText
    {
        get => _headerText;
        set
        {
            SetAndRaise(HeaderTextProperty, ref _headerText, value);
        }
    }

    public static readonly DirectProperty<FillAndStrokeSettings, string>
        HeaderTextProperty = AvaloniaProperty.RegisterDirect<FillAndStrokeSettings, string>
            (
                nameof(HeaderText),
                getter: settings => settings._headerText,
                setter: (settings, text) => settings._headerText = text,
                defaultBindingMode: BindingMode.TwoWay
            );

    public string StrokeColor
    {
        get => _strokeColor;
        set { SetAndRaise(StrokeColorProperty, ref _strokeColor, value); }
    }

    public static readonly DirectProperty<FillAndStrokeSettings, string>
        StrokeColorProperty = AvaloniaProperty.RegisterDirect<FillAndStrokeSettings, string>
            (
                nameof(StrokeColor),
                getter: settings => settings.StrokeColor,
                setter: (settings, color) => settings.StrokeColor = color,
                defaultBindingMode: BindingMode.TwoWay
            );

    public double StrokeThickness
    {
        get => _strokeThickness;
        set
        {
            SetAndRaise(StrokeThicknessProperty, ref _strokeThickness, value);
        }
    }

    public static readonly DirectProperty<FillAndStrokeSettings, double>
        StrokeThicknessProperty = AvaloniaProperty.RegisterDirect<FillAndStrokeSettings, double>
            (
                nameof(StrokeThickness),
                getter: settings => settings.StrokeThickness,
                setter: (settings, thickness) => settings.StrokeThickness = thickness,
                defaultBindingMode: BindingMode.TwoWay
            );

    public string FillColor
    {
        get => _fillColor;
        set
        {
            SetAndRaise(FillColorProperty, ref _fillColor, value);
        }
    }

    public static readonly DirectProperty<FillAndStrokeSettings, string>
        FillColorProperty = AvaloniaProperty.RegisterDirect<FillAndStrokeSettings, string>
            (
                nameof(FillColor),
                getter: settings => settings.FillColor,
                setter: (settings, color) => settings.FillColor = color,
                defaultBindingMode: BindingMode.TwoWay
            );

    private string _headerText = "";
    private double _strokeThickness;
    private string _fillColor = "";
    private string _strokeColor = "";
    //private bool _isSettingEnable = true;
}

public class StringToColorBrushValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string colorString)
        {
            var color = ColorToHexConverter.ParseHexString(colorString, AlphaComponentPosition.Leading);
            if (color != null)
            {
                return new SolidColorBrush(color.Value);
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}