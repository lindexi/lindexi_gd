using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DireljelcoDaicejuniredere;

[Guid("85EF9417-1D59-49B2-A13C-702C85430894")]
[DefaultMember("Name")]
[SuppressUnmanagedCodeSecurity]
[TypeLibType(4160)]
[ComImport]
internal interface IInkCursorButton
{
    [DispId(0)]
    string Name { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(1)]
    string Id { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(2)]
    InkCursorButtonState State { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }
}