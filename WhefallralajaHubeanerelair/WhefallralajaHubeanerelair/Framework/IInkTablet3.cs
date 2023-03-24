using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace WhefallralajaHubeanerelair;

[TypeLibType(4160)]
[DefaultMember("IsMultiTouch")]
[Guid("7E313997-1327-41DD-8CA9-79F24BE17250")]
[SuppressUnmanagedCodeSecurity]
[ComImport]
internal interface IInkTablet3
{
    [DispId(0)]
    bool IsMultiTouch { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(1)]
    uint MaximumCursors { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }
}