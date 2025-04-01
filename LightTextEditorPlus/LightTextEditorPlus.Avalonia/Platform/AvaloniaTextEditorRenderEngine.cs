using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using LightTextEditorPlus.Utils;
using System.Collections.Generic;

using System.Diagnostics;
using System.Linq;
using LightTextEditorPlus.Rendering;

namespace LightTextEditorPlus;

partial class TextEditor
{
    private readonly AvaloniaTextEditorRenderEngine _renderEngine;

    /// <summary>
    /// 使用 Skia 渲染承载的文本编辑器的渲染引擎
    /// </summary>
    /// 核心处理问题： 渲染资源释放
    /// 渲染资源为什么存在释放问题？原因是 Avalonia 设计上 Render 是在另一个线程进行的。在文本库里面会创建出 <see cref="SkiaSharp.SKPicture"/> 这个 Skia 资源用于提供渲染内容，即生产方是在 UI 线程的 <see cref="SkiaTextEditor"/> 进行的，当文本内容变更的时候。只有 UI 线程的 <see cref="SkiaTextEditor"/> 能够感知到，但此时 <see cref="SkiaTextEditor"/> 不能立刻释放 Skia 资源，因为不知道渲染线程是否还在使用。所以需要一个机制来解决这个问题。
    /// 额外地，在 Avalonia 存在渲染时机不对齐问题，即可能存在连续两次文本布局渲染，但 Avalonia 只执行一次。这就意味着首次的布局渲染内容没有进入到 <see cref="TextEditorCustomDrawOperation"/> 里面，无法被 <see cref="TextEditorCustomDrawOperation"/> 通过引用计数的方式释放。为了解决此问题，在 ITextEditorContentSkiaRender 添加 IsUsed 属性，通过这个属性判断是否渲染内容被交给 UI 框架了，一旦交给 UI 框架了，那就应该交给 UI 框架释放，不能在 <see cref="SkiaTextEditor"/> 里面释放。如果交给了 UI 框架，在 <see cref="SkiaTextEditor"/> 里面只标记 <see cref="ITextEditorContentSkiaRender.IsObsoleted"/> 属性，通过此属性让 <see cref="TextEditorCustomDrawOperation"/> 通过引用计数的方式释放。如果没有交给 UI 框架，那就在 <see cref="SkiaTextEditor"/> 里面释放。
    /// 机制：
    /// 1. 在 ITextEditorContentSkiaRender 加上引用计数机制。当引用计数为 0 且 IsObsoleted 为 true 时，释放资源
    ///   - 为什么不是引用计数为 0 时释放资源，而是需要等待同时满足 IsObsoleted 为 true 条件？因为切页，如切 Tab 时，此时渲染量为 0 的值，但文本内容没有变更，此时文本不产生新的渲染内容。等 Tab 切回来时，文本底层提供了相同的渲染对象内容。如果在之前释放了资源，那就会导致切回来时，文本内容无法渲染。这就是待办里面记录的 `切换 Tab 再回来，文本无渲染` 问题
    /// 2. 添加 <see cref="_contentSkiaRenderCache"/> 记录历史上的所有参与渲染的内容，判断如果已经被标记 IsObsoleted 释放，则加入到 <see cref="DrawingContext.Custom"/> 里面，加入到此仅仅是为了被调度到渲染线程进行资源释放
    ///   - 为什么需要调度到渲染线程才能释放资源？因为 Skia 资源可能还在渲染线程里面使用着，在 UI 线程释放会导致渲染线程出现异常，进而炸掉进程。全部调度到渲染线程释放可以确保没有渲染线程在使用资源的时候释放资源
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

            var showCaret = textEditor.IsInEditingInputMode;
            // 如果配置了选择区域在非编辑模式下也会绘制，那在非编辑模式下也会绘制选择区域
            showCaret = showCaret ||
                        (
                            textEditor
                                .CaretConfiguration
                                .ShowSelectionWhenNotInEditingInputMode
                            // 有选择时才能绘制选择范围，否则不应该只显示光标
                            && !textEditor.CurrentSelection.IsEmpty
                        );

            if (showCaret)
            {
                // 只有编辑模式下才会绘制光标和选择区域
                context.Custom(new TextEditorCustomDrawOperation(renderBounds,
                    skiaTextEditor.GetCurrentCaretAndSelectionRender(new CaretAndSelectionRenderContext(TextEditor.IsOvertypeMode)), toDisposedList: null));
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

        if (_toDisposedList is { } list)
        {
            foreach (ITextEditorContentSkiaRender textEditorContentSkiaRender in list)
            {
                if (!textEditorContentSkiaRender.IsDisposed)
                {
                    // 还没被释放的，非预期分支，那就在这里释放
                    textEditorContentSkiaRender.Dispose();
                }
                else
                {
                    // 预期都已被释放了
                }
            }
        }
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