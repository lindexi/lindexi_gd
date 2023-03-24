// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.StylusDownData
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System;

namespace Microsoft.StylusInput.PluginData
{
  public sealed class StylusDownData : StylusDataBase
  {
    private StylusDownData()
    {
    }

    public StylusDownData(Stylus stylus, int packetPropertyCount, int[] packetData) => this.Initialize(stylus, packetPropertyCount, packetData);

    protected internal override void VerifyPacketData(int packetPropertyCount, int[] packetData)
    {
      base.VerifyPacketData(packetPropertyCount, packetData);
      if (packetData == null)
        throw new ArgumentNullException(nameof (packetData), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      if (packetData.Length == 0)
        throw new ArgumentOutOfRangeException(nameof (packetData), Helpers.SharedResources.Errors.GetString("ValueCannotBeEmptyArray"));
      if (packetData.Length > packetPropertyCount)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("MoreThanOnePacketData"));
    }
  }
}
