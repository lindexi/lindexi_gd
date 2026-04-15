using System.IO;
using VirtualFileExplorer.Core.PhysicalFileManagers;
using Xunit;

namespace VirtualFileExplorer.IntegrationTests;

public class PhysicalFileManagerAsyncAdapterIntegrationTests
{
    [Fact]
    public async Task WhenUsingAsyncAdapterThenCanCreateAndEnumerateFolders()
    {
        var rootPath = CreateUniqueTestDirectory();
        var manager = new PhysicalFileManager(rootPath, "测试目录");
        var adapter = manager.AsAsync();

        var folder = await adapter.CreateFolderAsync(manager.RootFolder, "文档");
        var folders = await adapter.GetFoldersAsync(manager.RootFolder);

        Assert.Contains(folders, item => string.Equals(item.Id, folder.Id, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task WhenUsingAsyncAdapterThenCanRenameCopyMoveAndDeleteFile()
    {
        var rootPath = CreateUniqueTestDirectory();
        var manager = new PhysicalFileManager(rootPath, "测试目录");
        var adapter = manager.AsAsync();
        var targetFolder = await adapter.CreateFolderAsync(manager.RootFolder, "目标目录");
        var moveFolder = await adapter.CreateFolderAsync(manager.RootFolder, "移动目录");
        var filePath = Path.Combine(rootPath, "sample.txt");
        await File.WriteAllTextAsync(filePath, "test");

        var file = (await adapter.GetFilesAsync(manager.RootFolder)).Single();
        await adapter.RenameEntryAsync(file, "renamed.txt");

        var renamedFile = (await adapter.GetFilesAsync(manager.RootFolder)).Single();
        var copiedFile = await adapter.CopyEntryAsync(renamedFile, targetFolder);
        var movedFile = await adapter.MoveEntryAsync(copiedFile, moveFolder);
        await adapter.DeleteEntryAsync(movedFile);

        Assert.True(File.Exists(Path.Combine(rootPath, "renamed.txt")));
        Assert.False(File.Exists(Path.Combine(rootPath, "sample.txt")));
        Assert.False(File.Exists(movedFile.Id));
    }

    private static string CreateUniqueTestDirectory()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "VirtualFileExplorerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);
        return rootPath;
    }
}
