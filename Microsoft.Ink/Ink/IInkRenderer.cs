// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IInkRenderer
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [TypeLibType(4160)]
  [Guid("E6257A9C-B511-4F4C-A8B0-A7DBC9506B83")]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal interface IInkRenderer
  {
    [DispId(1)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetViewTransform([MarshalAs(UnmanagedType.Interface), In] InkTransform ViewTransform);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetViewTransform([MarshalAs(UnmanagedType.Interface), In] InkTransform ViewTransform);

    [DispId(3)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetObjectTransform([MarshalAs(UnmanagedType.Interface), In] InkTransform ObjectTransform);

    [DispId(4)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetObjectTransform([MarshalAs(UnmanagedType.Interface), In] InkTransform ObjectTransform);

    [DispId(5)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Draw([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hDC, [MarshalAs(UnmanagedType.Interface), In] InkStrokes Strokes);

    [DispId(6)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void DrawStroke([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hDC, [MarshalAs(UnmanagedType.Interface), In] IInkStrokeDisp Stroke, [MarshalAs(UnmanagedType.Interface), In] InkDrawingAttributes DrawingAttributes = null);

    [DispId(7)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void PixelToInkSpace([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hDC, [In, Out] ref int x, [In, Out] ref int y);

    [DispId(8)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InkSpaceToPixel([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hdcDisplay, [In, Out] ref int x, [In, Out] ref int y);

    [DispId(9)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void PixelToInkSpaceFromPoints([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hDC, [MarshalAs(UnmanagedType.Struct), In, Out] ref object Points);

    [DispId(10)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InkSpaceToPixelFromPoints([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hDC, [MarshalAs(UnmanagedType.Struct), In, Out] ref object Points);

    [DispId(11)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    InkRectangle Measure([MarshalAs(UnmanagedType.Interface), In] InkStrokes Strokes);

    [DispId(12)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    InkRectangle MeasureStroke([MarshalAs(UnmanagedType.Interface), In] IInkStrokeDisp Stroke, [MarshalAs(UnmanagedType.Interface), In] InkDrawingAttributes DrawingAttributes = null);

    [DispId(13)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Move([In] float HorizontalComponent, [In] float VerticalComponent);

    [DispId(14)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Rotate([In] float Degrees, [In] float x = 0.0f, [In] float y = 0.0f);

    [DispId(15)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void ScaleTransform([In] float HorizontalMultiplier, [In] float VerticalMultiplier, [In] bool ApplyOnPenWidth = true);
  }
}
