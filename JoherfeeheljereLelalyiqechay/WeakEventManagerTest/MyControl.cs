using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using WeakEventManagerTest.Annotations;

namespace WeakEventManagerTest;

class MyControl : Control, INotifyPropertyChanged
{
    private bool _handlerAdded;
    public MyControl(MyList myList)
    {
        MyEventManager.AddHandler(MyHandler);
        CollectionChangedEventManager.AddHandler(myList, Handler);
    }

    private void Handler(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (MyListItem item in e.NewItems)
            {
                MyReceivedEventManager.AddHandler(item, Handler);
            }
        }

        if (e.OldItems != null)
        {
            foreach (MyListItem item in e.OldItems)
            {
                MyReceivedEventManager.RemoveHandler(item, Handler);
            }
        }
    }

    private void Handler(object? sender, EventArgs e)
    {
        ;
    }

    private void MyHandler(object? sender, EventArgs e)
    {
        ;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void AddMyHandler()
    {
        if (_handlerAdded) return;

        _handlerAdded = true;

        PropertyChangedEventManager.AddHandler(this, (sender, args) =>
        {
            ;
        }, "fake");
    }
}