using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Views;

/// <summary>
/// 显示历史会话列表。
/// </summary>
public partial class SessionListView : UserControl
{
    /// <summary>
    /// 初始化历史会话列表视图。
    /// </summary>
    public SessionListView()
    {
        InitializeComponent();
    }

    private void OnSessionDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border { DataContext: SessionItemViewModel { IsEditing: false } session }
            || this.FindAncestorOfType<MainView>()?.DataContext is not MainViewModel mainViewModel
            || !mainViewModel.OpenSessionCommand.CanExecute(session))
        {
            return;
        }

        mainViewModel.OpenSessionCommand.Execute(session);
        e.Handled = true;
    }
}
