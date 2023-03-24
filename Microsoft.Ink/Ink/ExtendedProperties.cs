// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.ExtendedProperties
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class ExtendedProperties : ICollection, IEnumerable
  {
    private IInkExtendedProperties m_Properties;
    internal ExtendedProperty.PropertyType type;

    internal ExtendedProperties(IInkExtendedProperties properties, ExtendedProperty.PropertyType t)
    {
      this.m_Properties = properties;
      this.type = t;
    }

    private ExtendedProperties()
    {
    }

    public int Count => this.m_Properties.Count;

    public bool IsFixedSize => false;

    public bool IsReadOnly => false;

    public void Clear() => this.m_Properties.Clear();

    public bool IsSynchronized => false;

    public object SyncRoot => (object) this;

    public int IndexOf(ExtendedProperty ep)
    {
      if (ep == null)
        throw new ArgumentNullException(nameof (ep), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      if (ep.type != this.type)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("InvalidProperty"));
      return this.IndexOf(ep.Id);
    }

    public int IndexOf(Guid id)
    {
      for (int Identifier = 0; Identifier < this.m_Properties.Count; ++Identifier)
      {
        if (id == new Guid(this.m_Properties.Item((object) Identifier).Guid))
          return Identifier;
      }
      return -1;
    }

    public bool Contains(ExtendedProperty ep)
    {
      if (ep == null)
        throw new ArgumentNullException(nameof (ep), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      if (ep.type != this.type)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("InvalidProperty"));
      return this.Contains(ep.Id);
    }

    public bool Contains(Guid id)
    {
      int count = this.Count;
      for (int index = 0; index < count; ++index)
      {
        if (this[index].Id == id)
          return true;
      }
      return false;
    }

    public ExtendedProperty this[Guid id]
    {
      get
      {
        try
        {
          return new ExtendedProperty(this.m_Properties.Item((object) id.ToString("B")), this.type);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public ExtendedProperty this[int index]
    {
      get
      {
        try
        {
          return new ExtendedProperty(this.m_Properties.Item((object) index), this.type);
        }
        catch (ArgumentException ex)
        {
          throw new ArgumentOutOfRangeException(nameof (index));
        }
      }
    }

    public ExtendedProperty Add(Guid id, object data) => new ExtendedProperty(this.m_Properties.Add(id.ToString("B"), data), this.type);

    public void Remove(Guid id)
    {
      try
      {
        this.m_Properties.Remove((object) id.ToString("B"));
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void Remove(ExtendedProperty ep)
    {
      if (ep == null)
        throw new ArgumentNullException(nameof (ep), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        this.m_Properties.Remove((object) ep.m_Property);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void RemoveAt(int i)
    {
      try
      {
        this.m_Properties.Remove((object) i);
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentOutOfRangeException(nameof (i));
      }
    }

    public bool DoesPropertyExist(Guid id) => this.m_Properties.DoesPropertyExist(id.ToString("B"));

    public void CopyTo(Array array, int index)
    {
      InkHelperMethods.ValidateCopyToArray(array, index, this.Count);
      int count = this.Count;
      for (int index1 = 0; index1 < count; ++index1)
      {
        array.SetValue((object) new ExtendedProperty(this[index1].m_Property, this.type), index);
        checked { ++index; }
      }
    }

    public ExtendedProperties.ExtendedPropertiesEnumerator GetEnumerator() => new ExtendedProperties.ExtendedPropertiesEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) new ExtendedProperties.ExtendedPropertiesEnumerator(this);

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class ExtendedPropertiesEnumerator : IEnumerator
    {
      private int position = -1;
      private ExtendedProperty[] p;

      public ExtendedPropertiesEnumerator(ExtendedProperties p)
      {
        this.p = p != null ? new ExtendedProperty[p.Count] : throw new ArgumentNullException(nameof (p), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        for (int index = 0; index < this.p.Length; ++index)
          this.p[index] = p[index];
      }

      public bool MoveNext()
      {
        if (this.position >= this.p.Length - 1)
          return false;
        ++this.position;
        return true;
      }

      public void Reset() => this.position = -1;

      object IEnumerator.Current => (object) this.Current;

      public ExtendedProperty Current => this.p[this.position];
    }
  }
}
