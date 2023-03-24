// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_Ink
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_Ink
  {
    DISPID_IStrokes = 1,
    DISPID_IExtendedProperties = 2,
    DISPID_IGetBoundingBox = 3,
    DISPID_IDeleteStrokes = 4,
    DISPID_IDeleteStroke = 5,
    DISPID_IExtractStrokes = 6,
    DISPID_IExtractWithRectangle = 7,
    DISPID_IDirty = 8,
    DISPID_ICustomStrokes = 9,
    DISPID_IClone = 10, // 0x0000000A
    DISPID_IHitTestCircle = 11, // 0x0000000B
    DISPID_IHitTestWithRectangle = 12, // 0x0000000C
    DISPID_IHitTestWithLasso = 13, // 0x0000000D
    DISPID_INearestPoint = 14, // 0x0000000E
    DISPID_ICreateStrokes = 15, // 0x0000000F
    DISPID_ICreateStroke = 16, // 0x00000010
    DISPID_IAddStrokesAtRectangle = 17, // 0x00000011
    DISPID_IClip = 18, // 0x00000012
    DISPID_ISave = 19, // 0x00000013
    DISPID_ILoad = 20, // 0x00000014
    DISPID_ICreateStrokeFromPoints = 21, // 0x00000015
    DISPID_IClipboardCopyWithRectangle = 22, // 0x00000016
    DISPID_IClipboardCopy = 23, // 0x00000017
    DISPID_ICanPaste = 24, // 0x00000018
    DISPID_IClipboardPaste = 25, // 0x00000019
  }
}
