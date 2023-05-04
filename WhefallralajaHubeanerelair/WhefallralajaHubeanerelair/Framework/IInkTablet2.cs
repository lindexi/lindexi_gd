using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace WhefallralajaHubeanerelair;

[TypeLibType(4160)]
[DefaultMember("DeviceKind")]
[Guid("90C91AD2-FA36-49D6-9516-CE8D570F6F85")]
[SuppressUnmanagedCodeSecurity]
[ComImport]
internal interface IInkTablet2
{
    [DispId(0)]
    TabletDeviceKindPrivate DeviceKind { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }
}