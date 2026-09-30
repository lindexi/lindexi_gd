using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Media;
using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class WorkTaskPinningTests
{
    [TestMethod]
    public void NewTaskShouldNotBePinned()
    {
        var shell = new MainViewModel();

        Assert.IsFalse(shell.ActiveWorkTask.IsPinned);
    }

    [TestMethod]
    public void PinningShouldMoveTaskAboveUnpinnedTasksWithoutChangingSelection()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);

        shell.ToggleWorkTaskPinCommand.Execute(second);

        Assert.AreEqual((second, first, true),
            (shell.FilteredWorkTasks[0], shell.ActiveWorkTask, second.IsPinned));
    }

    [TestMethod]
    public void UnpinningShouldRestoreOriginalOrder()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);
        shell.ToggleWorkTaskPinCommand.Execute(second);

        shell.ToggleWorkTaskPinCommand.Execute(second);

        CollectionAssert.AreEqual(new[] { first, second }, shell.FilteredWorkTasks.ToArray());
    }

    [TestMethod]
    public void SearchingShouldPreservePinnedFirstOrder()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        first.DisplayName = "Match first";
        var second = new MainViewModel().ActiveWorkTask;
        second.DisplayName = "Match second";
        shell.WorkTasks.Add(second);
        shell.ToggleWorkTaskPinCommand.Execute(second);

        shell.WorkTaskSearchText = "Match";

        CollectionAssert.AreEqual(new[] { second, first }, shell.FilteredWorkTasks.ToArray());
    }

    [TestMethod]
    public void MultiplePinnedTasksShouldKeepOriginalRelativeOrder()
    {
        var shell = new MainViewModel();
        var first = shell.ActiveWorkTask;
        var second = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(second);
        shell.ToggleWorkTaskPinCommand.Execute(second);

        shell.ToggleWorkTaskPinCommand.Execute(first);

        CollectionAssert.AreEqual(new[] { first, second }, shell.FilteredWorkTasks.ToArray());
    }

    [TestMethod]
    public void ArchivingShouldDiscardPinnedState()
    {
        var shell = new MainViewModel();
        var task = shell.ActiveWorkTask;
        shell.WorkTasks.Add(new MainViewModel().ActiveWorkTask);
        shell.ToggleWorkTaskPinCommand.Execute(task);

        shell.ArchiveWorkTaskCommand.Execute(task);

        Assert.IsFalse(shell.ArchivedWorkTasks.Single().Record.IsPinned);
    }

    [TestMethod]
    public void MissingPinConfigurationShouldDefaultToFalse()
    {
        var record = JsonSerializer.Deserialize<WorkTaskRecord>("""{"DisplayName":"Legacy"}""");

        Assert.IsFalse(record?.IsPinned ?? true);
    }

    [TestMethod]
    public void SerializationShouldPreservePinnedState()
    {
        var record = new WorkTaskRecord(Guid.NewGuid(), "Pinned", null, null, null, IsPinned: true);

        var restored = JsonSerializer.Deserialize<WorkTaskRecord>(JsonSerializer.Serialize(record));

        Assert.AreEqual(record, restored);
    }

    [DataTestMethod]
    [DataRow(false, false, "#06000000")]
    [DataRow(true, false, "#10000000")]
    [DataRow(false, true, "#0A3978D4")]
    [DataRow(true, true, "#183978D4")]
    public void PinnedBackgroundShouldYieldToActiveAndWorkingStates(bool active, bool working, string expected)
    {
        var tile = new Border { Classes = { "TaskTile", "Pinned" } };
        tile.Classes.Set("Active", active);
        tile.Classes.Set("Working", working);
        tile.Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://CodingChatRoom.AvaloniaShell/"))
        {
            Source = new Uri("avares://CodingChatRoom.AvaloniaShell/Styles/Controls.axaml"),
        });
        tile.ApplyStyling();

        Assert.AreEqual(Color.Parse(expected), ((ISolidColorBrush?) tile.Background)?.Color);
    }
}
