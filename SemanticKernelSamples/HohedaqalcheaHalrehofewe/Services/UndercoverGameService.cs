using System.ClientModel;
using System.IO;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace HohedaqalcheaHalrehofewe.Services;

internal sealed class UndercoverGameService
{
    private const string Endpoint = "https://ark.cn-beijing.volces.com/api/v3";

    public GameSession CreateSession(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var key = File.ReadAllText(settings.ApiKeyFilePath).Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("API Key 文件内容为空。");
        }

        var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions
        {
            Endpoint = new Uri(Endpoint),
        });

        var chatClient = openAiClient.GetChatClient(settings.DeploymentId);
        var agent = chatClient.AsIChatClient().AsAIAgent();
        var players = new List<PlayerRuntime>();

        for (var index = 0; index < settings.PlayerCount; index++)
        {
            var word = index == settings.PlayerCount - 1 ? settings.SpyWord : settings.MainWord;
            var prompt = CreatePlayerPrompt(index, word);
            players.Add(new PlayerRuntime(index, index == settings.HumanPlayerIndex, word, [new ChatMessage(ChatRole.System, prompt)]));
        }

        return new GameSession(settings, agent, players);
    }

    public async Task RunRoundAsync(
        GameSession session,
        Func<HumanTurnRequest, Task<string>> requestHumanInputAsync,
        Action<TimelineDelta> reportTimeline,
        Action<int, string> activatePlayer,
        Action<string> updateStatus,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(requestHumanInputAsync);
        ArgumentNullException.ThrowIfNull(reportTimeline);
        ArgumentNullException.ThrowIfNull(activatePlayer);
        ArgumentNullException.ThrowIfNull(updateStatus);

        var round = session.CurrentRound;
        ReportMessage(reportTimeline, $"round-{round}-start", "系统", $"第 {round} 回合开始。", TimelineKinds.System);
        await RunSpeakingPhaseAsync(session, round, requestHumanInputAsync, reportTimeline, activatePlayer, updateStatus, cancellationToken);
        await RunVotingPhaseAsync(session, round, requestHumanInputAsync, reportTimeline, activatePlayer, updateStatus, cancellationToken);
        ReportMessage(reportTimeline, $"round-{round}-complete", "系统", $"第 {round} 回合结束。你可以开始下一回合。", TimelineKinds.System);
        session.CurrentRound++;
    }

    private async Task RunSpeakingPhaseAsync(
        GameSession session,
        int round,
        Func<HumanTurnRequest, Task<string>> requestHumanInputAsync,
        Action<TimelineDelta> reportTimeline,
        Action<int, string> activatePlayer,
        Action<string> updateStatus,
        CancellationToken cancellationToken)
    {
        updateStatus($"第 {round} 回合发言阶段进行中。");
        BroadcastJudgeMessage(session, $"第 {round} 回合开始，请开始你们的发言");

        foreach (var player in session.Players)
        {
            cancellationToken.ThrowIfCancellationRequested();
            activatePlayer(player.Index, $"第 {round} 回合发言中");

            var output = player.IsHuman
                ? await requestHumanInputAsync(new HumanTurnRequest(player.Index, $"你是第 {player.Index} 人，请输入你的发言。", $"round-{round}-speech-human-{player.Index}", $"第 {player.Index} 人 发言", TimelineKinds.PlayerSpeech))
                : await RunAiTurnAsync(session, player, round, "发言", TimelineKinds.AiSpeech, reportTimeline, cancellationToken);

            BroadcastPlayerSpeech(session, player, output);
        }
    }

    private async Task RunVotingPhaseAsync(
        GameSession session,
        int round,
        Func<HumanTurnRequest, Task<string>> requestHumanInputAsync,
        Action<TimelineDelta> reportTimeline,
        Action<int, string> activatePlayer,
        Action<string> updateStatus,
        CancellationToken cancellationToken)
    {
        updateStatus($"第 {round} 回合投票阶段进行中。");
        ReportMessage(reportTimeline, $"round-{round}-vote-start", "系统", $"第 {round} 回合发言结束，进入投票阶段。", TimelineKinds.System);

        foreach (var player in session.Players)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var prompt = $"第 {round} 回合结束，请根据以上各人发言，请你猜测谁可能是卧底。你是第 {player.Index} 人。";
            player.Messages.Add(new ChatMessage(ChatRole.System, prompt));
            activatePlayer(player.Index, $"第 {round} 回合投票中");

            var output = player.IsHuman
                ? await requestHumanInputAsync(new HumanTurnRequest(player.Index, $"你是第 {player.Index} 人，请输入你的投票或判断理由。", $"round-{round}-vote-human-{player.Index}", $"第 {player.Index} 人 投票", TimelineKinds.PlayerVote))
                : await RunAiTurnAsync(session, player, round, "投票", TimelineKinds.AiVote, reportTimeline, cancellationToken);

            player.Messages.Add(new ChatMessage(ChatRole.Assistant, output));
        }
    }

    private async Task<string> RunAiTurnAsync(
        GameSession session,
        PlayerRuntime player,
        int round,
        string actionName,
        string outputKind,
        Action<TimelineDelta> reportTimeline,
        CancellationToken cancellationToken)
    {
        var reasoningKey = $"round-{round}-{actionName}-reasoning-{player.Index}";
        var outputKey = $"round-{round}-{actionName}-output-{player.Index}";
        var headerPrefix = actionName == "投票" ? "投票" : "发言";
        string? finalText = null;

        await foreach (var update in session.Agent.RunReasoningStreamingAsync(player.Messages, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Reasoning))
            {
                reportTimeline(new TimelineDelta(reasoningKey, $"第 {player.Index} 人 思考", update.Reasoning, TimelineKinds.Reasoning));
            }

            if (!string.IsNullOrEmpty(update.Text))
            {
                finalText ??= string.Empty;
                finalText += update.Text;
                reportTimeline(new TimelineDelta(outputKey, $"第 {player.Index} 人 {headerPrefix}", update.Text, outputKind));
            }
        }

        if (string.IsNullOrWhiteSpace(finalText))
        {
            throw new InvalidOperationException($"第 {player.Index} 人未生成有效的 {actionName} 内容。");
        }

        return finalText.Trim();
    }

    private static void BroadcastJudgeMessage(GameSession session, string message)
    {
        foreach (var player in session.Players)
        {
            player.Messages.Add(new ChatMessage(new ChatRole("裁判"), message));
        }
    }

    private static void BroadcastPlayerSpeech(GameSession session, PlayerRuntime speaker, string output)
    {
        foreach (var player in session.Players)
        {
            if (ReferenceEquals(player, speaker))
            {
                player.Messages.Add(new ChatMessage(ChatRole.Assistant, output));
            }
            else
            {
                player.Messages.Add(new ChatMessage(ChatRole.User, $"第 {speaker.Index} 人: {output}"));
            }
        }
    }

    private static void ReportMessage(Action<TimelineDelta> reportTimeline, string key, string header, string text, string kind)
    {
        reportTimeline(new TimelineDelta(key, header, text, kind));
    }

    private static string CreatePlayerPrompt(int index, string word)
    {
        return $$"""
                 你正在参与一个“谁是卧底”的游戏。每人每轮只能说一句话描述自己拿到的词语（不能直接说出那个词语），既不能让卧底发现，也要给同伴以暗示。当你能够确定某个人是“卧底”的时候，你可以去指认他，如果指认错误，你将会出局，请谨慎指认。如果指认成功，你将赢得比赛。如果你发现自己是卧底，也可以指认自己。
                 你是第 {{index}} 人
                 你拿到的词语是："{{word}}"
                 在发言阶段只输出一句公开发言；在投票阶段给出你的判断和理由。
                 """;
    }
}
