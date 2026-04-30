using AvaloniaAgentLib.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaAgentLib.Logging;

public sealed class FileCopilotChatLogger : ICopilotChatLogger
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly Dictionary<Guid, string> _sessionLogFilePathMap = [];

    public FileCopilotChatLogger()
        : this(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AvaloniaAgentLib", "CopilotChatLogs"))
    {
    }

    public FileCopilotChatLogger(string chatLogFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chatLogFolder);
        ChatLogFolder = chatLogFolder;
    }

    public string ChatLogFolder { get; }

    /// <summary>
    /// 将聊天消息追加到当前会话对应的日志文件。
    /// </summary>
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

            if (chatMessage.UsageDetails is { } usageDetails)
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
