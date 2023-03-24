// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Line
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Drawing;

namespace Microsoft.Ink
{
  public struct Line
  {
    private Point m_BeginPoint;
    private Point m_EndPoint;

    public Line(Point beginPoint, Point endPoint)
    {
      this.m_BeginPoint = beginPoint;
      this.m_EndPoint = endPoint;
    }

    internal Line(int[] xyarray)
    {
      this.m_BeginPoint = new Point(xyarray[0], xyarray[1]);
      this.m_EndPoint = new Point(xyarray[2], xyarray[3]);
    }

    public Point BeginPoint
    {
      get => this.m_BeginPoint;
      set => this.m_BeginPoint = value;
    }

    public Point EndPoint
    {
      get => this.m_EndPoint;
      set => this.m_EndPoint = value;
    }
  }
}
