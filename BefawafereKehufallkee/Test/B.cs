using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BefawafereKehufallkee;

class B : INotifyPropertyChanged
{

    public string BProperty1
    {
        get => _bProperty1;
        set
        {
            if (value == _bProperty1) return;
            _bProperty1 = value;
            OnPropertyChanged();
        }
    }
    private string _bProperty1 = string.Empty;

    public int BProperty2
    {
        get => _bProperty2;
        set
        {
            if (value == _bProperty2) return;
            _bProperty2 = value;
            OnPropertyChanged();
        }
    }
    private int _bProperty2;

    public string BPropertyWithoutSet { get; }

    public string? BPropertyWithoutGet
    {
        set
        {
            if (value == _bPropertyWithoutGet) return;
            _bPropertyWithoutGet = value;
            OnPropertyChanged();
        }
    }
    private string? _bPropertyWithoutGet;

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