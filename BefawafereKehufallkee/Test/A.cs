using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BefawafereKehufallkee;

class A : INotifyPropertyChanged
{
    public string AProperty1
    {
        set
        {
            if (value == _aProperty1) return;
            _aProperty1 = value;
            OnPropertyChanged();
        }
        get => _aProperty1;
    }

    private string _aProperty1 = string.Empty;

    public int AProperty2
    {
        set
        {
            if (value == _aProperty2) return;
            _aProperty2 = value;
            OnPropertyChanged();
        }
        get => _aProperty2;
    }
    private int _aProperty2;

    public string? APropertyWithoutSet { get; }

    public string? APropertyWithoutGet
    {
        set
        {
            if (value == _aPropertyWithoutGet) return;
            _aPropertyWithoutGet = value;
            OnPropertyChanged();
        }
    }
    private string? _aPropertyWithoutGet;

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