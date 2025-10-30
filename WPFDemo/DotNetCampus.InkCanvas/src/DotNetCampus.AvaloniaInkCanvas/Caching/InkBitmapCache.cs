using DotNetCampus.Inking.Contexts;
using DotNetCampus.Logging;
using DotNetCampus.Numerics;
using DotNetCampus.Numerics.Geometry;

using SkiaSharp;
using System.Runtime.CompilerServices;
using DotNetCampus.Inking.Utils;

namespace DotNetCampus.Inking.Caching;

/// <summary>
/// 辅助绘制笔迹的位图缓存。
/// </summary>
internal sealed class InkBitmapCache : IDisposable
{
    private readonly object _lock = new();

    private readonly AvaSkiaInkCanvasSettings _settings;
    private readonly int _uiThreadId;
    private int _renderThreadId;
    private bool _useCacheOnNextRender;
    private InkBitmapCacheContext? _cacheContext;

    /// <summary>
    /// 笔迹缓存数据。
    /// </summary>
    /// <remarks>
    /// isValid 由 UI 线程写入，渲染线程读取。<br/>
    /// CachedData 由渲染线程写入，渲染线程读取。
    /// </remarks>
    private volatile ThreadSafeInkBitmapCachedData _threadSafeCachedData = new(false, null);

    /// <summary>
    /// 笔迹缓存上下文信息。
    /// </summary>
    /// <remarks>
    /// 此字段由 UI 线程写入，由渲染线程读取。
    /// </remarks>
    private InkBitmapCacheContext? CacheContext
    {
        get => _cacheContext.VerifyOnRenderThread(_renderThreadId);
        set => _cacheContext = value.VerifyOnUIThread(_uiThreadId);
    }

    /// <summary>
    /// 将此值设置为 <see langword="true"/> 以在下次绘制时使用位图缓存；反之，设置为 <see langword="false"/> 将在下帧不使用位图缓存。
    /// </summary>
    /// <remarks>
    /// 如果希望立即使用位图缓存，请在将此值设置为 <see langword="true"/> 后立即调用 <see cref="Avalonia.Controls.Control.InvalidateVisual"/> 方法刷新渲染。
    /// </remarks>
    public bool UseCacheOnNextRender
    {
        get => _useCacheOnNextRender;
        set
        {
            if (Equals(_useCacheOnNextRender, value))
            {
                return;
            }

            _useCacheOnNextRender = value.VerifyOnUIThread(_uiThreadId);
            Log.Info($"[Ink][InkBitmapCache] 将在下一帧{(value ? "开启" : "关闭")}笔迹位图缓存。");
            InvalidateCache();
        }
    }

    /// <summary>
    /// 创建 <see cref="InkBitmapCache"/> 的新实例。
    /// </summary>
    /// <param name="settings">包含位图缓存的设置。</param>
    public InkBitmapCache(AvaSkiaInkCanvasSettings settings)
    {
        _uiThreadId = Environment.CurrentManagedThreadId;
        _settings = settings;
    }

    /// <summary>
    /// 更新缓存所需的上下文信息，包括 DPI（画板到屏幕的缩放）和元素变换（元素到画板的变换矩阵）。
    /// </summary>
    /// <param name="dpi">画板相对于屏幕物理像素的缩放比例。</param>
    /// <param name="visibleBounds">画板可见区域的边界（画板坐标系）。</param>
    /// <param name="transformFromInkToRoot">笔迹变换到画板应使用的变换矩阵。</param>
    public void UpdateCacheContext(double dpi, BoundingBox2D visibleBounds, SimilarityTransformation2D transformFromInkToRoot)
    {
        this.VerifyOnUIThread(_uiThreadId);
        CacheContext = (dpi, visibleBounds, transformFromInkToRoot);
        InvalidateCache();
    }

    public void Dispose() => InvalidateCache();

    /// <summary>
    /// 使缓存失效，如果下次绘制时缓存启用（<see cref="UseCacheOnNextRender"/> 为 <see langword="true"/>），则会重新生成缓存。
    /// </summary>
    /// <remarks>
    /// 注意，虽然使用此方法可以使缓存失效，以便下次绘制时会使用缓存；但如果不调用 <see cref="UpdateCacheContext"/> 方法更新上下文信息，则生成的新缓存依旧会使用旧的信息。<br/>
    /// 在此情况下，生成的新缓存图片在参数（清晰度、旋转方向、缩放等）上会跟旧缓存图片完全相同，只是新增和擦除的笔迹会在新的缓存图片上体现出来。<br/>
    /// 如果你希望按照新的笔迹旋转方向、缩放等生成匹配此时应有清晰度的缓存图片，你应该调用 <see cref="UpdateCacheContext"/> 而不是本方法。那个方法本质上也会使缓存失效的。
    /// </remarks>
    internal void InvalidateCache()
    {
        this.VerifyOnUIThread(_uiThreadId);
        lock (_lock)
        {
            var (_, data) = _threadSafeCachedData;
            _threadSafeCachedData = new ThreadSafeInkBitmapCachedData(false, data);
        }
    }

    /// <summary>
    /// 以位图缓存的形式画出笔迹。
    /// </summary>
    /// <param name="paths">笔迹的路径。</param>
    /// <param name="canvas">要绘制到的画布。</param>
    /// <param name="skPaint">用于绘制的画笔。</param>
    /// <exception cref="InvalidOperationException">在使用绘制的路径前，请先调用 <see cref="UpdateCacheContext"/> 方法。</exception>
    internal void DrawBitmap(in ReadOnlySpan<SkiaStrokeDrawContext> paths, SKCanvas canvas, SKPaint skPaint)
    {
        _renderThreadId = Environment.CurrentManagedThreadId;

        if (paths.Length is 0 || paths.HasNoPoints())
        {
            // 没有任何路径，不需要绘制。
            // 所有的路径都没有点，无法被绘制。
            return;
        }

        var context = CacheContext ?? throw new InvalidOperationException("在使用绘制的路径前，请先调用 UpdateCacheContext 方法。");

        var threadSafeCachedData = _threadSafeCachedData;
        var (isValid, cachedData) = threadSafeCachedData;
        if (!isValid || cachedData is not { } data)
        {
            cachedData?.Dispose();
            // 如果缓存已失效，则重新生成缓存。
            data = CreateBitmapData(paths, skPaint, context, _settings);
            lock (_lock)
            {
                _threadSafeCachedData = new ThreadSafeInkBitmapCachedData(true, data);
            }
        }

        DrawBitmapData(canvas, context, data);
    }

    /// <summary>
    /// 创建笔迹的位图缓存。
    /// </summary>
    /// <param name="paths">笔迹的路径。</param>
    /// <param name="skPaint">用于绘制的画笔。</param>
    /// <param name="context">缓存所用的上下文信息。</param>
    /// <param name="settings">位图缓存的设置。</param>
    /// <returns>缓存的位图数据。</returns>
    private static InkBitmapCachedData CreateBitmapData(in ReadOnlySpan<SkiaStrokeDrawContext> paths, SKPaint skPaint, in InkBitmapCacheContext context, AvaSkiaInkCanvasSettings settings)
    {
        // 将原始的笔迹数据（元素坐标系）转换为画板坐标系，并求取其边界。
        var inkBounds = paths.GetTransformedBounds(context.TransformFromInkToRoot);
        // 生成一个伪的可视区域，使其有画板区域的 41 倍大，以便生成全景笔迹图作为背景位图（将镂空前景位图）。放心最终不会有这么大的，一来会根据实际笔迹区域裁剪，二来会根据 settings.MaxBitmapCacheSize 限制位图大小。
        var backVisibleBounds = context.VisibleBounds.Inflate(context.VisibleBounds.Width * 20, context.VisibleBounds.Height * 20);
        // 生成一个真实的可视区域，使其在画板的周围扩大 100 个单位，以便生成高清前景位图。
        var foreVisibleBounds = context.VisibleBounds.Inflate(100);

        // 如果笔迹区域比可视区域小，那么全景笔迹图就是高清前景图。不再需要背景图了。
        var isForeBitmapEnough = foreVisibleBounds.Contains(inkBounds);

        // 创建低清背景位图，使其显示全景笔迹。
        var backBitmap = isForeBitmapEnough
            ? null
            : CreateBitmapCore(paths, skPaint, context, settings, inkBounds, backVisibleBounds, foreVisibleBounds);

        // 创建高清可见区域位图，使其高质量显示局部笔迹。
        var foreBitmap = CreateBitmapCore(paths, skPaint, context, settings, inkBounds, foreVisibleBounds);

        // 记录日志并返回数据。
        var data = new InkBitmapCachedData(backBitmap, foreBitmap);
        LogBitmapCached(data, inkBounds);
        return data;
    }

    /// <summary>
    /// 创建一张位图，使其显示笔迹的一部分。
    /// </summary>
    /// <param name="paths">要画的笔迹的路径。</param>
    /// <param name="skPaint">用于绘制的画笔。</param>
    /// <param name="context">位图缓存所用的上下文信息。</param>
    /// <param name="settings">位图缓存的设置。</param>
    /// <param name="inkBounds">所有笔迹的边界（画板坐标系）。</param>
    /// <param name="visibleBounds">画板的可视区域（画板坐标系）。</param>
    /// <param name="clipBounds">如果需要镂空一个区域，则传入此区域的边界（画板坐标系）。</param>
    /// <returns>创建的位图数据。</returns>
    /// <remarks>
    /// 如果笔迹完全移出了可视区域，则不会创建位图，返回 <see langword="null"/>。<br/>
    /// </remarks>
    private static InkQualityBitmapData? CreateBitmapCore(
        in ReadOnlySpan<SkiaStrokeDrawContext> paths, SKPaint skPaint,
        in InkBitmapCacheContext context, AvaSkiaInkCanvasSettings settings,
        BoundingBox2D inkBounds, BoundingBox2D visibleBounds, BoundingBox2D? clipBounds = null)
    {
        // 笔迹和可视区域的交集。使用此交集可以避免笔迹区域没那么大时生成过大的位图。
        var intersectedBounds = inkBounds.Intersect(visibleBounds);
        if (clipBounds is { } cb1)
        {
            intersectedBounds = intersectedBounds.Exclude(cb1);
        }
        if (intersectedBounds.IsEmpty)
        {
            // 如果交集为空，则不需要绘制。
            return null;
        }
        // 根据用户的设置，决定要显示多清晰的一张位图缓存。
        var (bitmapWidth, bitmapHeight, scalingRootToBitmap, quality) =
            CalculateBestBitmapScaling(intersectedBounds.Width, intersectedBounds.Height, context.DpiScaling, settings.MaxBitmapCacheSize);

        // 创建位图。
        var bitmap = new SKBitmap(bitmapWidth, bitmapHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);

        // 将位图坐标系转换为笔迹坐标系，这样后面画笔迹时可以直接使用其数据。
        canvas.Scale((float) scalingRootToBitmap, (float) scalingRootToBitmap);
        canvas.Translate(-(float) intersectedBounds.MinX, -(float) intersectedBounds.MinY);
        if (clipBounds is { } cb2)
        {
            // 如果有镂空需求，则镂空一个区域（画板坐标系）。
            // 当我们期望用一个全景背景图和高清前景图拼接时，背景图就需要将前景图的区域镂空。
            canvas.ClipRect(cb2, SKClipOperation.Difference);
        }
        var skMatrix = context.TransformFromInkToRoot.ToSkMatrix();
        canvas.Concat(ref skMatrix);

        // 使用笔迹坐标系绘制笔迹。
        foreach (var c in paths)
        {
            skPaint.Color = c.Color;
            canvas.DrawPath(c.Path, skPaint);
        }

        // 返回位图数据。
        return new(bitmap, intersectedBounds, scalingRootToBitmap, quality);
    }

    /// <summary>
    /// 画出参数 <paramref name="data"/> 中指定的多张缓存位图。
    /// </summary>
    /// <param name="canvas">要绘制到的画布。</param>
    /// <param name="context">缓存所用的上下文信息。</param>
    /// <param name="data">缓存的位图数据。</param>
    private static void DrawBitmapData(SKCanvas canvas, in InkBitmapCacheContext context, in InkBitmapCachedData data)
    {
        // 绘制镂空的笔迹全景图。
        if (data.BackBitmap is { } backBitmap)
        {
            DrawBitmapCore(canvas, in context, in backBitmap);
        }

        // 绘制高清的笔迹局部图。
        if (data.ForeBitmap is { } foreBitmap)
        {
            DrawBitmapCore(canvas, in context, in foreBitmap);
        }
    }

    /// <summary>
    /// 画出参数 <paramref name="data"/> 中指定的单张缓存位图。
    /// </summary>
    /// <param name="canvas">要绘制到的画布。</param>
    /// <param name="context">缓存所用的上下文信息。</param>
    /// <param name="data">缓存的位图数据。</param>
    private static void DrawBitmapCore(SKCanvas canvas, in InkBitmapCacheContext context, in InkQualityBitmapData data)
    {
        var numericMatrix = canvas.TotalMatrix.ToSimilarityTransformation();
        var transform = SimilarityTransformation2D.Identity
            .Scale(1 / data.ScalingRootToBitmap)
            .Translate(new Vector2D(data.InkBounds.MinX, data.InkBounds.MinY))
            .Apply(context.TransformFromInkToRoot.Inverse())
            .Apply(numericMatrix);

        canvas.Save();
        canvas.SetMatrix(transform.ToSkMatrix());
        try
        {
            using var paint = new SKPaint();
            paint.IsAntialias = true;
            paint.FilterQuality = SKFilterQuality.High;
            canvas.DrawBitmap(data.Bitmap, 0, 0, paint);
        }
        finally
        {
            canvas.Restore();
        }
    }

    /// <summary>
    /// 计算最佳的位图缩放比例，以便在显示时不会产生过大的位图。
    /// </summary>
    /// <param name="inkWidth">笔迹在笔迹元素坐标系中的宽度。</param>
    /// <param name="inkHeight">笔迹在笔迹元素坐标系中的高度。</param>
    /// <param name="dpiScaling">画板到屏幕像素的缩放比例。</param>
    /// <param name="maxBitmapPixelCount">缓存位图的最大像素数（避免过大导致性能问题）。</param>
    /// <returns>
    /// 在最佳缩放比例下的：<br/>
    /// BitmapWidth：位图的宽度（像素）。<br/>
    /// BitmapHeight：位图的高度（像素）。<br/>
    /// Quality：位图的清晰度，即位图的像素数与最大像素数的比例的平方根。<br/>
    /// ScalingInkToBitmap：笔迹/元素坐标系内的长度乘以此值，可以得到位图缓存中的像素长度。
    /// </returns>
    private static (int BitmapWidth, int BitmapHeight, double ScalingRootToBitmap, double Quality) CalculateBestBitmapScaling(
        double inkWidth, double inkHeight, double dpiScaling, int maxBitmapPixelCount)
    {
        // 为防止计算溢出（当宽高足够大时），下面的像素数计算我们尽量使用 double 类型。
        var desiredBitmapPixelWidth = (int) Math.Round(inkWidth * dpiScaling);
        var desiredBitmapPixelHeight = (int) Math.Round(inkHeight * dpiScaling);
        var totalBitmapPixelCount = desiredBitmapPixelWidth * (double) desiredBitmapPixelHeight;

        // 如果原尺寸显示笔迹也不会产生太大的位图，则直接使用原尺寸。
        if (totalBitmapPixelCount <= maxBitmapPixelCount)
        {
            return (desiredBitmapPixelWidth, desiredBitmapPixelHeight, dpiScaling, 1d);
        }

        // 如果原尺寸显示笔迹会产生过大的位图，则缩小位图，以更低清晰度来显示。
        var quality = Math.Sqrt(maxBitmapPixelCount / totalBitmapPixelCount);
        return (
            (int) Math.Round(desiredBitmapPixelWidth * quality),
            (int) Math.Round(desiredBitmapPixelHeight * quality),
            dpiScaling * quality,
            quality);
    }

    /// <summary>
    /// 记录一条日志，表示笔迹位图缓存已更新。
    /// </summary>
    /// <param name="data"></param>
    /// <param name="inkBounds"></param>
    private static void LogBitmapCached(InkBitmapCachedData data, BoundingBox2D inkBounds) => Log.Info(data switch
    {
        ({ } bb, { } fb) =>
            $"[Ink][InkBitmapCache] 笔迹位图缓存更新。笔迹 {inkBounds.Width}×{inkBounds.Height}，全景图 {bb.Bitmap.Width}×{bb.Bitmap.Height}（清晰度 {bb.Quality}），前景图 {fb.Bitmap.Width}×{fb.Bitmap.Height}（清晰度 {fb.Quality}）",
        ({ } bb, null) =>
            $"[Ink][InkBitmapCache] 笔迹位图缓存更新。笔迹 {inkBounds.Width}×{inkBounds.Height}，全景图 {bb.Bitmap.Width}×{bb.Bitmap.Height}（清晰度 {bb.Quality}）",
        (null, { } fb) =>
            $"[Ink][InkBitmapCache] 笔迹位图缓存更新。笔迹 {inkBounds.Width}×{inkBounds.Height}，前景图 {fb.Bitmap.Width}×{fb.Bitmap.Height}（清晰度 {fb.Quality}）",
        _ =>
            $"[Ink][InkBitmapCache] 笔迹位图缓存无需更新。笔迹 {inkBounds.Width}×{inkBounds.Height}，不在可视区域内。",
    });

    private record ThreadSafeInkBitmapCachedData(bool IsValid, InkBitmapCachedData? CachedData);
}

/// <summary>
/// 包含一些用于 <see cref="InkBitmapCache"/> 的扩展方法。
/// </summary>
file static class Extensions
{
    internal static T VerifyOnUIThread<T>(this T field, int threadId, [CallerMemberName] string callerMemberName = "")
    {
        if (Environment.CurrentManagedThreadId != threadId)
        {
            throw new InvalidOperationException($"成员 {callerMemberName} 只能在 UI 线程访问。");
        }
        return field;
    }

    internal static T VerifyOnRenderThread<T>(this T field, int threadId, [CallerMemberName] string callerMemberName = "")
    {
        if (Environment.CurrentManagedThreadId != threadId)
        {
            throw new InvalidOperationException($"成员 {callerMemberName} 只能在渲染线程访问。");
        }
        return field;
    }

    internal static BoundingBox2D GetTransformedBounds(this in ReadOnlySpan<SkiaStrokeDrawContext> paths, SimilarityTransformation2D transform)
    {
        var bounds = paths[0].Path.GetTransformedBounds(transform);
        for (var i = 1; i < paths.Length; i++)
        {
            var path = paths[i].Path;
            if (path.Points.Length > 0)
            {
                bounds.Union(path.GetTransformedBounds(transform));
            }
        }
        return BoundingBox2D.Create(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
    }

    private static SKRect GetTransformedBounds(this SKPath path, SimilarityTransformation2D transform)
    {
        using var newPath = new SKPath(path);
        newPath.Transform(transform.ToSkMatrix());
        return newPath.Bounds;
    }

    internal static void ClipRect(this SKCanvas canvas, BoundingBox2D clipBounds, SKClipOperation operation = SKClipOperation.Intersect)
    {
        canvas.ClipRect(new SKRect(
            (float) clipBounds.MinX,
            (float) clipBounds.MinY,
            (float) clipBounds.MaxX,
            (float) clipBounds.MaxY
        ), operation);
    }

    /// <summary>
    /// 已知一个矩形边界，排除另一个矩形边界；返回排除后异形图形的边界。
    /// </summary>
    /// <param name="bounds">原始边界。</param>
    /// <param name="excludeBounds">要排除掉的边界。</param>
    /// <returns>排除后的边界。</returns>
    internal static BoundingBox2D Exclude(this BoundingBox2D bounds, BoundingBox2D excludeBounds)
    {
        var includeTopLeft = excludeBounds.Contains(new Point2D(bounds.MinX, bounds.MinY));
        var includeTopRight = excludeBounds.Contains(new Point2D(bounds.MaxX, bounds.MinY));
        var includeBottomRight = excludeBounds.Contains(new Point2D(bounds.MaxX, bounds.MaxY));
        var includeBottomLeft = excludeBounds.Contains(new Point2D(bounds.MinX, bounds.MaxY));

        if (includeTopLeft && includeTopRight && includeBottomRight && includeBottomLeft)
        {
            return BoundingBox2D.Empty;
        }

        if (includeTopLeft && includeBottomLeft)
        {
            return BoundingBox2D.Create(excludeBounds.MaxX, bounds.MinY, bounds.MaxX, bounds.MaxY);
        }

        if (includeTopLeft && includeTopRight)
        {
            return BoundingBox2D.Create(bounds.MinX, excludeBounds.MaxY, bounds.MaxX, bounds.MaxY);
        }

        if (includeTopRight && includeBottomRight)
        {
            return BoundingBox2D.Create(bounds.MinX, bounds.MinY, excludeBounds.MinX, bounds.MaxY);
        }

        if (includeBottomRight && includeBottomLeft)
        {
            return BoundingBox2D.Create(bounds.MinX, bounds.MinY, bounds.MaxX, excludeBounds.MinY);
        }

        return bounds;
    }

    /// <summary>
    /// 判断所有路径是否都没有点。（这种路径没有宽高，无法被绘制。）
    /// </summary>
    /// <param name="paths">要检查的路径。</param>
    /// <returns>如果所有路径都没有点，则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool HasNoPoints(this in ReadOnlySpan<SkiaStrokeDrawContext> paths)
    {
        var allZeroPoints = true;
        for (var i = 0; i < paths.Length; i++)
        {
            var path = paths[i];
            if (path.Path.Points.Length is not 0)
            {
                allZeroPoints = false;
                break;
            }
        }
        return allZeroPoints;
    }
}
