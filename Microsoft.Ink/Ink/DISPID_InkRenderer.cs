// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkRenderer
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkRenderer
  {
    DISPID_IRGetViewTransform = 1,
    DISPID_IRSetViewTransform = 2,
    DISPID_IRGetObjectTransform = 3,
    DISPID_IRSetObjectTransform = 4,
    DISPID_IRDraw = 5,
    DISPID_IRDrawStroke = 6,
    DISPID_IRPixelToInkSpace = 7,
    DISPID_IRInkSpaceToPixel = 8,
    DISPID_IRPixelToInkSpaceFromPoints = 9,
    DISPID_IRInkSpaceToPixelFromPoints = 10, // 0x0000000A
    DISPID_IRMeasure = 11, // 0x0000000B
    DISPID_IRMeasureStroke = 12, // 0x0000000C
    DISPID_IRMove = 13, // 0x0000000D
    DISPID_IRRotate = 14, // 0x0000000E
    DISPID_IRScale = 15, // 0x0000000F
  }
}
