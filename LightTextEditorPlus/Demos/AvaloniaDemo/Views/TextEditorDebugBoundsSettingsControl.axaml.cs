using Avalonia.Controls;
using Avalonia.Controls.Converters;
using Avalonia.Interactivity;
using Avalonia.Skia;

using LightTextEditorPlus.AvaloniaDemo.Views.Controls;
using LightTextEditorPlus.Diagnostics;

using SkiaSharp;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class TextEditorDebugBoundsSettingsControl : UserControl
{
    public TextEditorDebugBoundsSettingsControl()
    {
        InitializeComponent();
        InitSettings();
    }

    public TextEditor? TextEditor
    {
        get;
        set;
    }

    private void InitSettings()
    {
        CharBoundsSettings.StrokeThickness = 1;
        CharBoundsSettings.StrokeColor = "#A05F9EA0";

        CharSpanBoundsSettings.StrokeThickness = 1;
        CharSpanBoundsSettings.StrokeColor = "#A0FF0000";

        LineContentBoundsSettings.StrokeColor = "#5000FF00";
        LineContentBoundsSettings.StrokeThickness = 1;
    }

    private void ResetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        InitSettings();
    }

    private void UpdateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Update();
    }

    public void Update()
    {
        if (TextEditor is null)
        {
            return;
        }

        SkiaTextEditorDebugConfiguration skiaTextEditorDebugConfiguration = TextEditor.SkiaTextEditor.DebugConfiguration;

        if (!skiaTextEditorDebugConfiguration.IsInDebugMode)
        {
            return;
        }

        skiaTextEditorDebugConfiguration.DebugDrawCharBoundsInfo = ToDrawInfo(CharBoundsSettings);
        skiaTextEditorDebugConfiguration.DebugDrawCharSpanBoundsInfo = ToDrawInfo(CharSpanBoundsSettings);
        skiaTextEditorDebugConfiguration.DebugDrawLineContentBoundsInfo = ToDrawInfo(LineContentBoundsSettings);

        skiaTextEditorDebugConfiguration.DebugReRender();
    }

    private static TextEditorDebugBoundsDrawInfo? ToDrawInfo(FillAndStrokeSettings settings)
    {
        if (!settings.IsSettingEnable)
        {
            return null;
        }

        var info = new TextEditorDebugBoundsDrawInfo
        {
            StrokeThickness = (float) settings.StrokeThickness
        };

        var strokeColor = ColorToHexConverter.ParseHexString(settings.StrokeColor, AlphaComponentPosition.Leading);

        if (strokeColor is not null)
        {
            info.StrokeColor = strokeColor.Value.ToSKColor();
        }
        var fillColor = ColorToHexConverter.ParseHexString(settings.FillColor, AlphaComponentPosition.Leading);
        if (fillColor is not null)
        {
            info.FillColor = fillColor.Value.ToSKColor();
        }
        return info;
    }
}