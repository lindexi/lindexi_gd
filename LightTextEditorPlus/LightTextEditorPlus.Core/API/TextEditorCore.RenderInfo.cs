using System;
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
    public Rect GetDocumentLayoutBounds()
    {
        VerifyNotDirty();

        return _layoutManager.DocumentRenderData.DocumentBounds;
    }

    /// <summary>
    /// 获取文本的渲染信息，必须要在布局完成之后才能获取
    /// </summary>
    /// <returns></returns>
    public RenderInfoProvider GetRenderInfo()
    {
        VerifyNotDirty();
        return _renderInfoProvider;
    }

    #region WaitLayoutCompleted

    public Task WaitLayoutCompletedAsync()
    {
        if (IsDirty())
        {
            return Task.CompletedTask;
        }
        else
        {
            if (_waitLayoutCompletedTask == null)
            {
                _waitLayoutCompletedTask = new TaskCompletionSource();
                DocumentManager.InternalDocumentChanging += SetLayoutCompleted;
            }

            return _waitLayoutCompletedTask.Task;
        }
    }

    private void SetLayoutCompleted(object? sender, EventArgs e)
    {
        _waitLayoutCompletedTask?.TrySetResult();
        _waitLayoutCompletedTask = null;
    }

    private TaskCompletionSource? _waitLayoutCompletedTask;

    #endregion

    #region 辅助方法

    private bool IsDirty() => _layoutManager.DocumentRenderData.IsDirty;

    internal void VerifyNotDirty()
    {
        if (IsDirty())
        {
            ThrowTextEditorDirtyException();
        }
    }

    /// <summary>
    /// 抛出文本是脏的异常
    /// </summary>
    /// 拆分一个方法是为了减少 JIT 过程需要生成异常的代码，提升 JIT 性能。同时让 <see cref="VerifyNotDirty"/> 被内联
    /// <exception cref="TextEditorDirtyException"></exception>
    private static void ThrowTextEditorDirtyException()=> throw new TextEditorDirtyException();

    #endregion
}

