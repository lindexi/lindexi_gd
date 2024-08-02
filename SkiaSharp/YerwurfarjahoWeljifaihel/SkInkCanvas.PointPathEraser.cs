using SkiaInkCore.Interactives;
using SkiaInkCore.Primitive;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaInkCore;
using SkiaInkCore.Utils;

namespace ReewheaberekaiNayweelehe;

class PointPathEraserManager
{


    #region 辅助类型

    class InkInfoForEraserPointPath
    {
        public InkInfoForEraserPointPath(SkiaStrokeSynchronizer strokeSynchronizer)
        {
            StrokeSynchronizer = strokeSynchronizer;
            SubInkInfoList = new List<ErasingSubInkInfoForEraserPointPath>();

            var subInk = new ErasingSubInkInfoForEraserPointPath(new StylusPointListSpan(0, strokeSynchronizer.StylusPoints.Count));
            if (strokeSynchronizer.InkStrokePath is { } skPath)
            {
                subInk.CacheBounds = skPath.Bounds.ToMauiRect();
            }

            SubInkInfoList.Add(subInk);
        }

        public SkiaStrokeSynchronizer StrokeSynchronizer { get; set; }
        public List<ErasingSubInkInfoForEraserPointPath> SubInkInfoList { get; }
    }

    class ErasingSubInkInfoForEraserPointPath
    {
        public ErasingSubInkInfoForEraserPointPath(StylusPointListSpan stylusPointListSpan)
        {
            StylusPointListSpan = stylusPointListSpan;
        }

        public Rect CacheBounds { get; set; }
        public StylusPointListSpan StylusPointListSpan { get; }
    }

    readonly record struct StylusPointListSpan(int Start, int Length);

    #endregion
}

partial class SkInkCanvas
{
    private void StartEraserPointPath()
    {
        _isEraserPointPathStart = true;

    }
    

    private bool _isEraserPointPathStart;

    private void MoveEraserPointPath(InkingModeInputArgs info, double width, double height)
    {
        if (_skCanvas is not { } canvas || _originBackground is null)
        {
            return;
        }

        if (!_isEraserPointPathStart)
        {
            StartEraserPointPath();
        }

        var point = info.StylusPoint.Point;
        var x = (float) point.X;
        var y = (float) point.Y;

        // 变换为左上角
        x -= (float) width / 2;
        y -= (float) height / 2;

        var skRect = new SKRect(x, y, (float) (x + width), (float) (y + height));
        // 比擦掉的范围更大的范围，用于持续更新
        var expandRect = RectExtension.ExpandSKRect(skRect, 10);
        if (_lastEraserRenderBounds is not null)
        {
            // 理论上此时需要从原先的拷贝覆盖，否则将不能清掉上次的橡皮擦内容
            // 重新绘制 _origin 的，用于修复清理的问题
            // 为什么其他的模式不需要？原因是其他的模式的裁剪是全部的
            // 用于修复橡皮擦图标没有删除
            expandRect.Union(_lastEraserRenderBounds.Value.ToSkRect());
        }
        expandRect = LimitRectInAppBitmapRect(expandRect);

        ApplicationDrawingSkBitmap.ReplacePixels(_originBackground, SKRectI.Ceiling(expandRect));





        //重新更新 _originBackground 的内容，需要在画出橡皮擦之前
        UpdateOriginBackground();

        // 画出橡皮擦
        canvas.Save();
        canvas.Translate(x, y);
        EraserView.DrawEraserView(canvas, (int) width, (int) height);
        canvas.Restore();

        // 更新范围
        var addition = 20;

        var currentEraserRenderBounds = new Rect(x - addition, y - addition, width + addition * 2, height + addition * 2);
        currentEraserRenderBounds = LimitRectInAppBitmapRect(currentEraserRenderBounds);

        var rect = currentEraserRenderBounds;
        if (_lastEraserRenderBounds != null)
        {
            // 将上次的绘制范围进行重新绘制，防止出现橡皮擦图标
            rect = rect.Union(_lastEraserRenderBounds.Value);
        }
        rect = LimitRectInAppBitmapRect(rect);
        _lastEraserRenderBounds = currentEraserRenderBounds;

        RenderBoundsChanged?.Invoke(this, rect);
    }

    private void CleanEraserPointPath()
    {
        _isEraserPointPathStart = false;

        if (_lastEraserRenderBounds is null)
        {
            return;
        }

        if (_skCanvas is not { } canvas || _originBackground is null)
        {
            return;
        }

        var rect = _lastEraserRenderBounds.Value;
        rect = LimitRectInAppBitmapRect(rect);
        ApplicationDrawingSkBitmap.ReplacePixels(_originBackground, SKRectI.Ceiling(rect.ToSkRect()));

        RenderBoundsChanged?.Invoke(this, rect);
    }
}
