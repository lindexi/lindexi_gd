using AgentLib.ChatRoom.Model;
using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom;

internal sealed class StandardChatRoomRoleExecutor : IChatRoomRoleExecutor
{
    public ChatRoomRoleExecutionKind ExecutionKind => ChatRoomRoleExecutionKind.Standard;

    public Task<ChatRoomRoleExecutionResult> RunAsync(
        ChatRoomRoleExecutionContext context,
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contents);

        SendMessageResult sendResult = context.ChatManager.SendMessage(new SendMessageRequest(contents)
        {
            WithHistory = true,
            CreateNewSession = false,
            Tools = context.AdditionalTools,
            SystemPrompt = context.SystemPrompt,
            CancellationToken = cancellationToken,
        });

        var result = new ChatRoomRoleExecutionResult(
            sendResult.AssistantChatMessage,
            CompleteAsync(sendResult));
        return Task.FromResult(result);
    }

    public Task SetWorkspacePathAsync(
        CopilotChatManager chatManager,
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        cancellationToken.ThrowIfCancellationRequested();
        chatManager.WorkspacePath = workspacePath;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => default;

    private static async Task<ChatRoomRoleExecutionCompletion> CompleteAsync(SendMessageResult sendResult)
    {
        SendMessageRunState runState = await sendResult.RunTask.ConfigureAwait(false);
        if (runState.WasCanceled)
        {
            return new ChatRoomRoleExecutionCompletion(null, WasCanceled: true);
        }

        if (!runState.IsSuccess)
        {
            throw new InvalidOperationException("标准角色发送失败。详细错误已写入该角色的私有会话。");
        }

        string content = sendResult.AssistantChatMessage.Content;
        return new ChatRoomRoleExecutionCompletion(
            string.IsNullOrWhiteSpace(content) ? null : content,
            WasCanceled: false);
    }
}

internal sealed class StandardChatRoomRoleExecutorFactory : IChatRoomRoleExecutorFactory
{
    public ChatRoomRoleExecutionKind ExecutionKind => ChatRoomRoleExecutionKind.Standard;

    public IChatRoomRoleExecutor Create(ChatRoomRoleExecutorCreationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new StandardChatRoomRoleExecutor();
    }
}
