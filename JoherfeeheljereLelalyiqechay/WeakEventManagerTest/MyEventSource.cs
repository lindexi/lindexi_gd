using System;

namespace WeakEventManagerTest;

class MyEventSource
{
    public static MyEventSource Instance { get; } = new MyEventSource();

    public event EventHandler<EventArgs> MyEvent;

    public void SendEvent()
    {
        MyEvent?.Invoke(this, EventArgs.Empty);
    }
}