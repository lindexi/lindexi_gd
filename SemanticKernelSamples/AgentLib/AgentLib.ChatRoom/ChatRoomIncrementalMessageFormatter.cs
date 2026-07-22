namespace AgentLib.ChatRoom;

internal static class ChatRoomIncrementalMessageFormatter
{
    internal static string Format(
        string content,
        bool isHuman,
        string? senderRoleName,
        bool omitHumanPrefix)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (isHuman)
        {
            return omitHumanPrefix ? content : $"用户说：{content}";
        }

        string senderName = string.IsNullOrWhiteSpace(senderRoleName)
            ? "另一位参与者"
            : senderRoleName;
        return $"{senderName}说：{content}";
    }
}
