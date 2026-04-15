using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using VirtualFileExplorer.Core;
using VirtualFileExplorer.ViewModels;

namespace VirtualFileExplorer.Views;

public partial class FileExplorerContentControl : UserControl
{
    private bool _isUpdatingSelection;
    private FileExplorerViewModel? _viewModel;

    public FileExplorerContentControl()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateTemplateSelectors();
        DataContextChanged += OnDataContextChanged;
    }

    public DataTemplate? TileFileTemplate
    {
        get => (DataTemplate?)GetValue(TileFileTemplateProperty);
        set => SetValue(TileFileTemplateProperty, value);
    }

    public static readonly DependencyProperty TileFileTemplateProperty = DependencyProperty.Register(
        nameof(TileFileTemplate), typeof(DataTemplate), typeof(FileExplorerContentControl), new PropertyMetadata(null, OnTemplateChanged));

    public DataTemplate? TileFolderTemplate
    {
        get => (DataTemplate?)GetValue(TileFolderTemplateProperty);
        set => SetValue(TileFolderTemplateProperty, value);
    }

    public static readonly DependencyProperty TileFolderTemplateProperty = DependencyProperty.Register(
        nameof(TileFolderTemplate), typeof(DataTemplate), typeof(FileExplorerContentControl), new PropertyMetadata(null, OnTemplateChanged));

    public DataTemplate? TileFallbackTemplate
    {
        get => (DataTemplate?)GetValue(TileFallbackTemplateProperty);
        set => SetValue(TileFallbackTemplateProperty, value);
    }

    public static readonly DependencyProperty TileFallbackTemplateProperty = DependencyProperty.Register(
        nameof(TileFallbackTemplate), typeof(DataTemplate), typeof(FileExplorerContentControl), new PropertyMetadata(null, OnTemplateChanged));

    public event EventHandler<EntryInvokedEventArgs>? EntryInvoked;

    private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (FileExplorerContentControl)d;
        control.UpdateTemplateSelectors();
    }

    private void UpdateTemplateSelectors()
    {
        if (Resources["TileTemplateSelector"] is EntryTemplateSelector selector)
        {
            selector.FileTemplate = TileFileTemplate ?? TileFallbackTemplate ?? (DataTemplate)Resources["DefaultTileFileTemplate"];
            selector.FolderTemplate = TileFolderTemplate ?? TileFallbackTemplate ?? (DataTemplate)Resources["DefaultTileFolderTemplate"];
            selector.DefaultTemplate = TileFallbackTemplate ?? (DataTemplate)Resources["DefaultTileFileTemplate"];
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.SelectedEntries.CollectionChanged -= OnViewModelSelectedEntriesChanged;
        }

        _viewModel = e.NewValue as FileExplorerViewModel;
        if (_viewModel is not null)
        {
            _viewModel.SelectedEntries.CollectionChanged += OnViewModelSelectedEntriesChanged;
            ApplySelectionFromViewModel();
        }
    }

    private void OnViewModelSelectedEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ApplySelectionFromViewModel();
    }

    private void ApplySelectionFromViewModel()
    {
        if (_viewModel is null || _isUpdatingSelection)
        {
            return;
        }

        var selectedIds = _viewModel.SelectedEntries.Select(entry => entry.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _isUpdatingSelection = true;
        try
        {
            SyncSelection(TileListBox, selectedIds, _viewModel.SelectedEntry);
            SyncSelection(DetailsListView, selectedIds, _viewModel.SelectedEntry);
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private static void SyncSelection(ListBox listBox, HashSet<string> selectedIds, VirtualFileSystemEntry? primarySelection)
    {
        ArgumentNullException.ThrowIfNull(listBox);
        ArgumentNullException.ThrowIfNull(selectedIds);

        var matchingEntries = listBox.Items.OfType<VirtualFileSystemEntry>()
            .Where(entry => selectedIds.Contains(entry.Id))
            .ToList();
        var resolvedPrimarySelection = primarySelection is null
            ? matchingEntries.FirstOrDefault()
            : listBox.Items.OfType<VirtualFileSystemEntry>().FirstOrDefault(entry => string.Equals(entry.Id, primarySelection.Id, StringComparison.OrdinalIgnoreCase));

        if (listBox.SelectionMode == SelectionMode.Single)
        {
            listBox.SelectedItem = resolvedPrimarySelection;
            return;
        }

        listBox.SelectedItems.Clear();
        foreach (var entry in matchingEntries)
        {
            listBox.SelectedItems.Add(entry);
        }

        listBox.SelectedItem = resolvedPrimarySelection;
    }

    private static IReadOnlyList<VirtualFileSystemEntry> GetSelectedEntries(Selector selector)
    {
        return selector switch
        {
            ListView listView when listView.SelectionMode == SelectionMode.Single => GetSingleSelectedEntry(listView.SelectedItem),
            ListView listView => listView.SelectedItems.OfType<VirtualFileSystemEntry>().ToList(),
            ListBox listBox when listBox.SelectionMode == SelectionMode.Single => GetSingleSelectedEntry(listBox.SelectedItem),
            ListBox listBox => listBox.SelectedItems.OfType<VirtualFileSystemEntry>().ToList(),
            _ => Array.Empty<VirtualFileSystemEntry>()
        };
    }

    private static IReadOnlyList<VirtualFileSystemEntry> GetSingleSelectedEntry(object? selectedItem)
    {
        return selectedItem is VirtualFileSystemEntry entry
            ? new[] { entry }
            : Array.Empty<VirtualFileSystemEntry>();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null || _isUpdatingSelection)
        {
            return;
        }

        var selector = (Selector)sender;
        var selectedEntries = GetSelectedEntries(selector);

        _viewModel.UpdateSelection(selectedEntries, selector.SelectedItem as VirtualFileSystemEntry);
    }

    private void OnEntryDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as Selector)?.SelectedItem is VirtualFileSystemEntry entry)
        {
            EntryInvoked?.Invoke(this, new EntryInvokedEventArgs(entry));
        }
    }

    private void OnListPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if ((sender as Selector)?.SelectedItem is VirtualFileSystemEntry entry)
        {
            EntryInvoked?.Invoke(this, new EntryInvokedEventArgs(entry));
            e.Handled = true;
        }
    }
}
