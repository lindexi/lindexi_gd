using System;
using System.Threading.Tasks;
using AgentLib;
using AgentLib.Coding;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using CodingChatRoom.AvaloniaShell.Infrastructure;

namespace CodingChatRoom.AvaloniaShell.Services;

/// <summary>
/// 保存由 Shell 组合根创建的核心运行时对象。
/// </summary>
internal sealed class CodingChatRuntime : IAsyncDisposable
{
    private readonly CodingAgent _codingAgent;

    public CodingChatRuntime
    (
        CodingChatRoomPaths paths,
        AgentApiEndpointManager endpointManager,
        FileCopilotChatLogger chatLogger,
        CopilotChatManager chatManager,
        CodingAgent codingAgent,
        ILanguageModel primaryModel,
        CodingChatApplication application,
        CodingWorkspaceController workspaceController,
        CodingChatSettingsService settingsService
    )
    {
        Paths = paths;
        EndpointManager = endpointManager;
        ChatLogger = chatLogger;
        ChatManager = chatManager;
        _codingAgent = codingAgent;
        PrimaryModel = primaryModel;
        Application = application;
        WorkspaceController = workspaceController;
        SettingsService = settingsService;
    }

    public CodingChatRoomPaths Paths { get; }

    public AgentApiEndpointManager EndpointManager { get; }

    public FileCopilotChatLogger ChatLogger { get; }

    public CopilotChatManager ChatManager { get; }

    public ILanguageModel PrimaryModel { get; }

    public CodingChatApplication Application { get; }

    public CodingWorkspaceController WorkspaceController { get; }

    public CodingChatSettingsService SettingsService { get; }

    public string ModelDisplayName
    {
        get
        {
            string provider = PrimaryModel.ModelDefinition.Provider;
            string modelName = PrimaryModel.ModelDefinition.ModelName;
            return string.IsNullOrWhiteSpace(provider) ? modelName : $"{provider}/{modelName}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        Guid sessionId = ChatManager.SelectedSession.SessionId;
        await ChatLogger.LogDiagnosticAsync
        (
            sessionId,
            "运行时释放",
            $"开始释放工作任务运行时。活动运行={Application.IsRunActive}；循环活动={Application.IsLoopActive}；正在收尾={Application.IsFinalizing}。"
        ).ConfigureAwait(false);
        Application.StopActiveRun();
        await ChatLogger.LogDiagnosticAsync
        (
            sessionId,
            "运行时释放",
            "已请求取消活动运行，开始释放 CodingAgent。"
        ).ConfigureAwait(false);
        await _codingAgent.DisposeAsync().ConfigureAwait(false);
        await ChatLogger.LogDiagnosticAsync(sessionId, "运行时释放", "工作任务运行时释放完成。").ConfigureAwait(false);
    }
}