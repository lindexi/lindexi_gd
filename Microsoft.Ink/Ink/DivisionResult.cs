// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DivisionResult
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class DivisionResult
  {
    private Strokes m_Strokes;
    private int[] m_aWordStrokeIDs;
    private int[] m_aLineStrokeIDs;
    private int[] m_aParagraphStrokeIDs;
    private int[] m_aDrawingStrokeIDs;
    private string[] m_astrWords;
    private string[] m_astrLines;
    private string[] m_astrParagraphs;
    private int[] m_aWordRotationCenterX;
    private int[] m_aWordRotationCenterY;
    private int[] m_aLineRotationCenterX;
    private int[] m_aLineRotationCenterY;
    private float[] m_aWordAngle;
    private float[] m_aLineAngle;

    internal DivisionResult(
      Strokes pStrokes,
      int[] aWordStrokeIDs,
      int[] aLineStrokeIDs,
      int[] aParagraphStrokeIDs,
      int[] aDrawingStrokeIDs,
      string[] astrWords,
      string[] astrLines,
      string[] astrParagraphs,
      int[] aWordRotationCenterX,
      int[] aWordRotationCenterY,
      float[] aWordAngle,
      int[] aLineRotationCenterX,
      int[] aLineRotationCenterY,
      float[] aLineAngle)
    {
      this.m_Strokes = pStrokes;
      this.m_aWordStrokeIDs = aWordStrokeIDs;
      this.m_aLineStrokeIDs = aLineStrokeIDs;
      this.m_aParagraphStrokeIDs = aParagraphStrokeIDs;
      this.m_aDrawingStrokeIDs = aDrawingStrokeIDs;
      this.m_astrWords = astrWords;
      this.m_astrLines = astrLines;
      this.m_astrParagraphs = astrParagraphs;
      this.m_aWordRotationCenterX = aWordRotationCenterX;
      this.m_aWordRotationCenterY = aWordRotationCenterY;
      this.m_aWordAngle = aWordAngle;
      this.m_aLineRotationCenterX = aLineRotationCenterX;
      this.m_aLineRotationCenterY = aLineRotationCenterY;
      this.m_aLineAngle = aLineAngle;
    }

    private DivisionResult()
    {
    }

    public Strokes Strokes => this.m_Strokes;

    public DivisionUnits ResultByType(InkDivisionType divisionType)
    {
      DivisionUnits divisionUnits = (DivisionUnits) null;
      switch (divisionType)
      {
        case InkDivisionType.Segment:
          divisionUnits = new DivisionUnits(this.m_Strokes.Ink, divisionType, this.m_aWordStrokeIDs, this.m_astrWords, this.m_aWordRotationCenterX, this.m_aWordRotationCenterY, this.m_aWordAngle);
          break;
        case InkDivisionType.Line:
          divisionUnits = new DivisionUnits(this.m_Strokes.Ink, divisionType, this.m_aLineStrokeIDs, this.m_astrLines, this.m_aLineRotationCenterX, this.m_aLineRotationCenterY, this.m_aLineAngle);
          break;
        case InkDivisionType.Paragraph:
          divisionUnits = this.m_astrParagraphs == null ? new DivisionUnits(this.m_Strokes.Ink, divisionType, this.m_aParagraphStrokeIDs) : new DivisionUnits(this.m_Strokes.Ink, divisionType, this.m_aParagraphStrokeIDs, this.m_astrParagraphs);
          break;
        case InkDivisionType.Drawing:
          divisionUnits = new DivisionUnits(this.m_Strokes.Ink, divisionType, this.m_aDrawingStrokeIDs);
          break;
      }
      return divisionUnits;
    }
  }
}
