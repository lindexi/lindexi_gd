// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DivisionUnit
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class DivisionUnit
  {
    internal Strokes m_Strokes;
    internal string m_UnitString;
    internal float m_Angle;
    internal InkDivisionType m_divisionType;
    internal Point m_RotationCenter;

    internal DivisionUnit(Strokes pStrokes, InkDivisionType divisionType)
    {
      this.m_divisionType = divisionType;
      this.m_Strokes = pStrokes;
    }

    internal DivisionUnit(Strokes pStrokes, InkDivisionType divisionType, string UnitString)
    {
      this.m_divisionType = divisionType;
      this.m_Strokes = pStrokes;
      this.m_UnitString = UnitString;
    }

    internal DivisionUnit(
      Strokes pStrokes,
      InkDivisionType divisionType,
      string UnitString,
      Point RotationCenter,
      float angle)
    {
      this.m_divisionType = divisionType;
      this.m_Strokes = pStrokes;
      this.m_UnitString = UnitString;
      this.m_Angle = angle;
      this.m_RotationCenter = RotationCenter;
    }

    private DivisionUnit()
    {
    }

    public override string ToString() => this.m_UnitString;

    public InkDivisionType DivisionType => this.m_divisionType;

    public Matrix Transform
    {
      get
      {
        Matrix transform = new Matrix();
        transform.RotateAt(this.m_Angle, (PointF) this.m_RotationCenter);
        return transform;
      }
    }

    public string RecognitionString => this.m_UnitString == null ? (string) null : this.m_UnitString;

    public Strokes Strokes => this.m_Strokes;
  }
}
