// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkMousePointer
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal enum InkMousePointer
  {
    IMP_Default = 0,
    IMP_Arrow = 1,
    IMP_Crosshair = 2,
    IMP_Ibeam = 3,
    IMP_SizeNESW = 4,
    IMP_SizeNS = 5,
    IMP_SizeNWSE = 6,
    IMP_SizeWE = 7,
    IMP_UpArrow = 8,
    IMP_Hourglass = 9,
    IMP_NoDrop = 10, // 0x0000000A
    IMP_ArrowHourglass = 11, // 0x0000000B
    IMP_ArrowQuestion = 12, // 0x0000000C
    IMP_SizeAll = 13, // 0x0000000D
    IMP_Hand = 14, // 0x0000000E
    IMP_Custom = 99, // 0x00000063
  }
}
