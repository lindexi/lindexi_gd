// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IRunningObjectTable
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [Guid("00000010-0000-0000-C000-000000000046")]
  [InterfaceType(1)]
  [ComImport]
  internal interface IRunningObjectTable
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Register([In] uint grfFlags, [MarshalAs(UnmanagedType.IUnknown), In] object punkObject, [MarshalAs(UnmanagedType.Interface), In] IMoniker pmkObjectName, out uint pdwRegister);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Revoke([In] uint dwRegister);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void IsRunning([MarshalAs(UnmanagedType.Interface), In] IMoniker pmkObjectName);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetObject([MarshalAs(UnmanagedType.Interface), In] IMoniker pmkObjectName, [MarshalAs(UnmanagedType.IUnknown)] out object ppunkObject);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void NoteChangeTime([In] uint dwRegister, [In] ref _FILETIME pfiletime);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetTimeOfLastChange([MarshalAs(UnmanagedType.Interface), In] IMoniker pmkObjectName, out _FILETIME pfiletime);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void EnumRunning([MarshalAs(UnmanagedType.Interface)] out IEnumMoniker ppenumMoniker);
  }
}
