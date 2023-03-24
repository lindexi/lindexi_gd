// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.CursorButton
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class CursorButton
  {
    internal IInkCursorButton m_CursorButton;

    internal CursorButton(IInkCursorButton button) => this.m_CursorButton = button;

    private CursorButton()
    {
    }

    public string Name => this.m_CursorButton.Name;

    public override string ToString() => this.Name;

    public Guid Id => new Guid(this.m_CursorButton.Id);

    public CursorButtonState State => (CursorButtonState) this.m_CursorButton.State;
  }
}
