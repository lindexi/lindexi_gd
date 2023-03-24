// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.RecognitionAlternate
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class RecognitionAlternate
  {
    internal IInkRecognitionAlternate m_Alternate;
    private Strokes m_Strokes;

    internal RecognitionAlternate(IInkRecognitionAlternate alternate) => this.m_Alternate = alternate;

    private RecognitionAlternate()
    {
    }

    public Strokes Strokes
    {
      get
      {
        if (this.m_Strokes == null)
          this.m_Strokes = new Strokes(this.m_Alternate.Strokes);
        return this.m_Strokes;
      }
    }

    public override string ToString() => this.m_Alternate.String;

    public Line Midline
    {
      get
      {
        try
        {
          return new Line((int[]) this.m_Alternate.Midline);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public Line Ascender
    {
      get
      {
        try
        {
          return new Line((int[]) this.m_Alternate.Ascender);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public Line Descender
    {
      get
      {
        try
        {
          return new Line((int[]) this.m_Alternate.Descender);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public Line Baseline
    {
      get
      {
        try
        {
          return new Line((int[]) this.m_Alternate.Baseline);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public int LineNumber
    {
      get
      {
        try
        {
          return this.m_Alternate.LineNumber;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public RecognitionAlternates LineAlternates
    {
      get
      {
        try
        {
          return new RecognitionAlternates(this.m_Alternate.LineAlternates);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public RecognitionAlternates ConfidenceAlternates
    {
      get
      {
        try
        {
          return new RecognitionAlternates(this.m_Alternate.ConfidenceAlternates);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public RecognitionConfidence Confidence
    {
      get
      {
        try
        {
          return (RecognitionConfidence) this.m_Alternate.Confidence;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public byte[] GetPropertyValue(Guid g) => (byte[]) this.m_Alternate.GetPropertyValue(g.ToString("B"));

    public void GetTextRangeFromStrokes(Strokes s, ref int selectionStart, ref int selectionLength)
    {
      if (s == null)
        throw new ArgumentNullException(nameof (s));
      this.m_Alternate.GetTextRangeFromStrokes(s.m_Strokes, ref selectionStart, ref selectionLength);
    }

    public Strokes GetStrokesFromTextRange(ref int selectionStart, ref int selectionLength) => new Strokes(this.m_Alternate.GetStrokesFromTextRange(ref selectionStart, ref selectionLength));

    public Strokes GetStrokesFromStrokeRanges(Strokes s)
    {
      if (s == null)
        throw new ArgumentNullException(nameof (s));
      return new Strokes(this.m_Alternate.GetStrokesFromStrokeRanges(s.m_Strokes));
    }

    public RecognitionAlternates AlternatesWithConstantPropertyValues(Guid g)
    {
      try
      {
        return new RecognitionAlternates(this.m_Alternate.AlternatesWithConstantPropertyValues(g.ToString("B")));
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }
  }
}
