﻿using Avalonia.Controls;
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

        DocumentRenderBoundsSettings.StrokeColor = "#FF565656";
        DocumentRenderBoundsSettings.StrokeThickness = 0;// 默认不给显示

        DocumentContentBoundsSettings.FillColor = "#56CA2AAA";
        DocumentOutlineBoundsSettings.FillColor = "#506C0CCC";
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
        skiaTextEditorDebugConfiguration.DebugDrawLineOutlineBoundsInfo = ToDrawInfo(LineOutlineBoundsSettings);
        skiaTextEditorDebugConfiguration.DebugDrawParagraphContentBoundsInfo = ToDrawInfo(ParagraphContentBoundsSettings);
        skiaTextEditorDebugConfiguration.DebugDrawParagraphOutlineBoundsInfo = ToDrawInfo(ParagraphOutlineBoundsSettings);
        skiaTextEditorDebugConfiguration.DebugDrawDocumentRenderBoundsInfo = ToDrawInfo(DocumentRenderBoundsSettings);
        skiaTextEditorDebugConfiguration.DebugDrawDocumentContentBoundsInfo = ToDrawInfo(DocumentContentBoundsSettings);
        skiaTextEditorDebugConfiguration.DebugDrawDocumentOutlineBoundsInfo = ToDrawInfo(DocumentOutlineBoundsSettings);

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

        var strokeColor = HexTextToColorConverter.ParseHexString(settings.StrokeColor);

        if (strokeColor is not null)
        {
            info.StrokeColor = strokeColor.Value.ToSKColor();
        }
        var fillColor = HexTextToColorConverter.ParseHexString(settings.FillColor);
        if (fillColor is not null)
        {
            info.FillColor = fillColor.Value.ToSKColor();
        }
        return info;
    }
}