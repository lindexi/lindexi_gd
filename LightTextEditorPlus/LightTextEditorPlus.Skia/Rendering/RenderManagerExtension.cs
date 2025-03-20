using System;
using System.Collections.Generic;
using System.Linq;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering;

public static class RenderManagerExtension
{
    /// <summary>
    /// 获取四线格信息
    /// </summary>
    /// <param name="renderInfoProvider"></param>
    /// <param name="paragraphLineRenderInfo"></param>
    /// <param name="charData">指定字体的四线格。如为空，则计算整行的四线格信息</param>
    /// 详细计算方法请参阅 《Skia 字体信息属性.enbx》 文档
    public static CharHandwritingPaperInfo GetHandwritingPaperInfo(this RenderInfoProvider renderInfoProvider, in ParagraphLineRenderInfo paragraphLineRenderInfo, CharData? charData = null)
    {
        renderInfoProvider.VerifyNotDirty();

        var lineDrawingArgument = paragraphLineRenderInfo.Argument;

        if (renderInfoProvider.TextEditor.IsInDebugMode)
        {
            if (charData is not null)
            {
                if (!lineDrawingArgument.CharList.Contains(charData, ReferenceEqualityComparer.Instance))
                {
                    throw new InvalidOperationException($"传入的 CharData 不在当前的 LineDrawingArgument 里面，不能用于获取四线格信息");
                }
            }
            else
            {
                // 如果一行内存在多个不同的字符属性信息，则要求必定传入 CharData 指定使用哪个字符属性信息
                if (lineDrawingArgument.CharList.GetCharSpanContinuous().Count() > 1)
                {
                    throw new ArgumentException($"一行内存在多个不同的字符属性，必须传入 CharData 指定使用哪个字符属性信息");
                }
            }
        }

        IReadOnlyRunProperty runProperty;
        TextPoint startPoint;
        if (charData is not null)
        {
            runProperty = charData.RunProperty;
            //startPoint = charData.GetStartPoint();
        }
        else
        {
            // 没有指定的情况，则取整行的字符属性
            runProperty =
                paragraphLineRenderInfo.ParagraphStartRunProperty;
            //startPoint = lineDrawingArgument.StartPoint;
        }

        startPoint = lineDrawingArgument.StartPoint;

        var skiaTextRunProperty = runProperty.AsSkiaRunProperty();
        RenderingRunPropertyInfo renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo();

        SKFont skFont = renderingRunPropertyInfo.Font;

        var x = startPoint.X;
        var y = startPoint.Y;

        _ = x;

        // 开始的高度应该是计算行高减去字高的距离
        TextSize lineSize = lineDrawingArgument.LineSize;
        var charHeight = renderingRunPropertyInfo.GetLayoutCharHeight();

        var distance = lineSize.Height - charHeight;
        y += distance;

        SKFontMetrics metrics = skFont.Metrics;
        var ascent = metrics.Ascent;
        var descent = metrics.Descent;
        var leading = metrics.Leading;
        var capHeight = metrics.CapHeight;
        var xHeight = metrics.XHeight;

        _ = leading; // 由于 leading 是渲染的，而这里期望拿到布局的，所以不使用

        var baseline = (-ascent) + y;
        return new CharHandwritingPaperInfo()
        {
            AssociatedTextEditor = renderInfoProvider.TextEditor,

            // 顶部线（Top Line）：这是最上面的一条线。大写字母和一些字母的上半部分会到达这条线。对应拼音的四线三格的第一线
            // CapHeight 表示大写字母的高度，即大写字母的顶部到基线的距离。这个值对于调整字体大小和行高很有用
            TopLineGradation = baseline - capHeight,
            // 中线（Middle Line）：这条线位于顶部线和基线之间。小写字母如“a”、“c”、“e” 等的顶部会达到这条线。对应拼音的四线三格的第二线
            // XHeight 表示小写字母的高度，即小写字母 x 的顶部到基线的距离。它通常用于调整字体大小和行高，以确保小写字母的清晰显示
            MiddleLineGradation = baseline - xHeight,
            // 基线（Baseline）：这是写字的主要参考线，绝大多数字母都会停在这条线上。对应拼音的四线三格的第三线
            BaselineGradation = baseline,
            // 底线（Bottom Line）：这是最下面的一条线，用来指导字母的下方部分，例如“g”、“j”、“p”等。对应拼音的四线三格的第四线
            BottomLineGradation = baseline + descent,
        };
    }
}
