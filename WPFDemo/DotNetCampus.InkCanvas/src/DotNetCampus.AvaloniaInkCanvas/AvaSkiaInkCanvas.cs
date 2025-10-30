using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using DotNetCampus.Inking.Caching;
using DotNetCampus.Inking.Contexts;
using DotNetCampus.Inking.Erasing;
using DotNetCampus.Logging;
using DotNetCampus.Numerics.Geometry;

using SkiaSharp;

using System.Diagnostics;
using DotNetCampus.Inking.Utils;
using UnoInk.Inking.InkCore.Interactives;

using InkTransformContext = (DotNetCampus.Numerics.Geometry.BoundingBox2D VisibleBounds, DotNetCampus.Numerics.Geometry.SimilarityTransformation2D TransformToRoot);

namespace DotNetCampus.Inking;

/// <summary>
/// 为 Avalonia 实现的基于 Skia 的 InkCanvas 笔迹画布，可提供动态和静态笔迹层
/// </summary>
/// 既可以单独用作笔迹绘制的接收输入层执行绘制，也可以作为静态笔迹层的承载
public class AvaSkiaInkCanvas : Control
{
    private readonly InkBitmapCache _cache;
    private InkTransformContext? _inkTransformContext;

    public AvaSkiaInkCanvas()
    {
        _cache = new InkBitmapCache(Context.Settings);

        // 以下是调试代码，用于从文件中读取点列表，绘制到画布上
        // 测试文件要求： 一行一个点，使用逗号分隔，格式为 x,y
        //#if DEBUG
        //        var inkPointList = Path.Join(AppContext.BaseDirectory, "Assets", "Tests", "InkPointList.txt");
        //        if (File.Exists(inkPointList))
        //        {
        //            List<(double x, double y)> pointList = [];
        //            var lines = File.ReadAllLines(inkPointList);
        //            foreach (var line in lines)
        //            {
        //                var point = line.Split(',');
        //                var x = double.Parse(point[0]);
        //                var y = double.Parse(point[1]);
        //                pointList.Add((x, y));
        //            }

        //            var skiaStroke = new SkiaStroke(InkId.NewId())
        //            {
        //                Color = SKColors.Red,
        //                InkThickness = 20,
        //                InkCanvas = this,
        //            };
        //            skiaStroke.AddPoints(pointList.Select(t => new StylusPoint(t.x, t.y)));
        //            skiaStroke.SetAsStatic();
        //            AddStaticStroke(skiaStroke);
        //        }
        //#endif
    }

    /// <summary>
    /// 获取笔迹渲染相关的设置和状态上下文。
    /// </summary>
    internal AvaSkiaInkCanvasContext Context { get; } = new();

    public AvaSkiaInkCanvasSettings Settings => Context.Settings;

    internal void AddChild(Control childControl)
    {
        LogicalChildren.Add(childControl);
        VisualChildren.Add(childControl);
    }

    internal void RemoveChild(Control childControl)
    {
        LogicalChildren.Remove(childControl);
        VisualChildren.Remove(childControl);
    }

    public void WritingStart()
    {
        if (_contextDictionary.Count > 0)
        {
            Log.Warn($"[AvaSkiaInkCanvas][WritingStart] 开始写的时候发现上次书写存在点没有结束 {string.Join(';', _contextDictionary.Keys)}");
            // 兼容性处理，如果上次书写没有结束，那就清空好了
            _contextDictionary.Clear();
        }
    }

    public void WritingDown(InkingModeInputArgs args)
    {
        var dynamicStrokeContext = new DynamicStrokeContext(args, Context.Settings);
        _contextDictionary[args.Id] = dynamicStrokeContext;
        dynamicStrokeContext.Stroke.AddPoint(args.StylusPoint);

        InvalidateVisual();
    }

    public void WritingMove(InkingModeInputArgs args)
    {
        if (_contextDictionary.TryGetValue(args.Id, out var context))
        {
            context.Stroke.AddPoint(args.StylusPoint);
            InvalidateVisual();
        }
    }

    public void WritingUp(InkingModeInputArgs args)
    {
        if (_contextDictionary.Remove(args.Id, out var context))
        {
            context.Stroke.AddPoint(args.StylusPoint);
            //_staticStrokeDictionary[context.Stroke.Id] = context.Stroke;
            context.Stroke.SetAsStatic();
            _staticStrokeList.Add(context.Stroke);

            StrokesCollected?.Invoke(this, new SkiaStrokeCollectionEventArgs(args.Id, context.Stroke));
        }
        InvalidateVisual();
    }

    public event EventHandler<SkiaStrokeCollectionEventArgs>? StrokesCollected;

    public void WritingCompleted()
    {
        Debug.Assert(_contextDictionary.Count == 0, "书写完成时，不应该有未抬起的点");
        _contextDictionary.Clear();
    }

    /// <summary>
    /// 现在正在写的过程中的字典
    /// </summary>
    private readonly Dictionary<int, DynamicStrokeContext> _contextDictionary = [];

#if DEBUG
    private int _count;
    private readonly List<Rect> _list = [];
#endif

    public IReadOnlyList<SkiaStroke> StaticStrokeList => _staticStrokeList;

    /// <summary>
    /// 用作静态笔迹层的笔迹列表
    /// </summary>
    private readonly List<SkiaStroke> _staticStrokeList = [];

    internal void AddStaticStroke(SkiaStroke skiaStroke)
    {
        skiaStroke.EnsureIsStaticStroke();
        _staticStrokeList.Add(skiaStroke);
        skiaStroke.InkCanvas = this;
        InvalidateVisual();
    }

    internal void RemoveStaticStroke(SkiaStroke skiaStroke)
    {
        skiaStroke.EnsureIsStaticStroke();
        _staticStrokeList.Remove(skiaStroke);
        InvalidateVisual();
    }

    internal void AddStaticStrokeWithRenderSynchronizer(SkiaStrokeRenderSynchronizer renderSynchronizer)
    {
        Context.UseBitmapCache(false);
        _renderSynchronizerList.Add(renderSynchronizer);
        _staticStrokeList.AddRange(renderSynchronizer.StrokeList);
        foreach (var skiaStroke in renderSynchronizer.StrokeList)
        {
            skiaStroke.InkCanvas = this;
        }
        InvalidateVisual();

#if DEBUG
        foreach (var skiaStroke in _staticStrokeList)
        {
            Debug.Assert(skiaStroke != null);
        }
#endif

        //#if DEBUG
        //        foreach (var skiaStroke in renderSynchronizer.StrokeList)
        //        {
        //            DumpStrokeData(skiaStroke);
        //        }
        //#endif
    }

    /// <summary>
    /// 调试用，输出笔迹数据到文件，文件放在桌面
    /// </summary>
    /// <param name="skiaStroke"></param>
    private void DumpStrokeData(SkiaStroke skiaStroke)
    {
        var folder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nameof(AvaSkiaInkCanvas));
        Directory.CreateDirectory(folder);
        var fileName = Path.Join(folder, $"{DateTime.Now:yyMMdd_HH-mm-ss} {skiaStroke.Id.Value}.txt");
        using var streamWriter = new StreamWriter(fileName, append: true);
        streamWriter.WriteLine($"Id: {skiaStroke.Id}");
        streamWriter.WriteLine($"Color: {skiaStroke.Color}");
        streamWriter.WriteLine($"InkThickness: {skiaStroke.InkThickness}");
        streamWriter.WriteLine($"Path: {skiaStroke.Path.ToSvgPathData()}");
        streamWriter.WriteLine($"PointCount: {skiaStroke.PointList.Count}");
        streamWriter.WriteLine("PointList: ");
        foreach (var point in skiaStroke.PointList)
        {
            streamWriter.WriteLine($"{point.X},{point.Y}");
        }
    }

    /// <summary>
    /// 渲染用的同步列表
    /// </summary>
    private readonly List<SkiaStrokeRenderSynchronizer> _renderSynchronizerList = [];

    internal void ResetStaticStrokeListByEraserResult(IEnumerable<SkiaStroke> skiaStrokeList)
    {
        _staticStrokeList.Clear();
        _staticStrokeList.AddRange(skiaStrokeList);
        foreach (var skiaStroke in _staticStrokeList)
        {
#if DEBUG
            skiaStroke.EnsureIsStaticStroke();
#endif
            skiaStroke.InkCanvas = this;
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
#if DEBUG
        foreach (var skiaStroke in _staticStrokeList)
        {
            Debug.Assert(skiaStroke != null);
        }
#endif

        foreach (var skiaStroke in _staticStrokeList)
        {
            if (skiaStroke.InkCanvas is null)
            {
                skiaStroke.InkCanvas = this;
            }
        }

#if DEBUG
        _count++;
        var n = Math.Sin(Math.Pow(Math.E * _count, Math.PI));
        var x = Math.Abs(n) * Bounds.Width;
        _count++;
        n = Math.Sin(Math.Pow(Math.E * _count, Math.PI));
        var y = Math.Abs(n) * Bounds.Height;

        _list.Add(new Rect(x, y, 10, 10));
#endif

        UpdateCacheCore();
        var inkCanvasCustomDrawOperation = new InkCanvasCustomDrawOperation(this, _cache);
        context.Custom(inkCanvasCustomDrawOperation);

        if (ElementComposition.GetElementVisual(this) is { } selfVisual)
        {
            Compositor compositor = selfVisual.Compositor;
            CompositionBatch batch = compositor.RequestCompositionBatchCommitAsync();
            batch.Rendered.ContinueWith(_ =>
            {
#if DEBUG // 实际测试不会进入此分支，也就是 Avalonia 的 Render 已经跑完了，但就不知道为什么还没有真的 commit 渲染画面到 DWM 那边，导致 Avalonia 还是落后一个帧。于是 WPF 这边就愉快先消失了，再等一个帧到 Avalonia 显示笔迹出来，表现就是闪烁一下
                if (!inkCanvasCustomDrawOperation.IsFinishRender)
                {
                    // 不再抛出，最小化时，会进入此分支，此时还是预期的
                    //throw new Exception($"笔迹开始通知退出时，还没有完成一次渲染！！！ 仅调试下抛出");
                }
#endif

                var list = inkCanvasCustomDrawOperation.CurrentRenderSynchronizerList;
                foreach (var skiaStrokeRenderSynchronizer in list)
                {
                    skiaStrokeRenderSynchronizer.OnRender();
                }
            });
        }
    }

    /// <summary>
    /// 更新笔迹位图缓存所需的一些上下文信息。
    /// </summary>
    /// <param name="visibleBounds">用户可见区域的边界。根坐标系。</param>
    /// <param name="transformToRoot">笔迹变换到根（通常是画板，要求画板相对于顶级窗口没有额外变换）的变换矩阵。</param>
    public void UpdateInkTransform(BoundingBox2D visibleBounds, SimilarityTransformation2D transformToRoot)
    {
        _inkTransformContext = (visibleBounds, transformToRoot);
    }

    /// <summary>
    /// 更新位图缓存的状态。如果没有开启位图缓存，则会关闭位图缓存。
    /// </summary>
    private void UpdateCacheCore()
    {
        var scale = VisualRoot?.RenderScaling ?? 1;
        if (_cache.UseCacheOnNextRender is false && Context.ShouldUseBitmapCache)
        {
            if (_inkTransformContext is { } inkContext)
            {
                _cache.UpdateCacheContext(scale, inkContext.VisibleBounds, inkContext.TransformToRoot);
            }
            else
            {
                Log.Warn("[Ink][AvaSkiaInkCanvas][UpdateCacheCore] 未设置 InkTransformContext，无法更新缓存。请由笔迹元素调用 UpdateInkTransform 方法更新之。");
            }
            _cache.UseCacheOnNextRender = true;
        }
        else if (_cache.UseCacheOnNextRender && !Context.ShouldUseBitmapCache)
        {
            _cache.UseCacheOnNextRender = false;
        }
    }

    class InkCanvasCustomDrawOperation : ICustomDrawOperation
    {
        private readonly InkBitmapCache _cache;

        public InkCanvasCustomDrawOperation(AvaSkiaInkCanvas inkCanvas, InkBitmapCache cache)
        {
            _cache = cache;
            var contextDictionary = inkCanvas._contextDictionary;
            _list = [];
            _pathList = [];

            foreach (var strokeContext in contextDictionary.Values)
            {
                var stroke = strokeContext.Stroke;

                var skiaStrokeDrawContext = stroke.CreateDrawContext();
                _pathList.Add(skiaStrokeDrawContext);
            }

            foreach (var skiaStroke in inkCanvas._staticStrokeList)
            {
                var skiaStrokeDrawContext = skiaStroke.CreateDrawContext();
                _pathList.Add(skiaStrokeDrawContext);
            }

            foreach (var skiaStrokeDrawContext in _pathList)
            {
                _list.Add(skiaStrokeDrawContext.DrawBounds);
            }

            _currentRenderSynchronizerList = [];
            foreach (var renderSynchronizer in inkCanvas._renderSynchronizerList)
            {
                bool canAdd = renderSynchronizer.StrokeList.All(skiaStroke => inkCanvas._staticStrokeList.Contains(skiaStroke));

                if (canAdd)
                {
                    _currentRenderSynchronizerList.Add(renderSynchronizer);
                }
            }

            foreach (var skiaStrokeRenderSynchronizer in _currentRenderSynchronizerList)
            {
                inkCanvas._renderSynchronizerList.Remove(skiaStrokeRenderSynchronizer);
            }

#if DEBUG
            if (_list.Count == 0)
            {
                _list = inkCanvas._list;
            }
#endif
            if (_list.Count == 0)
            {
                // 如果没有笔迹，那就不需要绘制
                // 设置 Bounds 为 0 将在 Render 中不绘制
                Bounds = new Rect(0, 0, 0, 0);
                // 为了防止闪烁，在外层当前渲染次数结束后再通知渲染完成，因此这里不通知渲染完成
                //// 由于在 Render 中不绘制，所以需要先通知渲染完成
                //foreach (var skiaStrokeRenderSynchronizer in _currentRenderSynchronizerList)
                //{
                //    skiaStrokeRenderSynchronizer.OnRender();
                //}
                return;
            }

            var list = _list;

            Rect bounds = list[0];
            for (var i = 1; i < list.Count; i++)
            {
                bounds = bounds.Union(list[i]);
            }
            Bounds = bounds;

            // 理论上 inkCanvas._renderSynchronizerList 是零个
            Log.Debug($"[Ink][AvaSkiaInkCanvas] InkCanvasCustomDrawOperation 正常执行={inkCanvas._renderSynchronizerList.Count == 0}");
        }

        public IReadOnlyList<SkiaStrokeRenderSynchronizer> CurrentRenderSynchronizerList => _currentRenderSynchronizerList;
        private readonly List<SkiaStrokeRenderSynchronizer> _currentRenderSynchronizerList;
        private List<Rect> _list;
        private List<SkiaStrokeDrawContext> _pathList;

        public void Dispose()
        {
            foreach (var skiaStrokeDrawContext in _pathList)
            {
                skiaStrokeDrawContext.Dispose();
            }
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public bool HitTest(Point p)
        {
            return false;
        }

        public void Render(ImmediateDrawingContext context)
        {
            var skiaSharpApiLeaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (skiaSharpApiLeaseFeature == null)
            {
                return;
            }

            using var skiaSharpApiLease = skiaSharpApiLeaseFeature.Lease();
            var canvas = skiaSharpApiLease.SkCanvas;
            DrawCore(canvas);
            IsFinishRender = true;
        }

        /// <summary>
        /// 是否已经完成渲染
        /// </summary>
        public bool IsFinishRender { get; private set; }

        private void DrawCore(SKCanvas canvas)
        {
            using var skPaint = new SKPaint();

            skPaint.Color = SKColors.Red;
            skPaint.Style = SKPaintStyle.Fill;
            skPaint.IsAntialias = true;
            skPaint.StrokeWidth = 10;

            if (_cache.UseCacheOnNextRender)
            {
                // 当缓存可用时，使用此缓存。
                _cache.DrawBitmap([.. _pathList], canvas, skPaint);
            }
            else if (_pathList.Count > 0)
            {
                // 当笔迹路径可用时，使用此路径。

                // 绘制笔迹。
                foreach (var skiaStrokeDrawContext in _pathList)
                {
                    skPaint.Color = skiaStrokeDrawContext.Color;
                    var transform = skiaStrokeDrawContext.Transform;
                    var useTransform = transform != SKMatrix.Empty && transform != SKMatrix.Identity;
                    if (useTransform)
                    {
                        canvas.Save();
                        canvas.Concat(ref transform);
                    }

                    canvas.DrawPath(skiaStrokeDrawContext.Path, skPaint);

                    if (useTransform)
                    {
                        canvas.Restore();
                    }
                }

                //// 清除动态层的笔迹渲染，然后清除动态层。
                //foreach (var skiaStrokeRenderSynchronizer in _currentRenderSynchronizerList)
                //{
                //    Log.Debug($"[Ink][AvaSkiaInkCanvas] 渲染回调 OnRender");

                //    skiaStrokeRenderSynchronizer.OnRender();
                //}
                //// 防止重复渲染
                //_currentRenderSynchronizerList.Clear();
            }
            else
            {
                // 当笔迹路径不可用时，使用此调试。
#if DEBUG
                // 仅供调试。
                for (var i = 0; i < _list.Count; i++)
                {
                    var bounds = _list[i];
                    var x = (float) bounds.X;
                    var y = (float) bounds.Y;

                    skPaint.Color = new SKColor((uint) (Math.Sin(Math.Pow(Math.E * i, Math.PI)) * int.MaxValue));

                    canvas.DrawRect(x, y, 10, 10, skPaint);
                }
#endif
            }
        }

        public Rect Bounds { get; }
    }

    /// <summary>
    /// 指定是否立即使用位图缓存替代真实的笔迹，或是使用真实的笔迹渲染。
    /// </summary>
    /// <param name="useBitmapCache">
    /// 指定为 <see langword="true"/>，则立即使用位图缓存替代真实的笔迹；<br/>
    /// 指定为 <see langword="false"/>，则位图缓存将不会工作，而使用真实的笔迹渲染。
    /// </param>
    /// <remarks>
    /// 注意，此方法并不会修改用户设置的位图缓存开关，而是设置在某种程序状态下应该打开还是关闭位图缓存。<br/>
    /// 关于它们之间的区别，请参见 <see cref="AvaSkiaInkCanvasContext.UseBitmapCache(bool)"/> 的注释。
    /// </remarks>
    public void UseBitmapCache(bool useBitmapCache)
    {
        Context.UseBitmapCache(useBitmapCache);
        UpdateBitmapCache();
    }

    /// <summary>
    /// 使笔迹的位图缓存失效，并立即重新生成缓存。
    /// </summary>
    public void UpdateBitmapCache()
    {
        InvalidateBitmapCache();
        InvalidateVisual();
    }

    /// <summary>
    /// 使笔迹的位图缓存失效。下次绘制时，如果位图缓存启用，则会重新生成缓存。
    /// </summary>
    public void InvalidateBitmapCache()
    {
        var scale = VisualRoot?.RenderScaling ?? 1;
        if (_inkTransformContext is { } inkContext)
        {
            _cache.UpdateCacheContext(scale, inkContext.VisibleBounds, inkContext.TransformToRoot);
        }
        else
        {
            Log.Warn("[Ink][AvaSkiaInkCanvas][InvalidateBitmapCache] 未设置 InkTransformContext，无法更新缓存。请由笔迹元素调用 UpdateInkTransform 方法更新之。");
        }
    }
}