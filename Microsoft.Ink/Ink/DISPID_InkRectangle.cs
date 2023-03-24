// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkRectangle
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkRectangle
  {
    DISPID_IRTop = 1,
    DISPID_IRLeft = 2,
    DISPID_IRBottom = 3,
    DISPID_IRRight = 4,
    DISPID_IRGetRectangle = 5,
    DISPID_IRSetRectangle = 6,
    DISPID_IRData = 7,
  }
}
