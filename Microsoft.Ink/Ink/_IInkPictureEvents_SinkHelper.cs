// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkPictureEvents_SinkHelper
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [ClassInterface(ClassInterfaceType.None)]
  [TypeLibType(TypeLibTypeFlags.FHidden)]
  internal sealed class _IInkPictureEvents_SinkHelper : InkEventForwarder, _IInkPictureEvents
  {
    public _IInkPictureEvents_CursorButtonDownEventHandler m_CursorButtonDownDelegate;
    public _IInkPictureEvents_CursorButtonUpEventHandler m_CursorButtonUpDelegate;
    public _IInkPictureEvents_CursorInRangeEventHandler m_CursorInRangeDelegate;
    public _IInkPictureEvents_CursorOutOfRangeEventHandler m_CursorOutOfRangeDelegate;
    public _IInkPictureEvents_SystemGestureEventHandler m_SystemGestureDelegate;
    public _IInkPictureEvents_GestureEventHandler m_GestureDelegate;
    public _IInkPictureEvents_TabletAddedEventHandler m_TabletAddedDelegate;
    public _IInkPictureEvents_TabletRemovedEventHandler m_TabletRemovedDelegate;
    public _IInkPictureEvents_StrokeEventHandler m_StrokeDelegate;
    public _IInkPictureEvents_CursorDownEventHandler m_CursorDownDelegate;
    public _IInkPictureEvents_NewPacketsEventHandler m_NewPacketsDelegate;
    public _IInkPictureEvents_DblClickEventHandler m_DblClickDelegate;
    public _IInkPictureEvents_MouseMoveEventHandler m_MouseMoveDelegate;
    public _IInkPictureEvents_MouseDownEventHandler m_MouseDownDelegate;
    public _IInkPictureEvents_MouseUpEventHandler m_MouseUpDelegate;
    public _IInkPictureEvents_MouseWheelEventHandler m_MouseWheelDelegate;
    public _IInkPictureEvents_PaintingEventHandler m_PaintingDelegate;
    public _IInkPictureEvents_PaintedEventHandler m_PaintedDelegate;
    public _IInkPictureEvents_SelectionChangingEventHandler m_SelectionChangingDelegate;
    public _IInkPictureEvents_SelectionChangedEventHandler m_SelectionChangedDelegate;
    public _IInkPictureEvents_SelectionMovingEventHandler m_SelectionMovingDelegate;
    public _IInkPictureEvents_SelectionMovedEventHandler m_SelectionMovedDelegate;
    public _IInkPictureEvents_SelectionResizingEventHandler m_SelectionResizingDelegate;
    public _IInkPictureEvents_SelectionResizedEventHandler m_SelectionResizedDelegate;
    public _IInkPictureEvents_StrokesDeletingEventHandler m_StrokesDeletingDelegate;
    public _IInkPictureEvents_StrokesDeletedEventHandler m_StrokesDeletedDelegate;
    public _IInkPictureEvents_MouseEnterEventHandler m_MouseEnterDelegate;
    public _IInkPictureEvents_ClickEventHandler m_ClickDelegate;
    public _IInkPictureEvents_MouseHoverEventHandler m_MouseHoverDelegate;
    public _IInkPictureEvents_MouseLeaveEventHandler m_MouseLeaveDelegate;
    public _IInkPictureEvents_KeyDownEventHandler m_KeyDownDelegate;
    public _IInkPictureEvents_KeyUpEventHandler m_KeyUpDelegate;
    public _IInkPictureEvents_KeyPressEventHandler m_KeyPressDelegate;
    public _IInkPictureEvents_SizeModeChangedEventHandler m_SizeModeChangedDelegate;
    public _IInkPictureEvents_SystemColorsChangedEventHandler m_SystemColorsChangedDelegate;
    public _IInkPictureEvents_ResizeEventHandler m_ResizeDelegate;
    public _IInkPictureEvents_SizeChangedEventHandler m_SizeChangedDelegate;
    public _IInkPictureEvents_StyleChangedEventHandler m_StyleChangedDelegate;
    public _IInkPictureEvents_ChangeUICuesEventHandler m_ChangeUICuesDelegate;
    public _IInkPictureEvents_NewInAirPacketsEventHandler m_NewInAirPacketsDelegate;
    public int m_dwCookie;

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

    public virtual void MouseEnter()
    {
      if (this.m_MouseEnterDelegate == null)
        return;
      this.m_MouseEnterDelegate();
    }

    public virtual void Click()
    {
      if (this.m_ClickDelegate == null)
        return;
      this.m_ClickDelegate();
    }

    public virtual void MouseHover()
    {
      if (this.m_MouseHoverDelegate == null)
        return;
      this.m_MouseHoverDelegate();
    }

    public virtual void MouseLeave()
    {
      if (this.m_MouseLeaveDelegate == null)
        return;
      this.m_MouseLeaveDelegate();
    }

    public virtual void KeyDown(ref short A_1, ref short A_2)
    {
      if (this.m_KeyDownDelegate == null)
        return;
      this.m_KeyDownDelegate(ref A_1, ref A_2);
    }

    public virtual void KeyUp(ref short A_1, ref short A_2)
    {
      if (this.m_KeyUpDelegate == null)
        return;
      this.m_KeyUpDelegate(ref A_1, ref A_2);
    }

    public virtual void KeyPress(ref short A_1)
    {
      if (this.m_KeyPressDelegate == null)
        return;
      this.m_KeyPressDelegate(ref A_1);
    }

    public virtual void SizeModeChanged(InkPictureSizeMode A_1, InkPictureSizeMode A_2)
    {
      if (this.m_SizeModeChangedDelegate == null)
        return;
      this.m_SizeModeChangedDelegate(A_1, A_2);
    }

    public virtual void SystemColorsChanged()
    {
      if (this.m_SystemColorsChangedDelegate == null)
        return;
      this.m_SystemColorsChangedDelegate();
    }

    public virtual void Resize(ref int A_1, ref int A_2, ref int A_3, ref int A_4)
    {
      if (this.m_ResizeDelegate == null)
        return;
      this.m_ResizeDelegate(ref A_1, ref A_2, ref A_3, ref A_4);
    }

    public virtual void SizeChanged(int A_1, int A_2, int A_3, int A_4)
    {
      if (this.m_SizeChangedDelegate == null)
        return;
      this.m_SizeChangedDelegate(A_1, A_2, A_3, A_4);
    }

    public virtual void StyleChanged()
    {
      if (this.m_StyleChangedDelegate == null)
        return;
      this.m_StyleChangedDelegate();
    }

    public virtual void ChangeUICues(int A_1)
    {
      if (this.m_ChangeUICuesDelegate == null)
        return;
      this.m_ChangeUICuesDelegate(A_1);
    }

    public virtual void NewInAirPackets(IInkCursor A_1, int A_2, ref object A_3)
    {
      if (this.m_NewInAirPacketsDelegate == null)
        return;
      this.m_NewInAirPacketsDelegate(A_1, A_2, ref A_3);
    }

    internal _IInkPictureEvents_SinkHelper()
    {
      this.m_dwCookie = 0;
      this.m_CursorButtonDownDelegate = (_IInkPictureEvents_CursorButtonDownEventHandler) null;
      this.m_CursorButtonUpDelegate = (_IInkPictureEvents_CursorButtonUpEventHandler) null;
      this.m_CursorInRangeDelegate = (_IInkPictureEvents_CursorInRangeEventHandler) null;
      this.m_CursorOutOfRangeDelegate = (_IInkPictureEvents_CursorOutOfRangeEventHandler) null;
      this.m_SystemGestureDelegate = (_IInkPictureEvents_SystemGestureEventHandler) null;
      this.m_GestureDelegate = (_IInkPictureEvents_GestureEventHandler) null;
      this.m_TabletAddedDelegate = (_IInkPictureEvents_TabletAddedEventHandler) null;
      this.m_TabletRemovedDelegate = (_IInkPictureEvents_TabletRemovedEventHandler) null;
      this.m_StrokeDelegate = (_IInkPictureEvents_StrokeEventHandler) null;
      this.m_CursorDownDelegate = (_IInkPictureEvents_CursorDownEventHandler) null;
      this.m_NewPacketsDelegate = (_IInkPictureEvents_NewPacketsEventHandler) null;
      this.m_DblClickDelegate = (_IInkPictureEvents_DblClickEventHandler) null;
      this.m_MouseMoveDelegate = (_IInkPictureEvents_MouseMoveEventHandler) null;
      this.m_MouseDownDelegate = (_IInkPictureEvents_MouseDownEventHandler) null;
      this.m_MouseUpDelegate = (_IInkPictureEvents_MouseUpEventHandler) null;
      this.m_MouseWheelDelegate = (_IInkPictureEvents_MouseWheelEventHandler) null;
      this.m_PaintingDelegate = (_IInkPictureEvents_PaintingEventHandler) null;
      this.m_PaintedDelegate = (_IInkPictureEvents_PaintedEventHandler) null;
      this.m_SelectionChangingDelegate = (_IInkPictureEvents_SelectionChangingEventHandler) null;
      this.m_SelectionChangedDelegate = (_IInkPictureEvents_SelectionChangedEventHandler) null;
      this.m_SelectionMovingDelegate = (_IInkPictureEvents_SelectionMovingEventHandler) null;
      this.m_SelectionMovedDelegate = (_IInkPictureEvents_SelectionMovedEventHandler) null;
      this.m_SelectionResizingDelegate = (_IInkPictureEvents_SelectionResizingEventHandler) null;
      this.m_SelectionResizedDelegate = (_IInkPictureEvents_SelectionResizedEventHandler) null;
      this.m_StrokesDeletingDelegate = (_IInkPictureEvents_StrokesDeletingEventHandler) null;
      this.m_StrokesDeletedDelegate = (_IInkPictureEvents_StrokesDeletedEventHandler) null;
      this.m_MouseEnterDelegate = (_IInkPictureEvents_MouseEnterEventHandler) null;
      this.m_ClickDelegate = (_IInkPictureEvents_ClickEventHandler) null;
      this.m_MouseHoverDelegate = (_IInkPictureEvents_MouseHoverEventHandler) null;
      this.m_MouseLeaveDelegate = (_IInkPictureEvents_MouseLeaveEventHandler) null;
      this.m_KeyDownDelegate = (_IInkPictureEvents_KeyDownEventHandler) null;
      this.m_KeyUpDelegate = (_IInkPictureEvents_KeyUpEventHandler) null;
      this.m_KeyPressDelegate = (_IInkPictureEvents_KeyPressEventHandler) null;
      this.m_SizeModeChangedDelegate = (_IInkPictureEvents_SizeModeChangedEventHandler) null;
      this.m_SystemColorsChangedDelegate = (_IInkPictureEvents_SystemColorsChangedEventHandler) null;
      this.m_ResizeDelegate = (_IInkPictureEvents_ResizeEventHandler) null;
      this.m_SizeChangedDelegate = (_IInkPictureEvents_SizeChangedEventHandler) null;
      this.m_StyleChangedDelegate = (_IInkPictureEvents_StyleChangedEventHandler) null;
      this.m_ChangeUICuesDelegate = (_IInkPictureEvents_ChangeUICuesEventHandler) null;
      this.m_NewInAirPacketsDelegate = (_IInkPictureEvents_NewInAirPacketsEventHandler) null;
    }
  }
}
