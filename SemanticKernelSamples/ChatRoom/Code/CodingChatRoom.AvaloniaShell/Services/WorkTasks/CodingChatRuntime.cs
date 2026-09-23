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
    public required CodingChatRoomPaths Paths { get; init; }

    public required AgentApiEndpointManager EndpointManager { get; init; }

    public required FileCopilotChatLogger ChatLogger { get; init; }

    public required CopilotChatManager ChatManager { get; init; }

    public required CodingAgent CodingAgent { get; init; }

    public required ILanguageModel PrimaryModel { get; init; }

    public required CodingChatApplication Application { get; init; }

    public required CodingWorkspaceController WorkspaceController { get; init; }

    public required CodingChatSettingsService SettingsService { get; init; }

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
        await CodingAgent.DisposeAsync().ConfigureAwait(false);
        await ChatLogger.LogDiagnosticAsync(sessionId, "运行时释放", "工作任务运行时释放完成。").ConfigureAwait(false);
    }
}