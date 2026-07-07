using AgentLib;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace PptxGenerator.Pipeline;

/// <summary>
/// 负责从指定用户消息重新开始流式生成。
/// </summary>
internal sealed class SlideStreamingRestartService
{
    private static readonly TimeSpan ReplayTokenDelay = TimeSpan.FromMilliseconds(8);

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
        await ReplayStateBeforeTargetAsync(session, plan, cancellationToken).ConfigureAwait(false);
        await RestoreAgentHistoryBeforeTargetAsync(session, plan, cancellationToken).ConfigureAwait(false);
        await TruncateSessionFromTargetAsync(session, plan.TargetIndex, cancellationToken).ConfigureAwait(false);

        _chatManager.SelectedSession = session;
        await SendTargetMessageAsync(plan.TargetUserText, plan.IsTargetFirstMessage, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReplayStateBeforeTargetAsync(
        CopilotChatSession session,
        SlideStreamingRestartPlan plan,
        CancellationToken cancellationToken)
    {
        session.SetAgentSession(null);

        if (plan.PreviousTurns.Count == 0)
        {
            return;
        }

        var fakeChatClient = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, token) => StreamReplayTextAsync(plan, token),
            OnGetResponseAsync = (_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty))),
        };
        CopilotChatSession replaySession = await CreateReplaySessionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _chatManager.SelectedSession = replaySession;
            await ReplayPreviousTurnsAsync(plan, fakeChatClient, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _chatManager.SelectedSession = session;
            await RemoveReplaySessionAsync(replaySession, cancellationToken).ConfigureAwait(false);
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

    private async Task<CopilotChatSession> CreateReplaySessionAsync(CancellationToken cancellationToken)
    {
        var replaySession = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now)
        {
            MainThreadDispatcher = _chatManager.MainThreadDispatcher,
        };

        await _dispatcher.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _chatManager.ChatSessions.Insert(0, replaySession);
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        return replaySession;
    }

    private async Task RemoveReplaySessionAsync(CopilotChatSession replaySession, CancellationToken cancellationToken)
    {
        await _dispatcher.InvokeAsync(() =>
        {
            _chatManager.ChatSessions.Remove(replaySession);
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    private async Task ReplayPreviousTurnsAsync(
        SlideStreamingRestartPlan plan,
        IChatClient replayChatClient,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(replayChatClient);

        for (var i = 0; i < plan.PreviousTurns.Count; i++)
        {
            RestartReplayTurn turn = plan.PreviousTurns[i];
            await _pipeline.SendMessageAsync(
                turn.UserText,
                isFirstMessage: i == 0,
                attachPreview: false,
                skipAutoEvaluation: true,
                useStreaming: true,
                cancellationToken: cancellationToken,
                chatClientOverride: replayChatClient).ConfigureAwait(false);
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamReplayTextAsync(
        SlideStreamingRestartPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string assistantText = plan.DequeueReplayAssistantText();
        foreach (char ch in assistantText)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, ch.ToString());
            await Task.Delay(ReplayTokenDelay, cancellationToken).ConfigureAwait(false);
        }
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
    private int _replayIndex;

    public string DequeueReplayAssistantText()
    {
        int index = Interlocked.Increment(ref _replayIndex) - 1;
        if (index >= PreviousTurns.Count)
        {
            return string.Empty;
        }

        return PreviousTurns[index].AssistantText;
    }

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
