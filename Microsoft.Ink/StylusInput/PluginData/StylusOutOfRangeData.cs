// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.StylusOutOfRangeData
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System;

namespace Microsoft.StylusInput.PluginData
{
  public sealed class StylusOutOfRangeData
  {
    private Stylus m_Stylus;

    private StylusOutOfRangeData()
    {
    }

    public StylusOutOfRangeData(Stylus stylus) => this.m_Stylus = stylus != null ? stylus : throw new ArgumentNullException(nameof (stylus), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));

    public Stylus Stylus => this.m_Stylus;
  }
}
