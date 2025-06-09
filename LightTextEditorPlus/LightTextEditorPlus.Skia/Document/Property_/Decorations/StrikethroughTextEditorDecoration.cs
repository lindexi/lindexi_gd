using System.Windows;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;
using SkiaSharp;

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 文本删除线装饰
/// </summary>
/// 删除线不叫 DeleteLine 哦
public class StrikethroughTextEditorDecoration() : TextEditorDecoration(TextEditorDecorationLocation.Strikethrough)
{
    /// <summary>
    /// 文本删除线装饰
    /// </summary>
    public static StrikethroughTextEditorDecoration Instance { get; } = new StrikethroughTextEditorDecoration();

    /// <inheritdoc />
    public override BuildDecorationResult BuildDecoration(in BuildDecorationArgument argument)
    {
        var foregroundBrush = argument.RunProperty.Foreground;
        SKColor foreground = foregroundBrush;

        // 文本的下划线应该取当前行的最大字符所在的底部，而不是每个字符自己的底部。取最大字符的才能连接到一起
        if (argument.ArrangingType.IsHorizontal)
        {
            // todo 后续上下标需要在这里也进行处理
            TextRect bounds = GetDecorationLocationRecommendedBounds(TextDecorationLocation, argument.LineRenderInfo.CharList, argument.LineRenderInfo, argument.TextEditor);
            bounds = bounds with
            {
                X = argument.RecommendedBounds.X,
                Width = argument.RecommendedBounds.Width
            };

            var thickness = bounds.Height / 2; // 删除线的粗细

            var y = bounds.Top;
            var startPoint = new TextPoint(bounds.Left, y);
            var endPoint = new TextPoint(bounds.Right, y);

            SKPaint paint = argument.CachePaint;
            paint.StrokeWidth = (float) thickness;
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = foreground;

            argument.Canvas.DrawLine(startPoint.ToSKPoint(), endPoint.ToSKPoint(), paint);
        }

        return new BuildDecorationResult()
        {
            TakeCharCount = argument.CharDataList.Count
        };
    }
}