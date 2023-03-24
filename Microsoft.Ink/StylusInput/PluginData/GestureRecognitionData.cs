// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.GestureRecognitionData
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System;
using System.Collections;

namespace Microsoft.StylusInput.PluginData
{
  public sealed class GestureRecognitionData : IEnumerable
  {
    private GestureAlternate[] m_gestureAlternates;

    internal GestureRecognitionData(GestureAlternate[] gestureAlternates) => this.m_gestureAlternates = gestureAlternates;

    public GestureAlternate this[int index]
    {
      get
      {
        if (this.m_gestureAlternates.Length == 0 || index >= this.m_gestureAlternates.Length || index < 0)
          throw new ArgumentOutOfRangeException(nameof (index), Helpers.SharedResources.Errors.GetString("IndexOutOfRange"));
        return this.m_gestureAlternates[index];
      }
    }

    public int Count => this.m_gestureAlternates.Length;

    public IEnumerator GetEnumerator() => this.m_gestureAlternates.GetEnumerator();
  }
}
