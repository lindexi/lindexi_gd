using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AgentLib.Coding.Sandboxes;
using CodingChatRoom.AvaloniaShell.Abilities;
using CodingChatRoom.AvaloniaShell.Infrastructure;
using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            InitializeApp(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void InitializeApp(IClassicDesktopStyleApplicationLifetime desktop)
    {
        CodingChatRoomPaths paths = CodingChatRoomPaths.CreateForCurrentUser();
        var runtimes = new List<CodingWorkTaskRuntime>();
        try
        {
            var dispatcher = new AvaloniaMainThreadDispatcher();
            var windowsSandboxToolSource = new WindowsSandboxToolSource(false, string.Empty, string.Empty);
            await DefaultAbilityInstaller.InstallOnceAsync(paths.AbilitiesDirectory).ConfigureAwait(true);
            var abilityCatalog = new AbilityCatalog(paths.AbilitiesDirectory);
            _ = abilityCatalog.RefreshAsync();
            var workTaskStore = new WorkTaskStore(paths);
            IReadOnlyList<WorkTaskRecord> records = await workTaskStore.LoadAsync().ConfigureAwait(true);
            WorkTaskRecord[] activeRecords = records.Where(record => !record.IsArchived).ToArray();
            WorkTaskRecord[] archivedRecords = records.Where(record => record.IsArchived).ToArray();
            var tasks = new List<WorkTaskItemViewModel>();

            if (activeRecords.Length == 0)
            {
                CodingWorkTaskRuntime runtime = await CodingWorkTaskRuntimeFactory
                    .InitializeAsync(paths, dispatcher, windowsSandboxToolSource)
                    .ConfigureAwait(true);
                runtimes.Add(runtime);
                tasks.Add
                (
                    MainViewModel.CreateRuntimeTask
                    (
                        runtime, new WorkTaskRecord
                        (
                            Guid.NewGuid(),
                            GetDefaultTaskName(1),
                            null,
                            runtime.ModelDisplayName,
                            null
                        ),
                        abilityCatalog
                    )
                );
            }
            else
            {
                foreach (WorkTaskRecord record in activeRecords)
                {
                    CodingWorkTaskRuntime runtime = await CodingWorkTaskRuntimeFactory
                        .InitializeAsync(paths, dispatcher, windowsSandboxToolSource)
                        .ConfigureAwait(true);
                    runtimes.Add(runtime);
                    WorkTaskItemViewModel task = MainViewModel.CreateRuntimeTask(runtime, record, abilityCatalog);
                    await RestoreTaskConfigurationAsync(task, runtime, record).ConfigureAwait(true);
                    tasks.Add(task);
                }
            }

            var mainViewModel = MainViewModel.Create
            (
                tasks,
                archivedRecords,
                runtimes[0].SettingsService,
                workTaskStore,
                () => CodingWorkTaskRuntimeFactory.InitializeAsync
                    (paths, new AvaloniaMainThreadDispatcher(), windowsSandboxToolSource),
                abilityCatalog,
                FormatWorkTaskRecoveryMessage(workTaskStore.LastRecoveryInfo)
            );
            if (activeRecords.Length == 0)
            {
                await workTaskStore.SaveAsync([CreateRecord(tasks[0]), .. archivedRecords]).ConfigureAwait(true);
            }

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                                              or InvalidOperationException or ArgumentException or JsonException)
        {
            foreach (CodingWorkTaskRuntime runtime in runtimes)
            {
                await runtime.DisposeAsync().ConfigureAwait(true);
            }

            Trace.TraceError($"CodingChatRoom 应用初始化失败：{exception}");
            var failureWindow = new StartupFailureWindow
            {
                DataContext = new StartupFailureViewModel
                (
                    paths.ConfigurationFile.FullName,
                    exception,
                    () => desktop.TryShutdown(1)
                ),
            };
            desktop.MainWindow = failureWindow;
            failureWindow.Show();
        }
    }

    private static async Task RestoreTaskConfigurationAsync
    (
        WorkTaskItemViewModel task,
        CodingWorkTaskRuntime runtime,
        WorkTaskRecord record
    )
    {
        if (!string.IsNullOrWhiteSpace(record.WorkspacePath))
        {
            await runtime.WorkspaceController.ChangeWorkspaceAsync(record.WorkspacePath).ConfigureAwait(true);
        }

        LanguageModelOptionViewModel? model = task.Chat.AvailableModels.FirstOrDefault
            (option => string.Equals(option.DisplayName, record.ModelReference, StringComparison.Ordinal));
        if (model is not null)
        {
            task.Chat.SelectedModel = model;
        }

        ReasoningEffortOptionViewModel? reasoningEffort = task.Chat.AvailableReasoningEfforts.FirstOrDefault
            (option => option.Value == record.ReasoningEffort);
        if (reasoningEffort is not null)
        {
            task.Chat.SelectedReasoningEffort = reasoningEffort;
        }
    }

    private static string? FormatWorkTaskRecoveryMessage(WorkTaskRecoveryInfo? recoveryInfo)
    {
        if (recoveryInfo is null)
        {
            return null;
        }

        return $"已自动修复工作任务配置：补充 {recoveryInfo.AssignedMissingIdCount} 个缺失 Id，修复 {recoveryInfo.ReassignedDuplicateIdCount} 个重复 Id。原配置已备份到：{recoveryInfo.BackupFilePath}";
    }

    private static WorkTaskRecord CreateRecord(WorkTaskItemViewModel task)
        => new
        (
            task.Id,
            task.DisplayName,
            task.Chat.NextRunWorkspacePath,
            task.Chat.SelectedModel?.DisplayName,
            task.Chat.SelectedReasoningEffort?.Value,
            false
        );

    private static string GetDefaultTaskName(int number)
        => $"{Current?.FindResource("WorkTaskText") ?? "工作任务"} {number}";
}