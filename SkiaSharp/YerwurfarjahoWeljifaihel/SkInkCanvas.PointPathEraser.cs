using SkiaInkCore.Interactives;
using SkiaInkCore.Primitive;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BujeeberehemnaNurgacolarje;
using SkiaInkCore;
using SkiaInkCore.Diagnostics;
using SkiaInkCore.Utils;
using System.Threading;

namespace ReewheaberekaiNayweelehe;

partial class SkInkCanvas
{
    private void StartEraserPointPath()
    {
        _isEraserPointPathStart = true;
        _pointPathEraserManager = new PointPathEraserManager(this);
        _pointPathEraserManager.StartEraserPointPath();
    }

    private PointPathEraserManager? _pointPathEraserManager;

    private bool _isEraserPointPathStart;

    private Task? _lastTask;

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

        Debug.Assert(_pointPathEraserManager != null);

        var point = info.StylusPoint.Point;
        var x = (float) point.X;
        var y = (float) point.Y;

        // 变换为左上角
        x -= (float) width / 2;
        y -= (float) height / 2;

        var lastTask = _lastTask;
        _lastTask = Task.Run(async () =>
        {
            if (lastTask is { } task)
            {
                await task;
            }

            _pointPathEraserManager.Move(new Rect(x, y, width, height));
        });

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

        //ApplicationDrawingSkBitmap.ReplacePixels(_originBackground, SKRectI.Ceiling(expandRect));





        if (EraserPath is null)
        {
            EraserPath = new SKPath();
        }
        else
        {
            EraserPath.Reset();
        }
        EraserPath.AddRect(new SKRect(0, 0, _originBackground.Width, _originBackground.Height));
        // 几何裁剪本身无视顺序，因此先处理当前点再处理之前的点也是正确的
        using var skRoundRect = new SKPath();
        skRoundRect.AddRoundRect(skRect, 5, 5);
        EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);

        canvas.Clear();
        canvas.Save();
        canvas.ClipPath(EraserPath, antialias: true);
        canvas.DrawBitmap(_originBackground, 0, 0);
        canvas.Restore();

        canvas.Flush();

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

    private async void CleanEraserPointPath()
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

        if (_lastTask != null)
        {
            await _lastTask;
        }

        RequestDispatcher?.Invoke(this, () =>
        {
            DrawAllInk();
            RenderBoundsChanged?.Invoke(this, new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));
        });
    }

    public event EventHandler<Action>? RequestDispatcher;
}
