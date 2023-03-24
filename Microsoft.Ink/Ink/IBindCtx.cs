// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IBindCtx
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [InterfaceType(1)]
  [Guid("0000000E-0000-0000-C000-000000000046")]
  [ComImport]
  internal interface IBindCtx
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RegisterObjectBound([MarshalAs(UnmanagedType.IUnknown), In] object punk);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RevokeObjectBound([MarshalAs(UnmanagedType.IUnknown), In] object punk);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void ReleaseBoundObjects();

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteSetBindOptions([In] ref tagBIND_OPTS2 pbindopts);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoteGetBindOptions([In, Out] ref tagBIND_OPTS2 pbindopts);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetRunningObjectTable([MarshalAs(UnmanagedType.Interface)] out IRunningObjectTable pprot);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RegisterObjectParam([MarshalAs(UnmanagedType.LPWStr), In] string pszKey, [MarshalAs(UnmanagedType.IUnknown), In] object punk);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetObjectParam([MarshalAs(UnmanagedType.LPWStr), In] string pszKey, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void EnumObjectParam([MarshalAs(UnmanagedType.Interface)] out IEnumString ppenum);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RevokeObjectParam([MarshalAs(UnmanagedType.LPWStr), In] string pszKey);
  }
}
