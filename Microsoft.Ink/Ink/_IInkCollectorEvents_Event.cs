// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkCollectorEvents_Event
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [ComEventInterface(typeof (_IInkCollectorEvents), typeof (_IInkCollectorEvents_EventProvider))]
  [ComVisible(false)]
  [TypeLibType(16)]
  internal interface _IInkCollectorEvents_Event
  {
    event _IInkCollectorEvents_StrokeEventHandler Stroke;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Stroke(_IInkCollectorEvents_StrokeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Stroke(_IInkCollectorEvents_StrokeEventHandler A_1);

    event _IInkCollectorEvents_CursorDownEventHandler CursorDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorDown(_IInkCollectorEvents_CursorDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorDown(_IInkCollectorEvents_CursorDownEventHandler A_1);

    event _IInkCollectorEvents_NewPacketsEventHandler NewPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_NewPackets(_IInkCollectorEvents_NewPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_NewPackets(_IInkCollectorEvents_NewPacketsEventHandler A_1);

    event _IInkCollectorEvents_DblClickEventHandler DblClick;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_DblClick(_IInkCollectorEvents_DblClickEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_DblClick(_IInkCollectorEvents_DblClickEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseMove(_IInkCollectorEvents_MouseMoveEventHandler A_1);

    event _IInkCollectorEvents_MouseMoveEventHandler MouseMove;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseMove(_IInkCollectorEvents_MouseMoveEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseDown(_IInkCollectorEvents_MouseDownEventHandler A_1);

    event _IInkCollectorEvents_MouseDownEventHandler MouseDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseDown(_IInkCollectorEvents_MouseDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseUp(_IInkCollectorEvents_MouseUpEventHandler A_1);

    event _IInkCollectorEvents_MouseUpEventHandler MouseUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseUp(_IInkCollectorEvents_MouseUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_MouseWheel(_IInkCollectorEvents_MouseWheelEventHandler A_1);

    event _IInkCollectorEvents_MouseWheelEventHandler MouseWheel;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_MouseWheel(_IInkCollectorEvents_MouseWheelEventHandler A_1);

    event _IInkCollectorEvents_NewInAirPacketsEventHandler NewInAirPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_NewInAirPackets(
      _IInkCollectorEvents_NewInAirPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_NewInAirPackets(
      _IInkCollectorEvents_NewInAirPacketsEventHandler A_1);

    event _IInkCollectorEvents_CursorButtonDownEventHandler CursorButtonDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorButtonDown(
      _IInkCollectorEvents_CursorButtonDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorButtonDown(
      _IInkCollectorEvents_CursorButtonDownEventHandler A_1);

    event _IInkCollectorEvents_CursorButtonUpEventHandler CursorButtonUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorButtonUp(
      _IInkCollectorEvents_CursorButtonUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorButtonUp(
      _IInkCollectorEvents_CursorButtonUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorInRange(_IInkCollectorEvents_CursorInRangeEventHandler A_1);

    event _IInkCollectorEvents_CursorInRangeEventHandler CursorInRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorInRange(_IInkCollectorEvents_CursorInRangeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_CursorOutOfRange(
      _IInkCollectorEvents_CursorOutOfRangeEventHandler A_1);

    event _IInkCollectorEvents_CursorOutOfRangeEventHandler CursorOutOfRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_CursorOutOfRange(
      _IInkCollectorEvents_CursorOutOfRangeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_SystemGesture(_IInkCollectorEvents_SystemGestureEventHandler A_1);

    event _IInkCollectorEvents_SystemGestureEventHandler SystemGesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_SystemGesture(_IInkCollectorEvents_SystemGestureEventHandler A_1);

    event _IInkCollectorEvents_GestureEventHandler Gesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Gesture(_IInkCollectorEvents_GestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Gesture(_IInkCollectorEvents_GestureEventHandler A_1);

    event _IInkCollectorEvents_TabletAddedEventHandler TabletAdded;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_TabletAdded(_IInkCollectorEvents_TabletAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_TabletAdded(_IInkCollectorEvents_TabletAddedEventHandler A_1);

    event _IInkCollectorEvents_TabletRemovedEventHandler TabletRemoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_TabletRemoved(_IInkCollectorEvents_TabletRemovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_TabletRemoved(_IInkCollectorEvents_TabletRemovedEventHandler A_1);
  }
}
