// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.RecognitionAlternates
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
  public class RecognitionAlternates : ICollection, IEnumerable
  {
    private IInkRecognitionAlternates m_Alternates;
    private Strokes m_Strokes;

    internal RecognitionAlternates(IInkRecognitionAlternates alternates) => this.m_Alternates = alternates;

    private RecognitionAlternates()
    {
    }

    public Strokes Strokes
    {
      get
      {
        if (this.m_Strokes == null)
          this.m_Strokes = new Strokes(this.m_Alternates.Strokes);
        return this.m_Strokes;
      }
    }

    public object SyncRoot => (object) this;

    public int Count => this.m_Alternates.Count;

    public bool IsSynchronized => true;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CopyTo(Array array, int index)
    {
      InkHelperMethods.ValidateCopyToArray(array, index, this.Count);
      int count = this.Count;
      for (int index1 = 0; index1 < count; ++index1)
      {
        array.SetValue((object) new RecognitionAlternate(this[index1].m_Alternate), index);
        checked { ++index; }
      }
    }

    public RecognitionAlternate this[int index]
    {
      get
      {
        try
        {
          return new RecognitionAlternate(this.m_Alternates.Item(index));
        }
        catch (ArgumentException ex)
        {
          throw new ArgumentOutOfRangeException(nameof (index));
        }
      }
    }

    public RecognitionAlternates.RecognitionAlternatesEnumerator GetEnumerator() => new RecognitionAlternates.RecognitionAlternatesEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) new RecognitionAlternates.RecognitionAlternatesEnumerator(this);

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class RecognitionAlternatesEnumerator : IEnumerator
    {
      private int position = -1;
      private RecognitionAlternate[] a;

      public RecognitionAlternatesEnumerator(RecognitionAlternates a)
      {
        this.a = a != null ? new RecognitionAlternate[a.Count] : throw new ArgumentNullException(nameof (a), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        for (int index = 0; index < this.a.Length; ++index)
          this.a[index] = a[index];
      }

      public bool MoveNext()
      {
        if (this.position >= this.a.Length - 1)
          return false;
        ++this.position;
        return true;
      }

      public void Reset() => this.position = -1;

      object IEnumerator.Current => (object) this.Current;

      public RecognitionAlternate Current => this.a[this.position];
    }
  }
}
