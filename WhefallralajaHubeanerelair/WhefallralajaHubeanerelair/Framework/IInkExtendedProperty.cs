using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace WhefallralajaHubeanerelair;

[TypeLibType(4160)]
[System.Runtime.InteropServices.Guid("DB489209-B7C3-411D-90F6-1548CFFF271E")]
[SuppressUnmanagedCodeSecurity]
[ComImport]
internal interface IInkExtendedProperty
{
    [DispId(1)]
    string Guid { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(2)]
    object Data { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Struct)] get; [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: MarshalAs(UnmanagedType.Struct), In] set; }
}