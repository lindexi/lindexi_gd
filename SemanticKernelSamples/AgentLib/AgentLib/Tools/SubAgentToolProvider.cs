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
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.Tools;

/// <summary>
/// 提供子智能体委托工具。
/// </summary>
public sealed class SubAgentToolProvider
{
    internal const string InvokeSubAgentToolName = "InvokeSubAgent";
    internal const string ReturnOutputToParentToolName = "ReturnOutputToParent";
    internal const string InvokeSubAgentDisplayName = "调用子智能体";
    private const string RequireToolCallPrompt = "你刚才没有调用任何工具。必须调用工具方法将内容返回给上一级智能体；如果需要返回文本结果，请调用 ReturnOutputToParent 工具。";

    private readonly AgentApiEndpointManager _agentApiEndpointManager;
    private readonly WorkspaceToolProvider _workspaceToolProvider;

    public SubAgentToolProvider(AgentApiEndpointManager agentApiEndpointManager, WorkspaceToolProvider workspaceToolProvider)
    {
        _agentApiEndpointManager = agentApiEndpointManager ?? throw new ArgumentNullException(nameof(agentApiEndpointManager));
        _workspaceToolProvider = workspaceToolProvider ?? throw new ArgumentNullException(nameof(workspaceToolProvider));
    }

    public IReadOnlyList<AITool> CreateDefaultTools(CancellationToken cancellationToken = default)
    {
        return CreateTools(chatContext: null, includeReturnOutputTool: false, cancellationToken);
    }

    internal IReadOnlyList<AITool> CreateDefaultTools(CopilotChatContext? chatContext, CancellationToken cancellationToken = default)
    {
        return CreateTools(chatContext, includeReturnOutputTool: false, cancellationToken);
    }

    private IReadOnlyList<AITool> CreateTools(CopilotChatContext? chatContext, bool includeReturnOutputTool, CancellationToken cancellationToken)
    {
        var executor = new SubAgentToolExecutor(_agentApiEndpointManager, _workspaceToolProvider, chatContext, this, cancellationToken);
        List<AITool> tools =
        [
            AIFunctionFactory.Create(executor.InvokeSubAgentAsync, InvokeSubAgentToolName, "委托一个子代理执行独立任务。可通过子代理类型选择快速模型或多模态能力；只有确定性处理、总结输出、了解文件组织结构或大文件内容、意图识别等无需思考决策的任务才可使用 Flash，涉及分析、判断、设计或决策时不要使用 Flash。")
        ];

        if (includeReturnOutputTool)
        {
            tools.Add(AIFunctionFactory.Create(executor.ReturnOutputToParentAsync, ReturnOutputToParentToolName, "将需要返回给上一级代理的结果显式返回。只有在确实需要让上一级代理拿到输出时才调用此工具；不需要输出时不要调用。"));
        }

        return tools;
    }

    private sealed class SubAgentToolExecutor
    {
        private readonly AgentApiEndpointManager _agentApiEndpointManager;
        private readonly WorkspaceToolProvider _workspaceToolProvider;
        private readonly CopilotChatContext? _chatContext;
        private readonly SubAgentToolProvider _provider;
        private readonly CancellationToken _cancellationToken;
        private readonly SubAgentOutputCollector _outputCollector = new();

        public SubAgentToolExecutor(AgentApiEndpointManager agentApiEndpointManager, WorkspaceToolProvider workspaceToolProvider, CopilotChatContext? chatContext, SubAgentToolProvider provider, CancellationToken cancellationToken)
        {
            _agentApiEndpointManager = agentApiEndpointManager;
            _workspaceToolProvider = workspaceToolProvider;
            _chatContext = chatContext;
            _provider = provider;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// 调用子智能体执行任务。
        /// </summary>
        [Description("调用一个子智能体执行独立任务。只有确定性处理、总结输出、了解文件组织结构或大文件内容、意图识别等无需思考决策的任务才可使用 Flash；涉及思考和决策时不要使用 Flash。")]
        public async Task<string> InvokeSubAgentAsync(
            [Description("给子智能体的提示词信息。应描述明确任务与期望输出。")]
            string prompt,
            [Description("给子智能体的系统提示词信息。可以为空；为空时不传系统提示词。")]
            string? systemPrompt = null,
            [Description("选择的子智能体类型，使用 `;` 分号分割。可选值：Flash、ImageInput、VideoInput、ImageOutput。不写时默认使用非 Flash 文本模型。Flash 仅可用于确定性工作、总结输出、了解文件组织结构或大文件内容、意图识别等场景；涉及思考和决策的内容不要使用 Flash。")]
            string? subAgentType = null)
        {
            ArgumentHelper.ThrowIfNullOrWhiteSpace(prompt);
            _cancellationToken.ThrowIfCancellationRequested();

            SubAgentSelection selection = SubAgentSelection.Parse(subAgentType);
            ILanguageModel model = _agentApiEndpointManager.GetBestModel(languageModel => selection.IsMatch(languageModel));
            IChatClient chatClient = await model.GetChatClientAsync();
            CopilotChatSubAgentItem? subAgentItem = _chatContext?.CurrentContent.CreateSubAgentItem(InvokeSubAgentDisplayName, CreateInvocationInputText(prompt, systemPrompt, subAgentType));
            _outputCollector.Attach(subAgentItem);
            CopilotChatContext? subAgentChatContext = subAgentItem is not null ? _chatContext?.CreateSubAgentContext(subAgentItem) : null;

            ChatClientAgent chatClientAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
            {
                ChatOptions = new ChatOptions()
                {
                    Tools = [.. CreateSubAgentTools(subAgentChatContext)],
                }
            });

            List<ChatMessage> messages = [];
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages.Add(new ChatMessage(ChatRole.System, systemPrompt));
            }

            messages.Add(new ChatMessage(ChatRole.User, prompt));

            await RunSubAgentOnceAsync(chatClientAgent, messages, subAgentItem).ConfigureAwait(false);
            if (!_outputCollector.HasOutput)
            {
                messages.Add(new ChatMessage(ChatRole.User, RequireToolCallPrompt));
                await RunSubAgentOnceAsync(chatClientAgent, messages, subAgentItem).ConfigureAwait(false);
            }

            string output = _outputCollector.OutputText;
            if (subAgentItem is not null && !string.IsNullOrEmpty(output))
            {
                subAgentItem.OutputText = output;
            }

            return output;
        }

        private async Task RunSubAgentOnceAsync(ChatClientAgent chatClientAgent, List<ChatMessage> messages, CopilotChatSubAgentItem? subAgentItem)
        {
            ArgumentNullException.ThrowIfNull(chatClientAgent);
            ArgumentNullException.ThrowIfNull(messages);

            await foreach (AgentResponseUpdate responseUpdate in chatClientAgent.RunStreamingAsync(messages, cancellationToken: _cancellationToken).ConfigureAwait(false))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                AppendSubAgentResponseUpdate(subAgentItem, responseUpdate);
            }
        }

        /// <summary>
        /// 显式将子智能体输出返回给上一级代理。
        /// </summary>
        [Description("显式将子智能体输出返回给上一级代理。不需要向上级返回结果时不要调用。")]
        public Task<string> ReturnOutputToParentAsync(
            [Description("需要返回给上一级智能体的文本内容。")]
            string output)
        {
            _outputCollector.SetOutput(output);
            return Task.FromResult("已将结果返回给上一级智能体。");
        }

        private IReadOnlyList<AITool> CreateSubAgentTools(CopilotChatContext? chatContext)
        {
            List<AITool> tools = [];
            tools.AddRange(_workspaceToolProvider.CreateDefaultTools());
            tools.AddRange(_provider.CreateTools(chatContext, includeReturnOutputTool: true, cancellationToken: _cancellationToken));
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
                CopilotChatSubAgentItem nestedSubAgentItem = subAgentItem.CreateSubAgentItem(InvokeSubAgentDisplayName, CopilotChatMessageItemFormatter.FormatArguments(functionCallContent), functionCallContent.CallId);
                nestedSubAgentItem.ToolName = InvokeSubAgentDisplayName;
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
                CopilotChatSubAgentItem nestedSubAgentItem = subAgentItem.MessageItems
                    .OfType<CopilotChatSubAgentItem>()
                    .First(item => string.Equals(item.CallId, functionResultContent.CallId, StringComparison.Ordinal));
                nestedSubAgentItem.OutputText = CopilotChatMessageItemFormatter.FormatResult(functionResultContent) ?? string.Empty;
                return;
            }

            subAgentItem.AppendFunctionResult(functionResultContent);
        }
    }

    private sealed class SubAgentOutputCollector
    {
        private CopilotChatSubAgentItem? _subAgentItem;

        public bool HasOutput { get; private set; }

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
            HasOutput = true;
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
