using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Core;

public partial class TextEditorCore
{
    /// <summary>
    /// 获取文档的布局尺寸，实际布局尺寸。此方法必须在文本布局完成之后才能调用
    /// </summary>
    /// <returns></returns>
    /// <exception cref="TextEditorDirtyException"></exception>
    public TextRect GetDocumentLayoutBounds()
    {
        VerifyNotDirty();

        return _layoutManager.DocumentRenderData.DocumentBounds;
    }

    /// <summary>
    /// 获取文本的用于渲染信息，必须要在布局完成之后才能获取。可选使用 <see cref="WaitLayoutCompletedAsync"/> 等待布局完成
    /// </summary>
    /// <returns></returns>
    public RenderInfoProvider GetRenderInfo()
    {
        VerifyNotDirty();

        if (_renderInfoProvider is null && DocumentManager.CharCount == 0)
        {
            // 如果是空文本，也就是还在所有布局之前的情况下
            // 那就启动空文本布局，将会在布局完成之后，通过事件赋值 _renderInfoProvider 内容
            LayoutEmptyTextEditor();
            // 如果布局能完成，那就一定不是空
            Debug.Assert(_renderInfoProvider != null, nameof(_renderInfoProvider) + " != null");
        }

        Debug.Assert(_renderInfoProvider != null, nameof(_renderInfoProvider) + " != null");
        return _renderInfoProvider!;
    }

    #region WaitLayoutCompleted

    /// <summary>
    /// 等待布局完成
    /// </summary>
    /// <returns></returns>
    /// 为什么设计使用 Task 没有加返回值？返回值如果是 <see cref="RenderInfoProvider"/> 类型，但是等待调度过程中，文本再次是脏的，那将会导致获取到的渲染数据不对
    public Task WaitLayoutCompletedAsync()
    {
        if (IsDirty)
        {
            if (_waitLayoutCompletedTask == null)
            {
                _waitLayoutCompletedTask = new TaskCompletionSource();
            }

            return _waitLayoutCompletedTask.Task;
        }
        else
        {
            return Task.CompletedTask;
        }
    }

    private void SetLayoutCompleted()
    {
        _waitLayoutCompletedTask?.TrySetResult();
        _waitLayoutCompletedTask = null;
    }

    private TaskCompletionSource? _waitLayoutCompletedTask;

    #endregion

    #region 辅助方法

    internal void VerifyNotDirty()
    {
        if (IsDirty)
        {
            ThrowTextEditorDirtyException();
        }
    }

    /// <summary>
    /// 抛出文本是脏的异常
    /// </summary>
    /// 拆分一个方法是为了减少 JIT 过程需要生成异常的代码，提升 JIT 性能。同时让 <see cref="VerifyNotDirty"/> 被内联
    /// <exception cref="TextEditorDirtyException"></exception>
    internal void ThrowTextEditorDirtyException() => throw new TextEditorDirtyException(this);

    #endregion
}