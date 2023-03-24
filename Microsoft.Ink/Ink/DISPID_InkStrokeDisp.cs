// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkStrokeDisp
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkStrokeDisp
  {
    DISPID_ISDInkIndex = 1,
    DISPID_ISDID = 2,
    DISPID_ISDGetBoundingBox = 3,
    DISPID_ISDDrawingAttributes = 4,
    DISPID_ISDFindIntersections = 5,
    DISPID_ISDGetRectangleIntersections = 6,
    DISPID_ISDClip = 7,
    DISPID_ISDHitTestCircle = 8,
    DISPID_ISDNearestPoint = 9,
    DISPID_ISDSplit = 10, // 0x0000000A
    DISPID_ISDExtendedProperties = 11, // 0x0000000B
    DISPID_ISDInk = 12, // 0x0000000C
    DISPID_ISDBezierPoints = 13, // 0x0000000D
    DISPID_ISDPolylineCusps = 14, // 0x0000000E
    DISPID_ISDBezierCusps = 15, // 0x0000000F
    DISPID_ISDSelfIntersections = 16, // 0x00000010
    DISPID_ISDPacketCount = 17, // 0x00000011
    DISPID_ISDPacketSize = 18, // 0x00000012
    DISPID_ISDPacketDescription = 19, // 0x00000013
    DISPID_ISDDeleted = 20, // 0x00000014
    DISPID_ISDGetPacketDescriptionPropertyMetrics = 21, // 0x00000015
    DISPID_ISDGetPoints = 22, // 0x00000016
    DISPID_ISDSetPoints = 23, // 0x00000017
    DISPID_ISDGetPacketData = 24, // 0x00000018
    DISPID_ISDGetPacketValuesByProperty = 25, // 0x00000019
    DISPID_ISDSetPacketValuesByProperty = 26, // 0x0000001A
    DISPID_ISDGetFlattenedBezierPoints = 27, // 0x0000001B
    DISPID_ISDScaleToRectangle = 28, // 0x0000001C
    DISPID_ISDTransform = 29, // 0x0000001D
    DISPID_ISDMove = 30, // 0x0000001E
    DISPID_ISDRotate = 31, // 0x0000001F
    DISPID_ISDShear = 32, // 0x00000020
    DISPID_ISDScale = 33, // 0x00000021
  }
}
