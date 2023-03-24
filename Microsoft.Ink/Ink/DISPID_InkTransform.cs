// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkTransform
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [TypeLibType(16)]
  [SuppressUnmanagedCodeSecurity]
  internal enum DISPID_InkTransform
  {
    DISPID_ITReset = 1,
    DISPID_ITTranslate = 2,
    DISPID_ITRotate = 3,
    DISPID_ITReflect = 4,
    DISPID_ITShear = 5,
    DISPID_ITScale = 6,
    DISPID_ITeM11 = 7,
    DISPID_ITeM12 = 8,
    DISPID_ITeM21 = 9,
    DISPID_ITeM22 = 10, // 0x0000000A
    DISPID_ITeDx = 11, // 0x0000000B
    DISPID_ITeDy = 12, // 0x0000000C
    DISPID_ITGetTransform = 13, // 0x0000000D
    DISPID_ITSetTransform = 14, // 0x0000000E
    DISPID_ITData = 15, // 0x0000000F
  }
}
