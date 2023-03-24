// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.tagSTATSTG
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  internal struct tagSTATSTG
  {
    [MarshalAs(UnmanagedType.LPWStr)]
    public string pwcsName;
    public uint type;
    public _ULARGE_INTEGER cbSize;
    public _FILETIME mtime;
    public _FILETIME ctime;
    public _FILETIME atime;
    public uint grfMode;
    public uint grfLocksSupported;
    public Guid clsid;
    public uint grfStateBits;
    public uint reserved;
  }
}
