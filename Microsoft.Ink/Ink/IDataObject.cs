// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IDataObject
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [ComConversionLoss]
  [Guid("0000010E-0000-0000-C000-000000000046")]
  [SuppressUnmanagedCodeSecurity]
  [InterfaceType(1)]
  [ComImport]
  internal interface IDataObject
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteGetData([In] ref tagFORMATETC pformatetcIn, [ComAliasName("Microsoft.Ink.wireSTGMEDIUM"), Out] IntPtr pRemoteMedium);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteGetDataHere([In] ref tagFORMATETC pformatetc, [ComAliasName("Microsoft.Ink.wireSTGMEDIUM"), In, Out] IntPtr pRemoteMedium);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void QueryGetData([In] ref tagFORMATETC pformatetc);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetCanonicalFormatEtc([In] ref tagFORMATETC pformatectIn, out tagFORMATETC pformatetcOut);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteSetData([In] ref tagFORMATETC pformatetc, [ComAliasName("Microsoft.Ink.wireFLAG_STGMEDIUM"), In] IntPtr pmedium, [In] int fRelease);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void EnumFormatEtc([In] uint dwDirection, [MarshalAs(UnmanagedType.Interface)] out IEnumFORMATETC ppenumFormatEtc);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void DAdvise(
      [In] ref tagFORMATETC pformatetc,
      [In] uint advf,
      [MarshalAs(UnmanagedType.Interface), In] IAdviseSink pAdvSink,
      out uint pdwConnection);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void DUnadvise([In] uint dwConnection);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void EnumDAdvise([MarshalAs(UnmanagedType.Interface)] out IEnumSTATDATA ppenumAdvise);
  }
}
