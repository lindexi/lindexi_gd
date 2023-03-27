using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DireljelcoDaicejuniredere;

[TypeLibType(4160)]
[SuppressUnmanagedCodeSecurity]
[Guid("AD30C630-40C5-4350-8405-9C71012FC558")]
[DefaultMember("Name")]
[ComImport]
internal interface IInkCursor
{
    [DispId(0)]
    string Name { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(1)]
    int Id { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(4)]
    bool Inverted { [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(2)]
    InkDrawingAttributes DrawingAttributes { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Interface)] get; [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: MarshalAs(UnmanagedType.Interface), In] set; }

    [DispId(5)]
    IInkTablet Tablet { [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(3)]
    IInkCursorButtons Buttons { [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Interface)] get; }
}