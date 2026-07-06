using System.Runtime.CompilerServices;

using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace PptxGenerator.Pipeline;

/// <summary>
/// 负责从指定用户消息重新开始流式生成。
/// </summary>
internal sealed class SlideStreamingRestartService
{
    private readonly CopilotChatManager _chatManager;
    private readonly IMainThreadDispatcher _dispatcher;
    private readonly Func<CopilotChatMessage, bool, CancellationToken, Task> _sendTargetAsync;
    private readonly Func<string, bool, CancellationToken, Task> _sendReplayTurnAsync;
    private readonly Func<CancellationToken, Task> _resetStreamingStateAsync;

    public SlideStreamingRestartService(
        CopilotChatManager chatManager,
        IMainThreadDispatcher dispatcher,
        Func<CopilotChatMessage, bool, CancellationToken, Task> sendTargetAsync,
        Func<string, bool, CancellationToken, Task> sendReplayTurnAsync,
        Func<CancellationToken, Task> resetStreamingStateAsync)
    {
        _chatManager = chatManager ?? throw new ArgumentNullException(nameof(chatManager));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _sendTargetAsync = sendTargetAsync ?? throw new ArgumentNullException(nameof(sendTargetAsync));
        _sendReplayTurnAsync = sendReplayTurnAsync ?? throw new ArgumentNullException(nameof(sendReplayTurnAsync));
        _resetStreamingStateAsync = resetStreamingStateAsync ?? throw new ArgumentNullException(nameof(resetStreamingStateAsync));
    }

    public async Task RestartFromMessageAsync(CopilotChatMessage targetMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targetMessage);

        var plan = SlideStreamingRestartPlan.Create(_chatManager.SelectedSession, targetMessage);
        var originalSession = _chatManager.SelectedSession;
        var originalPrimaryModel = _chatManager.AgentApiEndpointManager.PrimaryModel;

        await _resetStreamingStateAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (plan.PreviousTurns.Count > 0)
            {
                var replaySession = CreateReplaySession();
                var replayModel = CreateReplayLanguageModel(plan.PreviousTurns);
                var replayModelProvider = new FakeLanguageModelProvider([replayModel]);

                _chatManager.ChatSessions.Insert(0, replaySession);
                _chatManager.SelectedSession = replaySession;
                _chatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(replayModelProvider);
                _chatManager.AgentApiEndpointManager.PrimaryModel = replayModel;

                try
                {
                    await ReplayPreviousTurnsAsync(plan, cancellationToken).ConfigureAwait(false);
                    originalSession.SetAgentSession(replaySession.AgentSession);
                }
                finally
                {
                    _chatManager.AgentApiEndpointManager.PrimaryModel = originalPrimaryModel;
                    _chatManager.AgentApiEndpointManager.UnregisterLanguageModelProvider(replayModelProvider);
                    _chatManager.SelectedSession = originalSession;
                    await RemoveReplaySessionAsync(replaySession).ConfigureAwait(false);
                }
            }
            else
            {
                originalSession.SetAgentSession(null);
            }
        }
        finally
        {
            _chatManager.AgentApiEndpointManager.PrimaryModel = originalPrimaryModel;
            _chatManager.SelectedSession = originalSession;
        }

        await TruncateSessionAsync(originalSession, plan.TargetIndex, cancellationToken).ConfigureAwait(false);
        _chatManager.SelectedSession = originalSession;
        await _sendTargetAsync(targetMessage, plan.TargetIndex == 0, cancellationToken).ConfigureAwait(false);
    }

    private async Task RemoveReplaySessionAsync(CopilotChatSession replaySession)
    {
        await _dispatcher.InvokeAsync(() =>
        {
            _chatManager.ChatSessions.Remove(replaySession);
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    private CopilotChatSession CreateReplaySession()
    {
        return new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now)
        {
            MainThreadDispatcher = _chatManager.MainThreadDispatcher,
        };
    }

    private async Task ReplayPreviousTurnsAsync(SlideStreamingRestartPlan plan, CancellationToken cancellationToken)
    {
        for (var i = 0; i < plan.PreviousTurns.Count; i++)
        {
            RestartReplayTurn turn = plan.PreviousTurns[i];
            await _sendReplayTurnAsync(turn.UserText, i == 0, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task TruncateSessionAsync(
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

    private static FakeLanguageModel CreateReplayLanguageModel(IReadOnlyList<RestartReplayTurn> replayTurns)
    {
        var replayTexts = new Queue<string>(replayTurns.Select(turn => turn.AssistantText));
        var fakeChatClient = new FakeChatClient
        {
            OnGetResponseAsync = (_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty))),
            OnGetStreamingResponseAsync = (_, _, ct) => StreamNextReplayTextAsync(replayTexts, ct),
        };

        return new FakeLanguageModel(fakeChatClient)
        {
            ModelDefinition = new ModelDefinition
            {
                ModelId = $"SlideReplay-{Guid.NewGuid():N}",
                ModelName = "SlideML Replay",
                Provider = "Fake",
            },
        };
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamNextReplayTextAsync(
        Queue<string> replayTexts,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!replayTexts.TryDequeue(out string? text))
        {
            throw new InvalidOperationException("没有可用于回放的助手消息。");
        }

        foreach (char ch in text)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, ch.ToString());
            await Task.Yield();
        }
    }
}

internal sealed record SlideStreamingRestartPlan(
    int TargetIndex,
    IReadOnlyList<RestartReplayTurn> PreviousTurns)
{
    public static SlideStreamingRestartPlan Create(CopilotChatSession session, CopilotChatMessage targetMessage)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(targetMessage);

        if (targetMessage.Role != ChatRole.User || targetMessage.IsPresetInfo)
        {
            throw new ArgumentException("只能从普通用户消息重新开始。", nameof(targetMessage));
        }

        int targetIndex = session.ChatMessages.IndexOf(targetMessage);
        if (targetIndex < 0)
        {
            throw new InvalidOperationException("在当前会话中找不到要重新开始的用户消息。");
        }

        return new SlideStreamingRestartPlan(targetIndex, CreatePreviousTurns(session.ChatMessages, targetIndex));
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

            previousTurns.Add(new RestartReplayTurn(userMessage.Content, assistantMessage.Content));
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
