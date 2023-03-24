// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._userBITMAP
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
  internal struct _userBITMAP
  {
    public int bmType;
    public int bmWidth;
    public int bmHeight;
    public int bmWidthBytes;
    public ushort bmPlanes;
    public ushort bmBitsPixel;
    public uint cbSize;
    [ComConversionLoss]
    public IntPtr pBuffer;
  }
}
