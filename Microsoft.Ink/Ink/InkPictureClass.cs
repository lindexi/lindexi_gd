// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkPictureClass
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [TypeLibType(2)]
  [SuppressUnmanagedCodeSecurity]
  [Guid("04A1E553-FE36-4FDE-865E-344194E69424")]
  [ClassInterface(0)]
  [ComSourceInterfaces("Microsoft.Ink._IInkPictureEvents\0\0")]
  [ComImport]
  internal class InkPictureClass : IInkPicture, InkPicturePrivate, _IInkPictureEvents_Event
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern InkPictureClass();

    [ComAliasName("Microsoft.Ink.LONG_PTR")]
    [DispId(2)]
    public virtual extern long hWnd { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.LONG_PTR")] get; }

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

    [DispId(35)]
    [ComAliasName("stdole.IPictureDisp")]
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

    [DispId(17)]
    [ComAliasName("stdole.IPictureDisp")]
    public virtual extern object Picture { [DispId(17), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("stdole.IPictureDisp"), MarshalAs(UnmanagedType.Interface), In] set; [DispId(17), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("stdole.IPictureDisp"), MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(17)]
    [SpecialName]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void let_Picture([ComAliasName("stdole.IPictureDisp"), MarshalAs(UnmanagedType.Interface), In] object ppPicture);

    [DispId(18)]
    public virtual extern InkPictureSizeMode SizeMode { [DispId(18), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; [DispId(18), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(19)]
    [ComAliasName("stdole.OLE_COLOR")]
    public virtual extern uint BackColor { [DispId(19), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("stdole.OLE_COLOR"), In] set; [DispId(19), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("stdole.OLE_COLOR")] get; }

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

    [DispId(37)]
    public virtual extern bool InkEnabled { [DispId(37), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(37), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(-514)]
    public virtual extern bool Enabled { [DispId(-514), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(-514), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Stroke(_IInkPictureEvents_StrokeEventHandler A_1);

    public virtual extern event _IInkPictureEvents_StrokeEventHandler Stroke;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Stroke(_IInkPictureEvents_StrokeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorDown(_IInkPictureEvents_CursorDownEventHandler A_1);

    public virtual extern event _IInkPictureEvents_CursorDownEventHandler CursorDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorDown(_IInkPictureEvents_CursorDownEventHandler A_1);

    public virtual extern event _IInkPictureEvents_NewPacketsEventHandler NewPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_NewPackets(_IInkPictureEvents_NewPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_NewPackets(_IInkPictureEvents_NewPacketsEventHandler A_1);

    public virtual extern event _IInkPictureEvents_DblClickEventHandler DblClick;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_DblClick(_IInkPictureEvents_DblClickEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_DblClick(_IInkPictureEvents_DblClickEventHandler A_1);

    public virtual extern event _IInkPictureEvents_MouseMoveEventHandler MouseMove;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseMove(_IInkPictureEvents_MouseMoveEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseMove(_IInkPictureEvents_MouseMoveEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseDown(_IInkPictureEvents_MouseDownEventHandler A_1);

    public virtual extern event _IInkPictureEvents_MouseDownEventHandler MouseDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseDown(_IInkPictureEvents_MouseDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseUp(_IInkPictureEvents_MouseUpEventHandler A_1);

    public virtual extern event _IInkPictureEvents_MouseUpEventHandler MouseUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseUp(_IInkPictureEvents_MouseUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseWheel(_IInkPictureEvents_MouseWheelEventHandler A_1);

    public virtual extern event _IInkPictureEvents_MouseWheelEventHandler MouseWheel;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseWheel(_IInkPictureEvents_MouseWheelEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Painting(_IInkPictureEvents_PaintingEventHandler A_1);

    public virtual extern event _IInkPictureEvents_PaintingEventHandler Painting;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Painting(_IInkPictureEvents_PaintingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Painted(_IInkPictureEvents_PaintedEventHandler A_1);

    public virtual extern event _IInkPictureEvents_PaintedEventHandler Painted;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Painted(_IInkPictureEvents_PaintedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionChanging(
      _IInkPictureEvents_SelectionChangingEventHandler A_1);

    public virtual extern event _IInkPictureEvents_SelectionChangingEventHandler SelectionChanging;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionChanging(
      _IInkPictureEvents_SelectionChangingEventHandler A_1);

    public virtual extern event _IInkPictureEvents_SelectionChangedEventHandler SelectionChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionChanged(
      _IInkPictureEvents_SelectionChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionChanged(
      _IInkPictureEvents_SelectionChangedEventHandler A_1);

    public virtual extern event _IInkPictureEvents_SelectionMovingEventHandler SelectionMoving;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionMoving(
      _IInkPictureEvents_SelectionMovingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionMoving(
      _IInkPictureEvents_SelectionMovingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionMoved(_IInkPictureEvents_SelectionMovedEventHandler A_1);

    public virtual extern event _IInkPictureEvents_SelectionMovedEventHandler SelectionMoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionMoved(
      _IInkPictureEvents_SelectionMovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionResizing(
      _IInkPictureEvents_SelectionResizingEventHandler A_1);

    public virtual extern event _IInkPictureEvents_SelectionResizingEventHandler SelectionResizing;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionResizing(
      _IInkPictureEvents_SelectionResizingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SelectionResized(
      _IInkPictureEvents_SelectionResizedEventHandler A_1);

    public virtual extern event _IInkPictureEvents_SelectionResizedEventHandler SelectionResized;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SelectionResized(
      _IInkPictureEvents_SelectionResizedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_StrokesDeleting(
      _IInkPictureEvents_StrokesDeletingEventHandler A_1);

    public virtual extern event _IInkPictureEvents_StrokesDeletingEventHandler StrokesDeleting;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_StrokesDeleting(
      _IInkPictureEvents_StrokesDeletingEventHandler A_1);

    public virtual extern event _IInkPictureEvents_StrokesDeletedEventHandler StrokesDeleted;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_StrokesDeleted(_IInkPictureEvents_StrokesDeletedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_StrokesDeleted(
      _IInkPictureEvents_StrokesDeletedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseEnter(_IInkPictureEvents_MouseEnterEventHandler A_1);

    public virtual extern event _IInkPictureEvents_MouseEnterEventHandler MouseEnter;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseEnter(_IInkPictureEvents_MouseEnterEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Click(_IInkPictureEvents_ClickEventHandler A_1);

    public virtual extern event _IInkPictureEvents_ClickEventHandler Click;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Click(_IInkPictureEvents_ClickEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseHover(_IInkPictureEvents_MouseHoverEventHandler A_1);

    public virtual extern event _IInkPictureEvents_MouseHoverEventHandler MouseHover;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseHover(_IInkPictureEvents_MouseHoverEventHandler A_1);

    public virtual extern event _IInkPictureEvents_MouseLeaveEventHandler MouseLeave;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_MouseLeave(_IInkPictureEvents_MouseLeaveEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_MouseLeave(_IInkPictureEvents_MouseLeaveEventHandler A_1);

    public virtual extern event _IInkPictureEvents_KeyDownEventHandler KeyDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_KeyDown(_IInkPictureEvents_KeyDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_KeyDown(_IInkPictureEvents_KeyDownEventHandler A_1);

    public virtual extern event _IInkPictureEvents_KeyUpEventHandler KeyUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_KeyUp(_IInkPictureEvents_KeyUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_KeyUp(_IInkPictureEvents_KeyUpEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_KeyPress(_IInkPictureEvents_KeyPressEventHandler A_1);

    public virtual extern event _IInkPictureEvents_KeyPressEventHandler KeyPress;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_KeyPress(_IInkPictureEvents_KeyPressEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SizeModeChanged(
      _IInkPictureEvents_SizeModeChangedEventHandler A_1);

    public virtual extern event _IInkPictureEvents_SizeModeChangedEventHandler SizeModeChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SizeModeChanged(
      _IInkPictureEvents_SizeModeChangedEventHandler A_1);

    public virtual extern event _IInkPictureEvents_SystemColorsChangedEventHandler SystemColorsChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SystemColorsChanged(
      _IInkPictureEvents_SystemColorsChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SystemColorsChanged(
      _IInkPictureEvents_SystemColorsChangedEventHandler A_1);

    public virtual extern event _IInkPictureEvents_ResizeEventHandler Resize;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Resize(_IInkPictureEvents_ResizeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Resize(_IInkPictureEvents_ResizeEventHandler A_1);

    public virtual extern event _IInkPictureEvents_SizeChangedEventHandler SizeChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SizeChanged(_IInkPictureEvents_SizeChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SizeChanged(_IInkPictureEvents_SizeChangedEventHandler A_1);

    public virtual extern event _IInkPictureEvents_StyleChangedEventHandler StyleChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_StyleChanged(_IInkPictureEvents_StyleChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_StyleChanged(_IInkPictureEvents_StyleChangedEventHandler A_1);

    public virtual extern event _IInkPictureEvents_ChangeUICuesEventHandler ChangeUICues;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_ChangeUICues(_IInkPictureEvents_ChangeUICuesEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_ChangeUICues(_IInkPictureEvents_ChangeUICuesEventHandler A_1);

    public virtual extern event _IInkPictureEvents_NewInAirPacketsEventHandler NewInAirPackets;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_NewInAirPackets(
      _IInkPictureEvents_NewInAirPacketsEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_NewInAirPackets(
      _IInkPictureEvents_NewInAirPacketsEventHandler A_1);

    public virtual extern event _IInkPictureEvents_CursorButtonDownEventHandler CursorButtonDown;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorButtonDown(
      _IInkPictureEvents_CursorButtonDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorButtonDown(
      _IInkPictureEvents_CursorButtonDownEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorButtonUp(_IInkPictureEvents_CursorButtonUpEventHandler A_1);

    public virtual extern event _IInkPictureEvents_CursorButtonUpEventHandler CursorButtonUp;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorButtonUp(
      _IInkPictureEvents_CursorButtonUpEventHandler A_1);

    public virtual extern event _IInkPictureEvents_CursorInRangeEventHandler CursorInRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorInRange(_IInkPictureEvents_CursorInRangeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorInRange(_IInkPictureEvents_CursorInRangeEventHandler A_1);

    public virtual extern event _IInkPictureEvents_CursorOutOfRangeEventHandler CursorOutOfRange;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_CursorOutOfRange(
      _IInkPictureEvents_CursorOutOfRangeEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_CursorOutOfRange(
      _IInkPictureEvents_CursorOutOfRangeEventHandler A_1);

    public virtual extern event _IInkPictureEvents_SystemGestureEventHandler SystemGesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_SystemGesture(_IInkPictureEvents_SystemGestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_SystemGesture(_IInkPictureEvents_SystemGestureEventHandler A_1);

    public virtual extern event _IInkPictureEvents_GestureEventHandler Gesture;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_Gesture(_IInkPictureEvents_GestureEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_Gesture(_IInkPictureEvents_GestureEventHandler A_1);

    public virtual extern event _IInkPictureEvents_TabletAddedEventHandler TabletAdded;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_TabletAdded(_IInkPictureEvents_TabletAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_TabletAdded(_IInkPictureEvents_TabletAddedEventHandler A_1);

    public virtual extern event _IInkPictureEvents_TabletRemovedEventHandler TabletRemoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_TabletRemoved(_IInkPictureEvents_TabletRemovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_TabletRemoved(_IInkPictureEvents_TabletRemovedEventHandler A_1);
  }
}
