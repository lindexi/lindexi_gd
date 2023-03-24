// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkCollectorPrivate
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkCollectorPrivate
  {
    DISPID_ICEnabled = 1,
    DISPID_ICHwnd = 2,
    DISPID_ICPaint = 3,
    DISPID_ICText = 4,
    DISPID_ICDefaultDrawingAttributes = 5,
    DISPID_ICRenderer = 6,
    DISPID_ICInk = 7,
    DISPID_ICAutoRedraw = 8,
    DISPID_ICCollectingInk = 9,
    DISPID_ICSetEventInterest = 10, // 0x0000000A
    DISPID_ICGetEventInterest = 11, // 0x0000000B
    DISPID_IOEditingMode = 12, // 0x0000000C
    DISPID_IOSelection = 13, // 0x0000000D
    DISPID_IOAttachMode = 14, // 0x0000000E
    DISPID_IOHitTestSelection = 15, // 0x0000000F
    DISPID_IODraw = 16, // 0x00000010
    DISPID_IPPicture = 17, // 0x00000011
    DISPID_IPSizeMode = 18, // 0x00000012
    DISPID_IPBackColor = 19, // 0x00000013
    DISPID_ICCursors = 20, // 0x00000014
    DISPID_ICMarginX = 21, // 0x00000015
    DISPID_ICMarginY = 22, // 0x00000016
    DISPID_ICSetWindowInputRectangle = 23, // 0x00000017
    DISPID_ICGetWindowInputRectangle = 24, // 0x00000018
    DISPID_ICTablet = 25, // 0x00000019
    DISPID_ICSetAllTabletsMode = 26, // 0x0000001A
    DISPID_ICSetSingleTabletIntegratedMode = 27, // 0x0000001B
    DISPID_ICCollectionMode = 28, // 0x0000001C
    DISPID_ICSetGestureStatus = 29, // 0x0000001D
    DISPID_ICGetGestureStatus = 30, // 0x0000001E
    DISPID_ICDynamicRendering = 31, // 0x0000001F
    DISPID_ICDesiredPacketDescription = 32, // 0x00000020
    DISPID_IOEraserMode = 33, // 0x00000021
    DISPID_IOEraserWidth = 34, // 0x00000022
    DISPID_ICMouseIcon = 35, // 0x00000023
    DISPID_ICMousePointer = 36, // 0x00000024
    DISPID_IPInkEnabled = 37, // 0x00000025
    DISPID_ICSupportHighContrastInk = 38, // 0x00000026
    DISPID_IOSupportHighContrastSelectionUI = 39, // 0x00000027
  }
}
