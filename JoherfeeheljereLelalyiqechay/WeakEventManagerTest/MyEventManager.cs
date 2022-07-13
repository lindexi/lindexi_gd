using System;
using System.Collections;
using System.Reflection;
using System.Windows;

namespace WeakEventManagerTest;

class MyEventManager : WeakEventManager
{
    private PropertyInfo tableProp;
    private FieldInfo? dataTableField;
    private FieldInfo? versionField;
    private Hashtable hashTable;
    private Type eventKeyType;
    private FieldInfo managerField;
    FieldInfo sourceField;

    private MyEventManager()
    {
        tableProp = typeof(WeakEventManager).GetProperty("Table", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var table = tableProp.GetValue(this);
        dataTableField = table.GetType().GetField("_dataTable", BindingFlags.Instance | BindingFlags.NonPublic)!;

        versionField = typeof(Hashtable).GetField("_version", BindingFlags.NonPublic | BindingFlags.Instance)!;
        hashTable = (Hashtable)dataTableField.GetValue(table)!;
    }

    public static void AddHandler(EventHandler<EventArgs> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        CurrentManager.ProtectedAddHandler(MyEventSource.Instance, handler);
    }

    public static void RemoveHandler(EventHandler<EventArgs> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        CurrentManager.ProtectedRemoveHandler(MyEventSource.Instance, handler);
    }

    private static MyEventManager CurrentManager
    {
        get
        {
            var managerType = typeof(MyEventManager);
            var manager = (MyEventManager)GetCurrentManager(managerType);

            if (manager == null)
            {
                manager = new MyEventManager();
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
        ((MyEventSource)source).MyEvent += OnSomeEvent;
    }

    protected override void StopListening(object source)
    {
        ((MyEventSource)source).MyEvent -= OnSomeEvent;
    }

    private void OnSomeEvent(object? sender, EventArgs args)
    {
        DeliverEvent(sender, args);
    }

    //protected override bool Purge(object source, object data, bool purgeAll)
    //{
    //    int version0 = (int)versionField.GetValue(hashTable)!;

    //    if (eventKeyType == null)
    //    {
    //        foreach (var key in hashTable.Keys)
    //        {
    //            eventKeyType = key.GetType();
    //            managerField = eventKeyType.GetField("_manager", BindingFlags.Instance | BindingFlags.NonPublic)!;
    //            sourceField = eventKeyType.GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic)!;
    //        }
    //    }

    //    var keys = new List<(WeakEventManager, object?)>();
    //    foreach (var key in hashTable.Keys)
    //    {
    //        var manager = (WeakEventManager)managerField.GetValue(key)!;
    //        var source1 = (WeakReference)sourceField.GetValue(key)!;
    //        keys.Add((manager, source1.Target));
    //    }

    //    var result = base.Purge(source, data, purgeAll);
    //    if (result)
    //    {
    //        ;
    //    }

    //    int version1 = (int)versionField.GetValue(hashTable)!;
    //    if (version0 != version1)
    //    {
    //        var keys2 = new List<(WeakEventManager, object)>();
    //        foreach (var key in hashTable.Keys)
    //        {
    //            var manager = (WeakEventManager)managerField.GetValue(key)!;
    //            var source1 = (WeakReference)sourceField.GetValue(key)!;
    //            keys2.Add((manager, source1.Target));
    //        }

    //        ;
    //    }

    //    return result;
    //}
}