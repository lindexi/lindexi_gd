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
    private readonly IAsyncDisposable? _responsesAgent;

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
        CodingChatSettingsService settingsService,
        IAsyncDisposable? responsesAgent = null
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
        _responsesAgent = responsesAgent;
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
        Application.StopActiveRun();
        await _codingAgent.DisposeAsync().ConfigureAwait(false);
        if (_responsesAgent is not null)
        {
            await _responsesAgent.DisposeAsync().ConfigureAwait(false);
        }
    }
}