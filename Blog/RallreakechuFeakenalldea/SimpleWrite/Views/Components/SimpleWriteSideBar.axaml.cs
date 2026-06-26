using System;

using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia;

using SimpleWrite.ViewModels;

namespace SimpleWrite.Views.Components;

public partial class SimpleWriteSideBar : UserControl
{
    private const double DefaultExpandedWidth = 200;
    private const double CollapsedWidth = 2;

    private bool _isExpanded = true;
    private bool _isInitialized;
    private double _expandedWidth = DefaultExpandedWidth;

    public SimpleWriteSideBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public FolderExplorerViewModel ViewModel => DataContext as FolderExplorerViewModel
        ?? throw new InvalidOperationException("SimpleWriteSideBar's DataContext must be of type FolderExplorerViewModel");

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        if (!double.IsNaN(Width) && Width > CollapsedWidth)
        {
            _expandedWidth = Width;
        }

        ApplySidebarState(isExpanded: true, storeCurrentWidth: false);
        _isInitialized = true;
    }

    private void ToggleSidebarButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ApplySidebarState(!_isExpanded);
    }

    private async void OpenFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await ViewModel.PickAndOpenFolderAsync();
    }

    private void ClearFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ClearFolder();
    }

    private async void FolderTreeView_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (FolderTreeView.SelectedItem is not FolderTreeItemViewModel folderTreeItem || folderTreeItem.IsDirectory)
        {
            return;
        }

        await ViewModel.OpenFileAsync(folderTreeItem);
        e.Handled = true;
    }

    private void ApplySidebarState(bool isExpanded, bool storeCurrentWidth = true)
    {
        if (!isExpanded && storeCurrentWidth && !double.IsNaN(Width) && Width > CollapsedWidth)
        {
            _expandedWidth = Width;
        }

        _isExpanded = isExpanded;

        SidebarContentHost.IsVisible = isExpanded;
        Width = isExpanded ? _expandedWidth : CollapsedWidth;
        ToggleChevronTextBlock.Text = isExpanded ? "❮" : "❯";
        ToggleSidebarButton.Margin = isExpanded ? new Thickness(0) : new Thickness(0, 0, -35, 0);
        ToolTip.SetTip(ToggleSidebarButton, isExpanded ? "收起侧边栏" : "展开侧边栏");
    }
}