using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace WhefallralajaHubeanerelair;

[ClassInterface((short) 0)]
[TypeLibType(2)]
[SuppressUnmanagedCodeSecurity]
[Guid("8770D941-A63A-4671-A375-2855A18EBA73")]
[ComImport]
internal class InkRecognizerGuideClass : IInkRecognizerGuide, InkRecognizerGuide
{
    //[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    //public extern InkRecognizerGuideClass();

    [DispId(1)]
    public virtual extern InkRectangle WritingBox { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Interface)] get; [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: MarshalAs(UnmanagedType.Interface), In] set; }

    [DispId(2)]
    public virtual extern InkRectangle DrawnBox { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Interface)] get; [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: MarshalAs(UnmanagedType.Interface), In] set; }

    [DispId(3)]
    public virtual extern int Rows { [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(4)]
    public virtual extern int Columns { [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(5)]
    public virtual extern int Midline { [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [ComAliasName("Microsoft.Ink.InkRecoGuide")]
    [DispId(6)]
    public virtual extern InkRecoGuide GuideData { [DispId(6), TypeLibFunc(64), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: ComAliasName("Microsoft.Ink.InkRecoGuide")] get; [DispId(6), TypeLibFunc(64), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: ComAliasName("Microsoft.Ink.InkRecoGuide"), In] set; }
}