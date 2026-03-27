using AvaloniaAgentLib.Model;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaAgentLib.Logging;

public sealed class FileCopilotChatLogger : ICopilotChatLogger
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    //public FileCopilotChatLogger()
    //    : this(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AvaloniaAgentLib", "CopilotChatLogs"))
    //{
    //}

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
            Directory.CreateDirectory(ChatLogFolder);

            string logFilePath = Path.Join(ChatLogFolder, $"{sessionId:N}.log");
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
            builder.AppendLine(chatMessage.Content);
            builder.AppendLine();

            await File.AppendAllTextAsync(logFilePath, builder.ToString(), Encoding.UTF8).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
