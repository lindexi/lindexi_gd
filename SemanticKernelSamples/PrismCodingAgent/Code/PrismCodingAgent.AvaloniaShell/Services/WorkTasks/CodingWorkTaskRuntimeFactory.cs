using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using AgentLib;
using AgentLib.Coding;
using AgentLib.Coding.Images;
using AgentLib.Coding.Sandboxes;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using PrismCodingAgent.AvaloniaShell.Infrastructure;
using PrismCodingAgent.AvaloniaShell.Services.Chat;
using PrismCodingAgent.AvaloniaShell.Services.Sessions;
using PrismCodingAgent.AvaloniaShell.Services.Settings;
using PrismCodingAgent.AvaloniaShell.Services.Workspace;

namespace PrismCodingAgent.AvaloniaShell.Services.WorkTasks;

/// <summary>
/// 按固定路径和严格失败策略创建 CodingChatRoom 核心运行时。
/// </summary>
internal static class CodingWorkTaskRuntimeFactory
{
    public static async Task<CodingWorkTaskRuntime> InitializeAsync
    (
        CodingChatRoomPaths paths,
        IMainThreadDispatcher mainThreadDispatcher,
        WindowsSandboxToolSource? windowsSandboxToolSource = null
    )
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(mainThreadDispatcher);

        paths.EnsureDirectories();
        paths.ConfigurationFile.Refresh();
        AgentApiManagerConfiguration configuration = paths.ConfigurationFile.Exists
            ? await AgentApiManagerConfiguration.FromJsonFileAsync(paths.ConfigurationFile).ConfigureAwait(false)
            : new AgentApiManagerConfiguration();

        var endpointManager = new AgentApiEndpointManager();
        endpointManager.LoadConfiguration(configuration);
        ILanguageModel? primaryModel = endpointManager.GetSupportedModels().Count > 0 ? endpointManager.PrimaryModel : null;

        var chatLogger = new FileCopilotChatLogger(paths.LogDirectory);
        var chatManager = new CopilotChatManager(chatLogger)
        {
            AgentApiEndpointManager = endpointManager,
            MainThreadDispatcher = mainThreadDispatcher,
        };
        windowsSandboxToolSource ??= new WindowsSandboxToolSource
        (
            isEnabled: false,
            winRemoteShellPath: string.Empty,
            serverAddress: string.Empty
        );
        var settingsService = new CodingChatSettingsService(paths, windowsSandboxToolSource);
        CodingChatShellSettings shellSettings = await settingsService
            .LoadInitialShellSettingsAsync()
            .ConfigureAwait(false);
        windowsSandboxToolSource.UpdateConfiguration
        (
            shellSettings.IsWindowsSandboxEnabled,
            shellSettings.WindowsSandboxToolPath,
            shellSettings.WindowsSandboxServerAddress
        );
        endpointManager.WebProxy = CreateWebProxy(shellSettings);
        var additionalToolSources = new List<ICodingWorkspaceToolSource>
        {
            new CodingImageAnalysisToolSource(chatManager),
            windowsSandboxToolSource,
        };

        var codingAgent = new CodingAgent
        (
            new CodingAgentOptions
            {
                AdditionalToolSources = additionalToolSources,
                CopilotInstructionsPath = GetCopilotInstructionsPath(shellSettings),
            }
        );
        var workspaceController = new CodingWorkspaceController(mainThreadDispatcher);
        var sessionStore = new FileCodingChatSessionStore
        (
            paths.SessionDirectory.FullName,
            paths.LogDirectory,
            chatManager,
            mainThreadDispatcher
        );
        var chatRunner = new CodingAgentChatRunner(chatManager, codingAgent);
        var controller = new CodingWorkTaskController
        (
            chatManager,
            sessionStore,
            chatRunner,
            workspaceController,
            codingAgent
        );

        return new CodingWorkTaskRuntime
        {
            Paths = paths,
            EndpointManager = endpointManager,
            ChatLogger = chatLogger,
            ChatManager = chatManager,
            CodingAgent = codingAgent,
            CodingRunner = chatRunner,
            CopilotInstructionsPath = GetCopilotInstructionsPath(shellSettings),
            PrimaryModel = primaryModel,
            Controller = controller,
            WorkspaceController = workspaceController,
            SettingsService = settingsService,
        };
    }

    internal static IWebProxy? CreateWebProxy(CodingChatShellSettings shellSettings)
    {
        if (!shellSettings.IsNetworkProxyEnabled || string.IsNullOrWhiteSpace(shellSettings.NetworkProxyAddress))
        {
            return null;
        }

        return new WebProxy(shellSettings.NetworkProxyAddress)
        {
            BypassProxyOnLocal = shellSettings.BypassProxyOnLocal,
        };
    }

    private static string? GetCopilotInstructionsPath(CodingChatShellSettings shellSettings)
    {
        if (!shellSettings.IsCopilotInstructionsEnabled)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(shellSettings.CopilotInstructionsPath))
        {
            var userCopilotInstructionsPath = Path.Join
                (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "copilot-instructions.md");
            if (!File.Exists(userCopilotInstructionsPath))
            {
                // 如果不存在，那也不能炸掉。如果传入不存在的，在后续会炸掉
                return null;
            }
            else
            {
                return userCopilotInstructionsPath;
            }
        }
        else
        {
            return Path.GetFullPath(shellSettings.CopilotInstructionsPath);
        }
    }
}