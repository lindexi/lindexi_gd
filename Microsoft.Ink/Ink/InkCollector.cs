// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkCollector
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;

namespace Microsoft.Ink
{
  [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class InkCollector : IDisposable
  {
    private const int FireEventsOnWindowThread = -2;
    private InkCollectorPrivate m_Collector;
    public readonly int DefaultMargin = int.MinValue;
    public readonly int ClipInkToMargin;
    private Control m_attachedControl;
    private ResourceManager m_Errors;
    private object thisLock = new object();
    private Renderer m_Renderer;
    private DrawingAttributes m_DrawingAttributes;
    private Microsoft.Ink.Ink m_Ink;
    private Cursors m_Cursors;
    private bool m_cachedInkEnabled = true;
    private System.Windows.Forms.Cursor m_MouseCursor;
    private InkCollectorCursorDownEventHandler onCursorDown;
    private InkCollectorMouseDownEventHandler onMouseDown;
    private InkCollectorMouseMoveEventHandler onMouseMove;
    private InkCollectorMouseUpEventHandler onMouseUp;
    private InkCollectorMouseWheelEventHandler onMouseWheel;
    private InkCollectorDoubleClickEventHandler onDoubleClick;
    private InkCollectorStrokeEventHandler onStroke;
    private InkCollectorNewPacketsEventHandler onNewPackets;
    private InkCollectorNewInAirPacketsEventHandler onNewInAirPackets;
    private InkCollectorCursorInRangeEventHandler onCursorInRange;
    private InkCollectorCursorOutOfRangeEventHandler onCursorOutOfRange;
    private InkCollectorCursorButtonDownEventHandler onCursorButtonDown;
    private InkCollectorCursorButtonUpEventHandler onCursorButtonUp;
    private InkCollectorTabletAddedEventHandler onTabletAdded;
    private InkCollectorTabletRemovedEventHandler onTabletRemoved;
    private InkCollectorSystemGestureEventHandler onSystemGesture;
    private InkCollectorGestureEventHandler onGesture;
    private bool m_disposing;
    private bool disposed;

    public event InkCollectorCursorDownEventHandler CursorDown
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onCursorDown += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorDown, true);
        if (value == null || this.onCursorDown.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_CursorDown(new _IInkCollectorEvents_CursorDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorDown)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onCursorDown == null)
          return;
        this.onCursorDown -= value;
        if (this.onCursorDown != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorDown, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_CursorDown(new _IInkCollectorEvents_CursorDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorDown)));
      }
    }

    public event InkCollectorMouseDownEventHandler MouseDown
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onMouseDown += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_MouseDown, true);
        if (value == null || this.onMouseDown.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_MouseDown(new _IInkCollectorEvents_MouseDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseDown)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onMouseDown == null)
          return;
        this.onMouseDown -= value;
        if (this.onMouseDown != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_MouseDown, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_MouseDown(new _IInkCollectorEvents_MouseDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseDown)));
      }
    }

    public event InkCollectorMouseMoveEventHandler MouseMove
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onMouseMove += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_MouseMove, true);
        if (value == null || this.onMouseMove.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_MouseMove(new _IInkCollectorEvents_MouseMoveEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseMove)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onMouseMove == null)
          return;
        this.onMouseMove -= value;
        if (this.onMouseMove != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_MouseMove, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_MouseMove(new _IInkCollectorEvents_MouseMoveEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseMove)));
      }
    }

    public event InkCollectorMouseUpEventHandler MouseUp
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onMouseUp += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_MouseUp, true);
        if (value == null || this.onMouseUp.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_MouseUp(new _IInkCollectorEvents_MouseUpEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseUp)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onMouseUp == null)
          return;
        this.onMouseUp -= value;
        if (this.onMouseUp != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_MouseUp, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_MouseUp(new _IInkCollectorEvents_MouseUpEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseUp)));
      }
    }

    public event InkCollectorMouseWheelEventHandler MouseWheel
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onMouseWheel += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_MouseWheel, true);
        if (value == null || this.onMouseWheel.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_MouseWheel(new _IInkCollectorEvents_MouseWheelEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseWheel)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onMouseWheel == null)
          return;
        this.onMouseWheel -= value;
        if (this.onMouseWheel != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_MouseWheel, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_MouseWheel(new _IInkCollectorEvents_MouseWheelEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseWheel)));
      }
    }

    public event InkCollectorDoubleClickEventHandler DoubleClick
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onDoubleClick += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_DblClick, true);
        if (value == null || this.onDoubleClick.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_DblClick(new _IInkCollectorEvents_DblClickEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_DoubleClick)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onDoubleClick == null)
          return;
        this.onDoubleClick -= value;
        if (this.onDoubleClick != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_DblClick, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_DblClick(new _IInkCollectorEvents_DblClickEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_DoubleClick)));
      }
    }

    public event InkCollectorStrokeEventHandler Stroke
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onStroke += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_Stroke, true);
        if (value == null || this.onStroke.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_Stroke(new _IInkCollectorEvents_StrokeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Stroke)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onStroke == null)
          return;
        this.onStroke -= value;
        if (this.onStroke != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_Stroke, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_Stroke(new _IInkCollectorEvents_StrokeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Stroke)));
      }
    }

    public event InkCollectorNewPacketsEventHandler NewPackets
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onNewPackets += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_NewPackets, true);
        if (value == null || this.onNewPackets.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_NewPackets(new _IInkCollectorEvents_NewPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewPackets)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onNewPackets == null)
          return;
        this.onNewPackets -= value;
        if (this.onNewPackets != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_NewPackets, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_NewPackets(new _IInkCollectorEvents_NewPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewPackets)));
      }
    }

    public event InkCollectorNewInAirPacketsEventHandler NewInAirPackets
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onNewInAirPackets += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_NewInAirPackets, true);
        if (value == null || this.onNewInAirPackets.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_NewInAirPackets(new _IInkCollectorEvents_NewInAirPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewInAirPackets)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onNewInAirPackets == null)
          return;
        this.onNewInAirPackets -= value;
        if (this.onNewInAirPackets != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_NewInAirPackets, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_NewInAirPackets(new _IInkCollectorEvents_NewInAirPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewInAirPackets)));
      }
    }

    public event InkCollectorCursorInRangeEventHandler CursorInRange
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onCursorInRange += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorInRange, true);
        if (value == null || this.onCursorInRange.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_CursorInRange(new _IInkCollectorEvents_CursorInRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorInRange)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onCursorInRange == null)
          return;
        this.onCursorInRange -= value;
        if (this.onCursorInRange != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorInRange, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_CursorInRange(new _IInkCollectorEvents_CursorInRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorInRange)));
      }
    }

    public event InkCollectorCursorOutOfRangeEventHandler CursorOutOfRange
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onCursorOutOfRange += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorOutOfRange, true);
        if (value == null || this.onCursorOutOfRange.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_CursorOutOfRange(new _IInkCollectorEvents_CursorOutOfRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorOutOfRange)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onCursorOutOfRange == null)
          return;
        this.onCursorOutOfRange -= value;
        if (this.onCursorOutOfRange != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorOutOfRange, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_CursorOutOfRange(new _IInkCollectorEvents_CursorOutOfRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorOutOfRange)));
      }
    }

    public event InkCollectorCursorButtonDownEventHandler CursorButtonDown
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onCursorButtonDown += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorButtonDown, true);
        if (value == null || this.onCursorButtonDown.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_CursorButtonDown(new _IInkCollectorEvents_CursorButtonDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonDown)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onCursorButtonDown == null)
          return;
        this.onCursorButtonDown -= value;
        if (this.onCursorButtonDown != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorButtonDown, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_CursorButtonDown(new _IInkCollectorEvents_CursorButtonDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonDown)));
      }
    }

    public event InkCollectorCursorButtonUpEventHandler CursorButtonUp
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onCursorButtonUp += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorButtonUp, true);
        if (value == null || this.onCursorButtonUp.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_CursorButtonUp(new _IInkCollectorEvents_CursorButtonUpEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonUp)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onCursorButtonUp == null)
          return;
        this.onCursorButtonUp -= value;
        if (this.onCursorButtonUp != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorButtonUp, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_CursorButtonUp(new _IInkCollectorEvents_CursorButtonUpEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonUp)));
      }
    }

    public event InkCollectorTabletAddedEventHandler TabletAdded
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onTabletAdded += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_TabletAdded, true);
        if (value == null || this.onTabletAdded.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_TabletAdded(new _IInkCollectorEvents_TabletAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletAdded)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onTabletAdded == null)
          return;
        this.onTabletAdded -= value;
        if (this.onTabletAdded != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_TabletAdded, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_TabletAdded(new _IInkCollectorEvents_TabletAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletAdded)));
      }
    }

    public event InkCollectorTabletRemovedEventHandler TabletRemoved
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onTabletRemoved += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_TabletRemoved, true);
        if (value == null || this.onTabletRemoved.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_TabletRemoved(new _IInkCollectorEvents_TabletRemovedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletRemoved)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onTabletRemoved == null)
          return;
        this.onTabletRemoved -= value;
        if (this.onTabletRemoved != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_TabletRemoved, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_TabletRemoved(new _IInkCollectorEvents_TabletRemovedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletRemoved)));
      }
    }

    public event InkCollectorSystemGestureEventHandler SystemGesture
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onSystemGesture += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_SystemGesture, true);
        if (value == null || this.onSystemGesture.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_SystemGesture(new _IInkCollectorEvents_SystemGestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SystemGesture)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onSystemGesture == null)
          return;
        this.onSystemGesture -= value;
        if (this.onSystemGesture != null || this.m_Collector == null)
          return;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_SystemGesture, false);
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_SystemGesture(new _IInkCollectorEvents_SystemGestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SystemGesture)));
      }
    }

    public event InkCollectorGestureEventHandler Gesture
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onGesture += value;
        if (value == null || this.onGesture.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_Gesture(new _IInkCollectorEvents_GestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Gesture)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onGesture == null)
          return;
        this.onGesture -= value;
        if (this.onGesture != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_Gesture(new _IInkCollectorEvents_GestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Gesture)));
      }
    }

    ~InkCollector() => this.Dispose(false);

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    protected virtual void Dispose(bool disposing)
    {
      if (this.disposed)
        return;
      if (this.m_disposing)
        return;
      try
      {
        this.m_disposing = true;
        if (disposing)
        {
          if (this.m_Collector != null)
          {
            IntSecurity.RemoveComEventHandler.Assert();
            if (this.onStroke != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_Stroke(new _IInkCollectorEvents_StrokeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Stroke)));
            }
            if (this.onMouseDown != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_MouseDown(new _IInkCollectorEvents_MouseDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseDown)));
            }
            if (this.onMouseMove != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_MouseMove(new _IInkCollectorEvents_MouseMoveEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseMove)));
            }
            if (this.onMouseUp != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_MouseUp(new _IInkCollectorEvents_MouseUpEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseUp)));
            }
            if (this.onMouseWheel != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_MouseWheel(new _IInkCollectorEvents_MouseWheelEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_MouseWheel)));
            }
            if (this.onDoubleClick != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_DblClick(new _IInkCollectorEvents_DblClickEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_DoubleClick)));
            }
            if (this.onCursorDown != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_CursorDown(new _IInkCollectorEvents_CursorDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorDown)));
            }
            if (this.onNewPackets != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_NewPackets(new _IInkCollectorEvents_NewPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewPackets)));
            }
            if (this.onNewInAirPackets != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_NewInAirPackets(new _IInkCollectorEvents_NewInAirPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewInAirPackets)));
            }
            if (this.onCursorButtonDown != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_CursorButtonDown(new _IInkCollectorEvents_CursorButtonDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonDown)));
            }
            if (this.onCursorButtonUp != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_CursorButtonUp(new _IInkCollectorEvents_CursorButtonUpEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonUp)));
            }
            if (this.onCursorInRange != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_CursorInRange(new _IInkCollectorEvents_CursorInRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorInRange)));
            }
            if (this.onCursorOutOfRange != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_CursorOutOfRange(new _IInkCollectorEvents_CursorOutOfRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorOutOfRange)));
            }
            if (this.onSystemGesture != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_SystemGesture(new _IInkCollectorEvents_SystemGestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SystemGesture)));
            }
            if (this.onGesture != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_Gesture(new _IInkCollectorEvents_GestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Gesture)));
            }
            if (this.onTabletAdded != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_TabletAdded(new _IInkCollectorEvents_TabletAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletAdded)));
            }
            if (this.onTabletRemoved != null)
            {
              // ISSUE: method pointer
              this.m_Collector.remove_TabletRemoved(new _IInkCollectorEvents_TabletRemovedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletRemoved)));
            }
            CodeAccessPermission.RevertAssert();
          }
          SystemEvents.UserPreferenceChanging -= new UserPreferenceChangingEventHandler(this.sysEventHandler_UserPreferenceChanging);
          try
          {
            if (this.m_Collector != null)
              this.Enabled = false;
          }
          catch (InvalidOperationException ex)
          {
          }
          finally
          {
            if (this.m_attachedControl != null)
            {
              this.m_attachedControl.HandleCreated -= new EventHandler(this.OnHandleCreated);
              this.m_attachedControl.HandleDestroyed -= new EventHandler(this.OnHandleDestroyed);
              this.m_attachedControl = (Control) null;
            }
            this.m_Renderer = (Renderer) null;
            this.m_DrawingAttributes = (DrawingAttributes) null;
            if (this.m_Ink != null)
            {
              this.m_Ink.Dispose();
              this.m_Ink = (Microsoft.Ink.Ink) null;
            }
            this.m_Cursors = (Cursors) null;
            if (this.m_Collector != null)
              Marshal.ReleaseComObject((object) this.m_Collector);
            this.m_Collector = (InkCollectorPrivate) null;
            this.m_Errors = (ResourceManager) null;
          }
        }
      }
      finally
      {
        this.m_disposing = false;
      }
      this.disposed = true;
    }

    public InkCollector()
    {
      this.m_Errors = new ResourceManager("Microsoft.Ink.Resources.Errors", this.GetType().Assembly);
      this.m_Collector = (InkCollectorPrivate) new InkCollectorClass();
      this.m_DrawingAttributes = new DrawingAttributes(new _InternalDrawingAttributes((IInkDrawingAttributes) this.m_Collector.DefaultDrawingAttributes));
      this.m_Renderer = new Renderer((IInkRenderer) this.m_Collector.Renderer);
      this.m_Ink = new Microsoft.Ink.Ink(this.m_Collector.Ink);
      this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_AllEvents, false);
      this.m_MouseCursor = System.Windows.Forms.Cursors.Default;
      this.m_cachedInkEnabled = false;
      if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        this.m_Collector.SetEventInterest(~InkCollectorEventInterest.ICEI_Stroke, true);
      SystemEvents.UserPreferenceChanging += new UserPreferenceChangingEventHandler(this.sysEventHandler_UserPreferenceChanging);
    }

    public InkCollector(IntPtr handle)
      : this()
    {
      this.Handle = handle;
    }

    public InkCollector(Control attachedControl)
      : this()
    {
      this.AttachedControl = attachedControl;
    }

    public InkCollector(IntPtr handle, bool useMouseForInput)
      : this(handle)
    {
      this.SetAllTabletsMode(useMouseForInput);
    }

    public InkCollector(Control attachedControl, bool useMouseForInput)
      : this(attachedControl)
    {
      this.SetAllTabletsMode(useMouseForInput);
    }

    public InkCollector(IntPtr handle, Tablet tablet)
      : this(handle)
    {
      this.SetSingleTabletIntegratedMode(tablet);
    }

    public InkCollector(Control attachedControl, Tablet tablet)
      : this(attachedControl)
    {
      this.SetSingleTabletIntegratedMode(tablet);
    }

    private void SetHandleInternal(int hwnd, Control newAttachedControl)
    {
      this.m_Collector.hWnd = (long) hwnd;
      if (this.m_attachedControl != null)
      {
        this.m_attachedControl.HandleCreated -= new EventHandler(this.OnHandleCreated);
        this.m_attachedControl.HandleDestroyed -= new EventHandler(this.OnHandleDestroyed);
      }
      if (newAttachedControl == null)
        return;
      newAttachedControl.HandleCreated += new EventHandler(this.OnHandleCreated);
      newAttachedControl.HandleDestroyed += new EventHandler(this.OnHandleDestroyed);
    }

    public IntPtr Handle
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_attachedControl == null ? (IntPtr) this.m_Collector.hWnd : IntPtr.Zero;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          IntSecurity.DemandPermissionToCollectOnWindow(value);
          this.SetHandleInternal((int) value, (Control) null);
          this.m_attachedControl = (Control) null;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public Control AttachedControl
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_attachedControl;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.SetHandleInternal(value == null ? 0 : (int) value.Handle, value);
          this.m_attachedControl = value;
          if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            return;
          this.m_Collector.SetEventInterest(~InkCollectorEventInterest.ICEI_Stroke, true);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    private void OnHandleCreated(object sender, EventArgs e)
    {
      if (this.m_Collector == null || this.m_attachedControl != sender as Control)
        return;
      this.m_Collector.Enabled = false;
      this.m_Collector.hWnd = (long) (int) this.m_attachedControl.Handle;
      this.m_Collector.Enabled = this.m_cachedInkEnabled;
    }

    private void OnHandleDestroyed(object sender, EventArgs e)
    {
      if (this.m_Collector == null || this.m_attachedControl != sender as Control)
        return;
      this.m_Collector.Enabled = false;
    }

    public bool AutoRedraw
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Collector.AutoRedraw;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.m_Collector.AutoRedraw = value;
      }
    }

    public bool DynamicRendering
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Collector.DynamicRendering;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.m_Collector.DynamicRendering = value;
      }
    }

    public bool CollectingInk
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Collector.CollectingInk;
      }
    }

    public DrawingAttributes DefaultDrawingAttributes
    {
      get
      {
        lock (this.thisLock)
        {
          if (this.disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          return this.m_DrawingAttributes;
        }
      }
      set
      {
        lock (this.thisLock)
        {
          if (value == null)
            throw new ArgumentNullException(nameof (value));
          if (this.disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            this.m_Collector.DefaultDrawingAttributes = (InkDrawingAttributes) value.m_DrawingAttributes;
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
          this.m_DrawingAttributes = value;
        }
      }
    }

    public Renderer Renderer
    {
      get
      {
        lock (this.thisLock)
        {
          if (this.disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
        }
        return this.m_Renderer;
      }
      set
      {
        lock (this.thisLock)
        {
          if (value == null)
            throw new ArgumentNullException(nameof (value));
          if (this.disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            this.m_Collector.Renderer = (InkRenderer) value.m_Renderer;
            this.m_Renderer = value;
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
    }

    public Cursors Cursors
    {
      get
      {
        if (this.m_Cursors == null)
        {
          if (this.disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          this.m_Cursors = new Cursors(this.m_Collector.Cursors);
        }
        return this.m_Cursors;
      }
    }

    public Microsoft.Ink.Ink Ink
    {
      get
      {
        lock (this.thisLock)
        {
          if (this.disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          return this.m_Ink;
        }
      }
      set
      {
        lock (this.thisLock)
        {
          if (value == null)
            throw new ArgumentNullException(nameof (value));
          if (this.disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            this.m_Collector.Ink = value.m_Ink;
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
          this.m_Ink = value;
        }
      }
    }

    public bool Enabled
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_cachedInkEnabled;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Collector.Enabled = value;
          this.m_cachedInkEnabled = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public CollectionMode CollectionMode
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (CollectionMode) this.m_Collector.CollectionMode;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Collector.CollectionMode = (InkCollectionMode) value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public Guid[] DesiredPacketDescription
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        string[] packetDescription1 = (string[]) this.m_Collector.DesiredPacketDescription;
        Guid[] packetDescription2 = new Guid[packetDescription1.Length];
        for (int index = 0; index < packetDescription1.Length; ++index)
          packetDescription2[index] = new Guid(packetDescription1[index]);
        return packetDescription2;
      }
      set
      {
        if (value == null || value.Length == 0)
          throw new ArgumentNullException(nameof (value));
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        string[] strArray = new string[value.Length];
        for (int index = 0; index < strArray.Length; ++index)
          strArray[index] = value[index].ToString("B");
        this.m_Collector.DesiredPacketDescription = (object) strArray;
      }
    }

    public void SetGestureStatus(ApplicationGesture gesture, bool listening)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        this.m_Collector.SetGestureStatus((InkApplicationGesture) gesture, listening);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public bool GetGestureStatus(ApplicationGesture gesture)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        return this.m_Collector.GetGestureStatus((InkApplicationGesture) gesture);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public Tablet Tablet
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          return new Tablet(this.m_Collector.Tablet);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public System.Windows.Forms.Cursor Cursor
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_MouseCursor;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (!(this.m_MouseCursor != value))
          return;
        try
        {
          if (value == System.Windows.Forms.Cursors.Default || value == (System.Windows.Forms.Cursor) null)
          {
            this.m_Collector.MousePointer = InkMousePointer.IMP_Default;
            this.m_Collector.MouseIcon = (object) null;
          }
          else
          {
            Guid guid = typeof (SafeNativeMethods.IPictureDisp).GUID;
            IntSecurity.UnmanagedCode.Assert();
            this.m_Collector.MouseIcon = (object) SafeNativeMethods.OleCreateIPictureDispIndirect((object) new NativeMethods.PICTDESCicon(Icon.FromHandle(value.Handle)), ref guid, false);
            this.m_Collector.MousePointer = InkMousePointer.IMP_Custom;
          }
          this.m_MouseCursor = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public bool SupportHighContrastInk
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Collector.SupportHighContrastInk;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.m_Collector.SupportHighContrastInk = value;
      }
    }

    public int MarginX
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Collector.MarginX;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Collector.MarginX = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public int MarginY
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Collector.MarginY;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Collector.MarginY = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public void SetWindowInputRectangle(Rectangle windowInputRectangle)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkRectangle WindowInputRectangle = (InkRectangle) new InkRectangleClass();
      WindowInputRectangle.SetRectangle(windowInputRectangle.Top, windowInputRectangle.Left, windowInputRectangle.Bottom, windowInputRectangle.Right);
      try
      {
        this.m_Collector.SetWindowInputRectangle(WindowInputRectangle);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void GetWindowInputRectangle(out Rectangle windowInputRectangle)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkRectangle WindowInputRectangle = (InkRectangle) new InkRectangleClass();
      this.m_Collector.GetWindowInputRectangle(ref WindowInputRectangle);
      int Top;
      int Left;
      int Bottom;
      int Right;
      WindowInputRectangle.GetRectangle(out Top, out Left, out Bottom, out Right);
      windowInputRectangle = new Rectangle(Left, Top, Right - Left, Bottom - Top);
    }

    public void SetAllTabletsMode() => this.SetAllTabletsMode(true);

    public void SetAllTabletsMode(bool useMouseForInput)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        this.m_Collector.SetAllTabletsMode(useMouseForInput);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void SetSingleTabletIntegratedMode(Tablet tablet)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (tablet == null)
        throw new ArgumentNullException(nameof (tablet), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      try
      {
        this.m_Collector.SetSingleTabletIntegratedMode(tablet.m_Tablet);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    private void m_Collector_MouseDown(
      InkMouseButton button,
      InkShiftKeyModifierFlags shift,
      int x,
      int y,
      ref bool cancel)
    {
      CancelMouseEventArgs e = new CancelMouseEventArgs((MouseButtons) button, 1, x, y, 0, cancel);
      this.OnMouseDown(e);
      cancel = e.Cancel;
    }

    protected virtual void OnMouseDown(CancelMouseEventArgs e)
    {
      if (this.onMouseDown == null)
        return;
      this.onMouseDown((object) this, e);
    }

    private void m_Collector_MouseMove(
      InkMouseButton button,
      InkShiftKeyModifierFlags shift,
      int x,
      int y,
      ref bool cancel)
    {
      CancelMouseEventArgs e = new CancelMouseEventArgs((MouseButtons) button, 1, x, y, 0, cancel);
      this.OnMouseMove(e);
      cancel = e.Cancel;
    }

    protected virtual void OnMouseMove(CancelMouseEventArgs e)
    {
      if (this.onMouseMove == null)
        return;
      this.onMouseMove((object) this, e);
    }

    private void m_Collector_MouseUp(
      InkMouseButton button,
      InkShiftKeyModifierFlags shift,
      int x,
      int y,
      ref bool cancel)
    {
      CancelMouseEventArgs e = new CancelMouseEventArgs((MouseButtons) button, 1, x, y, 0, cancel);
      this.OnMouseUp(e);
      cancel = e.Cancel;
    }

    protected virtual void OnMouseUp(CancelMouseEventArgs e)
    {
      if (this.onMouseUp == null)
        return;
      this.onMouseUp((object) this, e);
    }

    private void m_Collector_MouseWheel(
      InkMouseButton button,
      InkShiftKeyModifierFlags shift,
      int delta,
      int x,
      int y,
      ref bool cancel)
    {
      CancelMouseEventArgs e = new CancelMouseEventArgs((MouseButtons) button, 1, x, y, delta, cancel);
      this.OnMouseWheel(e);
      cancel = e.Cancel;
    }

    protected virtual void OnMouseWheel(CancelMouseEventArgs e)
    {
      if (this.onMouseWheel == null)
        return;
      this.onMouseWheel((object) this, e);
    }

    private void m_Collector_DoubleClick(ref bool cancel)
    {
      CancelEventArgs e = new CancelEventArgs(cancel);
      this.OnDoubleClick(e);
      cancel = e.Cancel;
    }

    protected virtual void OnDoubleClick(CancelEventArgs e)
    {
      if (this.onDoubleClick == null)
        return;
      this.onDoubleClick((object) this, e);
    }

    private void m_Collector_Stroke(IInkCursor cursor, IInkStrokeDisp NewStroke, ref bool Cancel)
    {
      InkCollectorStrokeEventArgs e = new InkCollectorStrokeEventArgs(new Cursor(cursor), new Microsoft.Ink.Stroke(NewStroke), Cancel);
      this.OnStroke(e);
      Cancel = e.Cancel;
    }

    protected virtual void OnStroke(InkCollectorStrokeEventArgs e)
    {
      if (this.onStroke == null)
        return;
      this.onStroke((object) this, e);
    }

    private void m_Collector_CursorDown(IInkCursor cursor, IInkStrokeDisp NewStroke) => this.OnCursorDown(new InkCollectorCursorDownEventArgs(new Cursor(cursor), new Microsoft.Ink.Stroke(NewStroke)));

    protected virtual void OnCursorDown(InkCollectorCursorDownEventArgs e)
    {
      if (this.onCursorDown == null)
        return;
      this.onCursorDown((object) this, e);
    }

    private void m_Collector_NewPackets(
      IInkCursor cursor,
      IInkStrokeDisp UpdatedStroke,
      int PacketCount,
      ref object PacketData)
    {
      this.OnNewPackets(new InkCollectorNewPacketsEventArgs(new Cursor(cursor), new Microsoft.Ink.Stroke(UpdatedStroke), PacketCount, (int[]) PacketData));
    }

    protected virtual void OnNewPackets(InkCollectorNewPacketsEventArgs e)
    {
      if (this.onNewPackets == null)
        return;
      this.onNewPackets((object) this, e);
    }

    private void m_Collector_NewInAirPackets(
      IInkCursor cursor,
      int PacketCount,
      ref object PacketData)
    {
      this.OnNewInAirPackets(new InkCollectorNewInAirPacketsEventArgs(new Cursor(cursor), PacketCount, (int[]) PacketData));
    }

    protected virtual void OnNewInAirPackets(InkCollectorNewInAirPacketsEventArgs e)
    {
      if (this.onNewInAirPackets == null)
        return;
      this.onNewInAirPackets((object) this, e);
    }

    private void m_Collector_CursorButtonDown(IInkCursor cursor, IInkCursorButton button) => this.OnCursorButtonDown(new InkCollectorCursorButtonDownEventArgs(new Cursor(cursor), new CursorButton(button)));

    protected virtual void OnCursorButtonDown(InkCollectorCursorButtonDownEventArgs e)
    {
      if (this.onCursorButtonDown == null)
        return;
      this.onCursorButtonDown((object) this, e);
    }

    private void m_Collector_CursorButtonUp(IInkCursor cursor, IInkCursorButton button) => this.OnCursorButtonUp(new InkCollectorCursorButtonUpEventArgs(new Cursor(cursor), new CursorButton(button)));

    protected virtual void OnCursorButtonUp(InkCollectorCursorButtonUpEventArgs e)
    {
      if (this.onCursorButtonUp == null)
        return;
      this.onCursorButtonUp((object) this, e);
    }

    private void m_Collector_CursorInRange(IInkCursor cursor, bool newCursor, object o) => this.OnCursorInRange(new InkCollectorCursorInRangeEventArgs(new Cursor(cursor), newCursor, (CursorButtonState[]) o));

    protected virtual void OnCursorInRange(InkCollectorCursorInRangeEventArgs e)
    {
      if (this.onCursorInRange == null)
        return;
      this.onCursorInRange((object) this, e);
    }

    private void m_Collector_CursorOutOfRange(IInkCursor cursor) => this.OnCursorOutOfRange(new InkCollectorCursorOutOfRangeEventArgs(new Cursor(cursor)));

    protected virtual void OnCursorOutOfRange(InkCollectorCursorOutOfRangeEventArgs e)
    {
      if (this.onCursorOutOfRange == null)
        return;
      this.onCursorOutOfRange((object) this, e);
    }

    private void m_Collector_SystemGesture(
      IInkCursor cursor,
      InkSystemGesture id,
      int x,
      int y,
      int modifier,
      string character,
      int mode)
    {
      this.OnSystemGesture(new InkCollectorSystemGestureEventArgs(new Cursor(cursor), (Microsoft.Ink.SystemGesture) id, new Point(x, y), modifier, string.IsNullOrEmpty(character) ? char.MinValue : character[0], mode));
    }

    protected virtual void OnSystemGesture(InkCollectorSystemGestureEventArgs e)
    {
      if (this.onSystemGesture == null)
        return;
      this.onSystemGesture((object) this, e);
    }

    private void m_Collector_Gesture(
      IInkCursor cursor,
      InkStrokes strokes,
      object oGestures,
      ref bool Cancel)
    {
      Array array = (Array) oGestures;
      Microsoft.Ink.Gesture[] gestures = new Microsoft.Ink.Gesture[array.Length];
      for (int index = 0; index < gestures.Length; ++index)
        gestures.SetValue((object) new Microsoft.Ink.Gesture((IInkGesture) array.GetValue(index)), index);
      InkCollectorGestureEventArgs e = new InkCollectorGestureEventArgs(new Cursor(cursor), new Strokes(strokes), gestures, Cancel);
      this.OnGesture(e);
      Cancel = e.Cancel;
    }

    protected virtual void OnGesture(InkCollectorGestureEventArgs e)
    {
      if (this.onGesture == null)
        return;
      this.onGesture((object) this, e);
    }

    private void m_Collector_TabletAdded(IInkTablet tablet) => this.OnTabletAdded(new InkCollectorTabletAddedEventArgs(new Tablet(tablet)));

    protected virtual void OnTabletAdded(InkCollectorTabletAddedEventArgs e)
    {
      if (this.onTabletAdded == null)
        return;
      this.onTabletAdded((object) this, e);
    }

    private void m_Collector_TabletRemoved(int tabletId) => this.OnTabletRemoved(new InkCollectorTabletRemovedEventArgs(tabletId));

    protected virtual void OnTabletRemoved(InkCollectorTabletRemovedEventArgs e)
    {
      if (this.onTabletRemoved == null)
        return;
      this.onTabletRemoved((object) this, e);
    }

    private void sysEventHandler_UserPreferenceChanging(
      object sender,
      UserPreferenceChangingEventArgs e)
    {
      if (this.m_Collector == null || e.Category != UserPreferenceCategory.Color)
        return;
      int num = (int) UnsafeNativeMethods.SendMessage((IntPtr) this.m_Collector.hWnd, 21, IntPtr.Zero, IntPtr.Zero);
    }
  }
}
