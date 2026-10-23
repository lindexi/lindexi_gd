using AgentLib.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace AgentLib.Model;

/// <summary>
/// 表示聊天消息中的音频片段。
/// </summary>
public sealed class CopilotChatAudioItem : NotifyBase, ICopilotChatMessageItem
{
    public CopilotChatAudioItem(BinaryData data, string mimeType)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentHelper.ThrowIfNullOrWhiteSpace(mimeType);

        _data = data;
        MimeType = mimeType;
    }

    /// <summary>
    /// 音频的二进制数据。
    /// </summary>
    public BinaryData Data
    {
        get => _data;
        private set
        {
            if (!SetField(ref _data, value))
            {
                return;
            }

            OnPropertyChanged(nameof(DisplayText));
        }
    }

    private BinaryData _data;

    /// <summary>
    /// 音频的 MIME 类型，例如 <c>audio/wav</c>、<c>audio/mpeg</c>。
    /// </summary>
    public string MimeType
    {
        get => _mimeType;
        private set
        {
            string normalizedValue = string.IsNullOrWhiteSpace(value) ? "audio/wav" : value;
            if (!SetField(ref _mimeType, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(DisplayText));
        }
    }

    private string _mimeType = "audio/wav";

    /// <summary>
    /// 调试/日志用的可读文本。
    /// </summary>
    public string DisplayText => $"[音频: {MimeType}]";

    /// <inheritdoc/>
    ICopilotChatMessageItem ICopilotChatMessageItem.Clone() => new CopilotChatAudioItem(Data, MimeType);
}