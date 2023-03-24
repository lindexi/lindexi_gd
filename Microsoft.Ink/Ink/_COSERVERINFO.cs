// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._COSERVERINFO
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [ComConversionLoss]
  [SuppressUnmanagedCodeSecurity]
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  internal struct _COSERVERINFO
  {
    public uint dwReserved1;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string pwszName;
    [ComConversionLoss]
    public IntPtr pAuthInfo;
    public uint dwReserved2;
  }
}
