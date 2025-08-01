using System;

namespace LightTextEditorPlus.Exceptions;

/// <summary>
/// 静态配置的属性被多次设置异常
/// </summary>
public class StaticConfigurationPropertyMultipleSettingsException : InvalidOperationException
{
    private StaticConfigurationPropertyMultipleSettingsException(string propertyName, string? message = null, Exception? innerException = null) : base(message, innerException)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// 多次设置的属性
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// 抛出静态配置的属性被多次设置异常
    /// </summary>
    /// <param name="propertyName"></param>
    /// <exception cref="StaticConfigurationPropertyMultipleSettingsException"></exception>
    public static void Throw(string propertyName) => throw new StaticConfigurationPropertyMultipleSettingsException(propertyName);
}