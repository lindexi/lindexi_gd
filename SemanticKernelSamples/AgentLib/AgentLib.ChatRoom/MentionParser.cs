using AgentLib.ChatRoom.Model;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AgentLib.ChatRoom;

/// <summary>
/// @mention 解析工具。从消息文本中提取被 @ 的角色 ID 列表。
/// </summary>
internal static class MentionParser
{
    // 匹配 @[Role Name]（优先）或 @RoleName 格式
    // 捕获组 1：方括号名称（@[xxx] 内的内容）
    // 捕获组 2：常规名称（@后连续非空格字符，直到遇到空格或结尾）
    private static readonly Regex MentionRegex = new(
        @"@\[([^\]]+)\]|@(\S+?)(?:\s|$)",
        RegexOptions.Compiled);

    /// <summary>
    /// 从消息文本中解析被 @ 的角色 ID 列表。
    /// 按 @ 在文本中的出现顺序返回，同一条消息中同一角色多次 @ 只保留首次出现的位置。
    /// </summary>
    /// <param name="content">消息文本内容。</param>
    /// <param name="roles">当前聊天室中的角色列表，用于 RoleName → RoleId 映射。</param>
    /// <returns>被提及的角色 RoleId 列表（去重，保留首次出现顺序）。</returns>
    public static IReadOnlyList<string> ParseMentions(string content, IReadOnlyList<ChatRoomRole> roles)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(roles);

        if (roles.Count == 0)
        {
            return [];
        }

        // 建立快速查找表：RoleName → RoleId（不区分大小写）
        var nameToId = new Dictionary<string, string>(roles.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            if (!string.IsNullOrWhiteSpace(role.Definition.RoleName))
            {
                // 同名以先注册的为准
                nameToId.TryAdd(role.Definition.RoleName, role.Definition.RoleId);
            }
        }

        if (nameToId.Count == 0)
        {
            return [];
        }

        var result = new List<string>();
        var seen = new HashSet<string>();

        foreach (Match match in MentionRegex.Matches(content))
        {
            // 捕获组 1：@[Role Name]，捕获组 2：@RoleName
            string name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (nameToId.TryGetValue(name, out string? roleId) && seen.Add(roleId))
            {
                result.Add(roleId);
            }
        }

        return result;
    }

    /// <summary>
    /// 校验角色名是否能被 @mention 正确解析。
    /// 构造模拟消息并调用 <see cref="ParseMentions"/> 验证，确保角色名在实际使用中能被 @ 正确匹配。
    /// </summary>
    /// <param name="roleName">待校验的角色名。</param>
    /// <returns>能被解析返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    internal static bool CanParseRoleName(string roleName)
    {
        ArgumentNullException.ThrowIfNull(roleName);
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("角色名不能为空或空白字符。", nameof(roleName));
        }

        // 构造一个临时角色用于 ParseMentions 的 RoleName → RoleId 映射
        var tempRole = new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "validation-test",
            RoleName = roleName,
        });

        // 构造模拟消息：@角色名 后跟空格（最宽松的 @ 格式）
        string testMessage = $"@{roleName} ";

        // 用 ParseMentions 尝试解析
        IReadOnlyList<string> result = ParseMentions(testMessage, [tempRole]);

        // 如果解析结果包含临时角色的 RoleId，说明角色名能被正确提取和匹配
        return result.Count > 0 && result[0] == "validation-test";
    }
}