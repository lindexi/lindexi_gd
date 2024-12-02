using System;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
///     文本的日志记录
/// </summary>
public interface ITextLogger
{
    /// <summary>
    /// 调试日志记录
    /// </summary>
    /// <param name="message"></param>
    void LogDebug(string message);

    /// <summary>
    /// 记录异常。基本只记录框架运行过程异常，注入框架内容部分的错误也属于框架异常。对于上层输入参数错误，将会直接抛出
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="message"></param>
    void LogException(Exception exception, string? message);

    /// <summary>
    /// 记录信息
    /// </summary>
    /// <param name="message"></param>
    void LogInfo(string message);

    /// <summary>
    /// 记录警告
    /// </summary>
    /// <param name="message"></param>
    void LogWarning(string message);
}