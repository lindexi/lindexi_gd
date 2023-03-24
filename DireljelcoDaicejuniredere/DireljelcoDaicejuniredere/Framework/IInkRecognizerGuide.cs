using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DireljelcoDaicejuniredere;

[SuppressUnmanagedCodeSecurity]
[Guid("D934BE07-7B84-4208-9136-83C20994E905")]
[TypeLibType(4160)]
[ComImport]
internal interface IInkRecognizerGuide
{
    [DispId(1)]
    InkRectangle WritingBox { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Interface)] get; [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: MarshalAs(UnmanagedType.Interface), In] set; }

    [DispId(2)]
    InkRectangle DrawnBox { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Interface)] get; [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: MarshalAs(UnmanagedType.Interface), In] set; }

    [DispId(3)]
    int Rows { [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(4)]
    int Columns { [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(5)]
    int Midline { [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(6)]
    [ComAliasName("Microsoft.Ink.InkRecoGuide")]
    InkRecoGuide GuideData { [TypeLibFunc(64), DispId(6), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: ComAliasName("Microsoft.Ink.InkRecoGuide")] get; [DispId(6), TypeLibFunc(64), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: ComAliasName("Microsoft.Ink.InkRecoGuide"), In] set; }
}