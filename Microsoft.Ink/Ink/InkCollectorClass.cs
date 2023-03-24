// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkCollectorClass
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [ClassInterface(0)]
  [ComSourceInterfaces("Microsoft.Ink._IInkCollectorEvents\0\0")]
  [SuppressUnmanagedCodeSecurity]
  [Guid("43FB1553-AD74-4EE8-88E4-3E6DAAC915DB")]
  [TypeLibType(2)]
  [ComImport]
  internal class InkCollectorClass : IInkCollector, InkCollectorPrivate, _IInkCollectorEvents_Event
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern InkCollectorClass();

    [DispId(2)]
    [ComAliasName("Microsoft.Ink.LONG_PTR")]
    public virtual extern long hWnd { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.LONG_PTR")] get; [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("Microsoft.Ink.LONG_PTR"), In] set; }

    [DispId(1)]
    public virtual extern bool Enabled { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(5)]
    public virtual extern InkDrawingAttributes DefaultDrawingAttributes { [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; [DispId(5), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: MarshalAs(UnmanagedType.Interface), In] set; }

    [DispId(6)]
    public virtual extern InkRenderer Renderer { [DispId(6), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; [DispId(6), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: MarshalAs(UnmanagedType.Interface), In] set; }

    [DispId(7)]
    public virtual extern InkDisp Ink { [DispId(7), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; [DispId(7), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: MarshalAs(UnmanagedType.Interface), In] set; }

    [DispId(8)]
    public virtual extern bool AutoRedraw { [DispId(8), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(8), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(9)]
    public virtual extern bool CollectingInk { [DispId(9), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(28)]
    public virtual extern InkCollectionMode CollectionMode { [DispId(28), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(28), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(31)]
    public virtual extern bool DynamicRendering { [DispId(31), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(31), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(32)]
    public virtual extern object DesiredPacketDescription { [DispId(32), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Struct)] get; [DispId(32), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: MarshalAs(UnmanagedType.Struct), In] set; }

    [ComAliasName("stdole.IPictureDisp")]
    [DispId(35)]
    public virtual extern object MouseIcon { [DispId(35), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("stdole.IPictureDisp"), MarshalAs(UnmanagedType.Interface)] get; [DispId(35), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("stdole.IPictureDisp"), MarshalAs(UnmanagedType.Interface), In] set; }

    [DispId(35)]
    [SpecialName]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void let_MouseIcon([ComAliasName("stdole.IPictureDisp"), MarshalAs(UnmanagedType.Interface), In] object MouseIcon);

    [DispId(36)]
    public virtual extern InkMousePointer MousePointer { [DispId(36), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(36), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(20)]
    public virtual extern IInkCursors Cursors { [DispId(20), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(21)]
    public virtual extern int MarginX { [DispId(21), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(21), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(22)]
    public virtual extern int MarginY { [DispId(22), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(22), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(25)]
    public virtual extern IInkTablet Tablet { [DispId(25), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(38)]
    public virtual extern bool SupportHighContrastInk { [DispId(38), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(38), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(29)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void SetGestureStatus([In] InkApplicationGesture Gesture, [In] bool Listen);

    [DispId(30)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern bool GetGestureStatus([In] InkApplicationGesture Gesture);

    [DispId(24)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void GetWindowInputRectangle([MarshalAs(UnmanagedType.Interface), In, Out] ref InkRectangle WindowInputRectangle);

    [DispId(23)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void SetWindowInputRectangle([MarshalAs(UnmanagedType.Interface), In] InkRectangle WindowInputRectangle);

    [DispId(26)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void SetAllTabletsMode([In] bool UseMouseForInput = true);

    [DispId(27)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void SetSingleTabletIntegratedMode([MarshalAs(UnmanagedType.Interface), In] IInkTablet Tablet);

    [DispId(11)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern bool GetEventInterest([In] InkCollectorEventInterest EventId);

    [DispId(10)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void SetEventInterest([In] InkCollectorEventInterest EventId, [In] bool Listen);

    public virtual extern event _IInkCollectorEvents_StrokeEventHandler Stroke;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Stroke(_IInkCollectorEvents_StrokeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Stroke(_IInkCollectorEvents_StrokeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorDown(_IInkCollectorEvents_CursorDownEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_CursorDownEventHandler CursorDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorDown(_IInkCollectorEvents_CursorDownEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_NewPacketsEventHandler NewPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_NewPackets(_IInkCollectorEvents_NewPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_NewPackets(_IInkCollectorEvents_NewPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_DblClick(_IInkCollectorEvents_DblClickEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_DblClickEventHandler DblClick;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_DblClick(_IInkCollectorEvents_DblClickEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_MouseMoveEventHandler MouseMove;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseMove(_IInkCollectorEvents_MouseMoveEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseMove(_IInkCollectorEvents_MouseMoveEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseDown(_IInkCollectorEvents_MouseDownEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_MouseDownEventHandler MouseDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseDown(_IInkCollectorEvents_MouseDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseUp(_IInkCollectorEvents_MouseUpEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_MouseUpEventHandler MouseUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseUp(_IInkCollectorEvents_MouseUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseWheel(_IInkCollectorEvents_MouseWheelEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_MouseWheelEventHandler MouseWheel;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseWheel(_IInkCollectorEvents_MouseWheelEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_NewInAirPacketsEventHandler NewInAirPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_NewInAirPackets(
      _IInkCollectorEvents_NewInAirPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_NewInAirPackets(
      _IInkCollectorEvents_NewInAirPacketsEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_CursorButtonDownEventHandler CursorButtonDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorButtonDown(
      _IInkCollectorEvents_CursorButtonDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorButtonDown(
      _IInkCollectorEvents_CursorButtonDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorButtonUp(
      _IInkCollectorEvents_CursorButtonUpEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_CursorButtonUpEventHandler CursorButtonUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorButtonUp(
      _IInkCollectorEvents_CursorButtonUpEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_CursorInRangeEventHandler CursorInRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorInRange(_IInkCollectorEvents_CursorInRangeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorInRange(
      _IInkCollectorEvents_CursorInRangeEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_CursorOutOfRangeEventHandler CursorOutOfRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorOutOfRange(
      _IInkCollectorEvents_CursorOutOfRangeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorOutOfRange(
      _IInkCollectorEvents_CursorOutOfRangeEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_SystemGestureEventHandler SystemGesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SystemGesture(_IInkCollectorEvents_SystemGestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SystemGesture(
      _IInkCollectorEvents_SystemGestureEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_GestureEventHandler Gesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Gesture(_IInkCollectorEvents_GestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Gesture(_IInkCollectorEvents_GestureEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_TabletAddedEventHandler TabletAdded;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_TabletAdded(_IInkCollectorEvents_TabletAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_TabletAdded(_IInkCollectorEvents_TabletAddedEventHandler A_1);

    public virtual extern event _IInkCollectorEvents_TabletRemovedEventHandler TabletRemoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_TabletRemoved(_IInkCollectorEvents_TabletRemovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_TabletRemoved(
      _IInkCollectorEvents_TabletRemovedEventHandler A_1);
  }
}
