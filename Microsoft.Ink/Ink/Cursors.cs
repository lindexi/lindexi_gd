// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Cursors
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
  public class Cursors : ICollection, IEnumerable
  {
    private IInkCursors m_Cursors;

    internal Cursors(IInkCursors cursors) => this.m_Cursors = cursors;

    private Cursors()
    {
    }

    public object SyncRoot => (object) this;

    public int Count => this.m_Cursors.Count;

    public bool IsSynchronized => true;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CopyTo(Array array, int index)
    {
      InkHelperMethods.ValidateCopyToArray(array, index, this.Count);
      int count = this.Count;
      for (int index1 = 0; index1 < count; ++index1)
      {
        array.SetValue((object) new Cursor(this[index1].m_Cursor), index);
        checked { ++index; }
      }
    }

    public Cursor this[int index]
    {
      get
      {
        try
        {
          return new Cursor(this.m_Cursors.Item(index));
        }
        catch (ArgumentException ex)
        {
          throw new ArgumentOutOfRangeException(nameof (index));
        }
      }
    }

    public Cursors.CursorsEnumerator GetEnumerator() => new Cursors.CursorsEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) new Cursors.CursorsEnumerator(this);

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class CursorsEnumerator : IEnumerator
    {
      private int position = -1;
      private Cursor[] c;

      public CursorsEnumerator(Cursors c)
      {
        this.c = c != null ? new Cursor[c.Count] : throw new ArgumentNullException(nameof (c), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        for (int index = 0; index < this.c.Length; ++index)
          this.c[index] = c[index];
      }

      public bool MoveNext()
      {
        if (this.position >= this.c.Length - 1)
          return false;
        ++this.position;
        return true;
      }

      public void Reset() => this.position = -1;

      object IEnumerator.Current => (object) this.Current;

      public Cursor Current => this.c[this.position];
    }
  }
}
