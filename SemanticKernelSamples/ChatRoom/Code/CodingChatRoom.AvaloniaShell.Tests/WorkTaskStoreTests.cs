using CodingChatRoom.AvaloniaShell.Infrastructure;
using CodingChatRoom.AvaloniaShell.Services;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class WorkTaskStoreTests
{
    [TestMethod]
    public void WorkTasksFileShouldUseApplicationRootDirectory()
    {
        string rootDirectory = CreateRootDirectory();
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(rootDirectory);

        Assert.AreEqual(Path.Join(Path.GetFullPath(rootDirectory), "WorkTasks.json"), paths.WorkTasksFile.FullName);
    }

    [TestMethod]
    public async Task SavingAndLoadingShouldPreserveAllTaskMetadata()
    {
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(CreateRootDirectory());
        var store = new WorkTaskStore(paths);
        Guid id = Guid.NewGuid();
        var record = new WorkTaskRecord(
            id,
            "Long running task",
            Path.GetFullPath(paths.RootDirectory),
            "test-provider/test-model",
            ReasoningEffort.High);

        await store.SaveAsync([record]);
        IReadOnlyList<WorkTaskRecord> loaded = await store.LoadAsync();

        Assert.AreEqual(record, loaded.Single());
    }

    [TestMethod]
    public async Task LoadingWhenFileDoesNotExistShouldReturnEmptyList()
    {
        var store = new WorkTaskStore(CodingChatRoomPaths.Create(CreateRootDirectory()));

        IReadOnlyList<WorkTaskRecord> loaded = await store.LoadAsync();

        Assert.IsEmpty(loaded);
    }

    private static string CreateRootDirectory()
        => Path.Join(Path.GetTempPath(), $"CodingChatRoom.WorkTasks.{Guid.NewGuid():N}");
}
