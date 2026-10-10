using AgentLib;
using CodingAgent.AvaloniaShell.Services.Chat;
using CodingAgent.AvaloniaShell.Services.Sessions;
using CodingAgent.AvaloniaShell.Services.Workspace;
using CodingAgent.AvaloniaShell.Services.WorkTasks;

namespace CodingAgent.AvaloniaShell.Tests;

internal static class CodingWorkTaskControllerTestFactory
{
    public static CodingWorkTaskController CreateApplication(
        CopilotChatManager chatManager,
        ICodingChatSessionStore sessionStore,
        ICodingChatRunner? chatRunner = null,
        CodingWorkspaceController? workspaceController = null,
        CodingAgent? codingAgent = null)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(sessionStore);
        codingAgent ??= new CodingAgent();
        chatRunner ??= new CodingAgentChatRunner(chatManager, codingAgent);
        workspaceController ??= new CodingWorkspaceController(new ImmediateMainThreadDispatcher());
        return new CodingWorkTaskController(
            chatManager,
            sessionStore,
            chatRunner,
            workspaceController,
            codingAgent);
    }

    private sealed class ImmediateMainThreadDispatcher : IMainThreadDispatcher
    {
        public Task InvokeAsync(Func<Task> action) => action();

        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();

        public bool CheckAccess() => true;
    }
}
