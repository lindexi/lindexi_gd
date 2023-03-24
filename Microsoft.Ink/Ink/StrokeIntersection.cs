// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.StrokeIntersection
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

namespace Microsoft.Ink
{
  public struct StrokeIntersection
  {
    private float m_BeginIndex;
    private float m_EndIndex;

    public StrokeIntersection(float beginIndex, float endIndex)
    {
      this.m_BeginIndex = beginIndex;
      this.m_EndIndex = endIndex;
    }

    public float BeginIndex
    {
      get => this.m_BeginIndex;
      set => this.m_BeginIndex = value;
    }

    public float EndIndex
    {
      get => this.m_EndIndex;
      set => this.m_EndIndex = value;
    }
  }
}
