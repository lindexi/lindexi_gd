// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.StylusSyncPluginCollection
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;

namespace Microsoft.StylusInput
{
  public sealed class StylusSyncPluginCollection : ICollection, IEnumerable
  {
    private StylusPluginCollectionBase m_List;
    private RealTimeStylus m_rts;

    internal StylusSyncPluginCollection(
      RealTimeStylus rts,
      Validate validate,
      ItemInsert onAdd,
      ItemInserted onAddComplete,
      ItemRemove onRemove,
      ListClear onClear,
      ItemSet onSet)
    {
      this.m_List = new StylusPluginCollectionBase(validate, onAdd, onAddComplete, onRemove, onClear, onSet);
      this.m_rts = rts;
    }

    public void Add(IStylusSyncPlugin item)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsSyncObjLock);
      try
      {
        this.m_List.CollectionList.Add((object) item);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsSyncObjLock);
      }
    }

    public void Insert(int index, IStylusSyncPlugin item)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsSyncObjLock);
      try
      {
        this.m_List.CollectionList.Insert(index, (object) item);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsSyncObjLock);
      }
    }

    public void Remove(IStylusSyncPlugin item)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsSyncObjLock);
      try
      {
        this.m_List.CollectionList.Remove((object) item);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsSyncObjLock);
      }
    }

    public void RemoveAt(int index)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsSyncObjLock);
      try
      {
        this.m_List.CollectionList.RemoveAt(index);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsSyncObjLock);
      }
    }

    public void Clear()
    {
      this.m_rts.AcquireLock(RtsLockType.RtsSyncObjLock);
      try
      {
        this.m_List.CollectionList.Clear();
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsSyncObjLock);
      }
    }

    public IStylusSyncPlugin this[int index]
    {
      set
      {
        this.m_rts.AcquireLock(RtsLockType.RtsSyncObjLock);
        try
        {
          this.m_List.CollectionList[index] = (object) value;
        }
        finally
        {
          this.m_rts.ReleaseLock(RtsLockType.RtsSyncObjLock);
        }
      }
      get
      {
        this.m_rts.AcquireLock(RtsLockType.RtsObjLock);
        try
        {
          return (IStylusSyncPlugin) this.m_List.CollectionList[index];
        }
        finally
        {
          this.m_rts.ReleaseLock(RtsLockType.RtsObjLock);
        }
      }
    }

    public int IndexOf(IStylusSyncPlugin plugin)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsObjLock);
      try
      {
        return this.m_List.CollectionList.IndexOf((object) plugin);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsObjLock);
      }
    }

    public bool Contains(IStylusSyncPlugin plugin)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsObjLock);
      try
      {
        return this.m_List.CollectionList.Contains((object) plugin);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsObjLock);
      }
    }

    public void CopyTo(IStylusSyncPlugin[] array, int index)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsObjLock);
      try
      {
        this.m_List.CollectionList.CopyTo((Array) array, index);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsObjLock);
      }
    }

    public int Count
    {
      get
      {
        this.m_rts.AcquireLock(RtsLockType.RtsObjLock);
        try
        {
          return this.m_List.CollectionList.Count;
        }
        finally
        {
          this.m_rts.ReleaseLock(RtsLockType.RtsObjLock);
        }
      }
    }

    public bool IsSynchronized => true;

    public bool IsReadOnly => false;

    public object SyncRoot => (object) null;

    void ICollection.CopyTo(Array array, int index) => this.CopyTo((IStylusSyncPlugin[]) array, index);

    IEnumerator IEnumerable.GetEnumerator()
    {
      this.m_rts.AcquireLock(RtsLockType.RtsObjLock);
      try
      {
        IStylusSyncPlugin[] array = new IStylusSyncPlugin[this.m_List.CollectionList.Count];
        if (this.m_List.CollectionList.Count != 0)
          this.CopyTo(array, 0);
        return array.GetEnumerator();
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsObjLock);
      }
    }
  }
}
