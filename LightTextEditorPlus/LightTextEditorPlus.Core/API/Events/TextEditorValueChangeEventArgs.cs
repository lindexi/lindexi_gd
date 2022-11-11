using System;

namespace LightTextEditorPlus.Core.Events;

/// <summary>
/// 值变更的事件参数
/// </summary>
/// <typeparam name="T"></typeparam>
public class TextEditorValueChangeEventArgs<T> : EventArgs
{
    /// <summary>
    /// 值变更的事件参数
    /// </summary>
    public TextEditorValueChangeEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public T OldValue { get; }
    public T NewValue { get; }
}