using AgentLib;
using AgentLib.Model;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace PptxGenerator.Pipeline;

/// <summary>
/// 负责从指定用户消息重新开始流式生成。
/// </summary>
internal sealed class SlideStreamingRestartService
{
    private readonly SlideGenerationPipeline _pipeline;
    private readonly CopilotChatManager _chatManager;
    private readonly IMainThreadDispatcher _dispatcher;

    public SlideStreamingRestartService(SlideGenerationPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _chatManager = pipeline.ChatManager;
        _dispatcher = _chatManager.MainThreadDispatcher ?? pipeline.SlideMlRenderTool.Dispatcher;
    }

    public async Task RestartFromMessageAsync(CopilotChatMessage targetMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targetMessage);

        var plan = SlideStreamingRestartPlan.Create(_chatManager.SelectedSession, targetMessage);
        CopilotChatSession session = _chatManager.SelectedSession;

        await _pipeline.ResetStreamingRestartStateAsync(cancellationToken).ConfigureAwait(false);
        await ReplayStateBeforeTargetAsync(plan, cancellationToken).ConfigureAwait(false);
        await RestoreAgentHistoryBeforeTargetAsync(session, plan, cancellationToken).ConfigureAwait(false);
        await TruncateSessionFromTargetAsync(session, plan.TargetIndex, cancellationToken).ConfigureAwait(false);

        _chatManager.SelectedSession = session;
        await SendTargetMessageAsync(plan.TargetUserText, plan.IsTargetFirstMessage, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReplayStateBeforeTargetAsync(SlideStreamingRestartPlan plan, CancellationToken cancellationToken)
    {
        foreach (RestartReplayTurn turn in plan.PreviousTurns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _pipeline.ReplayStreamingAssistantTextAsync(turn.AssistantText, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RestoreAgentHistoryBeforeTargetAsync(
        CopilotChatSession session,
        SlideStreamingRestartPlan plan,
        CancellationToken cancellationToken)
    {
        session.SetAgentSession(null);

        if (plan.PreviousTurns.Count == 0)
        {
            return;
        }

        var chatHistory = new List<ChatMessage>(plan.PreviousTurns.Count * 2);
        foreach (RestartReplayTurn turn in plan.PreviousTurns)
        {
            chatHistory.Add(CreateUserHistoryMessage(turn, chatHistory.Count == 0));
            chatHistory.Add(new ChatMessage(ChatRole.Assistant, turn.AssistantText));
        }

        AgentSession agentSession = await CreateAgentSessionAsync(cancellationToken).ConfigureAwait(false);
        agentSession.SetInMemoryChatHistory(chatHistory);
        session.SetAgentSession(agentSession);
    }

    private ChatMessage CreateUserHistoryMessage(RestartReplayTurn turn, bool isFirstTurn)
    {
        string content = isFirstTurn
            ? _pipeline.PromptProvider.BuildStreamingUserPrompt(turn.UserText)
            : turn.UserText;

        return new ChatMessage(ChatRole.User, content);
    }

    private async Task<AgentSession> CreateAgentSessionAsync(CancellationToken cancellationToken)
    {
        IManualSendMessageContext manualContext = await _chatManager
            .CreateManualSendMessageContextAsync(cancellationToken).ConfigureAwait(false);

        return await manualContext.GetAgentSessionAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SendTargetMessageAsync(string targetText, bool isFirstMessage, CancellationToken cancellationToken)
    {
        await _pipeline.SendMessageAsync(
            targetText,
            isFirstMessage,
            attachPreview: false,
            skipAutoEvaluation: false,
            useStreaming: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task TruncateSessionFromTargetAsync(
        CopilotChatSession session,
        int targetIndex,
        CancellationToken cancellationToken)
    {
        await _dispatcher.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (session.ChatMessages.Count > targetIndex)
            {
                session.ChatMessages.RemoveAt(session.ChatMessages.Count - 1);
            }

            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }
}

internal sealed record SlideStreamingRestartPlan(
    int TargetIndex,
    string TargetUserText,
    bool IsTargetFirstMessage,
    IReadOnlyList<RestartReplayTurn> PreviousTurns)
{
    public static SlideStreamingRestartPlan Create(CopilotChatSession session, CopilotChatMessage targetMessage)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(targetMessage);

        if (targetMessage.IsPresetInfo || targetMessage.Role != ChatRole.User)
        {
            throw new ArgumentException("只能从普通用户消息重新开始。", nameof(targetMessage));
        }

        int targetIndex = session.ChatMessages.IndexOf(targetMessage);
        if (targetIndex < 0)
        {
            throw new InvalidOperationException("在当前会话中找不到要重新开始的用户消息。");
        }

        return new SlideStreamingRestartPlan(
            targetIndex,
            targetMessage.Content,
            targetIndex == 0,
            CreatePreviousTurns(session.ChatMessages, targetIndex));
    }

    private static IReadOnlyList<RestartReplayTurn> CreatePreviousTurns(IReadOnlyList<CopilotChatMessage> messages, int targetIndex)
    {
        var previousTurns = new List<RestartReplayTurn>();
        for (var i = 0; i < targetIndex; i++)
        {
            CopilotChatMessage userMessage = messages[i];
            if (userMessage.IsPresetInfo || userMessage.Role != ChatRole.User || string.IsNullOrWhiteSpace(userMessage.Content))
            {
                continue;
            }

            CopilotChatMessage? assistantMessage = FindAssistantMessage(messages, i + 1, targetIndex, out int assistantIndex);
            if (assistantMessage is null)
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(assistantMessage.Content))
            {
                previousTurns.Add(new RestartReplayTurn(userMessage.Content, assistantMessage.Content));
            }

            i = assistantIndex;
        }

        return previousTurns;
    }

    private static CopilotChatMessage? FindAssistantMessage(
        IReadOnlyList<CopilotChatMessage> messages,
        int startIndex,
        int targetIndex,
        out int assistantIndex)
    {
        for (var i = startIndex; i < targetIndex; i++)
        {
            CopilotChatMessage candidate = messages[i];
            if (candidate.IsPresetInfo)
            {
                continue;
            }

            if (candidate.Role == ChatRole.User)
            {
                break;
            }

            if (candidate.Role == ChatRole.Assistant)
            {
                assistantIndex = i;
                return candidate;
            }
        }

        assistantIndex = -1;
        return null;
    }
}

internal sealed record RestartReplayTurn(string UserText, string AssistantText);
