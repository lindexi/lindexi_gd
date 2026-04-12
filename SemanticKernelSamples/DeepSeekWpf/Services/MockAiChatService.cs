using System.Linq;
using System.Runtime.CompilerServices;
using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public sealed class MockAiChatService : IAiChatService
{
    public async IAsyncEnumerable<AiResponseChunk> GetReplyAsync(
        ChatSession session,
        ChatMessage assistantMessage,
        AppSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var lastUserMessage = session.Messages
            .LastOrDefault(message => message.Role == ChatRole.User)?.Content?.Trim() ?? string.Empty;

        var preview = lastUserMessage.Length <= 120
            ? lastUserMessage
            : lastUserMessage[..120] + "...";

        var thought = $"正在分析用户输入，并基于模型 {settings.ModelName} 组织回答思路。重点关注最近一条消息：{preview}";
        var content = $"[{settings.ModelName}] 当前接入的是本地模拟 AI 服务，用于验证 WPF MVVM 聊天流程。\n\n我已经收到你的消息：{preview}\n\n当前配置的 API 地址为：{settings.ApiAddress}\n后续可以直接替换 `IAiChatService` 的实现，接入真实模型平台。";

        await foreach (var chunk in StreamByCharacterAsync(AiResponsePart.Thought, thought, cancellationToken))
        {
            yield return chunk;
        }

        await Task.Delay(200, cancellationToken);

        await foreach (var chunk in StreamByCharacterAsync(AiResponsePart.Content, content, cancellationToken))
        {
            yield return chunk;
        }
    }

    private static async IAsyncEnumerable<AiResponseChunk> StreamByCharacterAsync(
        AiResponsePart part,
        string content,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var character in content)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(35, cancellationToken);
            yield return new AiResponseChunk(part, character.ToString());
        }
    }
}
