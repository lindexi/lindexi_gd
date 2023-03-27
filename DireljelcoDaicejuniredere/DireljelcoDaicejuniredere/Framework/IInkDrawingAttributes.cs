using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DireljelcoDaicejuniredere;

[SuppressUnmanagedCodeSecurity]
[TypeLibType(4160)]
[Guid("BF519B75-0A15-4623-ADC9-C00D436A8092")]
[ComImport]
internal interface IInkDrawingAttributes
{
    [DispId(2)]
    int Color { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(3)]
    float Width { [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(1)]
    float Height { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(4)]
    bool FitToCurve { [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(4), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(5)]
    bool IgnorePressure { [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(6)]
    bool AntiAliased { [DispId(6), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(6), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(7)]
    int Transparency { [DispId(7), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(7), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(8)]
    InkRasterOperation RasterOperation { [DispId(8), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(8), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(9)]
    InkPenTip PenTip { [DispId(9), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(9), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: In] set; }

    [DispId(11)]
    IInkExtendedProperties ExtendedProperties { [DispId(11), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(10)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    InkDrawingAttributes Clone();
}