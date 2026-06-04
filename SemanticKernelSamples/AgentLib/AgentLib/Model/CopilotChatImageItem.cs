using System;

namespace AgentLib.Model;

/// <summary>
/// 表示聊天消息中的图片片段。
/// </summary>
public sealed class CopilotChatImageItem : NotifyBase, ICopilotChatMessageItem
{
    public CopilotChatImageItem(BinaryData data, string mimeType)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        _data = data;
        MimeType = mimeType;
    }

    /// <summary>
    /// 图片的二进制数据。
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

            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    private BinaryData _data;

    /// <summary>
    /// 图片的 MIME 类型，例如 <c>image/png</c>、<c>image/jpeg</c>。
    /// </summary>
    public string MimeType
    {
        get => _mimeType;
        private set
        {
            string normalizedValue = string.IsNullOrWhiteSpace(value) ? "image/png" : value;
            if (!SetField(ref _mimeType, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(DisplayText));
        }
    }

    private string _mimeType = "image/png";

    /// <summary>
    /// 是否有图片数据。
    /// </summary>
    public bool HasData => Data is not null;

    /// <summary>
    /// 调试/日志用的可读文本。
    /// </summary>
    public string DisplayText => HasData ? $"[图片: {MimeType}]" : string.Empty;
}