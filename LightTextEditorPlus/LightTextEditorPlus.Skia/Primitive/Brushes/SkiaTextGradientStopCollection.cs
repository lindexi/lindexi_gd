using System.Collections.Generic;
using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

public class SkiaTextGradientStopCollection : List<SkiaTextGradientStop>
{
    internal (SKColor[] ColorList, float[] OffsetList) GetList(double opacity)
    {
        SKColor[] colorList = new SKColor[this.Count];
        float[] offsetList = new float[this.Count];

        for (var i = 0; i < this.Count; i++)
        {
            var (skColor, offset) = this[i];
            colorList[i] = skColor.WithAlpha((byte) (skColor.Alpha * opacity));
            offsetList[i] = offset;
        }

        return (colorList, offsetList);
    }
}