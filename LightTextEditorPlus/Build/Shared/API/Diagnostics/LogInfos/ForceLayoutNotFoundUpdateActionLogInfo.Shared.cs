using System;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 进行 ForceLayout 立刻布局时，没有找到任何可更新的任务
/// </summary>
/// <param name="IsFinishUpdateLayoutWithException">最后一次的布局是否带着异常的</param>
/// 正常来说文本是脏的，那文本就必然有 UpdateAction 可用。如果文本没有 UpdateAction 可用，则这是进入了异常状态。可通过 <see cref="IsFinishUpdateLayoutWithException"/> 判断是否文本上次布局是异常的。即文本是由于异常导致布局中断，此时文本就是脏的，且没有任何 UpdateAction 可用
public readonly record struct ForceLayoutNotFoundUpdateActionLogInfo(bool IsFinishUpdateLayoutWithException)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var message = "进行 ForceLayout 立刻布局时，没有找到任何可更新的任务。";
        if(IsFinishUpdateLayoutWithException)
        {
            message += "上次布局是异常的，文本没有正确完成布局";
        }
        return message;
    }
}