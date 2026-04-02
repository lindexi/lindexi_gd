using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using SimpleWrite.ViewModels;

namespace SimpleWrite.Views.Components;

public partial class FindReplaceBar : UserControl
{
    public FindReplaceBar()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    public FindReplaceViewModel ViewModel => DataContext as FindReplaceViewModel
        ?? throw new InvalidOperationException("FindReplaceBar's DataContext must be of type FindReplaceViewModel");

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        }

        if (DataContext is FindReplaceViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        base.OnDataContextChanged(e);
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FindReplaceViewModel.IsPanelVisible)
            || e.PropertyName == nameof(FindReplaceViewModel.IsReplaceMode)
            || e.PropertyName == nameof(FindReplaceViewModel.IsFolderSearchScope))
        {
            if (ViewModel.IsPanelVisible)
            {
                Dispatcher.UIThread.Post(FocusActiveInput, DispatcherPriority.Input);
            }
        }
    }

    private void FindPreviousButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.FindPrevious();
    }

    private void FindNextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.FindNext();
    }

    private void ReplaceCurrentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ReplaceCurrent();
    }

    private void ReplaceAllButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ReplaceAll();
    }

    private void CloseFindReplaceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        HidePanel();
    }

    private async void SearchFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await ViewModel.SearchInFolderAsync();
    }

    private async void OpenFolderSearchResultButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: FolderSearchResultViewModel folderSearchResultViewModel })
        {
            return;
        }

        await ViewModel.OpenFolderSearchResultAsync(folderSearchResultViewModel);
    }

    private async void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && ViewModel.IsPanelVisible)
        {
            HidePanel();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && ViewModel.IsPanelVisible)
        {
            if (ViewModel.IsFolderSearchScope)
            {
                await ViewModel.SearchInFolderAsync();
                e.Handled = true;
                return;
            }

            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                ViewModel.FindPrevious();
            }
            else
            {
                ViewModel.FindNext();
            }

            e.Handled = true;
        }
    }

    private void HidePanel()
    {
        ViewModel.Hide();
        FocusCurrentTextEditor();
    }

    private void FocusActiveInput()
    {
        var targetTextBox = ViewModel.IsReplaceAvailable && !string.IsNullOrEmpty(ViewModel.FindText)
            ? ReplaceTextBox
            : FindTextBox;

        targetTextBox.Focus();
        targetTextBox.SelectAll();
    }

    private void FocusCurrentTextEditor()
    {
        var textEditor = TextEditorInfo.GetTextEditorInfo(this).CurrentTextEditor;
        textEditor.Focus();
    }

    private FindReplaceViewModel? _viewModel;
}
