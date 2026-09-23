using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed class LoopIterationChatReducer : IChatReducer
{
    public static LoopIterationChatReducer Instance { get; } = new();

    private LoopIterationChatReducer()
    {
    }

    public Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<ChatMessage> history = messages.ToList();
        int lastUserMessageIndex = history.FindLastIndex(message => message.Role == ChatRole.User);
        IEnumerable<ChatMessage> result = lastUserMessageIndex < 0
            ? history
            : history.Take(lastUserMessageIndex).ToArray();
        return Task.FromResult(result);
    }
}
