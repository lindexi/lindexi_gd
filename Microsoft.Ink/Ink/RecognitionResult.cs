// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.RecognitionResult
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class RecognitionResult
  {
    internal const int CompleteSelectionRecognitionAlternates = -1;
    internal const int StartOfSelectionRecognitionAlternates = 0;
    public static readonly int DefaultMaximumRecognitionAlternates = 10;
    internal IInkRecognitionResult m_RecognitionResult;

    internal RecognitionResult(IInkRecognitionResult recognitionResult) => this.m_RecognitionResult = recognitionResult;

    private RecognitionResult()
    {
    }

    public string TopString => this.m_RecognitionResult.TopString;

    public override string ToString() => this.TopString;

    public RecognitionConfidence TopConfidence
    {
      get
      {
        try
        {
          return (RecognitionConfidence) this.m_RecognitionResult.TopConfidence;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public Strokes Strokes
    {
      get
      {
        try
        {
          return new Strokes(this.m_RecognitionResult.Strokes);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public void ModifyTopAlternate(RecognitionAlternate alternate)
    {
      if (alternate == null)
        throw new ArgumentNullException(nameof (alternate), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        this.m_RecognitionResult.ModifyTopAlternate(alternate.m_Alternate);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public RecognitionAlternate TopAlternate
    {
      get
      {
        try
        {
          return new RecognitionAlternate(this.m_RecognitionResult.TopAlternate);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public RecognitionAlternates GetAlternatesFromSelection(
      int selectionStart,
      int selectionLength,
      int maximumAlternates)
    {
      return new RecognitionAlternates(this.m_RecognitionResult.AlternatesFromSelection(selectionStart, selectionLength, maximumAlternates));
    }

    public RecognitionAlternates GetAlternatesFromSelection(int selectionStart, int selectionLength) => this.GetAlternatesFromSelection(selectionStart, selectionLength, RecognitionResult.DefaultMaximumRecognitionAlternates);

    public RecognitionAlternates GetAlternatesFromSelection() => this.GetAlternatesFromSelection(0, -1, RecognitionResult.DefaultMaximumRecognitionAlternates);

    public void SetResultOnStrokes() => this.m_RecognitionResult.SetResultOnStrokes();
  }
}
