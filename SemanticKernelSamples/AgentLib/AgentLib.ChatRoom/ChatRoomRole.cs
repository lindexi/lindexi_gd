using AgentLib;
using AgentLib.ChatRoom.Model;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Logging;
using AgentLib.Model;
using AgentLib.Tools;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom;

/// <summary>
/// 聊天室角色运行时。内部包装 <see cref="CopilotChatManager"/>，复用其模型选择、工具注册、
/// 流式响应和历史压缩等能力。每个角色拥有独立的 <see cref="AgentApiEndpointManager"/> 和
/// <see cref="CopilotChatManager"/> 实例。
/// </summary>
public sealed class ChatRoomRole : NotifyBase
{
    private IMainThreadDispatcher? _mainThreadDispatcher;
    private CopilotChatManager? _chatManager;
    private readonly AgentApiEndpointManager _endpointManager;
    private UsageDetails? _lastUsageDetails;
    private bool _hasSpoken;

    /// <summary>
    /// 使用角色定义和可选的 API 终结点管理器创建角色。
    /// </summary>
    /// <param name="definition">角色定义配置。</param>
    /// <param name="endpointManager">
    /// 自定义的 API 终结点管理器。为 <see langword="null"/> 时创建新的默认实例。
    /// 传入自定义实例可实现多角色共享同一 provider。
    /// </param>
    public ChatRoomRole(ChatRoomRoleDefinition definition, AgentApiEndpointManager? endpointManager = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;

        _endpointManager = endpointManager ?? new AgentApiEndpointManager();
    }

    /// <summary>
    /// 角色定义配置。
    /// </summary>
    public ChatRoomRoleDefinition Definition { get; }

    /// <summary>
    /// 角色的 API 终结点管理器。可在初始化阶段注册模型提供商。
    /// </summary>
    public AgentApiEndpointManager EndpointManager => _endpointManager;

    /// <summary>
    /// 主线程调度器。仅在构造期可设置。传递给内部 <see cref="AgentLib.CopilotChatManager"/>。
    /// </summary>
    public IMainThreadDispatcher? MainThreadDispatcher
    {
        get => _mainThreadDispatcher;
        init => _mainThreadDispatcher = value;
    }

    /// <summary>
    /// 聊天室上下文信息。由 <see cref="ChatRoomManager"/> 在发言前设置，
    /// 用于在系统提示词中注入当前聊天室的角色列表和协作指引。
    /// </summary>
    public string? ChatRoomContext { get; set; }

    /// <summary>
    /// 角色最后一次发言的 Token 用量详情。没有返回用量信息时为 <see langword="null"/>。
    /// </summary>
    public UsageDetails? LastUsageDetails
    {
        get => _lastUsageDetails;
        private set
        {
            if (SetField(ref _lastUsageDetails, value))
            {
                OnPropertyChanged(nameof(LastUsageSummaryText));
            }
        }
    }

    /// <summary>
    /// 角色最后一次发言总用量的显示文本。
    /// </summary>
    public string LastUsageSummaryText
    {
        get
        {
            UsageDetails? usageDetails = LastUsageDetails;
            long? totalTokenCount = usageDetails?.TotalTokenCount;
            if (totalTokenCount is null && usageDetails is not null)
            {
                long? inputTokenCount = usageDetails.InputTokenCount;
                long? outputTokenCount = usageDetails.OutputTokenCount;
                if (inputTokenCount is not null || outputTokenCount is not null)
                {
                    totalTokenCount = (inputTokenCount ?? 0) + (outputTokenCount ?? 0);
                }
            }

            return totalTokenCount is { }
                ? $"上次用量 {totalTokenCount:N0}"
                : string.Empty;
        }
    }

    /// <summary>
    /// 工作区路径。设置后角色的文件系统工具将在此路径下操作文件。
    /// 转发到内部 <see cref="CopilotChatManager.WorkspacePath"/>。
    /// </summary>
    public string? WorkspacePath
    {
        get => ChatManager.WorkspacePath;
        set => ChatManager.WorkspacePath = value;
    }

    private CopilotChatManager ChatManager => _chatManager ??= new CopilotChatManager
    {
        AgentApiEndpointManager = _endpointManager,
        MainThreadDispatcher = _mainThreadDispatcher,
    };

    /// <summary>
    /// 初始化角色。注册模型 provider、加载技能文件夹、配置工具等。
    /// 应在角色开始发言前调用。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // 加载技能文件夹
        foreach (string skillFolder in Definition.SkillFolders)
        {
            try
            {
                ChatManager.AddSkillFolder(new DirectoryInfo(skillFolder));
            }
            catch (Exception)
            {
                // 技能加载失败不阻塞初始化，仅记录
            }
        }

        // 如果配置了独立的 Provider 和 Model，注册到 endpoint manager
        if (!string.IsNullOrWhiteSpace(Definition.ModelProviderId) && !string.IsNullOrWhiteSpace(Definition.ModelId))
        {
            // 注意：AgentApiEndpointManager 的 RegisterLanguageModelProvider 等 API 需要在外部完成。
            // ChatRoomRole 只负责通过 Definition 传递模型选择偏好。
            // 模型提供商的注册应由协调方（ChatRoomManager）在初始化阶段完成。
        }

        await Task.CompletedTask;
    }

    internal async Task<JsonElement?> SerializeAgentSessionStateAsync(CancellationToken cancellationToken = default)
    {
        if (Definition.IsHuman || ChatManager.SelectedSession.AgentSession is not { } agentSession)
        {
            return null;
        }

        IManualSendMessageContext manualContext = await ChatManager
            .CreateManualSendMessageContextAsync(cancellationToken)
            .ConfigureAwait(false);
        ChatClientAgent chatClientAgent = await manualContext
            .GetChatClientAgentAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        JsonElement serializedSessionState = await chatClientAgent
            .SerializeSessionAsync(agentSession, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return serializedSessionState.Clone();
    }

    internal async Task RestoreAgentSessionStateAsync(JsonElement agentSessionState,
        CancellationToken cancellationToken = default)
    {
        if (Definition.IsHuman || _endpointManager.GetSupportedModels().Count == 0)
        {
            return;
        }

        IManualSendMessageContext manualContext = await ChatManager
            .CreateManualSendMessageContextAsync(cancellationToken)
            .ConfigureAwait(false);
        ChatClientAgent chatClientAgent = await manualContext
            .GetChatClientAgentAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        AgentSession agentSession = await chatClientAgent
            .DeserializeSessionAsync(agentSessionState, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ChatManager.SelectedSession.SetAgentSession(agentSession);
        _hasSpoken = true;
    }

    /// <summary>
    /// 确保角色的 <see cref="EndpointManager"/> 中存在可用模型。
    /// 应在模型提供商注册完成后调用，以便在发言前尽早暴露配置缺失问题，
    /// 而不是等到 <see cref="SpeakAsync"/> 深层调用时才抛出底层异常。
    /// </summary>
    /// <exception cref="InvalidOperationException">没有可用模型时抛出。</exception>
    public void EnsureModelAvailable()
    {
        if (_endpointManager.GetSupportedModels().Count == 0)
        {
            throw new InvalidOperationException(
                $"角色「{Definition.RoleName}」没有可用的模型。" +
                $"请先通过 {nameof(EndpointManager)}.{nameof(AgentApiEndpointManager.RegisterLanguageModelProvider)} 注册模型提供商，" +
                $"或在设置中为该角色配置有效的 ModelProviderId。");
        }
    }

    /// <summary>
    /// 让角色发言一次。将增量的 User 消息注入到内部的 <see cref="CopilotChatManager"/>，
    /// 利用 AgentSession 历史记录机制延续对话上下文。
    /// 首次发言时注入角色人设和记忆。
    /// 流式增量内容通过返回的 <see cref="ChatRoomSpeakResult.AssistantChatMessage"/> 暴露，
    /// 调用方可直接绑定其 <see cref="CopilotChatMessage.Content"/> 属性感知实时更新。
    /// </summary>
    /// <param name="incrementalUserTexts">
    /// 自上次发言后其他角色产生的公开消息。每项会作为独立的 <see cref="ChatRole.User"/> 消息发送给模型。
    /// </param>
    /// <param name="additionalTools">
    /// 本次发言额外启用的工具集合。为 <see langword="null"/> 或空时仅使用角色默认工具。
    /// 这些工具会被追加到 <see cref="SendMessageRequest.Tools"/> 中。
    /// </param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>
    /// 包含底层 <see cref="CopilotChatMessage"/> 和最终内容任务的发言结果。
    /// 如果角色未产生有效回复，返回 <see langword="null"/>。
    /// </returns>
    public async Task<ChatRoomSpeakResult?> SpeakAsync
    (
        IReadOnlyList<string> incrementalUserTexts,
        IReadOnlyList<AITool>? additionalTools = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(incrementalUserTexts);
        if (incrementalUserTexts.Count == 0)
        {
            throw new ArgumentException("发言消息集合不能为空。", nameof(incrementalUserTexts));
        }

        foreach (string txt in incrementalUserTexts)
        {
            if (string.IsNullOrWhiteSpace(txt))
            {
                throw new ArgumentException("发言消息中包含空的内容。", nameof(incrementalUserTexts));
            }
        }

        // 早期校验：确保有可用模型，避免进入底层流程后才抛出含糊异常
        EnsureModelAvailable();

        // 首次发言时构建 SystemPrompt（角色人设 + 记忆）
        string? systemPrompt = null;
        if (!_hasSpoken)
        {
            systemPrompt = BuildSystemPrompt();
        }

        try
        {
            IManualSendMessageContext manualContext = await ChatManager.CreateManualSendMessageContextAsync(cancellationToken).ConfigureAwait(false);
            string combinedText = string.Join("\n", incrementalUserTexts);
            manualContext.UserChatMessage.AppendText(combinedText);
            await manualContext.AppendMessagesToSessionAsync().ConfigureAwait(false);

            var chatMessages = new List<ChatMessage>(incrementalUserTexts.Count + (string.IsNullOrWhiteSpace(systemPrompt) ? 0 : 1));
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                chatMessages.Add(new ChatMessage(ChatRole.System, systemPrompt));
            }

            foreach (string incrementalUserText in incrementalUserTexts)
            {
                chatMessages.Add(new ChatMessage(ChatRole.User, incrementalUserText));
            }

            Task<string?> finalContentTask = RunManualSendAsync(manualContext, chatMessages, additionalTools, cancellationToken);
            string modelDisplayName = GetCurrentModelDisplayName();
            return new ChatRoomSpeakResult(manualContext.AssistantChatMessage, finalContentTask, modelDisplayName);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private string GetCurrentModelDisplayName()
    {
        ModelDefinition modelDefinition = _endpointManager.PrimaryModel.ModelDefinition;
        string modelName = !string.IsNullOrWhiteSpace(modelDefinition.ModelName)
            ? modelDefinition.ModelName
            : modelDefinition.ModelId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(modelDefinition.Provider))
        {
            return modelName;
        }

        return string.IsNullOrWhiteSpace(modelName)
            ? modelDefinition.Provider
            : $"{modelDefinition.Provider}/{modelName}";
    }

    private async Task<string?> RunManualSendAsync(
        IManualSendMessageContext manualContext,
        IReadOnlyList<ChatMessage> chatMessages,
        IReadOnlyList<AITool>? additionalTools,
        CancellationToken cancellationToken)
    {
        try
        {
            using IDisposable chatting = manualContext.StartChatting();

            ChatClientAgent chatClientAgent = await manualContext.GetChatClientAgentAsync(options =>
            {
                if (additionalTools is { Count: > 0 })
                {
                    IReadOnlyList<AITool> tools = [.. manualContext.DefaultTools, .. additionalTools];
                    options.ChatOptions ??= new ChatOptions();
                    options.ChatOptions.Tools = [.. tools];
                    options.ChatOptions.ToolMode = tools.Count > 0 ? options.ChatOptions.ToolMode : null;
                }
            }, cancellationToken).ConfigureAwait(false);
            AgentSession agentSession = await manualContext.GetAgentSessionAsync(cancellationToken).ConfigureAwait(false);

            await foreach (AgentResponseUpdate update in chatClientAgent.RunWithHistoryCompletionAsync(chatMessages,
                agentSession,
                cancellationToken))
            {
                manualContext.AppendResponseUpdate(update);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            _hasSpoken = true;
            UpdateLastUsageDetails(manualContext.AssistantChatMessage.CurrentUsageDetails);
            if (string.IsNullOrWhiteSpace(manualContext.AssistantChatMessage.Content))
            {
                return null;
            }

            return manualContext.AssistantChatMessage.Content;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private void UpdateLastUsageDetails(UsageDetails? usageDetails)
    {
        LastUsageDetails = usageDetails;
    }

    /// <summary>
    /// 让角色以初始话题发言（首次发言，不带历史）。
    /// </summary>
    /// <param name="initialTopic">初始话题文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task<ChatRoomSpeakResult?> SpeakFirstAsync(string initialTopic, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(initialTopic);
        if (string.IsNullOrWhiteSpace(initialTopic))
        {
            throw new ArgumentException("发言内容不能为空或空白。", nameof(initialTopic));
        }

        return SpeakAsync(
            new[] { initialTopic },
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 压缩当前角色的内部 Agent 会话历史，不修改聊天室共享消息和其他角色状态。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task ReduceSessionAsync(CancellationToken cancellationToken = default)
    {
        if (Definition.IsHuman)
        {
            return;
        }

        await ChatManager.ReduceAgentSessionOnlyAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 构建角色的 SystemPrompt，包含聊天室协作指引、当前角色身份、角色人设和记忆内容。
    /// 仅在首次发言时调用。
    /// </summary>
    /// <returns>SystemPrompt 文本；如果角色未配置人设和记忆，返回 <see langword="null"/>。</returns>
    private string? BuildSystemPrompt()
    {
        var sb = new StringBuilder();

        // 注入聊天室上下文（角色列表、@用法、协作指引）
        if (!string.IsNullOrWhiteSpace(ChatRoomContext))
        {
            sb.AppendLine(ChatRoomContext);
        }

        if (!string.IsNullOrWhiteSpace(Definition.RoleName) || !string.IsNullOrWhiteSpace(Definition.RoleId))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }
            sb.AppendLine("你当前在聊天室中的身份：");
            if (!string.IsNullOrWhiteSpace(Definition.RoleName))
            {
                sb.AppendLine($"- 角色名：{Definition.RoleName}");
            }
            if (!string.IsNullOrWhiteSpace(Definition.RoleId))
            {
                sb.AppendLine($"- 角色 Id：{Definition.RoleId}");
            }
        }

        if (!string.IsNullOrWhiteSpace(Definition.SystemPrompt))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }
            sb.AppendLine(Definition.SystemPrompt);
        }

        if (!string.IsNullOrWhiteSpace(Definition.MemoryContent))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }
            sb.AppendLine("你的记忆内容：");
            sb.AppendLine(Definition.MemoryContent);
        }

        return sb.Length > 0 ? sb.ToString().TrimEnd() : null;
    }

    /// <summary>
    /// 同意指定审批工具继续执行。
    /// </summary>
    /// <param name="approvalToolItem">等待审批的工具片段。</param>
    public void ApproveToolExecution(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        ChatManager.ApproveToolExecution(approvalToolItem);
    }

    /// <summary>
    /// 拒绝指定审批工具继续执行。
    /// </summary>
    /// <param name="approvalToolItem">等待审批的工具片段。</param>
    /// <param name="reason">拒绝原因。</param>
    public void RejectToolExecution(CopilotChatApprovalToolItem approvalToolItem, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        ChatManager.RejectToolExecution(approvalToolItem, reason);
    }
}