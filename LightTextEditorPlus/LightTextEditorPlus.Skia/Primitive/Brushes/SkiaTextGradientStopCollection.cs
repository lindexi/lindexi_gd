using SkiaSharp;

using System;
using System.Collections;
using System.Collections.Generic;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 渐变色刻度集合
/// </summary>
[CollectionBuilderAttribute(typeof(SkiaTextGradientStopCollectionBuilder), "Create")]
public class SkiaTextGradientStopCollection : IReadOnlyList<SkiaTextGradientStop>
{
    public SkiaTextGradientStopCollection(IReadOnlyList<SkiaTextGradientStop> list)
    {
    }

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

    public IEnumerator<SkiaTextGradientStop> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count { get; set; }

    public SkiaTextGradientStop this[int index]
    {
        get => throw new System.NotImplementedException();
    }
}

static class SkiaTextGradientStopCollectionBuilder
{
    public static SkiaTextGradientStopCollection Create(IEnumerable<SkiaTextGradientStop> stops)
    {
        if (stops is null)
        {
            throw new ArgumentNullException(nameof(stops));
        }
        return new SkiaTextGradientStopCollection(new List<SkiaTextGradientStop>(stops));
    }
}

[System.AttributeUsage(
    System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Struct,
    Inherited = false)]
file sealed class CollectionBuilderAttribute : Attribute
{
    public Type BuilderType { get; }
    public string MethodName { get; }

    public CollectionBuilderAttribute(Type builderType, string methodName)
    {
        BuilderType = builderType;
        MethodName = methodName;
    }


}