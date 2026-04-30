namespace VideoComposerLib;

/// <summary>
/// 日志委托：上层可自定义日志输出逻辑
/// </summary>
/// <param name="level">日志级别</param>
/// <param name="message">日志内容</param>
public delegate void LogHandler(VideoComposerLogLevel level, string message);