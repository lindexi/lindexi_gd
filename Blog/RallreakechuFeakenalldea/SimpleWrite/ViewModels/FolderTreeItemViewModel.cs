using System.Collections.ObjectModel;

using SimpleWrite.Business.FolderExplorers;

namespace SimpleWrite.ViewModels;

public class FolderTreeItemViewModel : ViewModelBase
{
    internal FolderTreeItemViewModel(FolderTreeEntry folderTreeEntry)
    {
        Name = folderTreeEntry.Name;
        FullPath = folderTreeEntry.FullPath;
        IsDirectory = folderTreeEntry.IsDirectory;

        foreach (var child in folderTreeEntry.Children)
        {
            Children.Add(new FolderTreeItemViewModel(child));
        }
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    public string IconText => IsDirectory ? "📁" : "📄";

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }

    private bool _isExpanded;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    private bool _isSelected;

    public ObservableCollection<FolderTreeItemViewModel> Children { get; } = [];
}
