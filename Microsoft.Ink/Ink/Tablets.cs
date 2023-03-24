// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Tablets
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class Tablets : ICollection, IEnumerable
  {
    private IInkTablets m_Tablets;

    public Tablets() => this.m_Tablets = (IInkTablets) new InkTabletsClass();

    public object SyncRoot => (object) this;

    public int Count => this.m_Tablets.Count;

    public bool IsSynchronized => true;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CopyTo(Array array, int index)
    {
      InkHelperMethods.ValidateCopyToArray(array, index, this.Count);
      int count = this.Count;
      for (int index1 = 0; index1 < count; ++index1)
      {
        array.SetValue((object) new Tablet(this[index1].m_Tablet), index);
        checked { ++index; }
      }
    }

    public Tablet this[int index]
    {
      get
      {
        try
        {
          return new Tablet(this.m_Tablets.Item(index));
        }
        catch (ArgumentException ex)
        {
          throw new ArgumentOutOfRangeException(nameof (index));
        }
      }
    }

    public Tablet DefaultTablet => new Tablet(this.m_Tablets.DefaultTablet);

    public bool IsPacketPropertySupported(Guid g) => this.m_Tablets.IsPacketPropertySupported(g.ToString("B"));

    public Tablets.TabletsEnumerator GetEnumerator() => new Tablets.TabletsEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) new Tablets.TabletsEnumerator(this);

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class TabletsEnumerator : IEnumerator
    {
      private int position = -1;
      private Tablet[] t;

      public TabletsEnumerator(Tablets t)
      {
        this.t = t != null ? new Tablet[t.Count] : throw new ArgumentNullException(nameof (t), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        for (int index = 0; index < t.Count; ++index)
          this.t[index] = t[index];
      }

      public bool MoveNext()
      {
        if (this.position >= this.t.Length - 1)
          return false;
        ++this.position;
        return true;
      }

      public void Reset() => this.position = -1;

      object IEnumerator.Current => (object) this.Current;

      public Tablet Current => this.t[this.position];
    }
  }
}
