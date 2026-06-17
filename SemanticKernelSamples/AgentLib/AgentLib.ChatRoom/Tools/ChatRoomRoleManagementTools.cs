using AgentLib.ChatRoom.Model;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AgentLib.ChatRoom.Tools;

/// <summary>
/// 提供聊天室角色管理相关的 <see cref="AITool"/> 集合。
/// 包含 list_characters、create_character、edit_character 三个工具。
/// </summary>
public static class ChatRoomRoleManagementTools
{
    private const int SystemPromptSummaryMaxLength = 50;

    /// <summary>
    /// 创建角色管理工具集合。
    /// </summary>
    /// <param name="chatRoomManager">聊天室管理器，用于操作角色集合。</param>
    /// <returns>包含三个角色管理工具的列表。</returns>
    public static IReadOnlyList<AITool> CreateTools(ChatRoomManager chatRoomManager)
    {
        ArgumentNullException.ThrowIfNull(chatRoomManager);

        return new AITool[]
        {
            AIFunctionFactory.Create(
                (CancellationToken _) => ListCharacters(chatRoomManager),
                name: "list_characters",
                description: "列出当前聊天室中的所有角色，包括角色ID（RoleId）、名称（RoleName）、人设摘要、模型信息和参与模式。"),
            AIFunctionFactory.Create(
                (string roleName, string systemPrompt, string? modelId, string? modelProviderId, string? memoryContent, CancellationToken _) =>
                    CreateCharacter(chatRoomManager, roleName, systemPrompt, modelId, modelProviderId, memoryContent),
                name: "create_character",
                description: "创建一个新角色并立即加入当前聊天室。参数：roleName（必填，角色显示名）、systemPrompt（必填，角色人设）、modelId（可选）、modelProviderId（可选）、memoryContent（可选，长期记忆）。"),
            AIFunctionFactory.Create(
                (string roleId, string? roleName, string? systemPrompt, string? modelId, string? modelProviderId, string? memoryContent, CancellationToken _) =>
                    EditCharacter(chatRoomManager, roleId, roleName, systemPrompt, modelId, modelProviderId, memoryContent),
                name: "edit_character",
                description: "修改已有角色的属性，只更新传入的非空字段。参数：roleId（必填，目标角色的唯一标识）、roleName（可选）、systemPrompt（可选）、modelId（可选）、modelProviderId（可选）、memoryContent（可选）。"),
        };
    }

    private static string ListCharacters(ChatRoomManager chatRoomManager)
    {
        IReadOnlyList<ChatRoomRole> roles = chatRoomManager.Roles;
        if (roles.Count == 0)
        {
            return "当前聊天室中没有角色。";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"当前聊天室共有 {roles.Count} 个角色：");
        sb.AppendLine();
        sb.AppendLine("| RoleId | RoleName | 人设摘要 | 模型 | 参与模式 |");
        sb.AppendLine("|--------|----------|----------|------|----------|");

        foreach (ChatRoomRole role in roles)
        {
            string roleId = role.Definition.RoleId;
            string roleName = role.Definition.RoleName;
            string systemPrompt = role.Definition.SystemPrompt;
            string modelInfo = GetModelDisplay(role.Definition);
            string participationMode = GetParticipationModeDisplay(role.Definition.ParticipationMode);
            string promptSummary = GetPromptSummary(systemPrompt);

            // 转义可能破坏表格的竖线字符
            sb.AppendLine($"| {roleId} | {EscapeTableValue(roleName)} | {EscapeTableValue(promptSummary)} | {EscapeTableValue(modelInfo)} | {EscapeTableValue(participationMode)} |");
        }

        return sb.ToString().TrimEnd();
    }

    private static string CreateCharacter(
        ChatRoomManager chatRoomManager,
        string roleName,
        string systemPrompt,
        string? modelId,
        string? modelProviderId,
        string? memoryContent)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return "错误：角色名称不能为空。";
        }

        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            return "错误：角色人设不能为空。";
        }

        try
        {
            string roleId = Guid.NewGuid().ToString("N")[..8];

            var definition = new ChatRoomRoleDefinition
            {
                RoleId = roleId,
                RoleName = roleName.Trim(),
                SystemPrompt = systemPrompt.Trim(),
                IsHuman = false,
                ModelProviderId = modelId is not null && !string.IsNullOrWhiteSpace(modelId) ? modelProviderId : null,
                ModelId = modelId is not null && !string.IsNullOrWhiteSpace(modelId) ? modelId : null,
                MemoryContent = memoryContent is not null && !string.IsNullOrWhiteSpace(memoryContent) ? memoryContent.Trim() : null,
                ParticipationMode = ChatRoomParticipationMode.AlwaysParticipate,
            };

            var role = new ChatRoomRole(definition);
            chatRoomManager.Roles.Add(role);

            var sb = new StringBuilder();
            sb.AppendLine("✅ 角色创建成功并已加入聊天室：");
            sb.AppendLine();
            sb.AppendLine($"- RoleId      : {roleId}");
            sb.AppendLine($"- RoleName    : {roleName}");
            sb.AppendLine($"- 人设        : {GetPromptSummary(systemPrompt)}");
            sb.AppendLine($"- 模型        : {GetModelDisplay(definition)}");
            if (!string.IsNullOrWhiteSpace(memoryContent))
            {
                sb.AppendLine($"- 记忆        : {memoryContent.Trim()}");
            }

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"创建角色失败：{ex.Message}";
        }
    }

    private static string EditCharacter(
        ChatRoomManager chatRoomManager,
        string roleId,
        string? roleName,
        string? systemPrompt,
        string? modelId,
        string? modelProviderId,
        string? memoryContent)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return "错误：roleId 不能为空。";
        }

        ChatRoomRole? targetRole = chatRoomManager.Roles.FirstOrDefault(r => r.Definition.RoleId == roleId);

        if (targetRole is null)
        {
            return BuildRoleNotFoundError(chatRoomManager, roleId);
        }

        // 记录修改前的值用于对比
        string oldRoleName = targetRole.Definition.RoleName;
        string oldSystemPrompt = targetRole.Definition.SystemPrompt;
        string oldModelId = targetRole.Definition.ModelId;
        string oldModelProviderId = targetRole.Definition.ModelProviderId;
        string oldMemoryContent = targetRole.Definition.MemoryContent ?? "(空)";

        bool hasChanges = false;
        var changes = new StringBuilder();

        // 部分更新：只更新非 null 的字段
        if (roleName is not null && !string.IsNullOrWhiteSpace(roleName))
        {
            string newName = roleName.Trim();
            if (!string.Equals(oldRoleName, newName, StringComparison.Ordinal))
            {
                changes.AppendLine($"| RoleName | {EscapeTableValue(oldRoleName)} | {EscapeTableValue(newName)} |");
                targetRole.Definition.RoleName = newName;
                hasChanges = true;
            }
        }

        if (systemPrompt is not null && !string.IsNullOrWhiteSpace(systemPrompt))
        {
            string newPrompt = systemPrompt.Trim();
            if (!string.Equals(oldSystemPrompt, newPrompt, StringComparison.Ordinal))
            {
                changes.AppendLine($"| SystemPrompt | {EscapeTableValue(GetPromptSummary(oldSystemPrompt))} | {EscapeTableValue(GetPromptSummary(newPrompt))} |");
                targetRole.Definition.SystemPrompt = newPrompt;
                hasChanges = true;
            }
        }

        // modelProviderId 作为可选参数，为 null 时不更新
        if (modelProviderId is not null && !string.IsNullOrWhiteSpace(modelProviderId))
        {
            string newProvider = modelProviderId.Trim();
            if (!string.Equals(oldModelProviderId, newProvider, StringComparison.Ordinal))
            {
                changes.AppendLine($"| ModelProviderId | {EscapeTableValue(oldModelProviderId ?? "(默认)")} | {EscapeTableValue(newProvider)} |");
                targetRole.Definition.ModelProviderId = newProvider;
                hasChanges = true;
            }
        }

        if (modelId is not null && !string.IsNullOrWhiteSpace(modelId))
        {
            string newModel = modelId.Trim();
            if (!string.Equals(oldModelId, newModel, StringComparison.Ordinal))
            {
                changes.AppendLine($"| ModelId | {EscapeTableValue(oldModelId ?? "(默认)")} | {EscapeTableValue(newModel)} |");
                targetRole.Definition.ModelId = newModel;
                hasChanges = true;
            }
        }

        if (memoryContent is not null && !string.IsNullOrWhiteSpace(memoryContent))
        {
            string newMemory = memoryContent.Trim();
            if (!string.Equals(targetRole.Definition.MemoryContent, newMemory, StringComparison.Ordinal))
            {
                changes.AppendLine($"| MemoryContent | {EscapeTableValue(oldMemoryContent)} | {EscapeTableValue(GetPromptSummary(newMemory))} |");
                targetRole.Definition.MemoryContent = newMemory;
                hasChanges = true;
            }
        }

        if (!hasChanges)
        {
            return $"角色 \"{targetRole.Definition.RoleName}\" ({roleId}) 没有需要更新的字段。";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"✅ 角色 \"{targetRole.Definition.RoleName}\" ({roleId}) 编辑成功：");
        sb.AppendLine();
        sb.AppendLine("| 字段 | 修改前 | 修改后 |");
        sb.AppendLine("|------|--------|--------|");
        sb.Append(changes);

        return sb.ToString().TrimEnd();
    }

    private static string BuildRoleNotFoundError(ChatRoomManager chatRoomManager, string roleId)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"❌ 未找到 RoleId 为 \"{roleId}\" 的角色。当前聊天室中的角色：");
        sb.AppendLine();
        sb.AppendLine("| RoleId | RoleName |");
        sb.AppendLine("|--------|----------|");

        foreach (ChatRoomRole role in chatRoomManager.Roles)
        {
            sb.AppendLine($"| {role.Definition.RoleId} | {EscapeTableValue(role.Definition.RoleName)} |");
        }

        sb.AppendLine();
        sb.AppendLine("请使用 list_characters 工具查看完整角色列表，并使用正确的 RoleId 重试。");

        return sb.ToString().TrimEnd();
    }

    private static string GetPromptSummary(string? systemPrompt)
    {
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            return "(未设置)";
        }

        string trimmed = systemPrompt.Trim();
        return trimmed.Length <= SystemPromptSummaryMaxLength
            ? trimmed
            : trimmed[..SystemPromptSummaryMaxLength] + "...";
    }

    private static string GetModelDisplay(ChatRoomRoleDefinition definition)
    {
        bool hasProvider = !string.IsNullOrWhiteSpace(definition.ModelProviderId);
        bool hasModel = !string.IsNullOrWhiteSpace(definition.ModelId);

        if (hasProvider && hasModel)
        {
            return $"{definition.ModelProviderId}/{definition.ModelId}";
        }

        if (hasModel)
        {
            return definition.ModelId!;
        }

        return "(默认)";
    }

    private static string GetParticipationModeDisplay(ChatRoomParticipationMode mode)
    {
        return mode switch
        {
            ChatRoomParticipationMode.AlwaysParticipate => "总是参与",
            ChatRoomParticipationMode.MentionOnly => "仅被@时参与",
            _ => mode.ToString(),
        };
    }

    private static string EscapeTableValue(string value)
    {
        return value.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");
    }
}