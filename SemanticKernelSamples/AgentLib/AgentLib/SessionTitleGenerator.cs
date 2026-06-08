using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;

namespace AgentLib;

/// <summary>
/// 基于 LLM 的会话标题生成器。直接依赖 <see cref="AgentApiEndpointManager"/> 进行模型选择，
/// 模型解析结果在构造时缓存，<see cref="IChatClient"/> 在首次调用时懒加载并复用。
/// 通过工具调用接收标题，避免从模型自由文本输出中提取标题带来的干扰。
/// </summary>
public class SessionTitleGenerator
{
    /// <summary>
    /// 使用指定的 <see cref="AgentApiEndpointManager"/> 创建标题生成器。
    /// 标题生成所用的语言模型在首次生成时延迟解析（Flash 优先，PrimaryModel 回退），
    /// 避免构造时依赖尚未完成初始化的终结点管理器。
    /// </summary>
    /// <param name="endpointManager">API 终结点管理器，用于模型选择和 <see cref="IChatClient"/> 创建。</param>
    public SessionTitleGenerator(AgentApiEndpointManager endpointManager)
    {
        ArgumentNullException.ThrowIfNull(endpointManager);
        _endpointManager = endpointManager;
    }

    private readonly AgentApiEndpointManager _endpointManager;

    private IChatClient? _chatClient;

    /// <summary>
    /// 对指定会话生成标题。LLM 调用失败时静默返回，不更新会话标题。
    /// 如果会话标题已被 LLM 生成或用户设置，则跳过。
    /// </summary>
    /// <param name="session">目标会话。</param>
    /// <param name="systemPrompt">
    /// 自定义 System Prompt。为 <see langword="null"/> 时使用默认的 <see cref="SystemPrompt"/>。
    /// </param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task GenerateTitleAsync(CopilotChatSession session, string? systemPrompt = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.TitleSourceValue is TitleSource.Generated or TitleSource.UserSet)
        {
            return;
        }

        string? title = await GenerateTitleCoreAsync(session, systemPrompt, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(title))
        {
            session.SetTitle(title, TitleSource.Generated);
        }
    }

    private async Task<string?> GenerateTitleCoreAsync(CopilotChatSession session, string? systemPrompt, CancellationToken cancellationToken)
    {
        List<ChatMessage> messages = BuildMessages(session);
        if (messages.Count == 0)
        {
            return null;
        }

        string? submittedTitle = null;

        [Description("提交生成的会话标题文本。")]
        string SubmitTitle([Description("生成的会话标题文本，不超过 20 个字。")] string title)
        {
            ArgumentHelper.ThrowIfNullOrWhiteSpace(title);
            submittedTitle = title;
            return "已收到标题。";
        }

        try
        {
            IChatClient chatClient = await EnsureChatClientAsync().ConfigureAwait(false);

            messages.Insert(0, new ChatMessage(ChatRole.System, systemPrompt ?? SystemPrompt));

            var options = new ChatOptions
            {
                Tools = [AIFunctionFactory.Create(SubmitTitle, name: TitleSubmissionToolName, description: "提交生成的会话标题文本。")]
            };

            ChatClientAgent agent = chatClient.AsBuilder()
                .UseFunctionInvocation(configure: config =>
                {
                    config.FunctionInvoker = (context, token) =>
                    {
                        // 写入属性，即可在调用函数之后退出
                        context.Terminate = true;
                        return context.Function.InvokeAsync(context.Arguments, token);
                    };
                })
                .BuildAIAgent(new ChatClientAgentOptions()
                {
                    ChatOptions = options,
                });

            await agent.RunAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            return submittedTitle;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return null;
        }
    }

    /// <summary>
    /// 解析标题生成所用的语言模型。策略：Flash 模型优先，否则回退到 <see cref="AgentApiEndpointManager.PrimaryModel"/>。
    /// </summary>
    /// <returns>解析到的模型，必定非空（<see cref="AgentApiEndpointManager.PrimaryModel"/> 保证有可用模型）。</returns>
    private ILanguageModel ResolveTitleModel()
    {
        var supportedModels = _endpointManager.GetSupportedModels();

        var comparer = new LanguageModelCapabilityComparer();

        var flashModel = supportedModels
            .Where(m => m.ModelDefinition.Capabilities?.IsFlash == true)
            .OrderByDescending(m => m, comparer)
            .FirstOrDefault();

        return flashModel ?? _endpointManager.PrimaryModel;
    }

    /// <summary>
    /// 确保 <see cref="IChatClient"/> 已创建（懒加载 + 缓存），首次调用时异步创建，后续复用。
    /// </summary>
    /// <returns>聊天客户端实例。</returns>
    private async ValueTask<IChatClient> EnsureChatClientAsync()
    {
        if (_chatClient is not null)
        {
            return _chatClient;
        }

        var languageModel = ResolveTitleModel();
        var chatClient = await languageModel.GetChatClientAsync().ConfigureAwait(false);
        Interlocked.CompareExchange(ref _chatClient, chatClient, null);
        return _chatClient;
    }

    private static List<ChatMessage> BuildMessages(CopilotChatSession session)
    {
        var relevantMessages = session.ChatMessages
            .Where(m => !m.IsPresetInfo && m.HasContent)
            .ToList();

        int count = relevantMessages.Count;
        if (count == 0)
        {
            return [];
        }

        int start = Math.Max(0, count - MaxMessages);
        var messages = new List<ChatMessage>(MaxMessages);
        for (int i = start; i < count; i++)
        {
            CopilotChatMessage copilotMessage = relevantMessages[i];
            messages.Add(new ChatMessage(copilotMessage.Role, copilotMessage.Content));
        }

        return messages;
    }

    /// <summary>
    /// 标题生成用的 System Prompt。
    /// </summary>
    public const string SystemPrompt = """
        基于以下对话内容，生成一个简洁的会话标题（不超过 20 个字）。
        只使用对话中实际讨论的主题，不要编造内容。
        生成标题后，必须调用 submit_title 工具提交标题文本。
        """;

    /// <summary>
    /// 标题提交工具的名称。
    /// </summary>
    public const string TitleSubmissionToolName = "submit_title";

    /// <summary>
    /// 标题生成时最多提取的消息数量。
    /// </summary>
    public const int MaxMessages = 10;
}
