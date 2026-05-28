using AgentLib.Model;

using Microsoft.Extensions.AI;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace AgentLib.Tools;

/// <summary>
/// 为现有工具增加人工审批步骤。
/// </summary>
public static class HumanApprovalTool
{
    private const string DefaultApprovalDescription = "此工具需要人工审批后才会执行。";

    /// <summary>
    /// 包装指定工具，使其在执行前等待人工审批。
    /// </summary>
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

    private sealed class ConfiguredHumanApprovalFunction : AIFunction
    {
        private readonly AIFunction _innerFunction;

        public ConfiguredHumanApprovalFunction(AIFunction innerFunction, string? approvalDescription)
        {
            _innerFunction = innerFunction;
            Name = innerFunction.Name;
            Description = innerFunction.Description;
            JsonSchema = innerFunction.JsonSchema;
            AdditionalProperties = innerFunction.AdditionalProperties;
            ApprovalDescription = string.IsNullOrWhiteSpace(approvalDescription) ? DefaultApprovalDescription : approvalDescription.Trim();
        }

        public string ApprovalDescription { get; }

        public override string Name { get; }

        public override string Description { get; }

        public override JsonElement JsonSchema { get; }

        public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }

        public override JsonSerializerOptions JsonSerializerOptions => _innerFunction.JsonSerializerOptions;

        public override System.Reflection.MethodInfo? UnderlyingMethod => _innerFunction.UnderlyingMethod;

        protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            return _innerFunction.InvokeAsync(arguments, cancellationToken);
        }

        public RuntimeHumanApprovalFunction Bind(CopilotChatContext? chatContext, CancellationToken cancellationToken)
        {
            return new RuntimeHumanApprovalFunction(_innerFunction, ApprovalDescription, chatContext, cancellationToken);
        }
    }

    private sealed class RuntimeHumanApprovalFunction : AIFunction
    {
        private readonly AIFunction _innerFunction;
        private readonly CopilotChatContext? _chatContext;
        private readonly CancellationToken _chatCancellationToken;

        public RuntimeHumanApprovalFunction(AIFunction innerFunction, string approvalDescription, CopilotChatContext? chatContext,
            CancellationToken chatCancellationToken)
        {
            _innerFunction = innerFunction;
            _chatContext = chatContext;
            _chatCancellationToken = chatCancellationToken;
            ApprovalDescription = approvalDescription;
            Name = innerFunction.Name;
            Description = innerFunction.Description;
            JsonSchema = innerFunction.JsonSchema;
            AdditionalProperties = innerFunction.AdditionalProperties;
        }

        public string ApprovalDescription { get; }

        public override string Name { get; }

        public override string Description { get; }

        public override JsonElement JsonSchema { get; }

        public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }

        public override JsonSerializerOptions JsonSerializerOptions => _innerFunction.JsonSerializerOptions;

        public override System.Reflection.MethodInfo? UnderlyingMethod => _innerFunction.UnderlyingMethod;

        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            CancellationToken effectiveCancellationToken = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _chatCancellationToken).Token
                : _chatCancellationToken;

            string? inputText = CopilotChatMessageItemFormatter.FormatValue(arguments.Count > 0 ? arguments.ToDictionary(pair => pair.Key, pair => pair.Value) : null);
            CopilotChatApprovalToolItem? approvalToolItem = _chatContext?.CurrentContent.CreateApprovalToolItem(Name, inputText, ApprovalDescription);

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