using System.Collections.ObjectModel;

namespace AgentLib.ChatRoom.Domain;

/// <summary>
/// 不可变的聊天室公开消息。
/// </summary>
public sealed record ChatRoomMessage
{
    private static readonly IReadOnlyList<string> EmptyMentionedRoleIds = Array.Empty<string>();

    /// <summary>
    /// 创建公开消息。
    /// </summary>
    public ChatRoomMessage(
        long messageSequence,
        Guid messageId,
        ChatRoomMessageKind kind,
        string content,
        DateTimeOffset timestamp,
        string? senderRoleId = null,
        string? senderRoleName = null,
        IEnumerable<string>? mentionedRoleIds = null,
        string? modelDisplayName = null)
    {
        if (messageSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(messageSequence));
        }
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("消息标识不能为空。", nameof(messageId));
        }
        if (!Enum.IsDefined(typeof(ChatRoomMessageKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
        ArgumentNullException.ThrowIfNull(content);
        if (kind != ChatRoomMessageKind.System
            && (string.IsNullOrWhiteSpace(senderRoleId) || string.IsNullOrWhiteSpace(senderRoleName)))
        {
            throw new ArgumentException("人类和助手消息必须包含发送角色标识与名称。", nameof(senderRoleId));
        }

        MessageSequence = messageSequence;
        MessageId = messageId;
        Kind = kind;
        Content = content;
        Timestamp = timestamp;
        SenderRoleId = NormalizeOptionalValue(senderRoleId);
        SenderRoleName = NormalizeOptionalValue(senderRoleName);
        MentionedRoleIds = CopyMentionedRoleIds(mentionedRoleIds);
        ModelDisplayName = NormalizeOptionalValue(modelDisplayName);
    }

    /// <summary>
    /// 房间内单调递增的消息序号。
    /// </summary>
    public long MessageSequence { get; }

    /// <summary>
    /// 消息唯一标识。
    /// </summary>
    public Guid MessageId { get; }

    /// <summary>
    /// 消息种类。
    /// </summary>
    public ChatRoomMessageKind Kind { get; }

    /// <summary>
    /// 公开可见的纯文本内容。
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// 消息提交时间。
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// 发送角色标识；系统消息可以为空。
    /// </summary>
    public string? SenderRoleId { get; }

    /// <summary>
    /// 发送角色显示名；系统消息可以为空。
    /// </summary>
    public string? SenderRoleName { get; }

    /// <summary>
    /// 消息中提及的角色标识，按出现顺序去重。
    /// </summary>
    public IReadOnlyList<string> MentionedRoleIds { get; }

    /// <summary>
    /// 助手消息实际采用的模型显示名。
    /// </summary>
    public string? ModelDisplayName { get; }

    private static IReadOnlyList<string> CopyMentionedRoleIds(IEnumerable<string>? mentionedRoleIds)
    {
        if (mentionedRoleIds is null)
        {
            return EmptyMentionedRoleIds;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var values = new List<string>();
        foreach (string roleId in mentionedRoleIds)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                throw new ArgumentException("被提及的角色标识不能为空。", nameof(mentionedRoleIds));
            }

            string normalizedRoleId = roleId.Trim();
            if (seen.Add(normalizedRoleId))
            {
                values.Add(normalizedRoleId);
            }
        }

        return values.Count == 0
            ? EmptyMentionedRoleIds
            : new ReadOnlyCollection<string>(values.ToArray());
    }

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
