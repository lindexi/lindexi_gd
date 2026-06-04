using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AgentLib.Model;

/// <summary>
/// 提供属性变更通知的基础类，实现 <see cref="INotifyPropertyChanged"/> 接口。
/// </summary>
public class NotifyBase : INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发 <see cref="PropertyChanged"/> 事件。
    /// </summary>
    /// <param name="propertyName">变更的属性名称，由调用方成员名称自动填充。</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 设置字段值，并在值发生变化时触发属性变更通知。
    /// </summary>
    /// <typeparam name="T">字段类型。</typeparam>
    /// <param name="field">字段引用。</param>
    /// <param name="value">新值。</param>
    /// <param name="propertyName">属性名称，由调用方成员名称自动填充。</param>
    /// <returns>如果值发生变化则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
