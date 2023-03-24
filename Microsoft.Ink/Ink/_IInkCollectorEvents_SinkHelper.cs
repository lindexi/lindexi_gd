// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkCollectorEvents_SinkHelper
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [TypeLibType(TypeLibTypeFlags.FHidden)]
  [ClassInterface(ClassInterfaceType.None)]
  internal sealed class _IInkCollectorEvents_SinkHelper : InkEventForwarder, _IInkCollectorEvents
  {
    public _IInkCollectorEvents_StrokeEventHandler m_StrokeDelegate;
    public _IInkCollectorEvents_CursorDownEventHandler m_CursorDownDelegate;
    public _IInkCollectorEvents_NewPacketsEventHandler m_NewPacketsDelegate;
    public _IInkCollectorEvents_DblClickEventHandler m_DblClickDelegate;
    public _IInkCollectorEvents_MouseMoveEventHandler m_MouseMoveDelegate;
    public _IInkCollectorEvents_MouseDownEventHandler m_MouseDownDelegate;
    public _IInkCollectorEvents_MouseUpEventHandler m_MouseUpDelegate;
    public _IInkCollectorEvents_MouseWheelEventHandler m_MouseWheelDelegate;
    public _IInkCollectorEvents_NewInAirPacketsEventHandler m_NewInAirPacketsDelegate;
    public _IInkCollectorEvents_CursorButtonDownEventHandler m_CursorButtonDownDelegate;
    public _IInkCollectorEvents_CursorButtonUpEventHandler m_CursorButtonUpDelegate;
    public _IInkCollectorEvents_CursorInRangeEventHandler m_CursorInRangeDelegate;
    public _IInkCollectorEvents_CursorOutOfRangeEventHandler m_CursorOutOfRangeDelegate;
    public _IInkCollectorEvents_SystemGestureEventHandler m_SystemGestureDelegate;
    public _IInkCollectorEvents_GestureEventHandler m_GestureDelegate;
    public _IInkCollectorEvents_TabletAddedEventHandler m_TabletAddedDelegate;
    public _IInkCollectorEvents_TabletRemovedEventHandler m_TabletRemovedDelegate;
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

    internal _IInkCollectorEvents_SinkHelper()
    {
      this.m_dwCookie = 0;
      this.m_StrokeDelegate = (_IInkCollectorEvents_StrokeEventHandler) null;
      this.m_CursorDownDelegate = (_IInkCollectorEvents_CursorDownEventHandler) null;
      this.m_NewPacketsDelegate = (_IInkCollectorEvents_NewPacketsEventHandler) null;
      this.m_DblClickDelegate = (_IInkCollectorEvents_DblClickEventHandler) null;
      this.m_MouseMoveDelegate = (_IInkCollectorEvents_MouseMoveEventHandler) null;
      this.m_MouseDownDelegate = (_IInkCollectorEvents_MouseDownEventHandler) null;
      this.m_MouseUpDelegate = (_IInkCollectorEvents_MouseUpEventHandler) null;
      this.m_MouseWheelDelegate = (_IInkCollectorEvents_MouseWheelEventHandler) null;
      this.m_NewInAirPacketsDelegate = (_IInkCollectorEvents_NewInAirPacketsEventHandler) null;
      this.m_CursorButtonDownDelegate = (_IInkCollectorEvents_CursorButtonDownEventHandler) null;
      this.m_CursorButtonUpDelegate = (_IInkCollectorEvents_CursorButtonUpEventHandler) null;
      this.m_CursorInRangeDelegate = (_IInkCollectorEvents_CursorInRangeEventHandler) null;
      this.m_CursorOutOfRangeDelegate = (_IInkCollectorEvents_CursorOutOfRangeEventHandler) null;
      this.m_SystemGestureDelegate = (_IInkCollectorEvents_SystemGestureEventHandler) null;
      this.m_GestureDelegate = (_IInkCollectorEvents_GestureEventHandler) null;
      this.m_TabletAddedDelegate = (_IInkCollectorEvents_TabletAddedEventHandler) null;
      this.m_TabletRemovedDelegate = (_IInkCollectorEvents_TabletRemovedEventHandler) null;
    }
  }
}
