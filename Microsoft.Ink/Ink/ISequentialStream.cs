// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.ISequentialStream
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [InterfaceType(1)]
  [Guid("0C733A30-2A1C-11CE-ADE5-00AA0044773D")]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal interface ISequentialStream
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteRead(out byte pv, [In] uint cb, out uint pcbRead);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteWrite([In] ref byte pv, [In] uint cb, out uint pcbWritten);
  }
}
