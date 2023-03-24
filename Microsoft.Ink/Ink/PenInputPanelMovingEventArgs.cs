// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.PenInputPanelMovingEventArgs
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class PenInputPanelMovingEventArgs : EventArgs
  {
    private int m_left;
    private int m_top;

    public int Left
    {
      get => this.m_left;
      set => this.m_left = value;
    }

    public int Top
    {
      get => this.m_top;
      set => this.m_top = value;
    }

    public PenInputPanelMovingEventArgs(int left, int top)
    {
      this.m_left = left;
      this.m_top = top;
    }
  }
}
