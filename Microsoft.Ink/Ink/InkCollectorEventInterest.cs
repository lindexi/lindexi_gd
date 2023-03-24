// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkCollectorEventInterest
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal enum InkCollectorEventInterest
  {
    ICEI_DefaultEvents = -1, // 0xFFFFFFFF
    ICEI_CursorDown = 0,
    ICEI_Stroke = 1,
    ICEI_NewPackets = 2,
    ICEI_NewInAirPackets = 3,
    ICEI_CursorButtonDown = 4,
    ICEI_CursorButtonUp = 5,
    ICEI_CursorInRange = 6,
    ICEI_CursorOutOfRange = 7,
    ICEI_SystemGesture = 8,
    ICEI_TabletAdded = 9,
    ICEI_TabletRemoved = 10, // 0x0000000A
    ICEI_MouseDown = 11, // 0x0000000B
    ICEI_MouseMove = 12, // 0x0000000C
    ICEI_MouseUp = 13, // 0x0000000D
    ICEI_MouseWheel = 14, // 0x0000000E
    ICEI_DblClick = 15, // 0x0000000F
    [TypeLibVar(64)] ICEI_AllEvents = 16, // 0x00000010
  }
}
