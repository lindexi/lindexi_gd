#if !USE_SKIA || USE_AllInOne
using LightTextEditorPlus.Document;
using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Events;

/// <summary>
/// 样式更改的参数
/// </summary>
public class StyleChangeEventArgs : EventArgs
{
    /// <summary>
    /// 创建样式参数
    /// </summary>
    /// <param name="segment"></param>
    /// <param name="changedProperty"></param>
    /// <param name="isFromUndoRedo"></param>
    [DebuggerStepThrough]
    public StyleChangeEventArgs(Selection segment, PropertyType changedProperty, bool isFromUndoRedo)
    {
        ChangedSegment = segment;
        ChangedProperty = changedProperty;
        IsFromUndoRedo = isFromUndoRedo;
    }

    /// <summary>
    /// 是否从撤销重做触发
    /// </summary>
    public bool IsFromUndoRedo { get; }

    /// <summary>
    /// 被更改的文本段
    /// </summary>
    public Selection ChangedSegment { get; }

    /// <summary>
    /// 被更改的属性类型
    /// </summary>
    public PropertyType ChangedProperty { get; }
}
#endif
