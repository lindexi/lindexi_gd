using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class TaskTitleVisualTests
{
    [TestMethod]
    public void TaskMenuShouldAlignWithTitleCenter()
    {
        var model = new MainViewModel();
        var window = new MainWindow { DataContext = model };
        try
        {
            window.Show();
            window.UpdateLayout();
            var title = window.GetVisualDescendants().OfType<TextBlock>().Single(item => item.Text == model.ActiveWorkTask.DisplayName);
            var menu = window.GetVisualDescendants().OfType<Button>().Single(item => item.Classes.Contains("Icon") && item.Flyout is MenuFlyout);
            var titlePosition = title.TranslatePoint(default, window) ?? throw new InvalidOperationException("Title position unavailable.");
            var menuPosition = menu.TranslatePoint(default, window) ?? throw new InvalidOperationException("Menu position unavailable.");
            Assert.AreEqual(titlePosition.Y + title.Bounds.Height / 2, menuPosition.Y + menu.Bounds.Height / 2, 1);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void RenamingActiveTaskShouldUpdateWindowTitle()
    {
        var model = new MainViewModel();
        var window = new MainWindow { DataContext = model };
        try
        {
            window.Show();
            model.ActiveWorkTask.DisplayName = "Renamed task";
            StringAssert.Contains(window.Title, "Renamed task");
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void SwitchingTaskShouldUpdateWindowTitle()
    {
        var model = new MainViewModel();
        var other = new MainViewModel().ActiveWorkTask;
        other.DisplayName = "Other task";
        model.WorkTasks.Add(other);
        var window = new MainWindow { DataContext = model };
        try
        {
            window.Show();
            model.ActivateWorkTaskCommand.Execute(other);
            StringAssert.Contains(window.Title, "Other task");
        }
        finally { window.Close(); }
    }
}
