using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace WhefallralajaHubeanerelair;

[Guid("9794FF82-6071-4717-8A8B-6AC7C64A686E")]
[SuppressUnmanagedCodeSecurity]
[TypeLibType(4160)]
[ComImport]
internal interface IInkRectangle
{
    [DispId(1)]
    int Top { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(2)]
    int Left { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(3)]
    int Bottom { [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(4)]
    int Right { [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(7)]
    PenImcRect Data { [TypeLibFunc(64), DispId(7), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [TypeLibFunc(64), DispId(7), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(5)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetRectangle(out int top, out int left, out int bottom, out int right);

    [DispId(6)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetRectangle([In] int top, [In] int left, [In] int bottom, [In] int right);
}