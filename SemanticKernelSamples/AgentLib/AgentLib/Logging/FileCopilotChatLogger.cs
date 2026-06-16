using AgentLib.Core;
using AgentLib.Model;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace AgentLib.Logging;

/// <summary>
/// 将聊天消息记录到文件系统的日志记录器。支持纯文本日志和 XML 格式的聊天历史记录。
/// </summary>
public sealed class FileCopilotChatLogger : ICopilotChatLogger
{
    private const string CompactChatHistoryClosingFragment = "</Messages></CopilotChatSessionHistory>";
    private const string MessageElementIndent = "    ";
    private static readonly Encoding Utf8EncodingWithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly string FormattedChatHistoryClosingFragment = $"{Environment.NewLine}  </Messages>{Environment.NewLine}</CopilotChatSessionHistory>";

    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly Dictionary<Guid, string> _sessionLogFilePathMap = [];
    private readonly Dictionary<Guid, ChatHistoryFileInfo> _sessionHistoryFileInfoMap = [];

    /// <summary>
    /// 使用默认日志目录创建日志记录器。
    /// </summary>
    public FileCopilotChatLogger()
        : this(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AgentLib", "CopilotChatLogs"))
    {
    }

    /// <summary>
    /// 使用指定的日志目录和历史记录目录创建日志记录器。
    /// </summary>
    /// <param name="chatLogFolder">聊天日志文件夹路径。</param>
    /// <param name="chatHistoryFolder">聊天历史记录文件夹路径，可为 <see langword="null"/>。</param>
    public FileCopilotChatLogger(string chatLogFolder, string? chatHistoryFolder = null)
    {
        ArgumentHelper.ThrowIfNullOrWhiteSpace(chatLogFolder);
        ChatLogFolder = chatLogFolder;
        ChatHistoryFolder = string.IsNullOrWhiteSpace(chatHistoryFolder) ? null : chatHistoryFolder;
    }

    /// <summary>
    /// 聊天日志文件夹路径。
    /// </summary>
    public string ChatLogFolder { get; }

    /// <summary>
    /// 聊天历史记录文件夹路径，可为 <see langword="null"/>。
    /// </summary>
    public string? ChatHistoryFolder { get; }

    /// <inheritdoc/>
    public async Task LogMessageAsync(Guid sessionId, CopilotChatMessage chatMessage,
        ICopilotChatSessionStateProvider? agentSessionStateProvider = null)
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
            JsonElement? agentSessionState = agentSessionStateProvider is null
                ? null
                : await agentSessionStateProvider.GetSerializedSessionStateAsync().ConfigureAwait(false);
            await WriteChatHistoryAsync(sessionId, chatMessage, agentSessionState).ConfigureAwait(false);
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

    private async Task WriteChatHistoryAsync(Guid sessionId, CopilotChatMessage chatMessage, JsonElement? agentSessionState)
    {
        if (string.IsNullOrWhiteSpace(ChatHistoryFolder))
        {
            return;
        }

        ChatHistoryFileInfo chatHistoryFileInfo = GetSessionHistoryFileInfo(sessionId, chatMessage.CreatedTime);

        if (!File.Exists(chatHistoryFileInfo.FilePath))
        {
            await CreateChatHistoryFileAsync(chatHistoryFileInfo.FilePath, sessionId, chatMessage, agentSessionState).ConfigureAwait(false);
            return;
        }

        await AppendChatHistoryMessageAsync(chatHistoryFileInfo.FilePath, chatMessage, agentSessionState).ConfigureAwait(false);
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

    private static Task CreateChatHistoryFileAsync(string historyFilePath, Guid sessionId, CopilotChatMessage chatMessage,
        JsonElement? agentSessionState)
    {
        var rootElement = new XElement("CopilotChatSessionHistory",
            new XAttribute("SessionId", sessionId),
            new XAttribute("CreatedTime", chatMessage.CreatedTime.ToString("o")));

        AppendAgentSessionStateElement(rootElement, agentSessionState);
        rootElement.Add(new XElement("Messages", CreateMessageElement(chatMessage)));

        var document = new XDocument(rootElement);

        return SaveFormattedChatHistoryDocumentAsync(historyFilePath, document);
    }

    private static async Task AppendChatHistoryMessageAsync(string historyFilePath, CopilotChatMessage chatMessage,
        JsonElement? agentSessionState)
    {
        var document = await LoadChatHistoryDocumentAsync(historyFilePath).ConfigureAwait(false);
        XElement rootElement = document.Root ?? throw new InvalidDataException($"聊天历史文件 '{historyFilePath}' 的内容格式无效，无法追加消息。");

        rootElement.Element("AgentSessionState")?.Remove();
        AppendAgentSessionStateElement(rootElement, agentSessionState);

        XElement messagesElement = rootElement.Element("Messages")
                                   ?? throw new InvalidDataException($"聊天历史文件 '{historyFilePath}' 的内容格式无效，无法追加消息。");
        messagesElement.Add(CreateMessageElement(chatMessage));

        await SaveFormattedChatHistoryDocumentAsync(historyFilePath, document).ConfigureAwait(false);
    }

    private static async Task<XDocument> LoadChatHistoryDocumentAsync(string historyFilePath)
    {
        await using var fileStream = new FileStream(historyFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);
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

    private static void AppendAgentSessionStateElement(XElement rootElement, JsonElement? agentSessionState)
    {
        if (agentSessionState is not JsonElement serializedAgentSessionState)
        {
            return;
        }

        rootElement.AddFirst(new XElement("AgentSessionState",
            new XCData(serializedAgentSessionState.GetRawText())));
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

        if (chatMessage.TotalUsageDetails is { } usageDetails)
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
            CopilotChatTextItem textItem => new XElement("TextItem", new XAttribute("Text", textItem.Text)),
            CopilotChatReasoningItem reasoningItem => new XElement("ReasoningItem", new XAttribute("Text", reasoningItem.Text)),
            CopilotChatToolItem toolItem => new XElement("ToolItem",
                new XAttribute("CallId", toolItem.CallId),
                new XAttribute("ToolName", toolItem.ToolName),
                new XElement("Input", toolItem.InputText),
                new XElement("Output", toolItem.OutputText)),
            CopilotChatSubAgentItem subAgentItem => new XElement("SubAgentItem",
                new XAttribute("CallId", subAgentItem.CallId),
                new XAttribute("ToolName", subAgentItem.ToolName),
                new XElement("Input", subAgentItem.InputText),
                new XElement("Output", subAgentItem.OutputText),
                new XElement("MessageItems", subAgentItem.MessageItems.Select(CreateMessageItemElement))),
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
