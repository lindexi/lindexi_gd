// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.TextInput.HandwrittenTextInsertion
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Microsoft.Ink.TextInput
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
  public class HandwrittenTextInsertion
  {
    private HandwrittenTextInsertionPrivate tsfInserter;

    public HandwrittenTextInsertion()
    {
      try
      {
        this.tsfInserter = (HandwrittenTextInsertionPrivate) new HandwrittenTextInsertionClass();
      }
      catch (COMException ex)
      {
        this.tsfInserter = (HandwrittenTextInsertionPrivate) null;
        throw;
      }
    }

    public void InsertRecognitionResultsArray(
      string[][] alternates,
      CultureInfo culture,
      bool alternateContainsAutoSpacingInformation)
    {
      if (culture == null)
        throw new ArgumentNullException(nameof (culture));
      if (alternates == null)
        throw new ArgumentNullException(nameof (alternates));
      if (this.tsfInserter == null)
        return;
      int length = alternates.Length;
      long val1 = 0;
      long[] numArray = new long[length];
      for (int index = 0; index < length; ++index)
      {
        numArray[index] = (long) alternates[index].Length;
        val1 = Math.Max(val1, numArray[index]);
      }
      Array instance = Array.CreateInstance(typeof (string), (long) length, val1);
      for (int index1 = 0; index1 < length; ++index1)
      {
        for (int index2 = 0; (long) index2 < numArray[index1]; ++index2)
          instance.SetValue((object) alternates[index1][index2], index1, index2);
      }
      this.tsfInserter.InsertRecognitionResultsArray(instance, (uint) culture.LCID, alternateContainsAutoSpacingInformation ? 1 : 0);
    }

    public void InsertInkRecognitionResult(
      RecognitionResult recognitionResult,
      CultureInfo culture,
      bool alternateContainsAutoSpacingInformation)
    {
      if (culture == null)
        throw new ArgumentNullException(nameof (culture));
      if (recognitionResult == null)
        throw new ArgumentNullException(nameof (recognitionResult));
      if (this.tsfInserter == null || culture == null || recognitionResult == null)
        return;
      this.tsfInserter.InsertInkRecognitionResult(recognitionResult.m_RecognitionResult, (uint) culture.LCID, alternateContainsAutoSpacingInformation ? 1 : 0);
    }
  }
}
