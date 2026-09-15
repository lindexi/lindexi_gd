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
        var runtimes = new List<CodingChatRuntime>();
        try
        {
            var dispatcher = new AvaloniaMainThreadDispatcher();
            var windowsSandboxToolSource = new WindowsSandboxToolSource(false, string.Empty, string.Empty);
            var workTaskStore = new WorkTaskStore(paths);
            IReadOnlyList<WorkTaskRecord> records = await workTaskStore.LoadAsync().ConfigureAwait(true);
            WorkTaskRecord[] activeRecords = records.Where(record => !record.IsArchived).ToArray();
            WorkTaskRecord[] archivedRecords = records.Where(record => record.IsArchived).ToArray();
            var tasks = new List<WorkTaskItemViewModel>();

            if (activeRecords.Length == 0)
            {
                CodingChatRuntime runtime = await CodingChatStartup
                    .InitializeAsync(paths, dispatcher, windowsSandboxToolSource)
                    .ConfigureAwait(true);
                runtimes.Add(runtime);
                tasks.Add(MainViewModel.CreateRuntimeTask(runtime, new WorkTaskRecord(
                    Guid.NewGuid(),
                    GetDefaultTaskName(1),
                    null,
                    runtime.ModelDisplayName,
                    null)));
            }
            else
            {
                foreach (WorkTaskRecord record in activeRecords)
                {
                    CodingChatRuntime runtime = await CodingChatStartup
                        .InitializeAsync(paths, dispatcher, windowsSandboxToolSource)
                        .ConfigureAwait(true);
                    runtimes.Add(runtime);
                    WorkTaskItemViewModel task = MainViewModel.CreateRuntimeTask(runtime, record);
                    await RestoreTaskConfigurationAsync(task, runtime, record).ConfigureAwait(true);
                    tasks.Add(task);
                }
            }

            var mainViewModel = MainViewModel.Create(
                tasks,
                archivedRecords,
                runtimes[0].SettingsService,
                workTaskStore,
                () => CodingChatStartup.InitializeAsync(paths, new AvaloniaMainThreadDispatcher(), windowsSandboxToolSource));
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
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or JsonException)
        {
            foreach (CodingChatRuntime runtime in runtimes)
            {
                await runtime.DisposeAsync().ConfigureAwait(true);
            }

            Trace.TraceError($"CodingChatRoom 应用初始化失败：{exception}");
            var failureWindow = new StartupFailureWindow
            {
                DataContext = new StartupFailureViewModel(
                    paths.ConfigurationFile.FullName,
                    exception,
                    () => desktop.TryShutdown(1)),
            };
            desktop.MainWindow = failureWindow;
            failureWindow.Show();
        }
    }

    private static async Task RestoreTaskConfigurationAsync(
        WorkTaskItemViewModel task,
        CodingChatRuntime runtime,
        WorkTaskRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.WorkspacePath))
        {
            await runtime.WorkspaceController.ChangeWorkspaceAsync(record.WorkspacePath).ConfigureAwait(true);
        }

        LanguageModelOptionViewModel? model = task.Chat.AvailableModels.FirstOrDefault(
            option => string.Equals(option.DisplayName, record.ModelReference, StringComparison.Ordinal));
        if (model is not null)
        {
            task.Chat.SelectedModel = model;
        }

        ReasoningEffortOptionViewModel? reasoningEffort = task.Chat.AvailableReasoningEfforts.FirstOrDefault(
            option => option.Value == record.ReasoningEffort);
        if (reasoningEffort is not null)
        {
            task.Chat.SelectedReasoningEffort = reasoningEffort;
        }
    }

    private static WorkTaskRecord CreateRecord(WorkTaskItemViewModel task)
        => new(
            task.Id,
            task.DisplayName,
            task.Chat.NextRunWorkspacePath,
            task.Chat.SelectedModel?.DisplayName,
            task.Chat.SelectedReasoningEffort?.Value,
            false);

    private static string GetDefaultTaskName(int number)
        => $"{Current?.FindResource("WorkTaskText") ?? "工作任务"} {number}";
}
