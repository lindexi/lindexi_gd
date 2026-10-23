using System.Collections.Generic;

namespace AgentLib.Tools;

/// <summary>
/// 审批工具的展示配置，允许工具提供方自定义用户在审批面板中看到的信息。
/// </summary>
/// <remarks>
/// 通过 <see cref="HumanApprovalTool.Wrap(AITool, ApprovalOptions?)"/> 传入。
/// 所有属性均为可选——不设置时使用默认格式化逻辑。
/// </remarks>
public sealed class ApprovalOptions
{
    /// <summary>
    /// 获取或设置工具在审批面板中的显示名称。
    /// </summary>
    /// <remarks>
    /// 为 <see langword="null"/> 时回退到工具的技术名称（如 <c>set_workspace_path</c>）。
    /// 建议设置为用户可读的中文名称（如"设置工作区路径"）。
    /// </remarks>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 获取或设置审批描述信息。
    /// </summary>
    /// <remarks>
    /// 为 <see langword="null"/> 时使用默认描述"此工具需要人工审批后才会执行。"。
    /// </remarks>
    public string? ApprovalDescription { get; set; }

    /// <summary>
    /// 获取或设置输入参数的展示模板，支持 <c>{paramName}</c> 占位符。
    /// </summary>
    /// <remarks>
    /// 框架在审批前将 <c>{paramName}</c> 替换为实际参数值。
    /// 例如模板 <c>"目标路径：{path}"</c> 在参数 <c>path="C:\Code"</c> 时显示为 <c>"目标路径：C:\Code"</c>。
    /// 为 <see langword="null"/> 且 <see cref="InputFormatter"/> 也为 <see langword="null"/> 时，
    /// 使用默认的键值对格式化。
    /// </remarks>
    public string? InputTemplate { get; set; }

    /// <summary>
    /// 获取或设置输入参数的自定义格式化回调。
    /// </summary>
    /// <remarks>
    /// 适用于需要条件逻辑或复杂格式化的场景。
    /// 如果同时设置了 <see cref="InputFormatter"/> 和 <see cref="InputTemplate"/>，优先使用 <see cref="InputFormatter"/>。
    /// 为 <see langword="null"/> 时回退到 <see cref="InputTemplate"/> 或默认格式化。
    /// </remarks>
    public Func<IReadOnlyDictionary<string, object?>, string>? InputFormatter { get; set; }
}
