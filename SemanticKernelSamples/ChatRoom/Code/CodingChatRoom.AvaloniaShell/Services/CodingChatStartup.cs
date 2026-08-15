using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Coding.Sandboxes;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;

using CodingChatRoom.AvaloniaShell.Infrastructure;

namespace CodingChatRoom.AvaloniaShell.Services;

/// <summary>
/// 按固定路径和严格失败策略创建 CodingChatRoom 核心运行时。
/// </summary>
internal static class CodingChatStartup
{
    public static async Task<CodingChatRuntime> InitializeAsync
    (
        CodingChatRoomPaths paths,
        IMainThreadDispatcher mainThreadDispatcher
    )
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(mainThreadDispatcher);

        paths.EnsureDirectories();
        paths.ConfigurationFile.Refresh();
        if (!paths.ConfigurationFile.Exists)
        {
            throw new FileNotFoundException(
                $"未找到 CodingChatRoom 模型配置文件：{paths.ConfigurationFile.FullName}",
                paths.ConfigurationFile.FullName);
        }

        AgentApiManagerConfiguration configuration = await AgentApiManagerConfiguration
            .FromJsonFileAsync(paths.ConfigurationFile)
            .ConfigureAwait(false);

        var endpointManager = new AgentApiEndpointManager();
        endpointManager.LoadConfiguration(configuration);
        ILanguageModel primaryModel = endpointManager.PrimaryModel;

        var chatLogger = new FileCopilotChatLogger(paths.LogDirectory);
        var chatManager = new CopilotChatManager(chatLogger)
        {
            AgentApiEndpointManager = endpointManager,
            MainThreadDispatcher = mainThreadDispatcher,
        };
        CodingChatShellSettings shellSettings = await new CodingChatSettingsService(paths)
            .LoadShellSettingsAsync()
            .ConfigureAwait(false);
        var additionalToolSources = new List<ICodingWorkspaceToolSource>();
        if (shellSettings.IsWindowsSandboxEnabled)
        {
            additionalToolSources.Add(new WindowsSandboxToolSource(
                shellSettings.WindowsSandboxToolPath,
                shellSettings.WindowsSandboxServerAddress));
        }

        var codingAgent = new CodingAgent(new CodingAgentOptions
        {
            AdditionalToolSources = additionalToolSources,
            CopilotInstructionsPath = GetCopilotInstructionsPath(shellSettings),
        });
        var workspaceController = new CodingWorkspaceController(
            new CodingAgentWorkspaceRuntime(codingAgent),
            mainThreadDispatcher);
        var sessionStore = new FileCodingChatSessionStore(
            paths.SessionDirectory,
            paths.LogDirectory,
            chatManager,
            mainThreadDispatcher);
        var chatRunner = new CodingAgentChatRunner(chatManager, codingAgent);
        var application = new CodingChatApplication(chatManager, sessionStore, chatRunner, workspaceController);

        return new CodingChatRuntime
        (
            paths,
            endpointManager,
            chatLogger,
            chatManager,
            codingAgent,
            primaryModel,
            application,
            workspaceController
        );
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
