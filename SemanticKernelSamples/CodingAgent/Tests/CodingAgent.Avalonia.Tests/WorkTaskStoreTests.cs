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
            ReasoningEffort.High,
            true);

        await store.SaveAsync([record]);
        IReadOnlyList<WorkTaskRecord> loaded = await store.LoadAsync();

        Assert.AreEqual(record, loaded.Single());
    }

    [TestMethod]
    public async Task LoadingLegacyTasksShouldAssignStableUniqueIds()
    {
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(CreateRootDirectory());
        paths.EnsureDirectories();
        await File.WriteAllTextAsync(paths.WorkTasksFile.FullName,
            """
            [
              { "DisplayName": "First" },
              { "DisplayName": "Second" }
            ]
            """);
        var store = new WorkTaskStore(paths);

        IReadOnlyList<WorkTaskRecord> firstLoad = await store.LoadAsync();
        Assert.IsNotNull(store.LastRecoveryInfo);
        WorkTaskRecoveryInfo recoveryInfo = store.LastRecoveryInfo;
        IReadOnlyList<WorkTaskRecord> secondLoad = await store.LoadAsync();

        CollectionAssert.AreEqual(firstLoad.Select(task => task.Id).ToArray(), secondLoad.Select(task => task.Id).ToArray());
        Assert.AreEqual(2, recoveryInfo.AssignedMissingIdCount);
    }

    [TestMethod]
    public async Task LoadingDuplicateNonEmptyTaskIdsShouldRepairConflictAndKeepActiveTaskAssociation()
    {
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(CreateRootDirectory());
        paths.EnsureDirectories();
        Guid duplicateId = Guid.NewGuid();
        await File.WriteAllTextAsync(paths.WorkTasksFile.FullName,
            $$"""
            [
              { "Id": "{{duplicateId}}", "DisplayName": "Archived", "IsArchived": true },
              { "Id": "{{duplicateId}}", "DisplayName": "Active" }
            ]
            """);
        var store = new WorkTaskStore(paths);

        IReadOnlyList<WorkTaskRecord> loaded = await store.LoadAsync();
        Assert.IsNotNull(store.LastRecoveryInfo);
        WorkTaskRecoveryInfo recoveryInfo = store.LastRecoveryInfo;

        Assert.AreEqual(duplicateId, loaded.Single(task => !task.IsArchived).Id);
        Assert.AreNotEqual(duplicateId, loaded.Single(task => task.IsArchived).Id);
        Assert.AreEqual(1, recoveryInfo.ReassignedDuplicateIdCount);
        Assert.IsTrue(File.Exists(recoveryInfo.BackupFilePath));
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
