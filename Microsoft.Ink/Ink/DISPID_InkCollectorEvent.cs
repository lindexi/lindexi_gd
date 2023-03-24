// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkCollectorEvent
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkCollectorEvent
  {
    DISPID_ICEStroke = 1,
    DISPID_ICECursorDown = 2,
    DISPID_ICENewPackets = 3,
    DISPID_ICENewInAirPackets = 4,
    DISPID_ICECursorButtonDown = 5,
    DISPID_ICECursorButtonUp = 6,
    DISPID_ICECursorInRange = 7,
    DISPID_ICECursorOutOfRange = 8,
    DISPID_ICESystemGesture = 9,
    DISPID_ICEGesture = 10, // 0x0000000A
    DISPID_ICETabletAdded = 11, // 0x0000000B
    DISPID_ICETabletRemoved = 12, // 0x0000000C
    DISPID_IOEPainting = 13, // 0x0000000D
    DISPID_IOEPainted = 14, // 0x0000000E
    DISPID_IOESelectionChanging = 15, // 0x0000000F
    DISPID_IOESelectionChanged = 16, // 0x00000010
    DISPID_IOESelectionMoving = 17, // 0x00000011
    DISPID_IOESelectionMoved = 18, // 0x00000012
    DISPID_IOESelectionResizing = 19, // 0x00000013
    DISPID_IOESelectionResized = 20, // 0x00000014
    DISPID_IOEStrokesDeleting = 21, // 0x00000015
    DISPID_IOEStrokesDeleted = 22, // 0x00000016
    DISPID_IPEChangeUICues = 23, // 0x00000017
    DISPID_IPEClick = 24, // 0x00000018
    DISPID_IPEDblClick = 25, // 0x00000019
    DISPID_IPEInvalidated = 26, // 0x0000001A
    DISPID_IPEMouseDown = 27, // 0x0000001B
    DISPID_IPEMouseEnter = 28, // 0x0000001C
    DISPID_IPEMouseHover = 29, // 0x0000001D
    DISPID_IPEMouseLeave = 30, // 0x0000001E
    DISPID_IPEMouseMove = 31, // 0x0000001F
    DISPID_IPEMouseUp = 32, // 0x00000020
    DISPID_IPEMouseWheel = 33, // 0x00000021
    DISPID_IPESizeModeChanged = 34, // 0x00000022
    DISPID_IPEStyleChanged = 35, // 0x00000023
    DISPID_IPESystemColorsChanged = 36, // 0x00000024
    DISPID_IPEKeyDown = 37, // 0x00000025
    DISPID_IPEKeyPress = 38, // 0x00000026
    DISPID_IPEKeyUp = 39, // 0x00000027
    DISPID_IPEResize = 40, // 0x00000028
    DISPID_IPESizeChanged = 41, // 0x00000029
  }
}
