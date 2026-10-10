using System.Text.Json;
using AgentLib;
using CodingAgent.AvaloniaShell.Abilities;
using CodingAgent.AvaloniaShell.Infrastructure;
using CodingAgent.AvaloniaShell.Services.WorkTasks;
using CodingAgent.AvaloniaShell.ViewModels;

namespace CodingAgent.AvaloniaShell.Tests;

[TestClass]
public sealed class WorkTaskPinningTests
{
    [TestMethod]
    public void PinningShouldMoveTaskToTopWithoutChangingSelection()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);

        shell.PinWorkTaskCommand.Execute(second);

        Assert.AreEqual((second, first), (shell.FilteredWorkTasks[0], shell.ActiveWorkTask));
    }

    [TestMethod]
    public void PinningAgainShouldKeepTaskAtTop()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);
        shell.PinWorkTaskCommand.Execute(second);

        shell.PinWorkTaskCommand.Execute(second);

        CollectionAssert.AreEqual(new[] { second, first }, shell.FilteredWorkTasks.ToArray());
    }

    [TestMethod]
    public void LastMovedTaskShouldPrecedePreviouslyMovedTasks()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        var third = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);
        shell.WorkTasks.Add(third);
        shell.PinWorkTaskCommand.Execute(second);

        shell.PinWorkTaskCommand.Execute(third);

        CollectionAssert.AreEqual(new[] { third, second, first }, shell.FilteredWorkTasks.ToArray());
    }

    [TestMethod]
    public void SearchChangesShouldPreserveTemporaryOrder()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);
        shell.PinWorkTaskCommand.Execute(second);
        shell.WorkTaskSearchText = "No match";

        shell.WorkTaskSearchText = string.Empty;

        CollectionAssert.AreEqual(new[] { second, first }, shell.FilteredWorkTasks.ToArray());
    }

    [TestMethod]
    public void PinningShouldNotChangePersistenceOrder()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);

        shell.PinWorkTaskCommand.Execute(second);

        CollectionAssert.AreEqual(new[] { first, second }, shell.WorkTasks.ToArray());
    }

    [TestMethod]
    public void LegacyPinStateShouldBeIgnoredAndNotSerialized()
    {
        var record = JsonSerializer.Deserialize<WorkTaskRecord>("""{"DisplayName":"Legacy","IsPinned":true}""");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(record));

        Assert.IsFalse(document.RootElement.TryGetProperty("IsPinned", out _));
    }

    [TestMethod]
    public async Task CreatingTaskShouldPlaceItAbovePreviouslyMovedTask()
    {
        await using var shell = await CreateShellAsync();
        shell.PinWorkTaskCommand.Execute(shell.ActiveWorkTask);

        await ((SimpleAsyncCommand)shell.CreateWorkTaskCommand).ExecuteAsync();

        Assert.AreEqual((2, shell.ActiveWorkTask, shell.ActiveWorkTask),
            (shell.WorkTasks.Count, shell.WorkTasks[0], shell.FilteredWorkTasks[0]));
    }

    [TestMethod]
    public async Task SavingAfterPinningShouldNotPersistTemporaryOrder()
    {
        var paths = CreatePaths();
        await using var shell = await CreateShellAsync(paths);
        var first = shell.ActiveWorkTask;
        await ((SimpleAsyncCommand)shell.CreateWorkTaskCommand).ExecuteAsync();
        var second = shell.ActiveWorkTask;
        shell.PinWorkTaskCommand.Execute(first);
        shell.RenameWorkTaskCommand.Execute(first);
        first.EditedDisplayName = "Renamed";

        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.SaveWorkTaskNameCommand).ExecuteAsync(first);
        var saved = await new WorkTaskStore(paths).LoadAsync();

        CollectionAssert.AreEqual(new[] { second.Id, first.Id }, saved.Select(task => task.Id).ToArray());
    }

    private static CodingChatRoomPaths CreatePaths()
    {
        var paths = CodingChatRoomPaths.Create(Path.Join(Path.GetTempPath(), "CodingChatRoom-Pinning", Guid.NewGuid().ToString("N")));
        Console.WriteLine(paths.RootDirectory);
        return paths;
    }

    private static async Task<MainViewModel> CreateShellAsync(CodingChatRoomPaths? paths = null)
    {
        paths ??= CreatePaths();
        var runtime = await CodingWorkTaskRuntimeFactory.InitializeAsync(paths, new Dispatcher());
        var abilities = new AbilityCatalog(paths.AbilitiesDirectory);
        var task = MainViewModel.CreateRuntimeTask(runtime, new WorkTaskRecord(Guid.NewGuid(), "First", null, null, null), abilities);
        return MainViewModel.Create([task], [], runtime.SettingsService, new WorkTaskStore(paths),
            () => CodingWorkTaskRuntimeFactory.InitializeAsync(paths, new Dispatcher()), abilities);
    }

    private sealed class Dispatcher : IMainThreadDispatcher
    {
        public Task InvokeAsync(Func<Task> action) => action();
        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();
        public bool CheckAccess() => true;
    }
}
