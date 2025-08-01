using System;
using System.Windows;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using SkiaSharp;

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 中文着重号装饰
/// </summary>
/// 中文着重号装饰在文本下方
public class ChineseEmphasisDotsTextEditorDecoration : EmphasisDotsTextEditorDecoration
{
}

/// <summary>
/// 日文着重号装饰
/// </summary>
/// 和中文着重号装饰不同的是，日文着重号装饰在文本上方
[Obsolete("还没实现日文的着重号")]
public class JapaneseEmphasisDotsTextEditorDecoration
{
}

/// <summary>
/// 文本着重号装饰
/// </summary>
public class EmphasisDotsTextEditorDecoration() : TextEditorDecoration(TextEditorDecorationLocation.Underline)
{
    /// <summary>
    /// 文本着重号装饰
    /// </summary>
    public static EmphasisDotsTextEditorDecoration Instance { get; } = new EmphasisDotsTextEditorDecoration();

    /// <inheritdoc />
    public override BuildDecorationResult BuildDecoration(in BuildDecorationArgument argument)
    {
        SKColor foregroundBrush = argument.RunProperty.Foreground.AsSolidColor();
        var foreground = foregroundBrush;

        SKCanvas canvas = argument.Canvas;

        // 文本的下划线应该取当前行的最大字符所在的底部，而不是每个字符自己的底部。取最大字符的才能连接到一起
        if (argument.ArrangingType.IsHorizontal)
        {
            CharData firstCharData = argument.CharDataList[0];
            TextRect bounds = firstCharData.GetBounds();
            TextPoint center = bounds.Center;
            var x = center.X;
            var y = bounds.Bottom;

            //ParagraphLineRenderInfo lineRenderInfo = argument.LineRenderInfo;
            //// 在 WPF 里面文本是按照行渲染的，如此可以获得更多的缓存实现逻辑
            //// 而是 GetBounds 等获取的是文本框坐标系的，需要将其转换为行坐标系下
            //y -= lineRenderInfo.Argument.StartPoint.Y;

            // 0.06 是经验值，大概就是 0.1-0.05 之间
            var size = firstCharData.RunProperty.FontSize * 0.06;
            y += size;

            SKPaint skPaint = argument.CachePaint;
            skPaint.Color = foreground;
            skPaint.Style = SKPaintStyle.Fill;

            canvas.DrawCircle((float) x, (float) y, (float) size, skPaint);
        }

        return new BuildDecorationResult()
        {
            TakeCharCount = 1
        };
    }
}