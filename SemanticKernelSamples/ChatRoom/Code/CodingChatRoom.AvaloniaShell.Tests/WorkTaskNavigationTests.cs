using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class WorkTaskNavigationTests
{
    [TestMethod]
    public void OpeningArchiveShouldShowArchiveInsteadOfChat()
    {
        var shell = new MainViewModel();

        shell.OpenArchiveCommand.Execute(null);

        Assert.IsTrue(shell.IsArchiveOpen);
    }

    [TestMethod]
    public void ReturningFromArchiveShouldShowChat()
    {
        var shell = new MainViewModel();
        shell.OpenArchiveCommand.Execute(null);

        shell.CloseHistoryCommand.Execute(null);

        Assert.IsTrue(shell.IsChatOpen);
    }

    [TestMethod]
    public void EditingTaskNameShouldNotCommitUntilConfirmed()
    {
        var shell = new MainViewModel();
        var task = shell.ActiveWorkTask;
        string original = task.DisplayName;
        shell.RenameWorkTaskCommand.Execute(task);
        task.EditedDisplayName = "New task";
        Assert.AreEqual(original, task.DisplayName);
    }

    [TestMethod]
    public void ConfirmingTaskNameShouldSaveTrimmedName()
    {
        var shell = new MainViewModel();
        var task = shell.ActiveWorkTask;
        shell.RenameWorkTaskCommand.Execute(task);
        task.EditedDisplayName = "  New task  ";
        shell.SaveWorkTaskNameCommand.Execute(task);
        Assert.AreEqual("New task", task.DisplayName);
    }

    [TestMethod]
    public void EditingTaskNameShouldRefreshConfirmCommandState()
    {
        var shell = new MainViewModel();
        var task = shell.ActiveWorkTask;
        shell.RenameWorkTaskCommand.Execute(task);
        task.EditedDisplayName = string.Empty;
        int stateChangeCount = 0;
        shell.SaveWorkTaskNameCommand.CanExecuteChanged += (_, _) => stateChangeCount++;

        task.EditedDisplayName = "New task";

        Assert.AreEqual(1, stateChangeCount);
    }

    [TestMethod]
    public void ConfirmingTaskNameShouldCloseEditor()
    {
        var shell = new MainViewModel();
        shell.RenameWorkTaskCommand.Execute(shell.ActiveWorkTask);
        shell.SaveWorkTaskNameCommand.Execute(shell.ActiveWorkTask);
        Assert.IsFalse(shell.ActiveWorkTask.IsEditing);
    }

    [TestMethod]
    public void EmptyTaskNameShouldKeepEditorOpen()
    {
        var shell = new MainViewModel();
        shell.RenameWorkTaskCommand.Execute(shell.ActiveWorkTask);
        shell.ActiveWorkTask.EditedDisplayName = " ";
        shell.SaveWorkTaskNameCommand.Execute(shell.ActiveWorkTask);
        Assert.IsTrue(shell.ActiveWorkTask.IsEditing);
    }

    [TestMethod]
    public void SwitchingTasksShouldPreserveTheOriginalDraft()
    {
        var shell = new MainViewModel();
        var original = shell.ActiveWorkTask;
        var other = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(other);
        original.Chat.InputText = "original draft";

        shell.ActivateWorkTaskCommand.Execute(other);
        shell.ChatViewModel.InputText = "other draft";
        shell.ActivateWorkTaskCommand.Execute(original);

        Assert.AreEqual("original draft", shell.ChatViewModel.InputText);
    }

    [TestMethod]
    public void RenamingTaskShouldNotRenameItsSession()
    {
        var shell = new MainViewModel();
        string title = shell.ChatViewModel.CurrentSessionTitle;

        shell.ActiveWorkTask.DisplayName = "Independent task name";

        Assert.AreEqual(title, shell.ChatViewModel.CurrentSessionTitle);
    }

    [TestMethod]
    public void SwitchingTasksShouldProjectTheSelectedChatInstance()
    {
        var shell = new MainViewModel();
        var other = new MainViewModel().ActiveWorkTask;
        shell.WorkTasks.Add(other);

        shell.ActivateWorkTaskCommand.Execute(other);

        Assert.AreSame(other.Chat, shell.ChatViewModel);
    }
}
