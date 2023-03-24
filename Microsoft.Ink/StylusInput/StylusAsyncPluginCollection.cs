// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.StylusAsyncPluginCollection
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;

namespace Microsoft.StylusInput
{
  public sealed class StylusAsyncPluginCollection : ICollection, IEnumerable
  {
    private StylusPluginCollectionBase m_List;
    private RealTimeStylus m_rts;

    internal StylusAsyncPluginCollection(
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

    public void Add(IStylusAsyncPlugin item)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsAsyncObjLock);
      try
      {
        this.m_List.CollectionList.Add((object) item);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsAsyncObjLock);
      }
    }

    public void Insert(int index, IStylusAsyncPlugin item)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsAsyncObjLock);
      try
      {
        this.m_List.CollectionList.Insert(index, (object) item);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsAsyncObjLock);
      }
    }

    public void Remove(IStylusAsyncPlugin item)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsAsyncObjLock);
      try
      {
        this.m_List.CollectionList.Remove((object) item);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsAsyncObjLock);
      }
    }

    public void RemoveAt(int index)
    {
      this.m_rts.AcquireLock(RtsLockType.RtsAsyncObjLock);
      try
      {
        this.m_List.CollectionList.RemoveAt(index);
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsAsyncObjLock);
      }
    }

    public void Clear()
    {
      this.m_rts.AcquireLock(RtsLockType.RtsAsyncObjLock);
      try
      {
        this.m_List.CollectionList.Clear();
      }
      finally
      {
        this.m_rts.ReleaseLock(RtsLockType.RtsAsyncObjLock);
      }
    }

    public IStylusAsyncPlugin this[int index]
    {
      set
      {
        this.m_rts.AcquireLock(RtsLockType.RtsAsyncObjLock);
        try
        {
          this.m_List.CollectionList[index] = (object) value;
        }
        finally
        {
          this.m_rts.ReleaseLock(RtsLockType.RtsAsyncObjLock);
        }
      }
      get
      {
        this.m_rts.AcquireLock(RtsLockType.RtsObjLock);
        try
        {
          return (IStylusAsyncPlugin) this.m_List.CollectionList[index];
        }
        finally
        {
          this.m_rts.ReleaseLock(RtsLockType.RtsObjLock);
        }
      }
    }

    public int IndexOf(IStylusAsyncPlugin plugin)
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

    public bool Contains(IStylusAsyncPlugin plugin)
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

    void ICollection.CopyTo(Array array, int index) => this.CopyTo((IStylusAsyncPlugin[]) array, index);

    public void CopyTo(IStylusAsyncPlugin[] array, int index)
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

    IEnumerator IEnumerable.GetEnumerator()
    {
      this.m_rts.AcquireLock(RtsLockType.RtsObjLock);
      try
      {
        IStylusAsyncPlugin[] array = new IStylusAsyncPlugin[this.m_List.CollectionList.Count];
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
