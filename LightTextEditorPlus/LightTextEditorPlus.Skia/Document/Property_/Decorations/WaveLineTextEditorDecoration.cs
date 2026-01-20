using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Primitive;

using System;
using System.Collections.Generic;
using LightTextEditorPlus.Utils;
using SkiaSharp;

using Point = LightTextEditorPlus.Core.Primitive.TextPoint;

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 文本波浪线装饰
/// </summary>
public class WaveLineTextEditorDecoration() : TextEditorDecoration(TextEditorDecorationLocation.Underline)
{
    /// <summary>
    /// 文本波浪线装饰
    /// </summary>
    public static WaveLineTextEditorDecoration Instance { get; } = new WaveLineTextEditorDecoration();

    /// <inheritdoc />
    public override BuildDecorationResult BuildDecoration(in BuildDecorationArgument argument)
    {
        SkiaTextBrush foregroundBrush = argument.RunProperty.Foreground;
        //var foreground = foregroundBrush.AsSolidColor();
        if (argument.TextEditor.TextEditorCore.ArrangingType.IsHorizontal)
        {
            var bounds = argument.RecommendedBounds;
            var waveLine = new WaveLine()
            {
                WaveHeight = bounds.Height,
                WaveLength = bounds.Height * 2,
                BorderThickness = bounds.Height / 5
            };

            var halfHeight = bounds.Height / 2;
            var startPoint = new TextPoint(bounds.Left, bounds.Y + halfHeight);
            var endPoint = new TextPoint(bounds.Right, bounds.Y + halfHeight);

            SKPaint paint = argument.CachePaint;
            foregroundBrush.Apply(new SkiaTextBrushRenderContext(paint, argument.Canvas, bounds.ToSKRect(),
                argument.RunProperty.Opacity));
            paint.Style = SKPaintStyle.Stroke;
            
            waveLine.DrawWaveLine(startPoint, endPoint, argument.Canvas, paint);
        }
        else
        {
            // 竖排还没支持
        }

        return new BuildDecorationResult()
        {
            TakeCharCount = argument.CharDataList.Count,
        };
    }
}

file struct WaveLine()
{
    /// <summary>
    /// 线条宽度
    /// </summary>
    public double BorderThickness { get; set; } = 6.0;

    public double WaveLength { get; set; } = 3;
    public double WaveHeight { get; set; } = 40;

    public double CurveSquaring { get; set; } = 0.75;

    public void DrawWaveLine(Point startPoint, Point endPoint, SKCanvas canvas, SKPaint paint)
    {
        // 方法请看 https://blog.lindexi.com/post/WPF-%E5%A6%82%E4%BD%95%E7%BB%99%E5%AE%9A%E4%B8%A4%E4%B8%AA%E7%82%B9%E7%94%BB%E5%87%BA%E4%B8%80%E6%9D%A1%E6%B3%A2%E6%B5%AA%E7%BA%BF.html
        var p1 = startPoint;
        var p2 = endPoint;


        var distance = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)); //(p1 - p2).Length;

        var angle = CalculateAngle(p1, p2);
        var waveLength = WaveLength;
        var waveHeight = WaveHeight;
        var howManyWaves = distance / waveLength;
        //var waveInterval = distance / howManyWaves;
        var waveInterval = waveLength;
        var maxBcpLength =
            Math.Sqrt(waveInterval / 4.0 * (waveInterval / 4.0) + waveHeight / 2.0 * (waveHeight / 2.0));

        var curveSquaring = CurveSquaring;
        var bcpLength = maxBcpLength * curveSquaring;
        var bcpInclination = CalculateAngle(new Point(0, 0), new Point(waveInterval / 4.0, waveHeight / 2.0));

        var wigglePoints = new List<(Point bcpOut, Point bcpIn, Point anchor)>();
        var prevFlexPt = p1;
        var polarity = 1;

        for (var waveIndex = 0; waveIndex < howManyWaves * 2; waveIndex++)
        {
            var bcpOutAngle = angle + bcpInclination * polarity;
            var bcpOut = new Point(prevFlexPt.X + Math.Cos(bcpOutAngle) * bcpLength,
                prevFlexPt.Y + Math.Sin(bcpOutAngle) * bcpLength);
            var flexPt = new Point(prevFlexPt.X + Math.Cos(angle) * waveInterval / 2.0,
                prevFlexPt.Y + Math.Sin(angle) * waveInterval / 2.0);
            var bcpInAngle = angle + (Math.PI - bcpInclination) * polarity;
            var bcpIn = new Point(flexPt.X + Math.Cos(bcpInAngle) * bcpLength,
                flexPt.Y + Math.Sin(bcpInAngle) * bcpLength);

            wigglePoints.Add((bcpOut, bcpIn, flexPt));

            polarity *= -1;
            prevFlexPt = flexPt;
        }

        using SKPath skPath = new SKPath();

        skPath.MoveTo(wigglePoints[0].anchor.ToSKPoint());
        for (var i = 1; i < wigglePoints.Count; i += 1)
        {
            var (bcpOut, bcpIn, anchor) = wigglePoints[i];
            skPath.CubicTo(bcpOut.ToSKPoint(), bcpIn.ToSKPoint(), anchor.ToSKPoint());
        }

        canvas.DrawPath(skPath, paint);
        
        //{
        //    streamGeometryContext.BeginFigure(wigglePoints[0].anchor, true, false);

        //    for (var i = 1; i < wigglePoints.Count; i += 1)
        //    {
        //        var (bcpOut, bcpIn, anchor) = wigglePoints[i];

        //        streamGeometryContext.BezierTo(bcpOut, bcpIn, anchor, true, false);
        //    }
        //}

        //return streamGeometry;
    }

    private static double CalculateAngle(Point p1, Point p2)
    {
        return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
    }
}