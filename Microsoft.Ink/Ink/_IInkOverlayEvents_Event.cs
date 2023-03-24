// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkOverlayEvents_Event
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [ComVisible(false)]
  [ComEventInterface(typeof (_IInkOverlayEvents), typeof (_IInkOverlayEvents_EventProvider))]
  [TypeLibType(16)]
  internal interface _IInkOverlayEvents_Event
  {
    event _IInkOverlayEvents_StrokeEventHandler Stroke;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Stroke(_IInkOverlayEvents_StrokeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Stroke(_IInkOverlayEvents_StrokeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorDown(_IInkOverlayEvents_CursorDownEventHandler A_1);

    event _IInkOverlayEvents_CursorDownEventHandler CursorDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorDown(_IInkOverlayEvents_CursorDownEventHandler A_1);

    event _IInkOverlayEvents_NewPacketsEventHandler NewPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_NewPackets(_IInkOverlayEvents_NewPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_NewPackets(_IInkOverlayEvents_NewPacketsEventHandler A_1);

    event _IInkOverlayEvents_DblClickEventHandler DblClick;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_DblClick(_IInkOverlayEvents_DblClickEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_DblClick(_IInkOverlayEvents_DblClickEventHandler A_1);

    event _IInkOverlayEvents_MouseMoveEventHandler MouseMove;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseMove(_IInkOverlayEvents_MouseMoveEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseMove(_IInkOverlayEvents_MouseMoveEventHandler A_1);

    event _IInkOverlayEvents_MouseDownEventHandler MouseDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseDown(_IInkOverlayEvents_MouseDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseDown(_IInkOverlayEvents_MouseDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseUp(_IInkOverlayEvents_MouseUpEventHandler A_1);

    event _IInkOverlayEvents_MouseUpEventHandler MouseUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseUp(_IInkOverlayEvents_MouseUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseWheel(_IInkOverlayEvents_MouseWheelEventHandler A_1);

    event _IInkOverlayEvents_MouseWheelEventHandler MouseWheel;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseWheel(_IInkOverlayEvents_MouseWheelEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Painting(_IInkOverlayEvents_PaintingEventHandler A_1);

    event _IInkOverlayEvents_PaintingEventHandler Painting;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Painting(_IInkOverlayEvents_PaintingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Painted(_IInkOverlayEvents_PaintedEventHandler A_1);

    event _IInkOverlayEvents_PaintedEventHandler Painted;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Painted(_IInkOverlayEvents_PaintedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionChanging(
      _IInkOverlayEvents_SelectionChangingEventHandler A_1);

    event _IInkOverlayEvents_SelectionChangingEventHandler SelectionChanging;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionChanging(
      _IInkOverlayEvents_SelectionChangingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionChanged(
      _IInkOverlayEvents_SelectionChangedEventHandler A_1);

    event _IInkOverlayEvents_SelectionChangedEventHandler SelectionChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionChanged(
      _IInkOverlayEvents_SelectionChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionMoving(_IInkOverlayEvents_SelectionMovingEventHandler A_1);

    event _IInkOverlayEvents_SelectionMovingEventHandler SelectionMoving;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionMoving(_IInkOverlayEvents_SelectionMovingEventHandler A_1);

    event _IInkOverlayEvents_SelectionMovedEventHandler SelectionMoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionMoved(_IInkOverlayEvents_SelectionMovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionMoved(_IInkOverlayEvents_SelectionMovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionResizing(
      _IInkOverlayEvents_SelectionResizingEventHandler A_1);

    event _IInkOverlayEvents_SelectionResizingEventHandler SelectionResizing;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionResizing(
      _IInkOverlayEvents_SelectionResizingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionResized(
      _IInkOverlayEvents_SelectionResizedEventHandler A_1);

    event _IInkOverlayEvents_SelectionResizedEventHandler SelectionResized;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionResized(
      _IInkOverlayEvents_SelectionResizedEventHandler A_1);

    event _IInkOverlayEvents_StrokesDeletingEventHandler StrokesDeleting;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_StrokesDeleting(_IInkOverlayEvents_StrokesDeletingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_StrokesDeleting(_IInkOverlayEvents_StrokesDeletingEventHandler A_1);

    event _IInkOverlayEvents_StrokesDeletedEventHandler StrokesDeleted;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_StrokesDeleted(_IInkOverlayEvents_StrokesDeletedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_StrokesDeleted(_IInkOverlayEvents_StrokesDeletedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_NewInAirPackets(_IInkOverlayEvents_NewInAirPacketsEventHandler A_1);

    event _IInkOverlayEvents_NewInAirPacketsEventHandler NewInAirPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_NewInAirPackets(_IInkOverlayEvents_NewInAirPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorButtonDown(
      _IInkOverlayEvents_CursorButtonDownEventHandler A_1);

    event _IInkOverlayEvents_CursorButtonDownEventHandler CursorButtonDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorButtonDown(
      _IInkOverlayEvents_CursorButtonDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorButtonUp(_IInkOverlayEvents_CursorButtonUpEventHandler A_1);

    event _IInkOverlayEvents_CursorButtonUpEventHandler CursorButtonUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorButtonUp(_IInkOverlayEvents_CursorButtonUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorInRange(_IInkOverlayEvents_CursorInRangeEventHandler A_1);

    event _IInkOverlayEvents_CursorInRangeEventHandler CursorInRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorInRange(_IInkOverlayEvents_CursorInRangeEventHandler A_1);

    event _IInkOverlayEvents_CursorOutOfRangeEventHandler CursorOutOfRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorOutOfRange(
      _IInkOverlayEvents_CursorOutOfRangeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorOutOfRange(
      _IInkOverlayEvents_CursorOutOfRangeEventHandler A_1);

    event _IInkOverlayEvents_SystemGestureEventHandler SystemGesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SystemGesture(_IInkOverlayEvents_SystemGestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SystemGesture(_IInkOverlayEvents_SystemGestureEventHandler A_1);

    event _IInkOverlayEvents_GestureEventHandler Gesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Gesture(_IInkOverlayEvents_GestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Gesture(_IInkOverlayEvents_GestureEventHandler A_1);

    event _IInkOverlayEvents_TabletAddedEventHandler TabletAdded;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_TabletAdded(_IInkOverlayEvents_TabletAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_TabletAdded(_IInkOverlayEvents_TabletAddedEventHandler A_1);

    event _IInkOverlayEvents_TabletRemovedEventHandler TabletRemoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_TabletRemoved(_IInkOverlayEvents_TabletRemovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_TabletRemoved(_IInkOverlayEvents_TabletRemovedEventHandler A_1);
  }
}
