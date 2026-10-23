using AgentLib.Model;

using Microsoft.Extensions.AI;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace AgentLib.Tools;

/// <summary>
/// 为现有工具增加人工审批步骤。
/// </summary>
/// <remarks>
/// 审批工具采用两阶段生命周期：
/// <list type="number">
/// <item><b>配置阶段</b>——工具提供方调用 <see cref="Wrap"/> 注册审批描述，此时尚无聊天上下文，
/// 产出 <c>ConfiguredHumanApprovalFunction</c> 作为配置容器。</item>
/// <item><b>运行时阶段</b>——<see cref="CopilotChatManager.ResolveTools"/> 在发送消息时调用 <see cref="BindRuntimeTool"/>，
/// 将配置容器转换为 <c>RuntimeHumanApprovalFunction</c>，注入 <see cref="CopilotChatContext"/> 和 <see cref="CancellationToken"/>，
/// 后者才包含真正的审批等待逻辑。</item>
/// </list>
/// 分离的原因是 <see cref="Wrap"/> 在工具注册时调用，无法获取运行时上下文；
/// 而 <see cref="BindRuntimeTool"/> 在每次消息发送时调用，能拿到当前会话的聊天上下文和取消令牌。
/// </remarks>
public static class HumanApprovalTool
{
    private const string DefaultApprovalDescription = "此工具需要人工审批后才会执行。";

    /// <summary>
    /// 包装指定工具，使其在执行前等待人工审批。
    /// </summary>
    /// <remarks>
    /// 此方法在工具注册（配置阶段）调用，仅记录审批描述，不注入运行时上下文。
    /// 返回的 <c>ConfiguredHumanApprovalFunction</c> 需在消息发送时经 <see cref="BindRuntimeTool"/> 转换后才会执行审批逻辑。
    /// </remarks>
    public static AITool Wrap(AITool tool, string? approvalDescription = null)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (tool is ConfiguredHumanApprovalFunction configuredHumanApprovalFunction)
        {
            return configuredHumanApprovalFunction;
        }

        if (tool is not AIFunction function)
        {
            throw new ArgumentException("仅支持包装可调用函数工具。", nameof(tool));
        }

        return new ConfiguredHumanApprovalFunction(function, approvalDescription);
    }

    /// <summary>
    /// 包装指定工具，使其在执行前等待人工审批，并使用 <see cref="ApprovalOptions"/> 自定义审批面板的展示内容。
    /// </summary>
    /// <remarks>
    /// 此方法在工具注册（配置阶段）调用。通过 <paramref name="options"/> 可设置友好显示名称、
    /// 审批描述、输入参数模板或自定义格式化回调，从而让用户在审批面板中看到更清晰的信息。
    /// </remarks>
    /// <param name="tool">要包装的工具。</param>
    /// <param name="options">审批展示配置，为 <see langword="null"/> 时等效于 <see cref="Wrap(AITool, string?)"/>。</param>
    public static AITool Wrap(AITool tool, ApprovalOptions? options)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (tool is ConfiguredHumanApprovalFunction configuredHumanApprovalFunction)
        {
            return configuredHumanApprovalFunction;
        }

        if (tool is not AIFunction function)
        {
            throw new ArgumentException("仅支持包装可调用函数工具。", nameof(tool));
        }

        return new ConfiguredHumanApprovalFunction(function, options);
    }

    /// <summary>
    /// 将配置阶段的审批工具转换为运行时版本，注入聊天上下文和取消令牌。
    /// </summary>
    /// <remarks>
    /// 此方法在每次消息发送时由 <see cref="CopilotChatManager.ResolveTools"/> 调用。
    /// 对于已包装的审批工具，产出 <c>RuntimeHumanApprovalFunction</c> 以执行实际审批流程；
    /// 对于普通工具则原样返回。
    /// </remarks>
    internal static AITool BindRuntimeTool(AITool tool, CopilotChatContext? chatContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tool);

        return tool is ConfiguredHumanApprovalFunction configuredHumanApprovalFunction
            ? configuredHumanApprovalFunction.Bind(chatContext, cancellationToken)
            : tool;
    }

    internal static bool TryGetApprovalDescription(AITool tool, out string? approvalDescription)
    {
        ArgumentNullException.ThrowIfNull(tool);

        switch (tool)
        {
            case ConfiguredHumanApprovalFunction configuredHumanApprovalFunction:
                approvalDescription = configuredHumanApprovalFunction.ApprovalDescription;
                return true;
            case RuntimeHumanApprovalFunction runtimeHumanApprovalFunction:
                approvalDescription = runtimeHumanApprovalFunction.ApprovalDescription;
                return true;
            default:
                approvalDescription = null;
                return false;
        }
    }

    /// <summary>
    /// 配置阶段的审批工具包装器。仅保存审批描述，不包含运行时上下文。
    /// </summary>
    /// <remarks>
    /// 产出于 <see cref="Wrap"/>，在工具注册时创建。此时尚无聊天上下文和取消令牌，
    /// 因此 <see cref="InvokeCoreAsync"/> 只是透传给内部函数——实际不会被调用，
    /// 因为 <see cref="BindRuntimeTool"/> 会在消息发送时将其替换为 <see cref="RuntimeHumanApprovalFunction"/>。
    /// </remarks>
    private sealed class ConfiguredHumanApprovalFunction : AIFunction
    {
        private readonly AIFunction _innerFunction;

        public ConfiguredHumanApprovalFunction(AIFunction innerFunction, string? approvalDescription)
            : this(innerFunction, new ApprovalOptions { ApprovalDescription = approvalDescription })
        {
        }

        public ConfiguredHumanApprovalFunction(AIFunction innerFunction, ApprovalOptions? options)
        {
            _innerFunction = innerFunction;
            Name = innerFunction.Name;
            Description = innerFunction.Description;
            JsonSchema = innerFunction.JsonSchema;
            AdditionalProperties = innerFunction.AdditionalProperties;
            Options = options ?? new ApprovalOptions();
            ApprovalDescription = string.IsNullOrWhiteSpace(Options.ApprovalDescription)
                ? DefaultApprovalDescription
                : Options.ApprovalDescription.Trim();
        }

        public string ApprovalDescription { get; }

        public ApprovalOptions Options { get; }

        public override string Name { get; }

        public override string Description { get; }

        public override JsonElement JsonSchema { get; }

        public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }

        public override JsonSerializerOptions JsonSerializerOptions => _innerFunction.JsonSerializerOptions;

        public override System.Reflection.MethodInfo? UnderlyingMethod => _innerFunction.UnderlyingMethod;

        // 透传：正常流程下不会走到这里，因为 BindRuntimeTool 会先将本实例替换为 RuntimeHumanApprovalFunction。
        // 保留实现是为了防御性兜底——若调用方跳过了 BindRuntimeTool 直接执行，工具仍能正常工作（只是没有审批）。
        protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            return _innerFunction.InvokeAsync(arguments, cancellationToken);
        }

        /// <summary>
        /// 注入运行时上下文，创建可执行审批逻辑的 <see cref="RuntimeHumanApprovalFunction"/>。
        /// </summary>
        public RuntimeHumanApprovalFunction Bind(CopilotChatContext? chatContext, CancellationToken cancellationToken)
        {
            return new RuntimeHumanApprovalFunction(_innerFunction, ApprovalDescription, Options, chatContext, cancellationToken);
        }
    }

    /// <summary>
    /// 运行时阶段的审批工具。在 <see cref="InvokeCoreAsync"/> 中创建审批 UI 片段并等待用户决策，
    /// 同意后才执行内部函数，拒绝则直接返回拒绝消息。
    /// </summary>
    /// <remarks>
    /// 产出于 <see cref="BindRuntimeTool"/>，每次消息发送时创建，持有当前会话的聊天上下文和取消令牌。
    /// </remarks>
    private sealed class RuntimeHumanApprovalFunction : AIFunction
    {
        private readonly AIFunction _innerFunction;
        private readonly CopilotChatContext? _chatContext;
        private readonly CancellationToken _chatCancellationToken;

        public RuntimeHumanApprovalFunction(AIFunction innerFunction, string approvalDescription, ApprovalOptions options,
            CopilotChatContext? chatContext, CancellationToken chatCancellationToken)
        {
            _innerFunction = innerFunction;
            _chatContext = chatContext;
            _chatCancellationToken = chatCancellationToken;
            _options = options;
            ApprovalDescription = approvalDescription;
            Name = innerFunction.Name;
            Description = innerFunction.Description;
            JsonSchema = innerFunction.JsonSchema;
            AdditionalProperties = innerFunction.AdditionalProperties;
        }

        public string ApprovalDescription { get; }

        private readonly ApprovalOptions _options;

        public override string Name { get; }

        public override string Description { get; }

        public override JsonElement JsonSchema { get; }

        public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }

        public override JsonSerializerOptions JsonSerializerOptions => _innerFunction.JsonSerializerOptions;

        public override System.Reflection.MethodInfo? UnderlyingMethod => _innerFunction.UnderlyingMethod;

        // 按优先级格式化审批输入：自定义回调 > 模板替换 > 默认键值对展开。
        private string? FormatInputForApproval(IReadOnlyDictionary<string, object?>? arguments)
        {
            if (_options.InputFormatter is not null && arguments is not null)
            {
                return _options.InputFormatter(arguments);
            }

            if (!string.IsNullOrWhiteSpace(_options.InputTemplate) && arguments is not null)
            {
                return ReplaceTemplate(_options.InputTemplate, arguments);
            }

            return CopilotChatMessageItemFormatter.FormatArgumentsForApproval(arguments);
        }

        private static string ReplaceTemplate(string template, IReadOnlyDictionary<string, object?> arguments)
        {
            var builder = new System.Text.StringBuilder(template);
            foreach (KeyValuePair<string, object?> pair in arguments)
            {
                builder.Replace($"{{{pair.Key}}}", pair.Value?.ToString() ?? string.Empty);
            }
            return builder.ToString();
        }

        // 链接调用方令牌与会话令牌：任一取消即终止审批等待和工具执行。
        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            CancellationToken effectiveCancellationToken = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _chatCancellationToken).Token
                : _chatCancellationToken;

            Dictionary<string, object?>? argumentsDict = arguments.Count > 0 ? arguments.ToDictionary(pair => pair.Key, pair => pair.Value) : null;
            string? inputText = FormatInputForApproval(argumentsDict);
            string? displayName = string.IsNullOrWhiteSpace(_options.DisplayName) ? null : _options.DisplayName.Trim();
            CopilotChatApprovalToolItem? approvalToolItem = _chatContext?.CurrentContent.CreateApprovalToolItem(Name, inputText, ApprovalDescription, displayName: displayName);

            if (approvalToolItem is not null)
            {
                CopilotToolApprovalState approvalState = await approvalToolItem.WaitForApprovalAsync(effectiveCancellationToken).ConfigureAwait(false);
                if (approvalState == CopilotToolApprovalState.Rejected)
                {
                    string rejectedMessage = string.IsNullOrWhiteSpace(approvalToolItem.DecisionReason)
                        ? $"工具 {Name} 的执行请求已被人工拒绝。"
                        : $"工具 {Name} 的执行请求已被人工拒绝。原因：{approvalToolItem.DecisionReason}";
                    approvalToolItem.OutputText = rejectedMessage;
                    return rejectedMessage;
                }
            }

            object? result = await _innerFunction.InvokeAsync(arguments, effectiveCancellationToken).ConfigureAwait(false);
            if (approvalToolItem is not null)
            {
                approvalToolItem.OutputText = CopilotChatMessageItemFormatter.FormatArgumentsToHumans(result) ?? string.Empty;
            }

            return result;
        }
    }
}