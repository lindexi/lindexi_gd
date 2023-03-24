// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkOverlayEvents
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [Guid("31179B69-E563-489E-B16F-712F1E8A0651")]
  [TypeLibType(4096)]
  [InterfaceType(2)]
  [ComImport]
  internal interface _IInkOverlayEvents
  {
    [DispId(1)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Stroke([MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor, [MarshalAs(UnmanagedType.Interface), In] IInkStrokeDisp Stroke, [In, Out] ref bool Cancel);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void CursorDown([MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor, [MarshalAs(UnmanagedType.Interface), In] IInkStrokeDisp Stroke);

    [DispId(3)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void NewPackets(
      [MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor,
      [MarshalAs(UnmanagedType.Interface), In] IInkStrokeDisp Stroke,
      [In] int PacketCount,
      [MarshalAs(UnmanagedType.Struct), In, Out] ref object PacketData);

    [DispId(25)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void DblClick([In, Out] ref bool Cancel);

    [DispId(31)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void MouseMove(
      [In] InkMouseButton Button,
      [In] InkShiftKeyModifierFlags Shift,
      [In] int pX,
      [In] int pY,
      [In, Out] ref bool Cancel);

    [DispId(27)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void MouseDown(
      [In] InkMouseButton Button,
      [In] InkShiftKeyModifierFlags Shift,
      [In] int pX,
      [In] int pY,
      [In, Out] ref bool Cancel);

    [DispId(32)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void MouseUp(
      [In] InkMouseButton Button,
      [In] InkShiftKeyModifierFlags Shift,
      [In] int pX,
      [In] int pY,
      [In, Out] ref bool Cancel);

    [DispId(33)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void MouseWheel(
      [In] InkMouseButton Button,
      [In] InkShiftKeyModifierFlags Shift,
      [In] int Delta,
      [In] int x,
      [In] int y,
      [In, Out] ref bool Cancel);

    [DispId(13)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Painting([In] int hDC, [MarshalAs(UnmanagedType.Interface), In] InkRectangle Rect, [In, Out] ref bool Allow);

    [DispId(14)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Painted([In] int hDC, [MarshalAs(UnmanagedType.Interface), In] InkRectangle Rect);

    [DispId(15)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SelectionChanging([MarshalAs(UnmanagedType.Interface), In] InkStrokes NewSelection);

    [DispId(16)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SelectionChanged();

    [DispId(17)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SelectionMoving([MarshalAs(UnmanagedType.Interface), In] InkRectangle CurSelectionRect);

    [DispId(18)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SelectionMoved([MarshalAs(UnmanagedType.Interface), In] InkRectangle OldSelectionRect);

    [DispId(19)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SelectionResizing([MarshalAs(UnmanagedType.Interface), In] InkRectangle CurSelectionRect);

    [DispId(20)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SelectionResized([MarshalAs(UnmanagedType.Interface), In] InkRectangle OldSelectionRect);

    [DispId(21)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void StrokesDeleting([MarshalAs(UnmanagedType.Interface), In] InkStrokes Strokes);

    [DispId(22)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void StrokesDeleted();

    [DispId(4)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void NewInAirPackets([MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor, [In] int lPacketCount, [MarshalAs(UnmanagedType.Struct), In, Out] ref object PacketData);

    [DispId(5)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void CursorButtonDown([MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor, [MarshalAs(UnmanagedType.Interface), In] IInkCursorButton Button);

    [DispId(6)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void CursorButtonUp([MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor, [MarshalAs(UnmanagedType.Interface), In] IInkCursorButton Button);

    [DispId(7)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void CursorInRange([MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor, [In] bool NewCursor, [MarshalAs(UnmanagedType.Struct), In] object ButtonsState);

    [DispId(8)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void CursorOutOfRange([MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor);

    [DispId(9)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SystemGesture(
      [MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor,
      [In] InkSystemGesture Id,
      [In] int x,
      [In] int y,
      [In] int Modifier,
      [MarshalAs(UnmanagedType.BStr), In] string Character,
      [In] int CursorMode);

    [DispId(10)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Gesture([MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor, [MarshalAs(UnmanagedType.Interface), In] InkStrokes Strokes, [MarshalAs(UnmanagedType.Struct), In] object Gestures, [In, Out] ref bool Cancel);

    [DispId(11)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void TabletAdded([MarshalAs(UnmanagedType.Interface), In] IInkTablet Tablet);

    [DispId(12)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void TabletRemoved([In] int TabletId);
  }
}
