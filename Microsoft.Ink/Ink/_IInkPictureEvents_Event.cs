// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkPictureEvents_Event
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [ComVisible(false)]
  [TypeLibType(16)]
  [ComEventInterface(typeof (_IInkPictureEvents), typeof (_IInkPictureEvents_EventProvider))]
  internal interface _IInkPictureEvents_Event
  {
    event _IInkPictureEvents_StrokeEventHandler Stroke;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Stroke(_IInkPictureEvents_StrokeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Stroke(_IInkPictureEvents_StrokeEventHandler A_1);

    event _IInkPictureEvents_CursorDownEventHandler CursorDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorDown(_IInkPictureEvents_CursorDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorDown(_IInkPictureEvents_CursorDownEventHandler A_1);

    event _IInkPictureEvents_NewPacketsEventHandler NewPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_NewPackets(_IInkPictureEvents_NewPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_NewPackets(_IInkPictureEvents_NewPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_DblClick(_IInkPictureEvents_DblClickEventHandler A_1);

    event _IInkPictureEvents_DblClickEventHandler DblClick;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_DblClick(_IInkPictureEvents_DblClickEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseMove(_IInkPictureEvents_MouseMoveEventHandler A_1);

    event _IInkPictureEvents_MouseMoveEventHandler MouseMove;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseMove(_IInkPictureEvents_MouseMoveEventHandler A_1);

    event _IInkPictureEvents_MouseDownEventHandler MouseDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseDown(_IInkPictureEvents_MouseDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseDown(_IInkPictureEvents_MouseDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseUp(_IInkPictureEvents_MouseUpEventHandler A_1);

    event _IInkPictureEvents_MouseUpEventHandler MouseUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseUp(_IInkPictureEvents_MouseUpEventHandler A_1);

    event _IInkPictureEvents_MouseWheelEventHandler MouseWheel;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseWheel(_IInkPictureEvents_MouseWheelEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseWheel(_IInkPictureEvents_MouseWheelEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Painting(_IInkPictureEvents_PaintingEventHandler A_1);

    event _IInkPictureEvents_PaintingEventHandler Painting;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Painting(_IInkPictureEvents_PaintingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Painted(_IInkPictureEvents_PaintedEventHandler A_1);

    event _IInkPictureEvents_PaintedEventHandler Painted;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Painted(_IInkPictureEvents_PaintedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionChanging(
      _IInkPictureEvents_SelectionChangingEventHandler A_1);

    event _IInkPictureEvents_SelectionChangingEventHandler SelectionChanging;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionChanging(
      _IInkPictureEvents_SelectionChangingEventHandler A_1);

    event _IInkPictureEvents_SelectionChangedEventHandler SelectionChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionChanged(
      _IInkPictureEvents_SelectionChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionChanged(
      _IInkPictureEvents_SelectionChangedEventHandler A_1);

    event _IInkPictureEvents_SelectionMovingEventHandler SelectionMoving;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionMoving(_IInkPictureEvents_SelectionMovingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionMoving(_IInkPictureEvents_SelectionMovingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionMoved(_IInkPictureEvents_SelectionMovedEventHandler A_1);

    event _IInkPictureEvents_SelectionMovedEventHandler SelectionMoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionMoved(_IInkPictureEvents_SelectionMovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionResizing(
      _IInkPictureEvents_SelectionResizingEventHandler A_1);

    event _IInkPictureEvents_SelectionResizingEventHandler SelectionResizing;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionResizing(
      _IInkPictureEvents_SelectionResizingEventHandler A_1);

    event _IInkPictureEvents_SelectionResizedEventHandler SelectionResized;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SelectionResized(
      _IInkPictureEvents_SelectionResizedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SelectionResized(
      _IInkPictureEvents_SelectionResizedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_StrokesDeleting(_IInkPictureEvents_StrokesDeletingEventHandler A_1);

    event _IInkPictureEvents_StrokesDeletingEventHandler StrokesDeleting;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_StrokesDeleting(_IInkPictureEvents_StrokesDeletingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_StrokesDeleted(_IInkPictureEvents_StrokesDeletedEventHandler A_1);

    event _IInkPictureEvents_StrokesDeletedEventHandler StrokesDeleted;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_StrokesDeleted(_IInkPictureEvents_StrokesDeletedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseEnter(_IInkPictureEvents_MouseEnterEventHandler A_1);

    event _IInkPictureEvents_MouseEnterEventHandler MouseEnter;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseEnter(_IInkPictureEvents_MouseEnterEventHandler A_1);

    event _IInkPictureEvents_ClickEventHandler Click;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Click(_IInkPictureEvents_ClickEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Click(_IInkPictureEvents_ClickEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseHover(_IInkPictureEvents_MouseHoverEventHandler A_1);

    event _IInkPictureEvents_MouseHoverEventHandler MouseHover;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseHover(_IInkPictureEvents_MouseHoverEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseLeave(_IInkPictureEvents_MouseLeaveEventHandler A_1);

    event _IInkPictureEvents_MouseLeaveEventHandler MouseLeave;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseLeave(_IInkPictureEvents_MouseLeaveEventHandler A_1);

    event _IInkPictureEvents_KeyDownEventHandler KeyDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_KeyDown(_IInkPictureEvents_KeyDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_KeyDown(_IInkPictureEvents_KeyDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_KeyUp(_IInkPictureEvents_KeyUpEventHandler A_1);

    event _IInkPictureEvents_KeyUpEventHandler KeyUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_KeyUp(_IInkPictureEvents_KeyUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_KeyPress(_IInkPictureEvents_KeyPressEventHandler A_1);

    event _IInkPictureEvents_KeyPressEventHandler KeyPress;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_KeyPress(_IInkPictureEvents_KeyPressEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SizeModeChanged(_IInkPictureEvents_SizeModeChangedEventHandler A_1);

    event _IInkPictureEvents_SizeModeChangedEventHandler SizeModeChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SizeModeChanged(_IInkPictureEvents_SizeModeChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SystemColorsChanged(
      _IInkPictureEvents_SystemColorsChangedEventHandler A_1);

    event _IInkPictureEvents_SystemColorsChangedEventHandler SystemColorsChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SystemColorsChanged(
      _IInkPictureEvents_SystemColorsChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Resize(_IInkPictureEvents_ResizeEventHandler A_1);

    event _IInkPictureEvents_ResizeEventHandler Resize;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Resize(_IInkPictureEvents_ResizeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SizeChanged(_IInkPictureEvents_SizeChangedEventHandler A_1);

    event _IInkPictureEvents_SizeChangedEventHandler SizeChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SizeChanged(_IInkPictureEvents_SizeChangedEventHandler A_1);

    event _IInkPictureEvents_StyleChangedEventHandler StyleChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_StyleChanged(_IInkPictureEvents_StyleChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_StyleChanged(_IInkPictureEvents_StyleChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_ChangeUICues(_IInkPictureEvents_ChangeUICuesEventHandler A_1);

    event _IInkPictureEvents_ChangeUICuesEventHandler ChangeUICues;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_ChangeUICues(_IInkPictureEvents_ChangeUICuesEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_NewInAirPackets(_IInkPictureEvents_NewInAirPacketsEventHandler A_1);

    event _IInkPictureEvents_NewInAirPacketsEventHandler NewInAirPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_NewInAirPackets(_IInkPictureEvents_NewInAirPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorButtonDown(
      _IInkPictureEvents_CursorButtonDownEventHandler A_1);

    event _IInkPictureEvents_CursorButtonDownEventHandler CursorButtonDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorButtonDown(
      _IInkPictureEvents_CursorButtonDownEventHandler A_1);

    event _IInkPictureEvents_CursorButtonUpEventHandler CursorButtonUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorButtonUp(_IInkPictureEvents_CursorButtonUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorButtonUp(_IInkPictureEvents_CursorButtonUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorInRange(_IInkPictureEvents_CursorInRangeEventHandler A_1);

    event _IInkPictureEvents_CursorInRangeEventHandler CursorInRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorInRange(_IInkPictureEvents_CursorInRangeEventHandler A_1);

    event _IInkPictureEvents_CursorOutOfRangeEventHandler CursorOutOfRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorOutOfRange(
      _IInkPictureEvents_CursorOutOfRangeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorOutOfRange(
      _IInkPictureEvents_CursorOutOfRangeEventHandler A_1);

    event _IInkPictureEvents_SystemGestureEventHandler SystemGesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SystemGesture(_IInkPictureEvents_SystemGestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SystemGesture(_IInkPictureEvents_SystemGestureEventHandler A_1);

    event _IInkPictureEvents_GestureEventHandler Gesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Gesture(_IInkPictureEvents_GestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Gesture(_IInkPictureEvents_GestureEventHandler A_1);

    event _IInkPictureEvents_TabletAddedEventHandler TabletAdded;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_TabletAdded(_IInkPictureEvents_TabletAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_TabletAdded(_IInkPictureEvents_TabletAddedEventHandler A_1);

    event _IInkPictureEvents_TabletRemovedEventHandler TabletRemoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_TabletRemoved(_IInkPictureEvents_TabletRemovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_TabletRemoved(_IInkPictureEvents_TabletRemovedEventHandler A_1);
  }
}
