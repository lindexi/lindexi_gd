using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Media;

using LightTextEditorPlus.Configurations;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;

using SkiaSharp;

namespace LightTextEditorPlus.AvaloniaDemo.Views.Controls;

/// <summary>
/// 控制台文本编辑器
/// </summary>
public class ConsoleTextEditor : TextEditor
{
    public ConsoleTextEditor() : base(new Builder())
    {
        base.CaretConfiguration.CaretBrush = Colors.White;
        base.SkiaTextEditor.RenderConfiguration = SkiaTextEditor.RenderConfiguration with
        {
            UseRenderCharByCharMode = true,
            RenderFaceInFrameAlignment = SkiaTextEditorCharRenderFaceInFrameAlignment.Center,
        };

        Loaded += ConsoleTextEditor_Loaded;
    }

    private void ConsoleTextEditor_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        AppendText("123abc中文\r\n中文123abc");
    }
}

file class Builder : AvaloniaSkiaTextEditorPlatformProviderBuilder
{
    public override AvaloniaSkiaTextEditorPlatformProvider Build(TextEditor avaloniaTextEditor)
    {
        return new Provider(avaloniaTextEditor);
    }
}

file class Provider : AvaloniaSkiaTextEditorPlatformProvider
{
    public Provider(TextEditor avaloniaTextEditor) : base(avaloniaTextEditor)
    {
    }

    public override ICharInfoMeasurer GetCharInfoMeasurer()
    {
        return new CharInfoMeasurer();
    }
}

file class CharInfoMeasurer : ICharInfoMeasurer
{
    public void MeasureAndFillSizeOfRun(in FillSizeOfRunArgument argument)
    {
        UpdateLayoutContext updateLayoutContext = argument.UpdateLayoutContext;
        CharData currentCharData = argument.CurrentCharData;

        if (!currentCharData.IsInvalidCharDataInfo)
        {
            // 已有缓存的尺寸，直接返回即可
            return;
        }

        if (!updateLayoutContext.TextEditor.ArrangingType.IsHorizontal)
        {
            throw new NotSupportedException("控制台文本库不支持竖排文本");
        }

        var runProperty = (SkiaTextRunProperty) currentCharData.RunProperty;

        CacheInfo cacheInfo = GetOrCreateCacheInfo(runProperty);

        Span<char> destination = stackalloc char[2];
        Rune rune = currentCharData.CharObject.CodePoint.Rune;
        int length = rune.EncodeToUtf16(destination);
        if (length > 1)
        {
            throw new NotSupportedException($"控制台文本编辑器不支持合写字");
        }

        var c = destination[0];
        var useLatin = false;
        useLatin = Rune.IsNumber(rune);
        useLatin = useLatin || char.IsAsciiLetter(c);

        float charFrameWidth;
        if (useLatin)
        {
            charFrameWidth = cacheInfo.LatinMinWidth;
        }
        else
        {
            charFrameWidth = cacheInfo.EastAsianMinWidth;
        }

        var frameSize = new TextSize(charFrameWidth, cacheInfo.CharHeight);

        float charFaceWidth = MeasureCharWidth(c, cacheInfo.Font);
        //using SKPath skPath = cacheInfo.Font.GetGlyphPath(c);
        //// 这里取字符 b 的效果是不正确的，参阅 181f2a807c986c0146501bb9b66195347a53f7bf 的测试
        //if (charFaceWidth > skPath.Bounds.Width)
        //{
        //    charFaceWidth = skPath.Bounds.Width;
        //}

        var faceSize = new TextSize(charFaceWidth, cacheInfo.CharHeight);

        argument.UpdateLayoutContext.SetCharDataInfo(currentCharData, new CharDataInfo(frameSize, faceSize, cacheInfo.Baseline));
    }

    private CacheInfo GetOrCreateCacheInfo(SkiaTextRunProperty runProperty)
    {
        if (_cacheInfo is not null && _cacheInfo.RunProperty.Equals(runProperty))
        {
            return _cacheInfo;
        }
        _cacheInfo?.Dispose();

        using SKFontStyle skFontStyle = new SKFontStyle(runProperty.FontWeight, runProperty.Stretch, runProperty.FontStyle);
        using SKTypeface typeface = SKFontManager.Default.MatchFamily(runProperty.FontName.UserFontName, skFontStyle);
        SKFont renderSkFont = new SKFont(typeface, (float) runProperty.FontSize);

        float latinMinWidth = 0;
        float eastAsianMinWidth = Measure('十');

        for (int i = 0; i < 10; i++)
        {
            var width = Measure((char) ('0' + i));
            latinMinWidth = Math.Max(latinMinWidth, width);
        }

        for (char i = 'a'; i <= 'z'; i++)
        {
            var width = Measure(i);
            latinMinWidth = Math.Max(latinMinWidth, width);
        }

        for (char i = 'A'; i <= 'Z'; i++)
        {
            var width = Measure(i);
            latinMinWidth = Math.Max(latinMinWidth, width);
        }

        var doubleWidth = latinMinWidth * 2;
        if (eastAsianMinWidth > doubleWidth)
        {
            latinMinWidth = eastAsianMinWidth / 2;
        }
        else if (eastAsianMinWidth < doubleWidth)
        {
            if (latinMinWidth * 1.1 > eastAsianMinWidth)
            {
                // 如果中文字符宽度比英文大不了多少，在 1.1 倍以内。那就扩大英文字符好了
                latinMinWidth = eastAsianMinWidth;
            }
            else
            {
                // 否则就拉大中文字符
                eastAsianMinWidth = doubleWidth;
            }
        }

        float charHeight = GetCharHeight();
        _cacheInfo = new CacheInfo(runProperty, latinMinWidth, eastAsianMinWidth, charHeight, GetBaseline(), renderSkFont);
        return _cacheInfo;

        float Measure(char c) => MeasureCharWidth(c, renderSkFont);

        float GetCharHeight()
        {
            // 详细计算方法请参阅 《Skia 字体信息属性.enbx》 文档
            var enhance = 0f;
            var baseline = GetBaseline();
            var height =/*skFont.Metrics.Leading + 有些字体的 Leading 是不参与排版的，越过的，属于上加。不能将其加入计算 */ baseline + renderSkFont.Metrics.Descent + enhance;
            return height;
        }

        float GetBaseline() => -renderSkFont.Metrics.Ascent;
    }

    private float MeasureCharWidth(char c, SKFont renderSkFont)
    {
        Span<ushort> glyphs = stackalloc ushort[1] { c };
        Span<float> widths = stackalloc float[1];
        Span<SKRect> bounds = stackalloc SKRect[1];
        renderSkFont.GetGlyphWidths(glyphs, widths, bounds);

        return widths[0];
    }

    private CacheInfo? _cacheInfo;

    record CacheInfo
    (
        SkiaTextRunProperty RunProperty,
        float LatinMinWidth,
        float EastAsianMinWidth,
        float CharHeight,
        float Baseline,
        SKFont Font
    ) : IDisposable
    {
        public void Dispose()
        {
            Font.Dispose();
        }
    }
}

