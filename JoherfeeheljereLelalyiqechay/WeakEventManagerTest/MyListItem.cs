using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WeakEventManagerTest.Annotations;

namespace WeakEventManagerTest;

class MyListItem : INotifyPropertyChanged
{
    public event EventHandler<EventArgs> MyEvent; 

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}