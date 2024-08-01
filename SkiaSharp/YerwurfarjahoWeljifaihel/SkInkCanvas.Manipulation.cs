using SkiaInkCore.Diagnostics;
using SkiaInkCore.Primitive;
using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaInkCore;
using System.Runtime.CompilerServices;

namespace ReewheaberekaiNayweelehe;

partial class SkInkCanvas
{
    // 漫游相关
    public void ManipulateMoveStart(Point startPoint)
    {
        var inkList = new List<ManipulationStaticInkInfo>(StaticInkInfoList.Count);
        foreach (var skiaStrokeSynchronizer in StaticInkInfoList)
        {
            SKRect inkBounds = skiaStrokeSynchronizer.InkStrokePath?.Bounds ?? SKRect.Empty;

            inkList.Add(new ManipulationStaticInkInfo(skiaStrokeSynchronizer, inkBounds));
        }

        _manipulationInfo = new ManipulationInfo(StartAbsPoint: startPoint, StartMatrix: _totalMatrix, LastAbsPoint: startPoint, inkList);
    }


    public void ManipulateMove(Point absPoint)
    {
        //StaticDebugLogger.WriteLine($"[ManipulateMove] {delta.X:0.00},{delta.Y:0.00}");

        var x = absPoint.X - _manipulationInfo.LastAbsPoint.X;
        var y = absPoint.Y - _manipulationInfo.LastAbsPoint.Y;

        x = Math.Floor(x);
        y = Math.Floor(y);

        if (Math.Abs(x) < 0.01 && Math.Abs(y) < 0.01)
        {
            return;
        }

        var lastAbsPoint = new Point(_manipulationInfo.LastAbsPoint.X + x, _manipulationInfo.LastAbsPoint.Y + y);
        _manipulationInfo = _manipulationInfo with
        {
            LastAbsPoint = lastAbsPoint
        };

        // 需要解决缩放之后的平移，如果直接使用 Concat 方法，那将会在原有的基础上，叠加上缩放后的平移，导致平移的距离不准确
        //_totalMatrix = _totalMatrix * SKMatrix.CreateTranslation((float) delta.X, (float) delta.Y);
        var translation = SKMatrix.CreateTranslation((float) x / _totalMatrix.ScaleX, (float) y / _totalMatrix.ScaleY);
        _totalMatrix = SKMatrix.Concat(_totalMatrix, translation);

        //var stopwatch = Stopwatch.StartNew();
        // 像素漫游的方法
        MoveWithPixel(new Point(x, y));
        //stopwatch.Stop();
        //StaticDebugLogger.WriteLine($"[MoveWithPixel] {stopwatch.ElapsedMilliseconds}ms");

        // 这是用来测试几何漫游的方法
        //// 几何漫游的方法
        //ManipulateFinish();

        _isOriginBackgroundDisable = true;
    }

    public void ManipulateScale(ScaleContext scale)
    {
        StaticDebugLogger.WriteLine($"[ManipulateScale] {scale}");

        var scaleMatrix = SKMatrix.CreateScale(scale.X, scale.Y, scale.PivotX, scale.PivotY);
        _totalMatrix = SKMatrix.Concat(_totalMatrix, scaleMatrix);

        var skCanvas = _skCanvas;
        skCanvas.Clear(Settings.ClearColor);

        skCanvas.Save();
        skCanvas.SetMatrix(_totalMatrix);

        DrawAllInk();

        skCanvas.Restore();

        _isOriginBackgroundDisable = true;

        RenderBoundsChanged?.Invoke(this,
            new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));
    }

    /// <summary>
    /// 漫游完成，需要将内容重新使用路径绘制，保持清晰
    /// </summary>
    public void ManipulateFinish()
    {
        var skCanvas = _skCanvas;
        skCanvas.Clear(Settings.ClearColor);

        skCanvas.Save();
        skCanvas.SetMatrix(_totalMatrix);

        DrawAllInk();

        skCanvas.Restore();
        _isOriginBackgroundDisable = true;
    }


    private ManipulationInfo _manipulationInfo = default;

    private SKMatrix _totalMatrix = SKMatrix.CreateIdentity();

    private unsafe void MoveWithPixel(Point delta)
    {
        var stepCounter = new StepCounter();
        //stepCounter.Start();

        var pixels = ApplicationDrawingSkBitmap.GetPixels(out var length);

        UpdateOriginBackground();
        stepCounter.Record("UpdateOriginBackground");

        //var pixelLengthOfUint = length / 4;
        //if (_cachePixel is null || _cachePixel.Length != pixelLengthOfUint)
        //{
        //    _cachePixel = new uint[pixelLengthOfUint];
        //}

        //fixed (uint* pCachePixel = _cachePixel)
        //{
        //    //var byteCount = (uint) length * sizeof(uint);
        //    ////Buffer.MemoryCopy((uint*) pixels, pCachePixel, byteCount, byteCount);
        //    //////Buffer.MemoryCopy((uint*) pixels, pCachePixel, 0, byteCount);
        //    //for (int i = 0; i < length; i++)
        //    //{
        //    //    var pixel = ((uint*) pixels)[i];
        //    //    pCachePixel[i] = pixel;
        //    //}

        //    var byteCount = (uint) length;
        //    Unsafe.CopyBlock(pCachePixel, (uint*) pixels, byteCount);
        //}

        int destinationX, destinationY, destinationWidth, destinationHeight;
        int sourceX, sourceY, sourceWidth, sourceHeight;

        if (delta.X > 0)
        {
            // 不能直接做加法，这是不对的
            //delta.X += 20;

            destinationX = (int) delta.X;
            destinationWidth = ApplicationDrawingSkBitmap.Width - destinationX;
            sourceX = 0;
        }
        else
        {
            destinationX = 0;
            destinationWidth = ApplicationDrawingSkBitmap.Width - ((int) -delta.X);

            sourceX = (int) -delta.X;
        }

        if (delta.Y > 0)
        {
            destinationY = (int) delta.Y;
            destinationHeight = ApplicationDrawingSkBitmap.Height - destinationY;
            sourceY = 0;
        }
        else
        {
            destinationY = 0;
            destinationHeight = ApplicationDrawingSkBitmap.Height - (int) -delta.Y;

            sourceY = (int) -delta.Y;
        }

        sourceWidth = destinationWidth;
        sourceHeight = destinationHeight;

        SKRectI destinationRectI = SKRectI.Create(destinationX, destinationY, destinationWidth, destinationHeight);
        SKRectI sourceRectI = SKRectI.Create(sourceX, sourceY, sourceWidth, sourceHeight);

        // 计算脏范围，用于在此绘制笔迹
        var topRect = SKRect.Create(0, 0, ApplicationDrawingSkBitmap.Width, destinationY);
        var bottomRect = SKRect.Create(0, destinationY + destinationHeight, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height - destinationY - destinationHeight);
        var leftRect = SKRect.Create(0, destinationY, destinationX, destinationHeight);
        var rightRect = SKRect.Create(destinationX + destinationWidth, destinationY, ApplicationDrawingSkBitmap.Width - destinationX - destinationWidth, destinationHeight);

        var hitRectList = new List<SKRect>(4);
        var matrix = _totalMatrix.Invert();
        Span<SKRect> hitRectSpan = [topRect, bottomRect, leftRect, rightRect];
        foreach (var skRect in hitRectSpan)
        {
            if (!IsEmptySize(skRect))
            {
                hitRectList.Add(matrix.MapRect(skRect));
            }
        }

        stepCounter.Record("准备基础计算");

        var hitInk = new List<SkiaStrokeSynchronizer>();
        foreach (var inkInfo in _manipulationInfo.InkList)
        {
            foreach (var skRect in hitRectList)
            {
                if (inkInfo.InkBounds.IntersectsWith(skRect))
                {
                    hitInk.Add(inkInfo.InkInfo);
                    break;
                }
            }
        }

        //var skCanvas = _skCanvas;
        //skCanvas.Clear();
        //foreach (var skRectI in (Span<SKRectI>) [topRectI, bottomRectI, leftRectI, rightRectI])
        //{
        //    using var skPaint = new SKPaint();
        //    skPaint.StrokeWidth = 0;
        //    skPaint.IsAntialias = true;
        //    skPaint.FilterQuality = SKFilterQuality.High;
        //    skPaint.Style = SKPaintStyle.Fill;
        //    skPaint.Color = SKColors.Blue;
        //    var skRect = SKRect.Create(skRectI.Left, skRectI.Top, skRectI.Width, skRectI.Height);

        //    skCanvas.DrawRect(skRect, skPaint);
        //}
        //skCanvas.Flush();
     

        stepCounter.Record("统计所需命中的笔迹");

        var skCanvas = _skCanvas;
        skCanvas.Clear(Settings.ClearColor);
        skCanvas.Save();
        skCanvas.SetMatrix(_totalMatrix);
        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 0;
        skPaint.IsAntialias = true;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Fill;

        foreach (var skiaStrokeSynchronizer in hitInk)
        {
            DrawInk(skCanvas, skPaint, skiaStrokeSynchronizer);
        }

        skCanvas.Restore();
        skCanvas.Flush();

        stepCounter.Record("完成绘制笔迹");


        var cachePixel = _originBackground.GetPixels();
        uint* pCachePixel = (uint*) cachePixel;
        var pixelLength = (uint) (ApplicationDrawingSkBitmap.Width);

        ReplacePixels((uint*) pixels, pCachePixel, destinationRectI, sourceRectI, pixelLength, pixelLength);

        stepCounter.Record("拷贝像素");
        stepCounter.OutputToConsole();

        RenderBoundsChanged?.Invoke(this,
            new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));

        static bool IsEmptySize(SKRect skRect) => skRect.Width == 0 || skRect.Height == 0;

        static bool IsHit(SkiaStrokeSynchronizer inkInfo, SKRect skRect)
        {
            if (inkInfo.InkStrokePath is { } path)
            {
                var bounds = path.Bounds;
                if (skRect.IntersectsWith(bounds))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static unsafe bool ReplacePixels(uint* destinationBitmap, uint* sourceBitmap, SKRectI destinationRectI,
    SKRectI sourceRectI, uint destinationPixelWidthLengthOfUint, uint sourcePixelWidthLengthOfUint)
    {
        if (destinationRectI.Width != sourceRectI.Width || destinationRectI.Height != sourceRectI.Height)
        {
            return false;
        }

        //for(var sourceRow = sourceRectI.Top; sourceRow< sourceRectI.Bottom; sourceRow++)
        //{
        //    for (var sourceColumn = sourceRectI.Left; sourceColumn < sourceRectI.Right; sourceColumn++)
        //    {
        //        var sourceIndex = sourceRow * sourceRectI.Width + sourceColumn;

        //        var destinationRow = destinationRectI.Top + sourceRow - sourceRectI.Top;
        //        var destinationColumn = destinationRectI.Left + sourceColumn - sourceRectI.Left;
        //        var destinationIndex = destinationRow * destinationRectI.Width + destinationColumn;

        //        destinationBitmap[destinationIndex] = sourceBitmap[sourceIndex];
        //    }
        //}

        for (var sourceRow = sourceRectI.Top; sourceRow < sourceRectI.Bottom; sourceRow++)
        {
            var sourceStartColumn = sourceRectI.Left;
            var sourceStartIndex = sourceRow * destinationPixelWidthLengthOfUint + sourceStartColumn;

            var destinationRow = destinationRectI.Top + sourceRow - sourceRectI.Top;
            var destinationStartColumn = destinationRectI.Left;
            var destinationStartIndex = destinationRow * sourcePixelWidthLengthOfUint + destinationStartColumn;

            Unsafe.CopyBlockUnaligned((destinationBitmap + destinationStartIndex), (sourceBitmap + sourceStartIndex), (uint) (destinationRectI.Width * sizeof(uint)));

            //for (var sourceColumn = sourceRectI.Left; sourceColumn < sourceRectI.Right; sourceColumn++)
            //{
            //    var sourceIndex = sourceRow * destinationPixelWidthLengthOfUint + sourceColumn;

            //    var destinationColumn = destinationRectI.Left + sourceColumn - sourceRectI.Left;
            //    var destinationIndex = destinationRow * sourcePixelWidthLengthOfUint + destinationColumn;

            //    destinationBitmap[destinationIndex] = sourceBitmap[sourceIndex];
            //}
        }

        return true;
    }

    #region 辅助类型

    private record ManipulationStaticInkInfo(SkiaStrokeSynchronizer InkInfo, SKRect InkBounds);

    private readonly record struct ManipulationInfo(Point StartAbsPoint, SKMatrix StartMatrix, Point LastAbsPoint, List<ManipulationStaticInkInfo> InkList);

    #endregion
}