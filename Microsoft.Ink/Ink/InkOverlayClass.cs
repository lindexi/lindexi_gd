// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkOverlayClass
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [ComSourceInterfaces("Microsoft.Ink._IInkOverlayEvents\0\0")]
  [Guid("65D00646-CDE3-4A88-9163-6769F0F1A97D")]
  [ClassInterface(0)]
  [TypeLibType(2)]
  [SuppressUnmanagedCodeSecurity]
  [ComImport]
  internal class InkOverlayClass : IInkOverlay, InkOverlayPrivate, _IInkOverlayEvents_Event
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern InkOverlayClass();

    [ComAliasName("Microsoft.Ink.LONG_PTR")]
    [DispId(2)]
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

    [DispId(12)]
    public virtual extern InkOverlayEditingModePrivate EditingMode { [DispId(12), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(12), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(13)]
    public virtual extern InkStrokes Selection { [DispId(13), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; [DispId(13), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: MarshalAs(UnmanagedType.Interface), In] set; }

    [DispId(33)]
    public virtual extern InkOverlayEraserModePrivate EraserMode { [DispId(33), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(33), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(34)]
    public virtual extern int EraserWidth { [DispId(34), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(34), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(14)]
    public virtual extern InkOverlayAttachModePrivate AttachMode { [DispId(14), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(14), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

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

    [DispId(39)]
    public virtual extern bool SupportHighContrastSelectionUI { [DispId(39), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(39), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(15)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern SelectionHitResultPrivate HitTestSelection([In] int x, [In] int y);

    [DispId(16)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Draw([MarshalAs(UnmanagedType.Interface), In] InkRectangle Rect);

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

    public virtual extern event _IInkOverlayEvents_StrokeEventHandler Stroke;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Stroke(_IInkOverlayEvents_StrokeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Stroke(_IInkOverlayEvents_StrokeEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_CursorDownEventHandler CursorDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorDown(_IInkOverlayEvents_CursorDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorDown(_IInkOverlayEvents_CursorDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_NewPackets(_IInkOverlayEvents_NewPacketsEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_NewPacketsEventHandler NewPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_NewPackets(_IInkOverlayEvents_NewPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_DblClick(_IInkOverlayEvents_DblClickEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_DblClickEventHandler DblClick;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_DblClick(_IInkOverlayEvents_DblClickEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_MouseMoveEventHandler MouseMove;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseMove(_IInkOverlayEvents_MouseMoveEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseMove(_IInkOverlayEvents_MouseMoveEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseDown(_IInkOverlayEvents_MouseDownEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_MouseDownEventHandler MouseDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseDown(_IInkOverlayEvents_MouseDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseUp(_IInkOverlayEvents_MouseUpEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_MouseUpEventHandler MouseUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseUp(_IInkOverlayEvents_MouseUpEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_MouseWheelEventHandler MouseWheel;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseWheel(_IInkOverlayEvents_MouseWheelEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseWheel(_IInkOverlayEvents_MouseWheelEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Painting(_IInkOverlayEvents_PaintingEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_PaintingEventHandler Painting;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Painting(_IInkOverlayEvents_PaintingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Painted(_IInkOverlayEvents_PaintedEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_PaintedEventHandler Painted;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Painted(_IInkOverlayEvents_PaintedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionChanging(
      _IInkOverlayEvents_SelectionChangingEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_SelectionChangingEventHandler SelectionChanging;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionChanging(
      _IInkOverlayEvents_SelectionChangingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionChanged(
      _IInkOverlayEvents_SelectionChangedEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_SelectionChangedEventHandler SelectionChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionChanged(
      _IInkOverlayEvents_SelectionChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionMoving(
      _IInkOverlayEvents_SelectionMovingEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_SelectionMovingEventHandler SelectionMoving;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionMoving(
      _IInkOverlayEvents_SelectionMovingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionMoved(_IInkOverlayEvents_SelectionMovedEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_SelectionMovedEventHandler SelectionMoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionMoved(
      _IInkOverlayEvents_SelectionMovedEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_SelectionResizingEventHandler SelectionResizing;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionResizing(
      _IInkOverlayEvents_SelectionResizingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionResizing(
      _IInkOverlayEvents_SelectionResizingEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_SelectionResizedEventHandler SelectionResized;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionResized(
      _IInkOverlayEvents_SelectionResizedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionResized(
      _IInkOverlayEvents_SelectionResizedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_StrokesDeleting(
      _IInkOverlayEvents_StrokesDeletingEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_StrokesDeletingEventHandler StrokesDeleting;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_StrokesDeleting(
      _IInkOverlayEvents_StrokesDeletingEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_StrokesDeletedEventHandler StrokesDeleted;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_StrokesDeleted(_IInkOverlayEvents_StrokesDeletedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_StrokesDeleted(
      _IInkOverlayEvents_StrokesDeletedEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_NewInAirPacketsEventHandler NewInAirPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_NewInAirPackets(
      _IInkOverlayEvents_NewInAirPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_NewInAirPackets(
      _IInkOverlayEvents_NewInAirPacketsEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_CursorButtonDownEventHandler CursorButtonDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorButtonDown(
      _IInkOverlayEvents_CursorButtonDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorButtonDown(
      _IInkOverlayEvents_CursorButtonDownEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_CursorButtonUpEventHandler CursorButtonUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorButtonUp(_IInkOverlayEvents_CursorButtonUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorButtonUp(
      _IInkOverlayEvents_CursorButtonUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorInRange(_IInkOverlayEvents_CursorInRangeEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_CursorInRangeEventHandler CursorInRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorInRange(_IInkOverlayEvents_CursorInRangeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorOutOfRange(
      _IInkOverlayEvents_CursorOutOfRangeEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_CursorOutOfRangeEventHandler CursorOutOfRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorOutOfRange(
      _IInkOverlayEvents_CursorOutOfRangeEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_SystemGestureEventHandler SystemGesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SystemGesture(_IInkOverlayEvents_SystemGestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SystemGesture(_IInkOverlayEvents_SystemGestureEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_GestureEventHandler Gesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Gesture(_IInkOverlayEvents_GestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Gesture(_IInkOverlayEvents_GestureEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_TabletAddedEventHandler TabletAdded;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_TabletAdded(_IInkOverlayEvents_TabletAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_TabletAdded(_IInkOverlayEvents_TabletAddedEventHandler A_1);

    public virtual extern event _IInkOverlayEvents_TabletRemovedEventHandler TabletRemoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_TabletRemoved(_IInkOverlayEvents_TabletRemovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_TabletRemoved(_IInkOverlayEvents_TabletRemovedEventHandler A_1);
  }
}
