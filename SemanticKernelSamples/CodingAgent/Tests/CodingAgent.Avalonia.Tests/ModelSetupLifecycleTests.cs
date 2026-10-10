using AgentLib;
using CodingChatRoom.AvaloniaShell.Abilities;
using CodingChatRoom.AvaloniaShell.Infrastructure;
using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ModelSetupLifecycleTests
{
    [TestMethod]
    public async Task SavingThenRemovingModelShouldUpdateAllTasks()
    {
        var paths = CodingChatRoomPaths.Create(Path.Join(Path.GetTempPath(), "CodingChatRoom-ModelSetup", Guid.NewGuid().ToString("N")));
        Console.WriteLine(paths.RootDirectory);
        await using var runtime = await CodingWorkTaskRuntimeFactory.InitializeAsync(paths, new Dispatcher());
        var abilities = new AbilityCatalog(paths.AbilitiesDirectory);
        var task = MainViewModel.CreateRuntimeTask(runtime, new WorkTaskRecord(Guid.NewGuid(), "Task 1", null, null, null), abilities);
        var main = MainViewModel.Create([task], [], runtime.SettingsService, new WorkTaskStore(paths),
            () => CodingWorkTaskRuntimeFactory.InitializeAsync(paths, new Dispatcher()), abilities);
        await main.InitializeSettingsAsync();
        var settings = main.SettingsViewModel ?? throw new InvalidOperationException();
        var provider = settings.Providers.Single();
        provider.Models.Clear();
        provider.Models.Add(new ModelSettingsViewModel { Provider = "test", ModelName = "test-model" });
        await ((SimpleAsyncCommand)settings.SaveCommand).ExecuteAsync();
        Assert.HasCount(1, task.Chat.AvailableModels);
        provider.RemoveModelCommand.Execute(provider.Models.Single());
        await ((SimpleAsyncCommand)settings.SaveCommand).ExecuteAsync();
        Assert.IsEmpty(task.Chat.AvailableModels);
    }

    private sealed class Dispatcher : IMainThreadDispatcher
    {
        public Task InvokeAsync(Func<Task> action) => action();
        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();
        public bool CheckAccess() => true;
    }
}
