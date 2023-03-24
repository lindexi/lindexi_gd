// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.StylusDataBase
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System;
using System.Collections;
using System.Security.Permissions;

namespace Microsoft.StylusInput.PluginData
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public abstract class StylusDataBase : IEnumerable
  {
    private Stylus m_Stylus;
    private int m_PacketPropertyCount;
    private int[] m_PacketData;
    private bool m_Modified;
    internal StylusInfo m_stylusInfo;
    internal uint m_pktCount;

    internal StylusDataBase()
    {
    }

    internal void Initialize(Stylus stylus, int packetPropertyCount, int[] packetData)
    {
      if (stylus == null)
        throw new ArgumentNullException(nameof (stylus), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      if (packetPropertyCount <= 0)
        throw new ArgumentOutOfRangeException(nameof (packetPropertyCount), Helpers.SharedResources.Errors.GetString("ValueCannotBeSmallerThanZero"));
      this.VerifyPacketData(packetPropertyCount, packetData);
      this.m_Stylus = stylus;
      this.m_PacketPropertyCount = packetPropertyCount;
      this.m_PacketData = packetData;
      this.m_Modified = false;
    }

    public virtual Stylus Stylus => this.m_Stylus;

    public virtual int PacketPropertyCount => this.m_PacketPropertyCount;

    public virtual int[] GetData() => this.m_PacketData;

    public virtual int Count => this.m_PacketData != null ? this.m_PacketData.Length : 0;

    public virtual void SetData(int[] value)
    {
      this.VerifyPacketData(this.PacketPropertyCount, value);
      this.m_PacketData = value;
      this.m_Modified = true;
    }

    public virtual int this[int index]
    {
      get
      {
        if (this.m_PacketData == null)
          throw new InvalidOperationException();
        if (index < 0 || index >= this.m_PacketData.Length)
          throw new ArgumentOutOfRangeException(nameof (index));
        return this.m_PacketData[index];
      }
      set
      {
        if (this.m_PacketData == null)
          throw new InvalidOperationException();
        if (index < 0 || index >= this.m_PacketData.Length)
          throw new ArgumentOutOfRangeException(nameof (index));
        this.m_PacketData[index] = value;
        this.m_Modified = true;
      }
    }

    internal bool PacketDataModified => this.m_Modified;

    protected internal virtual void VerifyPacketData(int packetPropertyCount, int[] packetData)
    {
      if (packetData == null || packetData.Length == 0)
        return;
      if (packetPropertyCount > packetData.Length)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("InvalidPacketDataLength"));
      if (packetData.Length % packetPropertyCount != 0)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("InvalidPacketDataLength"));
    }

    public IEnumerator GetEnumerator()
    {
      IEnumerator enumerator = (IEnumerator) null;
      if (this.m_PacketData != null)
        enumerator = this.m_PacketData.GetEnumerator();
      return enumerator;
    }
  }
}
