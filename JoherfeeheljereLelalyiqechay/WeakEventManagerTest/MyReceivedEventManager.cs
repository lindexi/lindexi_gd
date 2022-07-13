using System;
using System.Windows;

namespace WeakEventManagerTest;

class MyReceivedEventManager : WeakEventManager
{
    private MyReceivedEventManager()
    {
    }

    public static void AddHandler(MyListItem item, EventHandler<EventArgs> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        CurrentManager.ProtectedAddHandler(item, handler);
    }

    public static void RemoveHandler(MyListItem item, EventHandler<EventArgs> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        CurrentManager.ProtectedRemoveHandler(item, handler);
    }

    private static MyReceivedEventManager CurrentManager
    {
        get
        {
            var managerType = typeof(MyReceivedEventManager);
            var manager = (MyReceivedEventManager)GetCurrentManager(managerType);

            if (manager == null)
            {
                manager = new MyReceivedEventManager();
                SetCurrentManager(managerType, manager);
            }

            return manager;
        }
    }

    protected override ListenerList NewListenerList()
    {
        return new ListenerList<EventArgs>();
    }

    protected override void StartListening(object source)
    {
        ((MyListItem)source).MyEvent += OnSomeEvent;
    }

    protected override void StopListening(object source)
    {
        ((MyListItem)source).MyEvent -= OnSomeEvent;
    }

    private void OnSomeEvent(object? sender, EventArgs args)
    {
        DeliverEvent(sender, args);
    }
}