using System.ComponentModel;
using System.IO;
using VirtualFileExplorer.Core;
using VirtualFileExplorer.Core.PhysicalFileManagers;
using VirtualFileExplorer.ViewModels;
using Xunit;

namespace VirtualFileExplorer.IntegrationTests;

public class FileExplorerViewModelIntegrationTests
{
    [Fact]
    public async Task WhenSearchFilterAndSortAreAppliedThenEntriesReflectRequestedView()
    {
        var rootPath = CreateUniqueTestDirectory();
        Directory.CreateDirectory(Path.Combine(rootPath, "图片目录"));
        await File.WriteAllTextAsync(Path.Combine(rootPath, "report.log"), "a");
        await File.WriteAllTextAsync(Path.Combine(rootPath, "alpha.txt"), "b");

        var viewModel = new FileExplorerViewModel
        {
            DisplayMode = FileExplorerDisplayMode.Details,
            EntryFilter = FileExplorerEntryFilter.FilesOnly,
            SortField = FileExplorerSortField.Name,
            SortDirection = ListSortDirection.Descending,
            SearchText = "a"
        };

        await viewModel.SetFileManagerAsync(new PhysicalFileManager(rootPath, "测试目录"));

        Assert.Equal(new[] { "report.log", "alpha.txt" }, viewModel.Entries.Select(entry => entry.Name).ToArray());
    }

    [Fact]
    public async Task WhenMultipleEntriesAreSelectedThenDeleteSelectedAsyncRemovesAll()
    {
        var rootPath = CreateUniqueTestDirectory();
        await File.WriteAllTextAsync(Path.Combine(rootPath, "first.txt"), "1");
        await File.WriteAllTextAsync(Path.Combine(rootPath, "second.txt"), "2");

        var manager = new PhysicalFileManager(rootPath, "测试目录");
        var viewModel = new FileExplorerViewModel();
        await viewModel.SetFileManagerAsync(manager);
        var entries = viewModel.Entries.OfType<VirtualFileInfo>().ToList();
        viewModel.UpdateSelection(entries, entries[0]);

        await viewModel.DeleteSelectedAsync();
        var remainingEntries = await manager.AsAsync().GetEntriesAsync(manager.RootFolder);

        Assert.Empty(remainingEntries);
    }

    private static string CreateUniqueTestDirectory()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "VirtualFileExplorerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);
        return rootPath;
    }
}
