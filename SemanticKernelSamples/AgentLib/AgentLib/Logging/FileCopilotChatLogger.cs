using AgentLib.Core;
using AgentLib.Model;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.Logging;

/// <summary>
/// 将聊天消息追加到文件系统中的人类可读文本日志。
/// </summary>
public sealed class FileCopilotChatLogger : ICopilotChatLogger
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly Dictionary<Guid, string> _sessionLogFilePathMap = [];

    /// <summary>
    /// 使用默认日志目录创建日志记录器。
    /// </summary>
    public FileCopilotChatLogger()
        : this(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AgentLib", "CopilotChatLogs"))
    {
    }

    /// <summary>
    /// 使用指定的日志目录创建日志记录器。
    /// </summary>
    /// <param name="chatLogFolder">聊天日志文件夹路径。</param>
    public FileCopilotChatLogger(string chatLogFolder)
    {
        ArgumentHelper.ThrowIfNullOrWhiteSpace(chatLogFolder);
        ChatLogFolder = chatLogFolder;
    }

    /// <summary>
    /// 聊天日志文件夹路径。
    /// </summary>
    public string ChatLogFolder { get; }

    /// <inheritdoc/>
    public async Task LogMessageAsync(Guid sessionId, CopilotChatMessage chatMessage)
    {
        ArgumentNullException.ThrowIfNull(chatMessage);

        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            string logFilePath = GetSessionLogFilePath(sessionId, chatMessage.CreatedTime);
            bool isNewFile = !File.Exists(logFilePath);

            var builder = new StringBuilder();
            if (isNewFile)
            {
                builder.AppendLine($"SessionId: {sessionId}");
                builder.AppendLine();
            }

            builder.Append('[')
                .Append(chatMessage.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
                .AppendLine("]");
            builder.Append(chatMessage.Author).AppendLine(":");
            builder.AppendLine(chatMessage.FullContent);

            if (chatMessage.TotalUsageDetails is { } usageDetails)
            {
                builder.AppendLine("用量:");
                AppendUsageLine(builder, "总计", usageDetails.TotalTokenCount);
                AppendUsageLine(builder, "输入", usageDetails.InputTokenCount);
                AppendUsageLine(builder, "输出", usageDetails.OutputTokenCount);
                AppendUsageLine(builder, "思考", usageDetails.ReasoningTokenCount);
                AppendUsageLine(builder, "缓存", usageDetails.CachedInputTokenCount);
            }

            builder.AppendLine();

            await File.AppendAllTextAsync(logFilePath, builder.ToString(), Encoding.UTF8).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private string GetSessionLogFilePath(Guid sessionId, DateTimeOffset createdTime)
    {
        if (_sessionLogFilePathMap.TryGetValue(sessionId, out string? logFilePath))
        {
            return logFilePath;
        }

        string dayFolderPath = Path.Join(ChatLogFolder, createdTime.ToString("yyyyMMdd"));
        Directory.CreateDirectory(dayFolderPath);

        logFilePath = Path.Join(dayFolderPath, $"{createdTime:yyyyMMdd_HHmmss}_{sessionId:N}.log");
        _sessionLogFilePathMap[sessionId] = logFilePath;
        return logFilePath;
    }

    private static void AppendUsageLine(StringBuilder builder, string label, long? value)
    {
        if (value is null)
        {
            return;
        }

        builder.Append("- ")
            .Append(label)
            .Append(": ")
            .AppendLine(value.Value.ToString("N0"));
    }
}
