using System;
using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

public readonly record struct GradientSkiaTextBrushRelativePoint
{
    public GradientSkiaTextBrushRelativePoint(float x, float y, GradientSkiaTextBrushRelativePoint.RelativeUnit unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative)
    {
        X = x;
        Y = y;
        Unit = unit;

        if (unit != RelativeUnit.Relative)
        {
            if (X is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(X), $"当 RelativeUnit 为 Relative 时，应该是在 [0-1] 范围内");
            }

            if (Y is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(Y), $"当 RelativeUnit 为 Relative 时，应该是在 [0-1] 范围内");
            }
        }
    }

    public SKPoint ToSKPoint(SKRect bounds)
    {
        return Unit switch
        {
            RelativeUnit.Relative => new SKPoint(bounds.Left + X * bounds.Width, bounds.Top + Y * bounds.Height),
            RelativeUnit.Absolute => new SKPoint(X, Y),
            _ => throw new ArgumentOutOfRangeException(nameof(Unit), Unit, null)
        };
    }

    public enum RelativeUnit : byte
    {
        Relative,
        Absolute,
    }

    public float X { get; init; }
    public float Y { get; init; }
    public GradientSkiaTextBrushRelativePoint.RelativeUnit Unit { get; init; }

    public void Deconstruct(out float X, out float Y, out GradientSkiaTextBrushRelativePoint.RelativeUnit Unit)
    {
        X = this.X;
        Y = this.Y;
        Unit = this.Unit;
    }
}