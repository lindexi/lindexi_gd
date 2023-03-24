// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.StylusButtonDataBase
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Security.Permissions;

namespace Microsoft.StylusInput.PluginData
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public abstract class StylusButtonDataBase
  {
    private Stylus m_stylus;
    private int m_buttonIndex;

    private StylusButtonDataBase()
    {
    }

    internal StylusButtonDataBase(Stylus stylus, int buttonIndex)
    {
      if (stylus == null)
        throw new ArgumentNullException(nameof (stylus));
      if (buttonIndex < 0)
        throw new ArgumentOutOfRangeException(nameof (buttonIndex));
      this.m_stylus = stylus;
      this.m_buttonIndex = buttonIndex;
    }

    public Stylus Stylus => this.m_stylus;

    public int ButtonIndex => this.m_buttonIndex;
  }
}
