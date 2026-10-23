using System.Text.Json;
using AgentLib;
using PrismCodingAgent.AvaloniaShell.Abilities;
using PrismCodingAgent.AvaloniaShell.Infrastructure;
using PrismCodingAgent.AvaloniaShell.Services.WorkTasks;
using PrismCodingAgent.AvaloniaShell.ViewModels;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[TestClass]
public sealed class WorkTaskPinningTests
{
    [TestMethod]
    public async Task PinningShouldMoveTaskToTopWithoutChangingSelection()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);

        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(second);

        Assert.AreEqual((second, first), (shell.FilteredWorkTasks[0], shell.ActiveWorkTask));
    }

    [TestMethod]
    public async Task PinningAgainShouldKeepTaskAtTop()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);
        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(second);

        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(second);

        CollectionAssert.AreEqual(new[] { second, first }, shell.FilteredWorkTasks.ToArray());
    }

    [TestMethod]
    public async Task LastMovedTaskShouldPrecedePreviouslyMovedTasks()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        var third = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);
        shell.WorkTasks.Add(third);
        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(second);

        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(third);

        CollectionAssert.AreEqual(new[] { third, second, first }, shell.FilteredWorkTasks.ToArray());
    }

    [TestMethod]
    public async Task SearchChangesShouldPreserveTaskOrder()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);
        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(second);
        shell.WorkTaskSearchText = "No match";

        shell.WorkTaskSearchText = string.Empty;

        CollectionAssert.AreEqual(new[] { second, first }, shell.FilteredWorkTasks.ToArray());
    }

    [TestMethod]
    public async Task PinningShouldChangePersistenceOrder()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);

        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(second);

        CollectionAssert.AreEqual(new[] { second, first }, shell.WorkTasks.ToArray());
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
        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(shell.ActiveWorkTask);

        await ((SimpleAsyncCommand)shell.CreateWorkTaskCommand).ExecuteAsync();

        Assert.AreEqual((2, shell.ActiveWorkTask, shell.ActiveWorkTask),
            (shell.WorkTasks.Count, shell.WorkTasks[0], shell.FilteredWorkTasks[0]));
    }

    [TestMethod]
    public async Task SavingAfterPinningShouldPreserveTaskOrder()
    {
        var paths = CreatePaths();
        await using var shell = await CreateShellAsync(paths);
        var first = shell.ActiveWorkTask;
        await ((SimpleAsyncCommand)shell.CreateWorkTaskCommand).ExecuteAsync();
        var second = shell.ActiveWorkTask;
        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(first);
        shell.RenameWorkTaskCommand.Execute(first);
        first.EditedDisplayName = "Renamed";

        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.SaveWorkTaskNameCommand).ExecuteAsync(first);
        var saved = await new WorkTaskStore(paths).LoadAsync();

        CollectionAssert.AreEqual(new[] { first.Id, second.Id }, saved.Select(task => task.Id).ToArray());
    }

    [TestMethod]
    public async Task RestoringTaskShouldPlaceItAbovePreviouslyMovedTask()
    {
        await using var shell = await CreateShellAsync();
        var original = shell.ActiveWorkTask;
        await ((SimpleAsyncCommand)shell.CreateWorkTaskCommand).ExecuteAsync();
        var other = shell.ActiveWorkTask;
        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.ArchiveWorkTaskCommand).ExecuteAsync(original);
        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(other);
        var archived = shell.ArchivedWorkTasks.Single();

        await ((SimpleAsyncCommand<ArchivedWorkTaskItemViewModel>)shell.RestoreWorkTaskCommand).ExecuteAsync(archived);

        CollectionAssert.AreEqual(new[] { original.Id, other.Id }, shell.FilteredWorkTasks.Select(task => task.Id).ToArray());
    }

    [TestMethod]
    public async Task RestoringTaskShouldPersistItAtTop()
    {
        var paths = CreatePaths();
        await using var shell = await CreateShellAsync(paths);
        var original = shell.ActiveWorkTask;
        await ((SimpleAsyncCommand)shell.CreateWorkTaskCommand).ExecuteAsync();
        var other = shell.ActiveWorkTask;
        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.ArchiveWorkTaskCommand).ExecuteAsync(original);
        var archived = shell.ArchivedWorkTasks.Single();

        await ((SimpleAsyncCommand<ArchivedWorkTaskItemViewModel>)shell.RestoreWorkTaskCommand).ExecuteAsync(archived);
        var saved = await new WorkTaskStore(paths).LoadAsync();

        CollectionAssert.AreEqual(new[] { original.Id, other.Id }, saved.Select(task => task.Id).ToArray());
    }

    [TestMethod]
    public async Task PinningShouldImmediatelyPersistTaskOrder()
    {
        var paths = CreatePaths();
        await using var shell = await CreateShellAsync(paths);
        var first = shell.ActiveWorkTask;
        await ((SimpleAsyncCommand)shell.CreateWorkTaskCommand).ExecuteAsync();
        var second = shell.ActiveWorkTask;

        await ((SimpleAsyncCommand<WorkTaskItemViewModel>)shell.PinWorkTaskCommand).ExecuteAsync(first);
        var saved = await new WorkTaskStore(paths).LoadAsync();

        CollectionAssert.AreEqual(new[] { first.Id, second.Id }, saved.Select(task => task.Id).ToArray());
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
