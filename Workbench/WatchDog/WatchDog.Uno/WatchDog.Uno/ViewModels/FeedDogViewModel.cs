using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WatchDog.Uno.ViewModels;

public class FeedDogViewModel : INotifyPropertyChanged
{
    public string? Id
    {
        get => _id;
        set
        {
            if (value == _id)
            {
                return;
            }

            _id = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (value == _status)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
        }
    }

    private string? _id;
    private string _name = "监控程序";
    private string _status = "正常";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
