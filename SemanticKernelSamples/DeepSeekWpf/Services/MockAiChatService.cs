using System.Linq;
using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public sealed class MockAiChatService : IAiChatService
{
    public async Task<string> GetReplyAsync(ChatSession session, AppSettings settings, CancellationToken cancellationToken)
    {
        var lastUserMessage = session.Messages
            .LastOrDefault(message => message.Role == ChatRole.User)?.Content?.Trim() ?? string.Empty;

        var preview = lastUserMessage.Length <= 120
            ? lastUserMessage
            : lastUserMessage[..120] + "...";

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        return $"[{settings.ModelName}] 当前接入的是本地模拟 AI 服务，用于验证 WPF MVVM 聊天流程。{Environment.NewLine}{Environment.NewLine}" +
               $"我已经收到你的消息：{preview}{Environment.NewLine}{Environment.NewLine}" +
               "后续可以直接替换 `IAiChatService` 的实现，接入真实模型平台。";
    }
}
