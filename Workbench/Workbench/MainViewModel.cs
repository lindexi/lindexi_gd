using HandyControl.Collections;

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Workbench;

public class CommandModel
{
    public string Name { get; init; }
}

public class MainViewModel : INotifyPropertyChanged
{
    public MainViewModel()
    {
        for (int i = 0; i < 100; i++)
        {
            _commandList.Add(new CommandModel()
            {
                Name = i.ToString()
            });
        }

        FilterItems(null);
    }

    public ManualObservableCollection<CommandModel> Items { get; set; } = new();
    private string? _searchText;

    public string? SearchText
    {
        get => _searchText;
        set
        {
            SetField(ref _searchText, value);
            FilterItems(value);
        }
    }

    private void FilterItems(string? key)
    {
        Items.CanNotify = false;

        Items.Clear();

        foreach (var data in _commandList)
        {
            if (key == null || data.Name.ToLower().Contains(key.ToLower()))
            {
                Items.Add(data);
            }
        }

        Items.CanNotify = true;
    }

    private readonly List<CommandModel> _commandList = new List<CommandModel>();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}