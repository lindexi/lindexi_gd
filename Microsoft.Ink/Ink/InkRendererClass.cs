// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkRendererClass
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [ClassInterface(0)]
  [TypeLibType(2)]
  [Guid("9C1CC6E4-D7EB-4EEB-9091-15A7C8791ED9")]
  [ComImport]
  internal class InkRendererClass : IInkRenderer, InkRenderer
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern InkRendererClass();

    [DispId(1)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void GetViewTransform([MarshalAs(UnmanagedType.Interface), In] InkTransform ViewTransform);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void SetViewTransform([MarshalAs(UnmanagedType.Interface), In] InkTransform ViewTransform);

    [DispId(3)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void GetObjectTransform([MarshalAs(UnmanagedType.Interface), In] InkTransform ObjectTransform);

    [DispId(4)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void SetObjectTransform([MarshalAs(UnmanagedType.Interface), In] InkTransform ObjectTransform);

    [DispId(5)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Draw([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hDC, [MarshalAs(UnmanagedType.Interface), In] InkStrokes Strokes);

    [DispId(6)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void DrawStroke(
      [ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hDC,
      [MarshalAs(UnmanagedType.Interface), In] IInkStrokeDisp Stroke,
      [MarshalAs(UnmanagedType.Interface), In] InkDrawingAttributes DrawingAttributes = null);

    [DispId(7)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void PixelToInkSpace([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hDC, [In, Out] ref int x, [In, Out] ref int y);

    [DispId(8)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void InkSpaceToPixel([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hdcDisplay, [In, Out] ref int x, [In, Out] ref int y);

    [DispId(9)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void PixelToInkSpaceFromPoints([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hDC, [MarshalAs(UnmanagedType.Struct), In, Out] ref object Points);

    [DispId(10)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void InkSpaceToPixelFromPoints([ComAliasName("Microsoft.Ink.LONG_PTR"), In] long hDC, [MarshalAs(UnmanagedType.Struct), In, Out] ref object Points);

    [DispId(11)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkRectangle Measure([MarshalAs(UnmanagedType.Interface), In] InkStrokes Strokes);

    [DispId(12)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkRectangle MeasureStroke(
      [MarshalAs(UnmanagedType.Interface), In] IInkStrokeDisp Stroke,
      [MarshalAs(UnmanagedType.Interface), In] InkDrawingAttributes DrawingAttributes = null);

    [DispId(13)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Move([In] float HorizontalComponent, [In] float VerticalComponent);

    [DispId(14)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Rotate([In] float Degrees, [In] float x = 0.0f, [In] float y = 0.0f);

    [DispId(15)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void ScaleTransform(
      [In] float HorizontalMultiplier,
      [In] float VerticalMultiplier,
      [In] bool ApplyOnPenWidth = true);
  }
}
