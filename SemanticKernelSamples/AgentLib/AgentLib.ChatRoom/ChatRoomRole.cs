using AgentLib;
using AgentLib.ChatRoom.Model;
using AgentLib.Core;
using AgentLib.Logging;
using AgentLib.Model;
using AgentLib.Tools;

using Microsoft.Extensions.AI;

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom;

/// <summary>
/// 聊天室角色运行时。内部包装 <see cref="CopilotChatManager"/>，复用其模型选择、工具注册、
/// 流式响应和历史压缩等能力。每个角色拥有独立的 <see cref="AgentApiEndpointManager"/> 和
/// <see cref="CopilotChatManager"/> 实例。
/// </summary>
public sealed class ChatRoomRole
{
    private IMainThreadDispatcher? _mainThreadDispatcher;
    private CopilotChatManager? _chatManager;
    private readonly AgentApiEndpointManager _endpointManager;
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
    /// 首次发言时通过 <see cref="SendMessageRequest.SystemPrompt"/> 注入角色人设和记忆。
    /// 流式增量内容通过返回的 <see cref="ChatRoomSpeakResult.AssistantChatMessage"/> 暴露，
    /// 调用方可直接绑定其 <see cref="CopilotChatMessage.Content"/> 属性感知实时更新。
    /// </summary>
    /// <param name="incrementalUserText">
    /// 自上次发言后其他角色产生的公开消息（已拼接为纯文本）。
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
    public ChatRoomSpeakResult? SpeakAsync(
        string incrementalUserText,
        IReadOnlyList<AITool>? additionalTools = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incrementalUserText);
        if (string.IsNullOrWhiteSpace(incrementalUserText))
        {
            throw new ArgumentException("发言内容不能为空或空白。", nameof(incrementalUserText));
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
            var request = new SendMessageRequest(incrementalUserText)
            {
                WithHistory = true,
                SystemPrompt = systemPrompt,
                CancellationToken = cancellationToken,
            };

            // 如果有额外工具，追加到 Tools 集合
            if (additionalTools is { Count: > 0 })
            {
                request = request with { Tools = [.. request.Tools, .. additionalTools] };
            }

            SendMessageResult result = ChatManager.SendMessage(request);
            CopilotChatMessage assistantMessage = result.AssistantChatMessage;

            // 构建最终内容任务：等待发言完成后提取最终文本
            Task<string?> finalContentTask = BuildFinalContentTask(result, cancellationToken);

            return new ChatRoomSpeakResult(assistantMessage, finalContentTask);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// 构建最终内容提取任务。等待发言完成后从 CopilotChatManager 提取最终文本。
    /// </summary>
    private async Task<string?> BuildFinalContentTask(SendMessageResult result, CancellationToken cancellationToken)
    {
        try
        {
            await result.RunTask.ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            _hasSpoken = true;

            // 从 CopilotChatManager 的 SelectedSession 提取最后一轮 Assistant 回复
            CopilotChatSession? session = ChatManager.SelectedSession;
            if (session is null)
            {
                return null;
            }

            // 取最后一条非 PresetInfo 的 Assistant 消息
            CopilotChatMessage? lastAssistant = session.ChatMessages
                .LastOrDefault(m => m.Role == ChatRole.Assistant && !m.IsPresetInfo);

            if (lastAssistant is null || string.IsNullOrWhiteSpace(lastAssistant.Content))
            {
                return null;
            }

            return lastAssistant.Content;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// 让角色以初始话题发言（首次发言，不带历史）。
    /// </summary>
    /// <param name="initialTopic">初始话题文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public ChatRoomSpeakResult? SpeakFirstAsync(string initialTopic, CancellationToken cancellationToken = default)
    {
        return SpeakAsync(initialTopic, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 构建角色的 SystemPrompt，包含角色人设、记忆内容和聊天室协作指引。
    /// 仅在首次发言时调用。
    /// </summary>
    /// <returns>SystemPrompt 文本；如果角色未配置人设和记忆，返回 <see langword="null"/>。</returns>
    private string? BuildSystemPrompt()
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(Definition.SystemPrompt))
        {
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

        // 注入聊天室上下文（角色列表、@用法、协作指引）
        if (!string.IsNullOrWhiteSpace(ChatRoomContext))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }
            sb.AppendLine(ChatRoomContext);
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