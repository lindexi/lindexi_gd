using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace WhefallralajaHubeanerelair;

[Guid("2DE25EAA-6EF8-42D5-AEE9-185BC81B912D")]
[DefaultMember("Name")]
[TypeLibType(4160)]
[SuppressUnmanagedCodeSecurity]
[ComImport]
internal interface IInkTablet
{
    [DispId(0)]
    string Name { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(1)]
    string PlugAndPlayId { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(4)]
    InkRectangle MaximumInputRectangle { [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(5)]
    TabletHardwareCapabilitiesPrivate HardwareCapabilities { [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(3)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    bool IsPacketPropertySupported([MarshalAs(UnmanagedType.BStr), In] string packetPropertyName);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetPropertyMetrics(
        [MarshalAs(UnmanagedType.BStr), In] string propertyName,
        out int minimum,
        out int maximum,
        out TabletPropertyMetricUnitPrivate units,
        out float resolution);
}