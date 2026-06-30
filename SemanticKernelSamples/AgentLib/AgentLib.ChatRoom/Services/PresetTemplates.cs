using System;
using System.Collections.Generic;

using AgentLib.ChatRoom.Model;

namespace AgentLib.ChatRoom.Services;

/// <summary>
/// 预置角色模板定义。首次启动时写入模板目录，降低使用门槛。
/// </summary>
internal static class PresetTemplates
{
    /// <summary>
    /// 获取所有预置模板列表。
    /// </summary>
    public static IReadOnlyList<RoleTemplate> GetPresets()
    {
        var now = DateTimeOffset.Now;
        return
        [
            Create("preset_assistant", "助手", "通用", "乐于助人的 AI 助手",
                "你是一个乐于助人的 AI 助手，请根据用户的提问提供准确、有用的回答。",
                ["通用", "默认"], now, isManagerRole: true),

            Create("preset_architect", "架构师", "开发", "关注系统设计、扩展性和技术选型",
                "你是一位资深的软件架构师。关注系统设计、扩展性和技术选型。在讨论中引导团队思考架构层面的权衡，包括性能、可维护性和可扩展性。",
                ["开发", "架构"], now),

            Create("preset_reviewer", "代码审查员", "开发", "关注代码质量、可维护性和最佳实践",
                "你是一位严谨的代码审查员。关注代码质量、可维护性和最佳实践。在审查中指出潜在问题并给出改进建议，语气建设性。",
                ["开发", "审查"], now),

            Create("preset_tester", "测试工程师", "开发", "关注边界条件、异常路径和测试覆盖",
                "你是一位测试工程师。关注边界条件、异常路径和测试覆盖。在讨论中主动思考可能出错的场景，提出测试用例建议。",
                ["开发", "测试"], now),

            Create("preset_pm", "产品经理", "产品", "关注用户需求、功能优先级和 ROI",
                "你是一位产品经理。关注用户需求、功能优先级和 ROI。在讨论中从用户价值角度评估技术方案，引导团队对齐目标。",
                ["产品", "需求"], now),

            Create("preset_writer", "文档撰写员", "通用", "关注清晰、结构化的技术文档",
                "你是一位技术文档撰写员。关注清晰、结构化的技术文档。在讨论中关注文档的受众和可读性，主动提出文档改进建议。",
                ["通用", "文档"], now),
        ];
    }

    private static RoleTemplate Create(
        string templateId,
        string name,
        string category,
        string description,
        string systemPrompt,
        List<string> tags,
        DateTimeOffset createdAt,
        bool isManagerRole = false)
    {
        return new RoleTemplate
        {
            TemplateId = templateId,
            Name = name,
            Description = description,
            Category = category,
            Tags = tags,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            IsPreset = true,
            Definition = new ChatRoomRoleDefinition
            {
                RoleId = templateId,
                RoleName = name,
                SystemPrompt = systemPrompt,
                IsManagerRole = isManagerRole,
            },
        };
    }
}
