// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.PenInputPanelVisibleChangedEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class PenInputPanelVisibleChangedEventArgs : EventArgs
  {
    private bool m_newVisibility;

    public bool NewVisibility => this.m_newVisibility;

    public PenInputPanelVisibleChangedEventArgs(bool newVisibility) => this.m_newVisibility = newVisibility;
  }
}
