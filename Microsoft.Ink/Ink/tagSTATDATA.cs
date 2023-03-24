// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.tagSTATDATA
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  internal struct tagSTATDATA
  {
    public tagFORMATETC formatetc;
    public uint advf;
    [MarshalAs(UnmanagedType.Interface)]
    public IAdviseSink pAdvSink;
    public uint dwConnection;
  }
}
