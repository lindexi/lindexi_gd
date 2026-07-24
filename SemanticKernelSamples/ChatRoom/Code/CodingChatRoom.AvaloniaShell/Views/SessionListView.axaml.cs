using Avalonia.Controls;
using Avalonia.Interactivity;

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

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not SessionListViewModel viewModel || sender is not ListBox { SelectedItem: SessionItemViewModel session })
        {
            return;
        }

        if (viewModel.OpenSessionCommand.CanExecute(session))
        {
            viewModel.OpenSessionCommand.Execute(session);
        }
    }
}
