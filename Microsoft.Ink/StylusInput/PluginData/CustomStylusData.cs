// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.CustomStylusData
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.StylusInput.PluginData
{
  public sealed class CustomStylusData
  {
    private Guid m_CustomDataId;
    private object m_Data;

    private CustomStylusData()
    {
    }

    internal CustomStylusData(Guid customDataId, object data)
    {
      this.m_CustomDataId = customDataId;
      this.m_Data = data;
    }

    public Guid CustomDataId => this.m_CustomDataId;

    public object Data => this.m_Data;
  }
}
