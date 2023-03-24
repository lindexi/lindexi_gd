// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Recognizers
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class Recognizers : ICollection, IEnumerable
  {
    private IInkRecognizers m_Recognizers;

    public Recognizers() => this.m_Recognizers = (IInkRecognizers) new InkRecognizersClass();

    public object SyncRoot => (object) this;

    public int Count => this.m_Recognizers.Count;

    public bool IsSynchronized => true;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CopyTo(Array array, int index)
    {
      InkHelperMethods.ValidateCopyToArray(array, index, this.Count);
      int count = this.Count;
      for (int index1 = 0; index1 < count; ++index1)
      {
        array.SetValue((object) new Recognizer(this[index1].m_Recognizer), index);
        checked { ++index; }
      }
    }

    public Recognizer this[int index]
    {
      get
      {
        try
        {
          return new Recognizer(this.m_Recognizers.Item(index));
        }
        catch (ArgumentException ex)
        {
          throw new ArgumentOutOfRangeException(nameof (index));
        }
      }
    }

    public Recognizer GetDefaultRecognizer()
    {
      try
      {
        return this.GetDefaultRecognizer(Thread.CurrentThread.CurrentCulture.LCID);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public Recognizer GetDefaultRecognizer(int lcid)
    {
      try
      {
        return new Recognizer(this.m_Recognizers.GetDefaultRecognizer(lcid));
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public Recognizers.RecognizersEnumerator GetEnumerator() => new Recognizers.RecognizersEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) new Recognizers.RecognizersEnumerator(this);

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class RecognizersEnumerator : IEnumerator
    {
      private int position = -1;
      private Recognizer[] r;

      public RecognizersEnumerator(Recognizers r)
      {
        this.r = r != null ? new Recognizer[r.Count] : throw new ArgumentNullException(nameof (r), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        for (int index = 0; index < this.r.Length; ++index)
          this.r[index] = r[index];
      }

      public bool MoveNext()
      {
        if (this.position >= this.r.Length - 1)
          return false;
        ++this.position;
        return true;
      }

      public void Reset() => this.position = -1;

      object IEnumerator.Current => (object) this.Current;

      public Recognizer Current => this.r[this.position];
    }
  }
}
