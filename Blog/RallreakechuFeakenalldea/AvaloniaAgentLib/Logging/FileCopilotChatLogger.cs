using AvaloniaAgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace AvaloniaAgentLib.Logging;

public sealed class FileCopilotChatLogger : ICopilotChatLogger
{
    private const string CompactChatHistoryClosingFragment = "</Messages></CopilotChatSessionHistory>";
    private const string MessageElementIndent = "    ";
    private static readonly Encoding Utf8EncodingWithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly string FormattedChatHistoryClosingFragment = $"{Environment.NewLine}  </Messages>{Environment.NewLine}</CopilotChatSessionHistory>";

    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly Dictionary<Guid, string> _sessionLogFilePathMap = [];
    private readonly Dictionary<Guid, ChatHistoryFileInfo> _sessionHistoryFileInfoMap = [];

    public FileCopilotChatLogger()
        : this(
            Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AvaloniaAgentLib", "CopilotChatLogs"),
            Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AvaloniaAgentLib", "CopilotChatHistory"))
    {
    }

    public FileCopilotChatLogger(string chatLogFolder)
        : this(chatLogFolder, null)
    {
    }

    public FileCopilotChatLogger(string chatLogFolder, string? chatHistoryFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chatLogFolder);
        ChatLogFolder = chatLogFolder;
        ChatHistoryFolder = string.IsNullOrWhiteSpace(chatHistoryFolder) ? null : chatHistoryFolder;
    }

    public string ChatLogFolder { get; }

    public string? ChatHistoryFolder { get; }

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

            await WriteChatHistoryAsync(sessionId, chatMessage).ConfigureAwait(false);
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

    private async Task WriteChatHistoryAsync(Guid sessionId, CopilotChatMessage chatMessage)
    {
        if (string.IsNullOrWhiteSpace(ChatHistoryFolder))
        {
            return;
        }

        ChatHistoryFileInfo chatHistoryFileInfo = GetSessionHistoryFileInfo(sessionId, chatMessage.CreatedTime);

        if (!File.Exists(chatHistoryFileInfo.FilePath))
        {
            await CreateChatHistoryFileAsync(chatHistoryFileInfo.FilePath, sessionId, chatMessage).ConfigureAwait(false);
            return;
        }

        await AppendChatHistoryMessageAsync(chatHistoryFileInfo.FilePath, chatMessage).ConfigureAwait(false);
    }

    private ChatHistoryFileInfo GetSessionHistoryFileInfo(Guid sessionId, DateTimeOffset createdTime)
    {
        if (_sessionHistoryFileInfoMap.TryGetValue(sessionId, out ChatHistoryFileInfo historyFileInfo))
        {
            return historyFileInfo;
        }

        Directory.CreateDirectory(ChatHistoryFolder!);
        historyFileInfo = new ChatHistoryFileInfo(Path.Join(ChatHistoryFolder, $"{createdTime:yyyyMMdd_HHmmss}_{sessionId:N}.xml"));
        _sessionHistoryFileInfoMap[sessionId] = historyFileInfo;
        return historyFileInfo;
    }

    private static Task CreateChatHistoryFileAsync(string historyFilePath, Guid sessionId, CopilotChatMessage chatMessage)
    {
        var document = new XDocument(
            new XElement("CopilotChatSessionHistory",
                new XAttribute("SessionId", sessionId),
                new XAttribute("CreatedTime", chatMessage.CreatedTime.ToString("o")),
                new XElement("Messages", CreateMessageElement(chatMessage))));

        return SaveFormattedChatHistoryDocumentAsync(historyFilePath, document);
    }

    private static async Task AppendChatHistoryMessageAsync(string historyFilePath, CopilotChatMessage chatMessage)
    {
        await using var fileStream = new FileStream(historyFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        string closingFragment = await GetChatHistoryClosingFragmentAsync(fileStream).ConfigureAwait(false);
        int trailingClosingFragmentByteCount = Utf8EncodingWithoutBom.GetByteCount(closingFragment);
        if (fileStream.Length < trailingClosingFragmentByteCount)
        {
            throw new InvalidDataException($"聊天历史文件 '{historyFilePath}' 的内容格式无效，无法追加消息。");
        }

        fileStream.SetLength(fileStream.Length - trailingClosingFragmentByteCount);
        fileStream.Seek(0, SeekOrigin.End);

        await using var streamWriter = new StreamWriter(fileStream, Utf8EncodingWithoutBom);
        string appendedXml = $"{Environment.NewLine}{CreateIndentedMessageElementXml(chatMessage)}{FormattedChatHistoryClosingFragment}";
        await streamWriter.WriteAsync(appendedXml).ConfigureAwait(false);
        await streamWriter.FlushAsync().ConfigureAwait(false);
    }

    private static async Task SaveFormattedChatHistoryDocumentAsync(string historyFilePath, XDocument document)
    {
        await using var fileStream = new FileStream(historyFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var xmlWriter = XmlWriter.Create(fileStream, CreateChatHistoryXmlWriterSettings());
        await document.SaveAsync(xmlWriter, CancellationToken.None).ConfigureAwait(false);
        await xmlWriter.FlushAsync().ConfigureAwait(false);
    }

    private static XmlWriterSettings CreateChatHistoryXmlWriterSettings()
    {
        return new XmlWriterSettings()
        {
            Async = true,
            Encoding = Utf8EncodingWithoutBom,
            Indent = true,
            IndentChars = "  ",
            NewLineChars = Environment.NewLine,
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = true
        };
    }

    private static async Task<string> GetChatHistoryClosingFragmentAsync(FileStream fileStream)
    {
        if (await EndsWithAsync(fileStream, FormattedChatHistoryClosingFragment).ConfigureAwait(false))
        {
            return FormattedChatHistoryClosingFragment;
        }

        if (await EndsWithAsync(fileStream, CompactChatHistoryClosingFragment).ConfigureAwait(false))
        {
            return CompactChatHistoryClosingFragment;
        }

        throw new InvalidDataException($"聊天历史文件 '{fileStream.Name}' 的内容格式无效，无法追加消息。");
    }

    private static async Task<bool> EndsWithAsync(FileStream fileStream, string fragment)
    {
        byte[] expectedBytes = Utf8EncodingWithoutBom.GetBytes(fragment);
        if (fileStream.Length < expectedBytes.Length)
        {
            return false;
        }

        byte[] buffer = new byte[expectedBytes.Length];
        fileStream.Seek(-expectedBytes.Length, SeekOrigin.End);
        int readLength = await fileStream.ReadAsync(buffer).ConfigureAwait(false);
        return readLength == expectedBytes.Length && buffer.AsSpan().SequenceEqual(expectedBytes);
    }

    private static string CreateIndentedMessageElementXml(CopilotChatMessage chatMessage)
    {
        string messageXml = CreateMessageElement(chatMessage).ToString(SaveOptions.None);
        using var stringReader = new StringReader(messageXml);
        var builder = new StringBuilder();
        bool isFirstLine = true;

        while (stringReader.ReadLine() is { } line)
        {
            if (!isFirstLine)
            {
                builder.AppendLine();
            }

            builder.Append(MessageElementIndent)
                .Append(line);
            isFirstLine = false;
        }

        return builder.ToString();
    }

    private static XElement CreateMessageElement(CopilotChatMessage chatMessage)
    {
        var messageElement = new XElement("Message",
            new XAttribute("Role", chatMessage.Role),
            new XAttribute("Author", chatMessage.Author),
            new XAttribute("CreatedTime", chatMessage.CreatedTime.ToString("o")),
            new XAttribute("IsPresetInfo", chatMessage.IsPresetInfo),
            new XElement("Content", chatMessage.Content),
            new XElement("Reason", chatMessage.Reason),
            new XElement("MessageItems", chatMessage.MessageItems.Select(CreateMessageItemElement)));

        if (chatMessage.UsageDetails is { } usageDetails)
        {
            messageElement.Add(CreateUsageDetailsElement(usageDetails));
        }

        return messageElement;
    }

    private static XElement CreateMessageItemElement(ICopilotChatMessageItem messageItem)
    {
        ArgumentNullException.ThrowIfNull(messageItem);

        return messageItem switch
        {
            CopilotChatTextItem textItem => new XElement("TextItem",
                new XAttribute("Text", textItem.Text)),
            CopilotChatReasoningItem reasoningItem => new XElement("ReasoningItem",
                new XAttribute("Text", reasoningItem.Text)),
            CopilotChatToolItem toolItem => new XElement("ToolItem",
                new XAttribute("CallId", toolItem.CallId),
                new XAttribute("ToolName", toolItem.ToolName),
                new XElement("Input", toolItem.InputText),
                new XElement("Output", toolItem.OutputText)),
            _ => throw new InvalidOperationException($"不支持的聊天消息片段类型: {messageItem.GetType().FullName}")
        };
    }

    private static XElement CreateUsageDetailsElement(Microsoft.Extensions.AI.UsageDetails usageDetails)
    {
        var usageDetailsElement = new XElement("UsageDetails");
        AddUsageAttribute(usageDetailsElement, "TotalTokenCount", usageDetails.TotalTokenCount);
        AddUsageAttribute(usageDetailsElement, "InputTokenCount", usageDetails.InputTokenCount);
        AddUsageAttribute(usageDetailsElement, "OutputTokenCount", usageDetails.OutputTokenCount);
        AddUsageAttribute(usageDetailsElement, "ReasoningTokenCount", usageDetails.ReasoningTokenCount);
        AddUsageAttribute(usageDetailsElement, "CachedInputTokenCount", usageDetails.CachedInputTokenCount);
        return usageDetailsElement;
    }

    private static void AddUsageAttribute(XElement parent, string name, long? value)
    {
        if (value is null)
        {
            return;
        }

        parent.Add(new XAttribute(name, value.Value.ToString(CultureInfo.InvariantCulture)));
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

    private readonly record struct ChatHistoryFileInfo(string FilePath);
}
