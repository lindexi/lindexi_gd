// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.RecognizerGuide
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Drawing;

namespace Microsoft.Ink
{
  public struct RecognizerGuide
  {
    private int m_rows;
    private int m_columns;
    private int m_midline;
    private Rectangle m_writingBox;
    private Rectangle m_drawnBox;

    public RecognizerGuide(
      int rows,
      int columns,
      int midline,
      Rectangle writingBox,
      Rectangle drawnBox)
    {
      this.m_rows = rows;
      this.m_columns = columns;
      this.m_midline = midline;
      this.m_writingBox = writingBox;
      this.m_drawnBox = drawnBox;
    }

    public int Rows
    {
      get => this.m_rows;
      set => this.m_rows = value;
    }

    public int Columns
    {
      get => this.m_columns;
      set => this.m_columns = value;
    }

    public int Midline
    {
      get => this.m_midline;
      set => this.m_midline = value;
    }

    public Rectangle WritingBox
    {
      get => this.m_writingBox;
      set => this.m_writingBox = value;
    }

    public Rectangle DrawnBox
    {
      get => this.m_drawnBox;
      set => this.m_drawnBox = value;
    }

    internal InkRecognizerGuideClass _InternalCopy()
    {
      InkRecognizerGuideClass recognizerGuideClass = new InkRecognizerGuideClass();
      InkRecoGuide inkRecoGuide;
      inkRecoGuide.cColumns = this.m_columns;
      inkRecoGuide.Midline = this.m_midline;
      inkRecoGuide.cRows = this.m_rows;
      inkRecoGuide.rectDrawnBox.Bottom = this.m_drawnBox.Bottom;
      inkRecoGuide.rectDrawnBox.Top = this.m_drawnBox.Top;
      inkRecoGuide.rectDrawnBox.Left = this.m_drawnBox.Left;
      inkRecoGuide.rectDrawnBox.Right = this.m_drawnBox.Right;
      inkRecoGuide.rectWritingBox.Bottom = this.m_writingBox.Bottom;
      inkRecoGuide.rectWritingBox.Top = this.m_writingBox.Top;
      inkRecoGuide.rectWritingBox.Left = this.m_writingBox.Left;
      inkRecoGuide.rectWritingBox.Right = this.m_writingBox.Right;
      recognizerGuideClass.GuideData = inkRecoGuide;
      return recognizerGuideClass;
    }

    internal RecognizerGuide(InkRecognizerGuide guide)
    {
      InkRecoGuide guideData = guide.GuideData;
      this.m_columns = guideData.cColumns;
      this.m_midline = guideData.Midline;
      this.m_rows = guideData.cRows;
      this.m_drawnBox = new Rectangle(guideData.rectDrawnBox.Left, guideData.rectDrawnBox.Top, guideData.rectDrawnBox.Right - guideData.rectDrawnBox.Left, guideData.rectDrawnBox.Bottom - guideData.rectDrawnBox.Top);
      this.m_writingBox = new Rectangle(guideData.rectWritingBox.Left, guideData.rectWritingBox.Top, guideData.rectWritingBox.Right - guideData.rectWritingBox.Left, guideData.rectWritingBox.Bottom - guideData.rectWritingBox.Top);
    }
  }
}
