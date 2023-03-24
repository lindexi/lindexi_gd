// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IAdviseSink
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [InterfaceType(1)]
  [ComConversionLoss]
  [Guid("0000010F-0000-0000-C000-000000000046")]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal interface IAdviseSink
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteOnDataChange([In] ref tagFORMATETC pformatetc, [ComAliasName("Microsoft.Ink.wireASYNC_STGMEDIUM"), In] IntPtr pStgmed);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteOnViewChange([In] uint dwAspect, [In] int lindex);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteOnRename([MarshalAs(UnmanagedType.Interface), In] IMoniker pmk);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteOnSave();

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteOnClose();
  }
}
