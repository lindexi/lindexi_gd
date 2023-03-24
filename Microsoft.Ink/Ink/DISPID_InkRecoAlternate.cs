// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkRecoAlternate
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkRecoAlternate
  {
    DISPID_InkRecoAlternate_String = 1,
    DISPID_InkRecoAlternate_LineNumber = 2,
    DISPID_InkRecoAlternate_Baseline = 3,
    DISPID_InkRecoAlternate_Midline = 4,
    DISPID_InkRecoAlternate_Ascender = 5,
    DISPID_InkRecoAlternate_Descender = 6,
    DISPID_InkRecoAlternate_Confidence = 7,
    DISPID_InkRecoAlternate_Strokes = 8,
    DISPID_InkRecoAlternate_GetStrokesFromStrokeRanges = 9,
    DISPID_InkRecoAlternate_GetStrokesFromTextRange = 10, // 0x0000000A
    DISPID_InkRecoAlternate_GetTextRangeFromStrokes = 11, // 0x0000000B
    DISPID_InkRecoAlternate_GetPropertyValue = 12, // 0x0000000C
    DISPID_InkRecoAlternate_LineAlternates = 13, // 0x0000000D
    DISPID_InkRecoAlternate_ConfidenceAlternates = 14, // 0x0000000E
    DISPID_InkRecoAlternate_AlternatesWithConstantPropertyValues = 15, // 0x0000000F
  }
}
