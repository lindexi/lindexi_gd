using System.Windows;
using System.Windows.Media;
using LightTextEditorPlus.Core.Primitive;

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
        ImmutableBrush foregroundBrush = argument.RunProperty.Foreground;
        var foreground = foregroundBrush.Value;
        Drawing? drawing = null;

        // 文本的下划线应该取当前行的最大字符所在的底部，而不是每个字符自己的底部。取最大字符的才能连接到一起
        if (argument.TextEditor.ArrangingType.IsHorizontal)
        {
            // todo 后续上下标需要在这里也进行处理
            TextRect bounds = GetDecorationLocationRecommendedBounds(TextDecorationLocation, argument.LineRenderInfo.CharList, argument.LineRenderInfo, argument.TextEditor);
            bounds = bounds with
            {
                X = argument.RecommendedBounds.X,
                Width = argument.RecommendedBounds.Width
            };

            var thickness = bounds.Height / 2; // 下划线的粗细

            var y = bounds.Top;
            //TextPoint center = bounds.Center;
            var startPoint = new Point(bounds.Left, y);
            var endPoint = new Point(bounds.Right, y);

            drawing = new GeometryDrawing(null, new Pen(foreground, thickness), new LineGeometry(startPoint, endPoint));
        }

        return new BuildDecorationResult()
        {
            Drawing = drawing,
            TakeCharCount = argument.CharDataList.Count
        };
    }
}