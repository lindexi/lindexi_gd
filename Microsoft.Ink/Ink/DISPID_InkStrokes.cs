// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkStrokes
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkStrokes
  {
    DISPID_ISs_NewEnum = -4, // 0xFFFFFFFC
    DISPID_ISsItem = 0,
    DISPID_ISsCount = 1,
    DISPID_ISsValid = 2,
    DISPID_ISsInk = 3,
    DISPID_ISsAdd = 4,
    DISPID_ISsAddStrokes = 5,
    DISPID_ISsRemove = 6,
    DISPID_ISsRemoveStrokes = 7,
    DISPID_ISsToString = 8,
    DISPID_ISsModifyDrawingAttributes = 9,
    DISPID_ISsGetBoundingBox = 10, // 0x0000000A
    DISPID_ISsScaleToRectangle = 11, // 0x0000000B
    DISPID_ISsTransform = 12, // 0x0000000C
    DISPID_ISsMove = 13, // 0x0000000D
    DISPID_ISsRotate = 14, // 0x0000000E
    DISPID_ISsShear = 15, // 0x0000000F
    DISPID_ISsScale = 16, // 0x00000010
    DISPID_ISsClip = 17, // 0x00000011
    DISPID_ISsRecognitionResult = 18, // 0x00000012
    DISPID_ISsRemoveRecognitionResult = 19, // 0x00000013
  }
}
