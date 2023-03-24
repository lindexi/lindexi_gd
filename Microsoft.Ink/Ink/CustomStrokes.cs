// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.CustomStrokes
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class CustomStrokes : ICollection, IEnumerable
  {
    internal IInkCustomStrokes m_CustomStrokes;

    internal CustomStrokes(IInkCustomStrokes customStrokes) => this.m_CustomStrokes = customStrokes;

    private CustomStrokes()
    {
    }

    public int Count => this.m_CustomStrokes.Count;

    public bool IsFixedSize => false;

    public bool IsSynchronized => false;

    public object SyncRoot => (object) this;

    public bool IsReadOnly => false;

    public void Clear() => this.m_CustomStrokes.Clear();

    public Strokes this[int index]
    {
      get
      {
        try
        {
          return new Strokes(this.m_CustomStrokes.Item((object) index));
        }
        catch (ArgumentException ex)
        {
          throw new ArgumentOutOfRangeException(nameof (index));
        }
      }
    }

    public Strokes this[string name] => name != null ? new Strokes(this.m_CustomStrokes.Item((object) name)) : throw new ArgumentNullException(nameof (name), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));

    public void Add(string name, Strokes strokes)
    {
      if (name == null)
        throw new ArgumentNullException(nameof (name), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      if (strokes == null)
        throw new ArgumentNullException(nameof (strokes), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      this.m_CustomStrokes.Add(name, strokes.m_Strokes);
    }

    public void Remove(string s)
    {
      if (s == null)
        throw new ArgumentNullException(nameof (s), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      this.m_CustomStrokes.Remove((object) s);
    }

    public void RemoveAt(int i)
    {
      try
      {
        this.m_CustomStrokes.Remove((object) i);
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentOutOfRangeException(nameof (i));
      }
    }

    public void CopyTo(Array array, int index)
    {
      InkHelperMethods.ValidateCopyToArray(array, index, this.Count);
      int count = this.Count;
      for (int index1 = 0; index1 < count; ++index1)
      {
        array.SetValue((object) new Strokes(this[index1].m_Strokes), index);
        checked { ++index; }
      }
    }

    public CustomStrokes.CustomStrokesEnumerator GetEnumerator() => new CustomStrokes.CustomStrokesEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) new CustomStrokes.CustomStrokesEnumerator(this);

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class CustomStrokesEnumerator : IEnumerator
    {
      private int position = -1;
      private Strokes[] s;

      public CustomStrokesEnumerator(CustomStrokes cs)
      {
        this.s = cs != null ? new Strokes[cs.Count] : throw new ArgumentNullException(nameof (cs), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        for (int index = 0; index < this.s.Length; ++index)
          this.s[index] = cs[index];
      }

      public bool MoveNext()
      {
        if (this.position >= this.s.Length - 1)
          return false;
        ++this.position;
        return true;
      }

      public void Reset() => this.position = -1;

      object IEnumerator.Current => (object) this.Current;

      public Strokes Current => this.s[this.position];
    }
  }
}
