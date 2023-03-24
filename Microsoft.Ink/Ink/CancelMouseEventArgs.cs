// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.CancelMouseEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Security.Permissions;
using System.Windows.Forms;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class CancelMouseEventArgs : MouseEventArgs
  {
    private bool m_bCancel;

    public bool Cancel
    {
      set => this.m_bCancel = value;
      get => this.m_bCancel;
    }

    public CancelMouseEventArgs(
      MouseButtons mb,
      int clicks,
      int x,
      int y,
      int delta,
      bool cancel)
      : base(mb, clicks, x, y, delta)
    {
      this.m_bCancel = cancel;
    }
  }
}
