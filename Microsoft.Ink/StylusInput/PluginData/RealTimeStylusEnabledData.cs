// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.RealTimeStylusEnabledData
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;

namespace Microsoft.StylusInput.PluginData
{
  public sealed class RealTimeStylusEnabledData : IEnumerable
  {
    private readonly int[] m_tcids;

    private RealTimeStylusEnabledData()
    {
    }

    internal RealTimeStylusEnabledData(int[] tcids) => this.m_tcids = tcids;

    public int this[int index]
    {
      get
      {
        if (index < 0 || index >= this.m_tcids.Length)
          throw new ArgumentOutOfRangeException(nameof (index));
        return this.m_tcids[index];
      }
    }

    public int Count => this.m_tcids.Length;

    public IEnumerator GetEnumerator() => this.m_tcids.GetEnumerator();
  }
}
