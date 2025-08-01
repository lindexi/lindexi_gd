using System.Collections;
using System.Collections.Generic;

using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 渐变色刻度集合
/// </summary>
public class SkiaTextGradientStopCollection : IReadOnlyList<SkiaTextGradientStop>
{
    /// <summary>
    /// 创建渐变色刻度集合
    /// </summary>
    public SkiaTextGradientStopCollection(IReadOnlyList<SkiaTextGradientStop> collection)
    {
        _collection = collection;
    }

    private readonly IReadOnlyList<SkiaTextGradientStop> _collection;

    private SKColor[]? _cacheColorList;
    private float[]? _cacheOffsetList;

    /// <summary>
    /// 获取缓存的列表。立刻拿就应该立刻用，禁止存放
    /// </summary>
    /// <param name="opacity"></param>
    /// <returns></returns>
    internal (SKColor[] ColorList, float[] OffsetList) GetCacheList(double opacity)
    {
        _cacheColorList ??= new SKColor[Count];
        _cacheOffsetList ??= new float[Count];

        SKColor[] colorList = _cacheColorList;
        float[] offsetList = _cacheOffsetList;

        for (var i = 0; i < Count; i++)
        {
            var (skColor, offset) = this[i];
            colorList[i] = skColor.WithAlpha((byte) (skColor.Alpha * opacity));
            offsetList[i] = offset;
        }

        return (colorList, offsetList);
    }

    IEnumerator<SkiaTextGradientStop> IEnumerable<SkiaTextGradientStop>.GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable) _collection).GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => _collection.Count;

    /// <inheritdoc />
    public SkiaTextGradientStop this[int index] => _collection[index];
}