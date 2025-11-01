using DotNetCampus.Numerics.Geometry;

using SkiaSharp;

namespace DotNetCampus.Inking.Caching;

/// <summary>
/// 为 <see cref="InkBitmapCache"/> 的缓存提供上下文信息。
/// </summary>
/// <param name="DpiScaling">画板到屏幕像素的缩放量。</param>
/// <param name="VisibleBounds">画板可见区域的边界</param>
/// <param name="TransformFromInkToRoot">笔迹变换到画板（要求没有额外变换）的变换矩阵。</param>
internal readonly record struct InkBitmapCacheContext(double DpiScaling, BoundingBox2D VisibleBounds, SimilarityTransformation2D TransformFromInkToRoot)
{
    public static implicit operator InkBitmapCacheContext((double ScalingFromRootToDevice, BoundingBox2D VisibleBounds, SimilarityTransformation2D TransformFromInkToRoot) tuple)
        => new(tuple.ScalingFromRootToDevice, tuple.VisibleBounds, tuple.TransformFromInkToRoot);
}

/// <summary>
/// 为 <see cref="InkBitmapCache"/> 的缓存提供数据。
/// </summary>
/// <param name="BackBitmap">低清背景位图，显示全景笔迹。（如果笔迹本身不大，则此全景笔迹位图是 <see langword="null"/>。）</param>
/// <param name="ForeBitmap">高清可见区域位图，高质量显示局部笔迹。（如果笔迹本身不大，则此高清位图很有可能就是全景笔迹图。如果笔迹完全移出了可视区域，则此高清位图是 <see langword="null"/>。）</param>
internal readonly record struct InkBitmapCachedData(InkQualityBitmapData? BackBitmap, InkQualityBitmapData? ForeBitmap) : IDisposable
{
    public void Dispose()
    {
        BackBitmap?.Dispose();
        ForeBitmap?.Dispose();
    }
}

/// <summary>
/// 为 <see cref="InkBitmapCache"/> 的缓存提供数据。
/// </summary>
/// <param name="Bitmap">位图。</param>
/// <param name="InkBounds">本次缓存的位图所使用的笔迹边界。</param>
/// <param name="ScalingRootToBitmap">画板到位图的缩放量。</param>
/// <param name="Quality">位图的清晰度，即位图的像素数与最大像素数的比例的平方根。</param>
internal readonly record struct InkQualityBitmapData(SKBitmap Bitmap, BoundingBox2D InkBounds, double ScalingRootToBitmap, double Quality) : IDisposable
{
    public void Dispose()
    {
        Bitmap.Dispose();
    }
}