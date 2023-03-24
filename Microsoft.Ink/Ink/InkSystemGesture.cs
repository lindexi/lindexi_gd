// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkSystemGesture
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal enum InkSystemGesture
  {
    ISG_Tap = 16, // 0x00000010
    ISG_DoubleTap = 17, // 0x00000011
    ISG_RightTap = 18, // 0x00000012
    ISG_Drag = 19, // 0x00000013
    ISG_RightDrag = 20, // 0x00000014
    ISG_HoldEnter = 21, // 0x00000015
    ISG_HoldLeave = 22, // 0x00000016
    ISG_HoverEnter = 23, // 0x00000017
    ISG_HoverLeave = 24, // 0x00000018
    ISG_Flick = 31, // 0x0000001F
  }
}
