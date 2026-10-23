using System;
using System.Threading.Tasks;
using AgentLib;
using AgentLib.Coding;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using PrismCodingAgent.AvaloniaShell.Infrastructure;
using PrismCodingAgent.AvaloniaShell.Services.Chat;
using PrismCodingAgent.AvaloniaShell.Services.Settings;
using PrismCodingAgent.AvaloniaShell.Services.Workspace;

namespace PrismCodingAgent.AvaloniaShell.Services.WorkTasks;

/// <summary>
/// 保存由 Shell 组合根创建的核心运行时对象。
/// </summary>
internal sealed class CodingWorkTaskRuntime : IAsyncDisposable
{

    public required CodingChatRoomPaths Paths { get; init; }

    public required AgentApiEndpointManager EndpointManager { get; init; }

    public required FileCopilotChatLogger ChatLogger { get; init; }

    public required CopilotChatManager ChatManager { get; init; }

    public required CodingAgent CodingAgent { get; init; }

    public string? CopilotInstructionsPath { get; init; }

    private ResponsesCodingChatRunner? _responsesRunner;

    public required CodingAgentChatRunner CodingRunner { get; init; }

    internal ResponsesCodingChatRunner ResponsesRunner => _responsesRunner ??= new ResponsesCodingChatRunner(
        ChatManager, new ResponsesCodingAgent(CodingAgent, CopilotInstructionsPath));

    public required ILanguageModel? PrimaryModel { get; init; }

    public required CodingWorkTaskController Controller { get; init; }

    public required CodingWorkspaceController WorkspaceController { get; init; }

    public required CodingChatSettingsService SettingsService { get; init; }

    public string ModelDisplayName
    {
        get
        {
            if (PrimaryModel is null) return string.Empty;
            string provider = PrimaryModel.ModelDefinition.Provider;
            string modelName = PrimaryModel.ModelDefinition.ModelName;
            return string.IsNullOrWhiteSpace(provider) ? modelName : $"{provider}/{modelName}";
        }
    }

    internal void ApplyModelConfiguration(AgentApiManagerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.OpenAIConfigurationList is null || !System.Linq.Enumerable.Any(configuration.OpenAIConfigurationList,
                provider => provider.ModelDefinitions is { Count: > 0 }))
        {
            var models = System.Linq.Enumerable.ToArray(EndpointManager.GetSupportedModels());
            EndpointManager.UnregisterLanguageModelProvider(new RegisteredModels(models));
            return;
        }
        EndpointManager.ReplaceConfiguration(configuration);
    }

    private sealed class RegisteredModels(System.Collections.Generic.IReadOnlyList<ILanguageModel> models) : ILanguageModelProvider
    {
        public System.Collections.Generic.IReadOnlyList<ILanguageModel> GetSupportedModels() => models;
    }

    public async ValueTask DisposeAsync()
    {
        Guid sessionId = ChatManager.SelectedSession.SessionId;
        await ChatLogger.LogDiagnosticAsync
        (
            sessionId,
            "运行时释放",
            $"开始释放工作任务运行时。活动运行={Controller.IsRunActive}；循环活动={Controller.IsLoopActive}；正在收尾={Controller.IsFinalizing}。"
        ).ConfigureAwait(false);
        Controller.StopActiveRun();
        await ChatLogger.LogDiagnosticAsync
        (
            sessionId,
            "运行时释放",
            "已请求取消活动运行，开始释放 CodingAgent。"
        ).ConfigureAwait(false);
        await CodingAgent.DisposeAsync().ConfigureAwait(false);
        await ChatLogger.LogDiagnosticAsync(sessionId, "运行时释放", "工作任务运行时释放完成。").ConfigureAwait(false);
    }
}