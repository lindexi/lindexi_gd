// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkOverlayEvents_SinkHelper
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [TypeLibType(TypeLibTypeFlags.FHidden)]
  [ClassInterface(ClassInterfaceType.None)]
  internal sealed class _IInkOverlayEvents_SinkHelper : InkEventForwarder, _IInkOverlayEvents
  {
    public _IInkOverlayEvents_StrokeEventHandler m_StrokeDelegate;
    public _IInkOverlayEvents_CursorDownEventHandler m_CursorDownDelegate;
    public _IInkOverlayEvents_NewPacketsEventHandler m_NewPacketsDelegate;
    public _IInkOverlayEvents_DblClickEventHandler m_DblClickDelegate;
    public _IInkOverlayEvents_MouseMoveEventHandler m_MouseMoveDelegate;
    public _IInkOverlayEvents_MouseDownEventHandler m_MouseDownDelegate;
    public _IInkOverlayEvents_MouseUpEventHandler m_MouseUpDelegate;
    public _IInkOverlayEvents_MouseWheelEventHandler m_MouseWheelDelegate;
    public _IInkOverlayEvents_PaintingEventHandler m_PaintingDelegate;
    public _IInkOverlayEvents_PaintedEventHandler m_PaintedDelegate;
    public _IInkOverlayEvents_SelectionChangingEventHandler m_SelectionChangingDelegate;
    public _IInkOverlayEvents_SelectionChangedEventHandler m_SelectionChangedDelegate;
    public _IInkOverlayEvents_SelectionMovingEventHandler m_SelectionMovingDelegate;
    public _IInkOverlayEvents_SelectionMovedEventHandler m_SelectionMovedDelegate;
    public _IInkOverlayEvents_SelectionResizingEventHandler m_SelectionResizingDelegate;
    public _IInkOverlayEvents_SelectionResizedEventHandler m_SelectionResizedDelegate;
    public _IInkOverlayEvents_StrokesDeletingEventHandler m_StrokesDeletingDelegate;
    public _IInkOverlayEvents_StrokesDeletedEventHandler m_StrokesDeletedDelegate;
    public _IInkOverlayEvents_NewInAirPacketsEventHandler m_NewInAirPacketsDelegate;
    public _IInkOverlayEvents_CursorButtonDownEventHandler m_CursorButtonDownDelegate;
    public _IInkOverlayEvents_CursorButtonUpEventHandler m_CursorButtonUpDelegate;
    public _IInkOverlayEvents_CursorInRangeEventHandler m_CursorInRangeDelegate;
    public _IInkOverlayEvents_CursorOutOfRangeEventHandler m_CursorOutOfRangeDelegate;
    public _IInkOverlayEvents_SystemGestureEventHandler m_SystemGestureDelegate;
    public _IInkOverlayEvents_GestureEventHandler m_GestureDelegate;
    public _IInkOverlayEvents_TabletAddedEventHandler m_TabletAddedDelegate;
    public _IInkOverlayEvents_TabletRemovedEventHandler m_TabletRemovedDelegate;
    public int m_dwCookie;

    public virtual void Stroke(IInkCursor A_1, IInkStrokeDisp A_2, ref bool A_3)
    {
      if (this.m_StrokeDelegate == null)
        return;
      this.m_StrokeDelegate(A_1, A_2, ref A_3);
    }

    public virtual void CursorDown(IInkCursor A_1, IInkStrokeDisp A_2)
    {
      if (this.m_CursorDownDelegate == null)
        return;
      this.m_CursorDownDelegate(A_1, A_2);
    }

    public virtual void NewPackets(IInkCursor A_1, IInkStrokeDisp A_2, int A_3, ref object A_4)
    {
      if (this.m_NewPacketsDelegate == null)
        return;
      this.m_NewPacketsDelegate(A_1, A_2, A_3, ref A_4);
    }

    public virtual void DblClick(ref bool A_1)
    {
      if (this.m_DblClickDelegate == null)
        return;
      this.m_DblClickDelegate(ref A_1);
    }

    public virtual void MouseMove(
      InkMouseButton A_1,
      InkShiftKeyModifierFlags A_2,
      int A_3,
      int A_4,
      ref bool A_5)
    {
      if (this.m_MouseMoveDelegate == null)
        return;
      this.m_MouseMoveDelegate(A_1, A_2, A_3, A_4, ref A_5);
    }

    public virtual void MouseDown(
      InkMouseButton A_1,
      InkShiftKeyModifierFlags A_2,
      int A_3,
      int A_4,
      ref bool A_5)
    {
      if (this.m_MouseDownDelegate == null)
        return;
      this.m_MouseDownDelegate(A_1, A_2, A_3, A_4, ref A_5);
    }

    public virtual void MouseUp(
      InkMouseButton A_1,
      InkShiftKeyModifierFlags A_2,
      int A_3,
      int A_4,
      ref bool A_5)
    {
      if (this.m_MouseUpDelegate == null)
        return;
      this.m_MouseUpDelegate(A_1, A_2, A_3, A_4, ref A_5);
    }

    public virtual void MouseWheel(
      InkMouseButton A_1,
      InkShiftKeyModifierFlags A_2,
      int A_3,
      int A_4,
      int A_5,
      ref bool A_6)
    {
      if (this.m_MouseWheelDelegate == null)
        return;
      this.m_MouseWheelDelegate(A_1, A_2, A_3, A_4, A_5, ref A_6);
    }

    public virtual void Painting(int A_1, InkRectangle A_2, ref bool A_3)
    {
      if (this.m_PaintingDelegate == null)
        return;
      this.m_PaintingDelegate(A_1, A_2, ref A_3);
    }

    public virtual void Painted(int A_1, InkRectangle A_2)
    {
      if (this.m_PaintedDelegate == null)
        return;
      this.m_PaintedDelegate(A_1, A_2);
    }

    public virtual void SelectionChanging(InkStrokes A_1)
    {
      if (this.m_SelectionChangingDelegate == null)
        return;
      this.m_SelectionChangingDelegate(A_1);
    }

    public virtual void SelectionChanged()
    {
      if (this.m_SelectionChangedDelegate == null)
        return;
      this.m_SelectionChangedDelegate();
    }

    public virtual void SelectionMoving(InkRectangle A_1)
    {
      if (this.m_SelectionMovingDelegate == null)
        return;
      this.m_SelectionMovingDelegate(A_1);
    }

    public virtual void SelectionMoved(InkRectangle A_1)
    {
      if (this.m_SelectionMovedDelegate == null)
        return;
      this.m_SelectionMovedDelegate(A_1);
    }

    public virtual void SelectionResizing(InkRectangle A_1)
    {
      if (this.m_SelectionResizingDelegate == null)
        return;
      this.m_SelectionResizingDelegate(A_1);
    }

    public virtual void SelectionResized(InkRectangle A_1)
    {
      if (this.m_SelectionResizedDelegate == null)
        return;
      this.m_SelectionResizedDelegate(A_1);
    }

    public virtual void StrokesDeleting(InkStrokes A_1)
    {
      if (this.m_StrokesDeletingDelegate == null)
        return;
      this.m_StrokesDeletingDelegate(A_1);
    }

    public virtual void StrokesDeleted()
    {
      if (this.m_StrokesDeletedDelegate == null)
        return;
      this.m_StrokesDeletedDelegate();
    }

    public virtual void NewInAirPackets(IInkCursor A_1, int A_2, ref object A_3)
    {
      if (this.m_NewInAirPacketsDelegate == null)
        return;
      this.m_NewInAirPacketsDelegate(A_1, A_2, ref A_3);
    }

    public virtual void CursorButtonDown(IInkCursor A_1, IInkCursorButton A_2)
    {
      if (this.m_CursorButtonDownDelegate == null)
        return;
      this.m_CursorButtonDownDelegate(A_1, A_2);
    }

    public virtual void CursorButtonUp(IInkCursor A_1, IInkCursorButton A_2)
    {
      if (this.m_CursorButtonUpDelegate == null)
        return;
      this.m_CursorButtonUpDelegate(A_1, A_2);
    }

    public virtual void CursorInRange(IInkCursor A_1, bool A_2, object A_3)
    {
      if (this.m_CursorInRangeDelegate == null)
        return;
      this.m_CursorInRangeDelegate(A_1, A_2, A_3);
    }

    public virtual void CursorOutOfRange(IInkCursor A_1)
    {
      if (this.m_CursorOutOfRangeDelegate == null)
        return;
      this.m_CursorOutOfRangeDelegate(A_1);
    }

    public virtual void SystemGesture(
      IInkCursor A_1,
      InkSystemGesture A_2,
      int A_3,
      int A_4,
      int A_5,
      string A_6,
      int A_7)
    {
      if (this.m_SystemGestureDelegate == null)
        return;
      this.m_SystemGestureDelegate(A_1, A_2, A_3, A_4, A_5, A_6, A_7);
    }

    public virtual void Gesture(IInkCursor A_1, InkStrokes A_2, object A_3, ref bool A_4)
    {
      if (this.m_GestureDelegate == null)
        return;
      this.m_GestureDelegate(A_1, A_2, A_3, ref A_4);
    }

    public virtual void TabletAdded(IInkTablet A_1)
    {
      if (this.m_TabletAddedDelegate == null)
        return;
      this.m_TabletAddedDelegate(A_1);
    }

    public virtual void TabletRemoved(int A_1)
    {
      if (this.m_TabletRemovedDelegate == null)
        return;
      this.m_TabletRemovedDelegate(A_1);
    }

    internal _IInkOverlayEvents_SinkHelper()
    {
      this.m_dwCookie = 0;
      this.m_StrokeDelegate = (_IInkOverlayEvents_StrokeEventHandler) null;
      this.m_CursorDownDelegate = (_IInkOverlayEvents_CursorDownEventHandler) null;
      this.m_NewPacketsDelegate = (_IInkOverlayEvents_NewPacketsEventHandler) null;
      this.m_DblClickDelegate = (_IInkOverlayEvents_DblClickEventHandler) null;
      this.m_MouseMoveDelegate = (_IInkOverlayEvents_MouseMoveEventHandler) null;
      this.m_MouseDownDelegate = (_IInkOverlayEvents_MouseDownEventHandler) null;
      this.m_MouseUpDelegate = (_IInkOverlayEvents_MouseUpEventHandler) null;
      this.m_MouseWheelDelegate = (_IInkOverlayEvents_MouseWheelEventHandler) null;
      this.m_PaintingDelegate = (_IInkOverlayEvents_PaintingEventHandler) null;
      this.m_PaintedDelegate = (_IInkOverlayEvents_PaintedEventHandler) null;
      this.m_SelectionChangingDelegate = (_IInkOverlayEvents_SelectionChangingEventHandler) null;
      this.m_SelectionChangedDelegate = (_IInkOverlayEvents_SelectionChangedEventHandler) null;
      this.m_SelectionMovingDelegate = (_IInkOverlayEvents_SelectionMovingEventHandler) null;
      this.m_SelectionMovedDelegate = (_IInkOverlayEvents_SelectionMovedEventHandler) null;
      this.m_SelectionResizingDelegate = (_IInkOverlayEvents_SelectionResizingEventHandler) null;
      this.m_SelectionResizedDelegate = (_IInkOverlayEvents_SelectionResizedEventHandler) null;
      this.m_StrokesDeletingDelegate = (_IInkOverlayEvents_StrokesDeletingEventHandler) null;
      this.m_StrokesDeletedDelegate = (_IInkOverlayEvents_StrokesDeletedEventHandler) null;
      this.m_NewInAirPacketsDelegate = (_IInkOverlayEvents_NewInAirPacketsEventHandler) null;
      this.m_CursorButtonDownDelegate = (_IInkOverlayEvents_CursorButtonDownEventHandler) null;
      this.m_CursorButtonUpDelegate = (_IInkOverlayEvents_CursorButtonUpEventHandler) null;
      this.m_CursorInRangeDelegate = (_IInkOverlayEvents_CursorInRangeEventHandler) null;
      this.m_CursorOutOfRangeDelegate = (_IInkOverlayEvents_CursorOutOfRangeEventHandler) null;
      this.m_SystemGestureDelegate = (_IInkOverlayEvents_SystemGestureEventHandler) null;
      this.m_GestureDelegate = (_IInkOverlayEvents_GestureEventHandler) null;
      this.m_TabletAddedDelegate = (_IInkOverlayEvents_TabletAddedEventHandler) null;
      this.m_TabletRemovedDelegate = (_IInkOverlayEvents_TabletRemovedEventHandler) null;
    }
  }
}
