using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.Tools;

/// <summary>
/// 提供子代理委托工具。
/// </summary>
public sealed class SubAgentToolProvider
{
    internal const string InvokeSubAgentToolName = "InvokeSubAgent";
    internal const string ReturnOutputToParentToolName = "ReturnOutputToParent";
    internal const string InvokeSubAgentDisplayName = "调用子代理";

    private readonly AgentApiEndpointManager _agentApiEndpointManager;
    private readonly WorkspaceToolProvider _workspaceToolProvider;

    public SubAgentToolProvider(AgentApiEndpointManager agentApiEndpointManager, WorkspaceToolProvider workspaceToolProvider)
    {
        _agentApiEndpointManager = agentApiEndpointManager ?? throw new ArgumentNullException(nameof(agentApiEndpointManager));
        _workspaceToolProvider = workspaceToolProvider ?? throw new ArgumentNullException(nameof(workspaceToolProvider));
    }

    public IReadOnlyList<AITool> CreateDefaultTools()
    {
        return CreateTools(progressContainer: null, includeReturnOutputTool: false);
    }

    internal IReadOnlyList<AITool> CreateDefaultTools(ISubAgentProgressContainer? progressContainer)
    {
        return CreateTools(progressContainer, includeReturnOutputTool: false);
    }

    private IReadOnlyList<AITool> CreateTools(ISubAgentProgressContainer? progressContainer, bool includeReturnOutputTool)
    {
        var executor = new SubAgentToolExecutor(_agentApiEndpointManager, _workspaceToolProvider, progressContainer, this);
        List<AITool> tools =
        [
            executor.CreateTool(nameof(SubAgentToolExecutor.InvokeSubAgentAsync), InvokeSubAgentToolName, "委托一个子代理执行独立任务。可通过子代理类型选择快速模型或多模态能力；只有确定性处理、总结输出、了解文件组织结构或大文件内容、意图识别等无需思考决策的任务才可使用 Flash，涉及分析、判断、设计或决策时不要使用 Flash。")
        ];

        if (includeReturnOutputTool)
        {
            tools.Add(executor.CreateTool(nameof(SubAgentToolExecutor.ReturnOutputToParentAsync), ReturnOutputToParentToolName, "将需要返回给上一级代理的结果显式返回。只有在确实需要让上一级代理拿到输出时才调用此工具；不需要输出时不要调用。"));
        }

        return tools;
    }

    private sealed class SubAgentToolExecutor
    {
        private readonly AgentApiEndpointManager _agentApiEndpointManager;
        private readonly WorkspaceToolProvider _workspaceToolProvider;
        private readonly ISubAgentProgressContainer? _progressContainer;
        private readonly SubAgentToolProvider _provider;
        private readonly SubAgentOutputCollector _outputCollector = new();

        public SubAgentToolExecutor(AgentApiEndpointManager agentApiEndpointManager, WorkspaceToolProvider workspaceToolProvider, ISubAgentProgressContainer? progressContainer, SubAgentToolProvider provider)
        {
            _agentApiEndpointManager = agentApiEndpointManager;
            _workspaceToolProvider = workspaceToolProvider;
            _progressContainer = progressContainer;
            _provider = provider;
        }

        /// <summary>
        /// 调用子代理执行任务。
        /// </summary>
        [Description("调用一个子代理执行独立任务。只有确定性处理、总结输出、了解文件组织结构或大文件内容、意图识别等无需思考决策的任务才可使用 Flash；涉及思考和决策时不要使用 Flash。")]
        public async Task<string> InvokeSubAgentAsync(
            [Description("给子代理的提示词信息。应描述明确任务与期望输出。")]
            string prompt,
            [Description("给子代理的系统提示词信息。可以为空；为空时不传系统提示词。")]
            string? systemPrompt = null,
            [Description("选择的子代理类型，使用 `;` 分号分割。可选值：Flash、ImageInput、VideoInput、ImageOutput。不写时默认使用非 Flash 文本模型。Flash 仅可用于确定性工作、总结输出、了解文件组织结构或大文件内容、意图识别等场景；涉及思考和决策的内容不要使用 Flash。")]
            string? subAgentType = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

            SubAgentSelection selection = SubAgentSelection.Parse(subAgentType);
            ILanguageModel model = _agentApiEndpointManager.GetBestModel(languageModel => selection.IsMatch(languageModel));
            IChatClient chatClient = await model.GetChatClientAsync().ConfigureAwait(false);
            CopilotChatSubAgentItem? subAgentItem = _progressContainer?.AppendSubAgentCall(InvokeSubAgentDisplayName, CreateInvocationInputText(prompt, systemPrompt, subAgentType));
            _outputCollector.Attach(subAgentItem);

            ChatClientAgent chatClientAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
            {
                ChatOptions = new ChatOptions()
                {
                    Tools = [.. CreateSubAgentTools(subAgentItem)],
                }
            });

            List<ChatMessage> messages = [];
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new ChatMessage(ChatRole.System, systemPrompt));
            }

            messages.Add(new ChatMessage(ChatRole.User, prompt));

            await foreach (AgentResponseUpdate responseUpdate in chatClientAgent.RunStreamingAsync(messages, cancellationToken: CancellationToken.None).ConfigureAwait(false))
            {
                AppendSubAgentResponseUpdate(subAgentItem, responseUpdate);
            }

            string output = _outputCollector.OutputText;
            if (subAgentItem is not null && !string.IsNullOrEmpty(output))
            {
                subAgentItem.OutputText = output;
            }

            return output;
        }

        /// <summary>
        /// 显式将子代理输出返回给上一级代理。
        /// </summary>
        [Description("显式将子代理输出返回给上一级代理。不需要向上级返回结果时不要调用。")]
        public Task<string> ReturnOutputToParentAsync(
            [Description("需要返回给上一级代理的文本内容。")]
            string output)
        {
            _outputCollector.SetOutput(output);
            return Task.FromResult("已将结果返回给上一级代理。");
        }

        public AITool CreateTool(string methodName, string toolName, string description)
        {
            MethodInfo methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
                                    ?? throw new InvalidOperationException($"未找到 {methodName} 方法。");
            return AIFunctionFactory.Create(methodInfo, this, toolName, description, serializerOptions: null);
        }

        private IReadOnlyList<AITool> CreateSubAgentTools(CopilotChatSubAgentItem? currentSubAgentItem)
        {
            List<AITool> tools = [];
            tools.AddRange(_workspaceToolProvider.CreateDefaultTools());
            tools.AddRange(_provider.CreateTools(currentSubAgentItem, includeReturnOutputTool: true));
            return tools;
        }

        private static string? CreateInvocationInputText(string prompt, string? systemPrompt, string? subAgentType)
        {
            return CopilotChatMessageItemFormatter.FormatValue(new
            {
                Prompt = prompt,
                SystemPrompt = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt,
                SubAgentType = string.IsNullOrWhiteSpace(subAgentType) ? null : subAgentType
            });
        }

        private static void AppendSubAgentResponseUpdate(CopilotChatSubAgentItem? subAgentItem, AgentResponseUpdate responseUpdate)
        {
            ArgumentNullException.ThrowIfNull(responseUpdate);

            if (subAgentItem is null)
            {
                return;
            }

            foreach (AIContent content in responseUpdate.Contents)
            {
                switch (content)
                {
                    case TextReasoningContent textReasoningContent when !string.IsNullOrEmpty(textReasoningContent.Text):
                        subAgentItem.AppendReasoning(textReasoningContent.Text);
                        break;
                    case TextContent textContent when !string.IsNullOrEmpty(textContent.Text):
                        subAgentItem.AppendText(textContent.Text);
                        break;
                    case FunctionCallContent functionCallContent:
                        AppendSubAgentFunctionCall(subAgentItem, functionCallContent);
                        break;
                    case FunctionResultContent functionResultContent:
                        AppendSubAgentFunctionResult(subAgentItem, functionResultContent);
                        break;
                }
            }
        }

        private static void AppendSubAgentFunctionCall(CopilotChatSubAgentItem subAgentItem, FunctionCallContent functionCallContent)
        {
            ArgumentNullException.ThrowIfNull(subAgentItem);
            ArgumentNullException.ThrowIfNull(functionCallContent);

            if (string.Equals(functionCallContent.Name, InvokeSubAgentToolName, StringComparison.Ordinal))
            {
                subAgentItem.AppendSubAgentCall(InvokeSubAgentDisplayName, CopilotChatMessageItemFormatter.FormatArguments(functionCallContent), functionCallContent.CallId);
                return;
            }

            subAgentItem.AppendFunctionCall(functionCallContent);
        }

        private static void AppendSubAgentFunctionResult(CopilotChatSubAgentItem subAgentItem, FunctionResultContent functionResultContent)
        {
            ArgumentNullException.ThrowIfNull(subAgentItem);
            ArgumentNullException.ThrowIfNull(functionResultContent);

            if (!string.IsNullOrWhiteSpace(functionResultContent.CallId) && subAgentItem.MessageItems.OfType<CopilotChatSubAgentItem>().Any(item => string.Equals(item.CallId, functionResultContent.CallId, StringComparison.Ordinal)))
            {
                subAgentItem.AppendSubAgentOutput(functionResultContent.CallId, CopilotChatMessageItemFormatter.FormatResult(functionResultContent));
                return;
            }

            subAgentItem.AppendFunctionResult(functionResultContent);
        }
    }

    private sealed class SubAgentOutputCollector
    {
        private CopilotChatSubAgentItem? _subAgentItem;

        public string OutputText { get; private set; } = string.Empty;

        public void Attach(CopilotChatSubAgentItem? subAgentItem)
        {
            _subAgentItem = subAgentItem;
            if (_subAgentItem is not null && !string.IsNullOrEmpty(OutputText))
            {
                _subAgentItem.OutputText = OutputText;
            }
        }

        public void SetOutput(string output)
        {
            OutputText = output ?? string.Empty;
            if (_subAgentItem is not null)
            {
                _subAgentItem.OutputText = OutputText;
            }
        }
    }

    private sealed record SubAgentSelection(bool UseFlash, bool RequireImageInput, bool RequireVideoInput, bool RequireImageOutput)
    {
        public static SubAgentSelection Parse(string? subAgentType)
        {
            bool useFlash = false;
            bool requireImageInput = false;
            bool requireVideoInput = false;
            bool requireImageOutput = false;

            if (string.IsNullOrWhiteSpace(subAgentType))
            {
                return new SubAgentSelection(useFlash, requireImageInput, requireVideoInput, requireImageOutput);
            }

            foreach (string rawItem in subAgentType.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                switch (rawItem)
                {
                    case "Flash":
                        useFlash = true;
                        break;
                    case "ImageInput":
                        requireImageInput = true;
                        break;
                    case "VideoInput":
                        requireVideoInput = true;
                        break;
                    case "ImageOutput":
                        requireImageOutput = true;
                        break;
                    default:
                        throw new ArgumentException($"不支持的子代理类型: {rawItem}", nameof(subAgentType));
                }
            }

            return new SubAgentSelection(useFlash, requireImageInput, requireVideoInput, requireImageOutput);
        }

        public bool IsMatch(ILanguageModel languageModel)
        {
            ArgumentNullException.ThrowIfNull(languageModel);

            LlmModelCapabilities? capabilities = languageModel.ModelDefinition.Capabilities;
            if (capabilities is null)
            {
                return false;
            }

            if (capabilities.IsFlash != UseFlash)
            {
                return false;
            }

            if (RequireImageInput && !capabilities.Input.Image)
            {
                return false;
            }

            if (RequireVideoInput && !capabilities.Input.Video)
            {
                return false;
            }

            if (RequireImageOutput && !capabilities.Output.Image)
            {
                return false;
            }

            return true;
        }
    }
}
