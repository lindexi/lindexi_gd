using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using LightTextEditorPlus.Utils;
using System.Collections.Generic;

using System.Diagnostics;
using System.Linq;

namespace LightTextEditorPlus;

partial class TextEditor
{
    private readonly AvaloniaTextEditorRenderEngine _renderEngine;

    private class AvaloniaTextEditorRenderEngine
    {
        public AvaloniaTextEditorRenderEngine(TextEditor textEditor)
        {
            TextEditor = textEditor;
        }

        public TextEditor TextEditor { get; }
        private Rect _lastRenderBounds = new Rect();

        /// <summary>
        /// 用于解决释放问题
        /// </summary>
        private readonly HashSet<ITextEditorContentSkiaRender> _contentSkiaRenderCache = new HashSet<ITextEditorContentSkiaRender>(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// 这是一个用于调试使用的字段
        /// </summary>
        private List<ITextEditorContentSkiaRender>? _debugAllContentSkiaRenderList;

        public void Render(DrawingContext context)
        {
            TextEditor textEditor = TextEditor;
            if (textEditor.IsDirty)
            {
                // 准备要渲染了，结果文本还是脏的，那就强制布局
                textEditor.ForceRedraw();
            }

            SkiaTextEditor skiaTextEditor = textEditor.SkiaTextEditor;
            ITextEditorContentSkiaRender textEditorSkiaRender = skiaTextEditor.GetCurrentTextRender();

            var currentBounds = new Rect(textEditor.DesiredSize);

            #region 解决渲染资源释放问题
            // 尝试解决释放问题。因为创建是 UI 线程，但不知道渲染线程是否还在使用，于是决定将释放逻辑放在渲染线程
            List<ITextEditorContentSkiaRender>? toDisposedList = null;
            _contentSkiaRenderCache.RemoveWhere(t => t.IsDisposed);
            foreach (var textEditorContentSkiaRender in _contentSkiaRenderCache.Where(textEditorContentSkiaRender => textEditorContentSkiaRender.IsObsoleted))
            {
                toDisposedList ??= new List<ITextEditorContentSkiaRender>();
                toDisposedList.Add(textEditorContentSkiaRender);
            }
            if (_contentSkiaRenderCache.Add(textEditorSkiaRender))
            {
                Debug.Assert(_contentSkiaRenderCache.Count < 3, "预期不会超过两个，一个是在 UI 线程准备等待渲染，另一个是在渲染线程进行渲染过程");
#if DEBUG
                _debugAllContentSkiaRenderList ??= new List<ITextEditorContentSkiaRender>();
                _debugAllContentSkiaRenderList.Add(textEditorSkiaRender);
#endif
            }

            var count = _debugAllContentSkiaRenderList?.Count(t => !t.IsDisposed);
            _ = count;
            Debug.WriteLine($"当前未释放数量： {count}/{_debugAllContentSkiaRenderList?.Count}");
            #endregion

            currentBounds = currentBounds.Union(textEditorSkiaRender.RenderBounds.ToAvaloniaRect());

            var renderBounds = currentBounds;

            if (_lastRenderBounds.Width > 0 || _lastRenderBounds.Height > 0)
            {
                // 之前有渲染过，那就要重绘之前的区域。这是 Avalonia 的问题，如果前一次范围比较大，本次比较小，如果依然按照本次的范围，则会让前一次的渲染内容不清掉
                renderBounds = renderBounds.Union(_lastRenderBounds);
            }
            _lastRenderBounds = currentBounds;

            context.Custom(new TextEditorCustomDrawOperation(renderBounds, textEditorSkiaRender, toDisposedList));

            if (textEditor.IsInEditingInputMode
                // 如果配置了选择区域在非编辑模式下也会绘制，那在非编辑模式下也会绘制选择区域
                || textEditor.CaretConfiguration.ShowSelectionWhenNotInEditingInputMode)
            {
                // 只有编辑模式下才会绘制光标和选择区域
                context.Custom(new TextEditorCustomDrawOperation(renderBounds,
                    skiaTextEditor.GetCurrentCaretAndSelectionRender(), toDisposedList: null));
            }
        }
    }
}

file class TextEditorCustomDrawOperation : ICustomDrawOperation
{
    public TextEditorCustomDrawOperation(Rect bounds, ITextEditorSkiaRender render, List<ITextEditorContentSkiaRender>? toDisposedList)
    {
        _render = render;
        _toDisposedList = toDisposedList;
        Bounds = bounds;

        render.AddReference();
    }

    private readonly ITextEditorSkiaRender _render;
    private readonly List<ITextEditorContentSkiaRender>? _toDisposedList;

    public void Dispose()
    {
        _render.ReleaseReference();
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(_render, (other as TextEditorCustomDrawOperation)?._render);
    }

    public bool HitTest(Point p)
    {
        return Bounds.Contains(p);
    }

    public void Render(ImmediateDrawingContext context)
    {
        ISkiaSharpApiLeaseFeature? skiaSharpApiLeaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (skiaSharpApiLeaseFeature != null)
        {
            using ISkiaSharpApiLease skiaSharpApiLease = skiaSharpApiLeaseFeature.Lease();
            _render.Render(skiaSharpApiLease.SkCanvas);
        }
        else
        {
            // 不支持 Skia 绘制
        }
    }

    public Rect Bounds { get; }
}