using AgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace AgentLib.Logging;

internal static class CopilotChatHistoryXmlCodec
{
    public static XElement CreateRootElement(
        Guid sessionId,
        DateTimeOffset startedTime,
        string title,
        int formatVersion,
        IEnumerable<CopilotChatMessage> messages,
        JsonElement? agentSessionState)
    {
        var rootElement = new XElement("CopilotChatSessionHistory",
            new XAttribute("FormatVersion", formatVersion),
            new XAttribute("SessionId", sessionId),
            new XAttribute("StartedTime", startedTime.ToString("o")),
            new XAttribute("CreatedTime", startedTime.ToString("o")),
            new XAttribute("Title", title));
        AppendAgentSessionStateElement(rootElement, agentSessionState);
        rootElement.Add(new XElement("Messages", messages.Select(CreateMessageElement)));
        return rootElement;
    }

    public static void UpdateRootElement(
        XElement rootElement,
        string? title,
        int formatVersion,
        JsonElement? agentSessionState)
    {
        rootElement.SetAttributeValue("FormatVersion", formatVersion);
        if (!string.IsNullOrWhiteSpace(title))
        {
            rootElement.SetAttributeValue("Title", title);
        }

        rootElement.Element("AgentSessionState")?.Remove();
        AppendAgentSessionStateElement(rootElement, agentSessionState);
    }

    public static XElement CreateMessageElement(CopilotChatMessage chatMessage)
    {
        return new XElement("Message",
            new XAttribute("Role", chatMessage.Role),
            new XAttribute("Author", chatMessage.Author),
            new XAttribute("CreatedTime", chatMessage.CreatedTime.ToString("o")),
            new XAttribute("IsPresetInfo", chatMessage.IsPresetInfo),
            new XElement("Content", chatMessage.Content),
            new XElement("Reason", chatMessage.Reason),
            new XElement("MessageItems", chatMessage.MessageItems.Select(CreateMessageItemElement)),
            chatMessage.TotalUsageDetails is { } usageDetails ? CreateUsageDetailsElement(usageDetails) : null);
    }

    public static CopilotChatSessionPersistenceData ReadPersistenceData(XDocument document)
    {
        XElement rootElement = document.Root ?? throw new InvalidDataException("聊天历史文件缺少根节点。");
        Guid sessionId = ParseRequiredGuid(rootElement, "SessionId");
        DateTimeOffset startedTime = ParseStartedTime(rootElement);
        int formatVersion = ParseOptionalInt(rootElement.Attribute("FormatVersion")?.Value) ?? 1;
        XElement messagesElement = rootElement.Element("Messages")
                                   ?? throw new InvalidDataException("聊天历史文件缺少 Messages 节点。");
        List<CopilotChatMessage> messages = messagesElement.Elements("Message").Select(ReadMessage).ToList();
        JsonElement? agentSessionState = ReadAgentSessionState(rootElement);
        return new CopilotChatSessionPersistenceData
        {
            FormatVersion = formatVersion,
            SessionId = sessionId,
            StartedTime = startedTime,
            Title = rootElement.Attribute("Title")?.Value ?? string.Empty,
            Messages = messages,
            AgentSessionState = agentSessionState,
        };
    }

    private static CopilotChatMessage ReadMessage(XElement messageElement)
    {
        ChatRole role = new(messageElement.Attribute("Role")?.Value ?? throw new InvalidDataException("消息缺少 Role 属性。"));
        DateTimeOffset createdTime = ParseRequiredDateTimeOffset(messageElement, "CreatedTime");
        bool isPresetInfo = bool.TryParse(messageElement.Attribute("IsPresetInfo")?.Value, out bool parsedPresetInfo) && parsedPresetInfo;
        XElement? messageItemsElement = messageElement.Element("MessageItems");
        List<ICopilotChatMessageItem> messageItems = messageItemsElement is null
            ? CreateLegacyMessageItems(messageElement)
            : messageItemsElement.Elements().Select(ReadMessageItem).ToList();
        UsageDetails? usageDetails = ReadUsageDetails(messageElement.Element("UsageDetails"));
        return CopilotChatMessage.Restore(role, createdTime, isPresetInfo, messageItems, usageDetails);
    }

    private static List<ICopilotChatMessageItem> CreateLegacyMessageItems(XElement messageElement)
    {
        var items = new List<ICopilotChatMessageItem>(2);
        string? reason = messageElement.Element("Reason")?.Value;
        string? content = messageElement.Element("Content")?.Value;
        if (!string.IsNullOrEmpty(reason))
        {
            items.Add(new CopilotChatReasoningItem(reason));
        }

        if (!string.IsNullOrEmpty(content))
        {
            items.Add(new CopilotChatTextItem(content));
        }

        return items;
    }

    private static ICopilotChatMessageItem ReadMessageItem(XElement element)
    {
        return element.Name.LocalName switch
        {
            "TextItem" => new CopilotChatTextItem(element.Attribute("Text")?.Value ?? string.Empty),
            "ReasoningItem" => new CopilotChatReasoningItem(element.Attribute("Text")?.Value ?? string.Empty),
            "ImageItem" => new CopilotChatImageItem(BinaryData.FromBytes(Convert.FromBase64String(element.Value)),
                element.Attribute("MimeType")?.Value ?? "application/octet-stream"),
            "AudioItem" => new CopilotChatAudioItem(BinaryData.FromBytes(Convert.FromBase64String(element.Value)),
                element.Attribute("MimeType")?.Value ?? "application/octet-stream"),
            "ToolItem" => new CopilotChatToolItem(
                element.Attribute("CallId")?.Value ?? string.Empty,
                element.Attribute("ToolName")?.Value ?? string.Empty,
                element.Element("Input")?.Value,
                element.Element("Output")?.Value),
            "ApprovalToolItem" => ReadApprovalToolItem(element),
            "SubAgentItem" => ReadSubAgentItem(element),
            _ => throw new InvalidDataException($"不支持的聊天消息片段类型：{element.Name.LocalName}")
        };
    }

    private static CopilotChatApprovalToolItem ReadApprovalToolItem(XElement element)
    {
        var item = new CopilotChatApprovalToolItem(
            element.Attribute("CallId")?.Value ?? string.Empty,
            element.Attribute("ToolName")?.Value ?? string.Empty,
            element.Element("Input")?.Value,
            element.Element("ApprovalDescription")?.Value,
            element.Attribute("DisplayName")?.Value)
        {
            OutputText = element.Element("Output")?.Value ?? string.Empty,
        };
        CopilotToolApprovalState state = Enum.TryParse(element.Attribute("ApprovalState")?.Value, out CopilotToolApprovalState parsedState)
            ? parsedState
            : CopilotToolApprovalState.Pending;
        switch (state)
        {
            case CopilotToolApprovalState.Approved:
                item.Approve();
                break;
            case CopilotToolApprovalState.Rejected:
                item.Reject(element.Element("DecisionReason")?.Value);
                break;
            case CopilotToolApprovalState.Canceled:
                item.Cancel();
                break;
        }

        return item;
    }

    private static CopilotChatSubAgentItem ReadSubAgentItem(XElement element)
    {
        var item = new CopilotChatSubAgentItem(
            element.Attribute("CallId")?.Value ?? string.Empty,
            element.Attribute("ToolName")?.Value ?? string.Empty,
            element.Element("Input")?.Value,
            element.Element("Output")?.Value);
        XElement? messageItemsElement = element.Element("MessageItems");
        if (messageItemsElement is not null)
        {
            foreach (ICopilotChatMessageItem messageItem in messageItemsElement.Elements().Select(ReadMessageItem))
            {
                item.MessageItems.Add(messageItem);
            }
        }

        return item;
    }

    private static XElement CreateMessageItemElement(ICopilotChatMessageItem messageItem)
    {
        return messageItem switch
        {
            CopilotChatTextItem textItem => new XElement("TextItem", new XAttribute("Text", textItem.Text)),
            CopilotChatReasoningItem reasoningItem => new XElement("ReasoningItem", new XAttribute("Text", reasoningItem.Text)),
            CopilotChatImageItem imageItem => new XElement("ImageItem", new XAttribute("MimeType", imageItem.MimeType), Convert.ToBase64String(imageItem.Data.ToArray())),
            CopilotChatAudioItem audioItem => new XElement("AudioItem", new XAttribute("MimeType", audioItem.MimeType), Convert.ToBase64String(audioItem.Data.ToArray())),
            CopilotChatToolItem toolItem => new XElement("ToolItem",
                new XAttribute("CallId", toolItem.CallId),
                new XAttribute("ToolName", toolItem.ToolName),
                new XElement("Input", toolItem.InputText),
                new XElement("Output", toolItem.OutputText)),
            CopilotChatApprovalToolItem approvalToolItem => new XElement("ApprovalToolItem",
                new XAttribute("CallId", approvalToolItem.CallId),
                new XAttribute("ToolName", approvalToolItem.ToolName),
                new XAttribute("DisplayName", approvalToolItem.DisplayName),
                new XAttribute("ApprovalState", approvalToolItem.ApprovalState),
                new XElement("Input", approvalToolItem.InputText),
                new XElement("Output", approvalToolItem.OutputText),
                new XElement("ApprovalDescription", approvalToolItem.ApprovalDescription),
                new XElement("DecisionReason", approvalToolItem.DecisionReason)),
            CopilotChatSubAgentItem subAgentItem => new XElement("SubAgentItem",
                new XAttribute("CallId", subAgentItem.CallId),
                new XAttribute("ToolName", subAgentItem.ToolName),
                new XElement("Input", subAgentItem.InputText),
                new XElement("Output", subAgentItem.OutputText),
                new XElement("MessageItems", subAgentItem.MessageItems.Select(CreateMessageItemElement))),
            _ => throw new InvalidOperationException($"不支持的聊天消息片段类型: {messageItem.GetType().FullName}")
        };
    }

    private static XElement CreateUsageDetailsElement(UsageDetails usageDetails)
    {
        var element = new XElement("UsageDetails");
        AddUsageAttribute(element, "TotalTokenCount", usageDetails.TotalTokenCount);
        AddUsageAttribute(element, "InputTokenCount", usageDetails.InputTokenCount);
        AddUsageAttribute(element, "OutputTokenCount", usageDetails.OutputTokenCount);
        AddUsageAttribute(element, "ReasoningTokenCount", usageDetails.ReasoningTokenCount);
        AddUsageAttribute(element, "CachedInputTokenCount", usageDetails.CachedInputTokenCount);
        return element;
    }

    private static UsageDetails? ReadUsageDetails(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        return new UsageDetails
        {
            TotalTokenCount = ParseOptionalLong(element.Attribute("TotalTokenCount")?.Value),
            InputTokenCount = ParseOptionalLong(element.Attribute("InputTokenCount")?.Value),
            OutputTokenCount = ParseOptionalLong(element.Attribute("OutputTokenCount")?.Value),
            ReasoningTokenCount = ParseOptionalLong(element.Attribute("ReasoningTokenCount")?.Value),
            CachedInputTokenCount = ParseOptionalLong(element.Attribute("CachedInputTokenCount")?.Value),
        };
    }

    private static JsonElement? ReadAgentSessionState(XElement rootElement)
    {
        string? json = rootElement.Element("AgentSessionState")?.Value;
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static void AppendAgentSessionStateElement(XElement rootElement, JsonElement? agentSessionState)
    {
        if (agentSessionState is JsonElement state)
        {
            rootElement.AddFirst(new XElement("AgentSessionState", new XCData(state.GetRawText())));
        }
    }

    private static DateTimeOffset ParseStartedTime(XElement rootElement)
    {
        string? value = rootElement.Attribute("StartedTime")?.Value ?? rootElement.Attribute("CreatedTime")?.Value;
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset result)
            ? result
            : throw new InvalidDataException("聊天历史文件缺少有效的 StartedTime 或 CreatedTime 属性。");
    }

    private static Guid ParseRequiredGuid(XElement element, string attributeName)
    {
        return Guid.TryParse(element.Attribute(attributeName)?.Value, out Guid result)
            ? result
            : throw new InvalidDataException($"聊天历史文件缺少有效的 {attributeName} 属性。");
    }

    private static DateTimeOffset ParseRequiredDateTimeOffset(XElement element, string attributeName)
    {
        return DateTimeOffset.TryParse(element.Attribute(attributeName)?.Value, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out DateTimeOffset result)
            ? result
            : throw new InvalidDataException($"消息缺少有效的 {attributeName} 属性。");
    }

    private static void AddUsageAttribute(XElement parent, string name, long? value)
    {
        if (value is not null)
        {
            parent.Add(new XAttribute(name, value.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static int? ParseOptionalInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : null;

    private static long? ParseOptionalLong(string? value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result) ? result : null;
}
