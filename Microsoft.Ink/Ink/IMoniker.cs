// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IMoniker
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [Guid("0000000F-0000-0000-C000-000000000046")]
  [InterfaceType(1)]
  [ComImport]
  internal interface IMoniker : IPersistStream
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    new void GetClassID(out Guid pClassID);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    new void IsDirty();

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    new void Load([MarshalAs(UnmanagedType.Interface), In] IStream pstm);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    new void Save([MarshalAs(UnmanagedType.Interface), In] IStream pstm, [In] int fClearDirty);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    new void GetSizeMax(out _ULARGE_INTEGER pcbSize);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteBindToObject(
      [MarshalAs(UnmanagedType.Interface), In] IBindCtx pbc,
      [MarshalAs(UnmanagedType.Interface), In] IMoniker pmkToLeft,
      [In] ref Guid riidResult,
      [MarshalAs(UnmanagedType.IUnknown)] out object ppvResult);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteBindToStorage([MarshalAs(UnmanagedType.Interface), In] IBindCtx pbc, [MarshalAs(UnmanagedType.Interface), In] IMoniker pmkToLeft, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObj);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Reduce(
      [MarshalAs(UnmanagedType.Interface), In] IBindCtx pbc,
      [In] uint dwReduceHowFar,
      [MarshalAs(UnmanagedType.Interface), In, Out] ref IMoniker ppmkToLeft,
      [MarshalAs(UnmanagedType.Interface)] out IMoniker ppmkReduced);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void ComposeWith([MarshalAs(UnmanagedType.Interface), In] IMoniker pmkRight, [In] int fOnlyIfNotGeneric, [MarshalAs(UnmanagedType.Interface)] out IMoniker ppmkComposite);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Enum([In] int fForward, [MarshalAs(UnmanagedType.Interface)] out IEnumMoniker ppenumMoniker);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void IsEqual([MarshalAs(UnmanagedType.Interface), In] IMoniker pmkOtherMoniker);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Hash(out uint pdwHash);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void IsRunning([MarshalAs(UnmanagedType.Interface), In] IBindCtx pbc, [MarshalAs(UnmanagedType.Interface), In] IMoniker pmkToLeft, [MarshalAs(UnmanagedType.Interface), In] IMoniker pmkNewlyRunning);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetTimeOfLastChange([MarshalAs(UnmanagedType.Interface), In] IBindCtx pbc, [MarshalAs(UnmanagedType.Interface), In] IMoniker pmkToLeft, out _FILETIME pfiletime);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Inverse([MarshalAs(UnmanagedType.Interface)] out IMoniker ppmk);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void CommonPrefixWith([MarshalAs(UnmanagedType.Interface), In] IMoniker pmkOther, [MarshalAs(UnmanagedType.Interface)] out IMoniker ppmkPrefix);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RelativePathTo([MarshalAs(UnmanagedType.Interface), In] IMoniker pmkOther, [MarshalAs(UnmanagedType.Interface)] out IMoniker ppmkRelPath);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetDisplayName([MarshalAs(UnmanagedType.Interface), In] IBindCtx pbc, [MarshalAs(UnmanagedType.Interface), In] IMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] out string ppszDisplayName);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void ParseDisplayName(
      [MarshalAs(UnmanagedType.Interface), In] IBindCtx pbc,
      [MarshalAs(UnmanagedType.Interface), In] IMoniker pmkToLeft,
      [MarshalAs(UnmanagedType.LPWStr), In] string pszDisplayName,
      out uint pchEaten,
      [MarshalAs(UnmanagedType.Interface)] out IMoniker ppmkOut);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void IsSystemMoniker(out uint pdwMksys);
  }
}
