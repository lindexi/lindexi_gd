// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.CursorButtons
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class CursorButtons : ICollection, IEnumerable
  {
    private IInkCursorButtons m_Buttons;

    internal CursorButtons(IInkCursorButtons buttons) => this.m_Buttons = buttons;

    private CursorButtons()
    {
    }

    public object SyncRoot => (object) this;

    public int Count => this.m_Buttons.Count;

    public bool IsSynchronized => true;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CopyTo(Array array, int index)
    {
      InkHelperMethods.ValidateCopyToArray(array, index, this.Count);
      int count = this.Count;
      for (int index1 = 0; index1 < count; ++index1)
      {
        array.SetValue((object) new CursorButton(this[index1].m_CursorButton), index);
        checked { ++index; }
      }
    }

    public CursorButton this[Guid id] => new CursorButton(this.m_Buttons.Item((object) id.ToString("B")));

    public CursorButton this[int index]
    {
      get
      {
        try
        {
          return new CursorButton(this.m_Buttons.Item((object) index));
        }
        catch (ArgumentException ex)
        {
          throw new ArgumentOutOfRangeException(nameof (index));
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public CursorButtons.CursorButtonsEnumerator GetEnumerator() => new CursorButtons.CursorButtonsEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) new CursorButtons.CursorButtonsEnumerator(this);

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class CursorButtonsEnumerator : IEnumerator
    {
      private int position = -1;
      private CursorButton[] c;

      public CursorButtonsEnumerator(CursorButtons cursorButtons)
      {
        this.c = cursorButtons != null ? new CursorButton[cursorButtons.Count] : throw new ArgumentNullException(nameof (cursorButtons), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        for (int index = 0; index < this.c.Length; ++index)
          this.c[index] = cursorButtons[index];
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

      public CursorButton Current => this.c[this.position];
    }
  }
}
