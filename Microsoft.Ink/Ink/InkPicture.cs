// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkPicture
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
  [DefaultProperty("InkEnabled")]
  [DefaultEvent("Stroke")]
  [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class InkPicture : PictureBox
  {
    private const int WS_EX_LAYOUTRTL = 4194304;
    private const int FireEventsOnWindowThread = -2;
    private InkOverlayPrivate m_Collector;
    public readonly int DefaultMargin = int.MinValue;
    public readonly int ClipInkToMargin;
    private ResourceManager m_Errors;
    private object thisLock = new object();
    private Renderer m_Renderer;
    private DrawingAttributes m_DrawingAttributes;
    private Microsoft.Ink.Ink m_Ink;
    private Cursors m_Cursors;
    private bool m_cachedInkEnabled = true;
    private InkCollectorCursorDownEventHandler onCursorDown;
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
    [SRCategory("CategoryInk")]
    [SRDescription("InkOverlayEventPainting")]
    private InkOverlayPaintingEventHandler onPainting;
    [SRDescription("InkOverlayEventPainted")]
    [SRCategory("CategoryInk")]
    private InkOverlayPaintedEventHandler onPainted;
    [SRDescription("InkOverlayEventSelectionChanging")]
    [SRCategory("CategoryInk")]
    private InkOverlaySelectionChangingEventHandler onSelectionChanging;
    [SRCategory("CategoryInk")]
    [SRDescription("InkOverlayEventSelectionChanged")]
    private InkOverlaySelectionChangedEventHandler onSelectionChanged;
    [SRCategory("CategoryInk")]
    [SRDescription("InkOverlayEventSelectionMoving")]
    private InkOverlaySelectionMovingEventHandler onSelectionMoving;
    [SRCategory("CategoryInk")]
    [SRDescription("InkOverlayEventSelectionResizing")]
    private InkOverlaySelectionResizingEventHandler onSelectionResizing;
    [SRDescription("InkOverlayEventSelectionMoved")]
    [SRCategory("CategoryInk")]
    private InkOverlaySelectionMovedEventHandler onSelectionMoved;
    [SRDescription("InkOverlayEventSelectionResized")]
    [SRCategory("CategoryInk")]
    private InkOverlaySelectionResizedEventHandler onSelectionResized;
    [SRDescription("InkOverlayEventStrokesDeleting")]
    [SRCategory("CategoryInk")]
    private InkOverlayStrokesDeletingEventHandler onStrokesDeleting;
    [SRDescription("InkOverlayEventStrokesDeleted")]
    [SRCategory("CategoryInk")]
    private InkOverlayStrokesDeletedEventHandler onStrokesDeleted;
    private InkCollectorGestureEventHandler onGesture;
    private bool m_disposing;

    [SRDescription("InkCollectorEventCursorDown")]
    [SRCategory("CategoryInk")]
    public event InkCollectorCursorDownEventHandler CursorDown
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onCursorDown += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorDown, true);
        if (value == null || this.onCursorDown.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_CursorDown(new _IInkOverlayEvents_CursorDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorDown)));
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
        this.m_Collector.remove_CursorDown(new _IInkOverlayEvents_CursorDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorDown)));
      }
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkCollectorEventStroke")]
    public event InkCollectorStrokeEventHandler Stroke
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onStroke += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_Stroke, true);
        if (value == null || this.onStroke.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_Stroke(new _IInkOverlayEvents_StrokeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Stroke)));
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
        this.m_Collector.remove_Stroke(new _IInkOverlayEvents_StrokeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Stroke)));
      }
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkCollectorEventNewPackets")]
    public event InkCollectorNewPacketsEventHandler NewPackets
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onNewPackets += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_NewPackets, true);
        if (value == null || this.onNewPackets.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_NewPackets(new _IInkOverlayEvents_NewPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewPackets)));
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
        this.m_Collector.remove_NewPackets(new _IInkOverlayEvents_NewPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewPackets)));
      }
    }

    [SRDescription("InkCollectorEventNewInAirPackets")]
    [SRCategory("CategoryInk")]
    public event InkCollectorNewInAirPacketsEventHandler NewInAirPackets
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onNewInAirPackets += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_NewInAirPackets, true);
        if (value == null || this.onNewInAirPackets.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_NewInAirPackets(new _IInkOverlayEvents_NewInAirPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewInAirPackets)));
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
        this.m_Collector.remove_NewInAirPackets(new _IInkOverlayEvents_NewInAirPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewInAirPackets)));
      }
    }

    [SRDescription("InkCollectorEventCursorInRange")]
    [SRCategory("CategoryInk")]
    public event InkCollectorCursorInRangeEventHandler CursorInRange
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onCursorInRange += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorInRange, true);
        if (value == null || this.onCursorInRange.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_CursorInRange(new _IInkOverlayEvents_CursorInRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorInRange)));
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
        this.m_Collector.remove_CursorInRange(new _IInkOverlayEvents_CursorInRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorInRange)));
      }
    }

    [SRDescription("InkCollectorEventCursorOutOfRange")]
    [SRCategory("CategoryInk")]
    public event InkCollectorCursorOutOfRangeEventHandler CursorOutOfRange
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onCursorOutOfRange += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorOutOfRange, true);
        if (value == null || this.onCursorOutOfRange.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_CursorOutOfRange(new _IInkOverlayEvents_CursorOutOfRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorOutOfRange)));
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
        this.m_Collector.remove_CursorOutOfRange(new _IInkOverlayEvents_CursorOutOfRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorOutOfRange)));
      }
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkCollectorEventCursorButtonDown")]
    public event InkCollectorCursorButtonDownEventHandler CursorButtonDown
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onCursorButtonDown += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorButtonDown, true);
        if (value == null || this.onCursorButtonDown.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_CursorButtonDown(new _IInkOverlayEvents_CursorButtonDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonDown)));
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
        this.m_Collector.remove_CursorButtonDown(new _IInkOverlayEvents_CursorButtonDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonDown)));
      }
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkCollectorEventCursorButtonUp")]
    public event InkCollectorCursorButtonUpEventHandler CursorButtonUp
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onCursorButtonUp += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_CursorButtonUp, true);
        if (value == null || this.onCursorButtonUp.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_CursorButtonUp(new _IInkOverlayEvents_CursorButtonUpEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonUp)));
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
        this.m_Collector.remove_CursorButtonUp(new _IInkOverlayEvents_CursorButtonUpEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonUp)));
      }
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkCollectorEventTabletAdded")]
    public event InkCollectorTabletAddedEventHandler TabletAdded
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onTabletAdded += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_TabletAdded, true);
        if (value == null || this.onTabletAdded.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_TabletAdded(new _IInkOverlayEvents_TabletAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletAdded)));
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
        this.m_Collector.remove_TabletAdded(new _IInkOverlayEvents_TabletAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletAdded)));
      }
    }

    [SRDescription("InkCollectorEventTabletRemoved")]
    [SRCategory("CategoryInk")]
    public event InkCollectorTabletRemovedEventHandler TabletRemoved
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onTabletRemoved += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_TabletRemoved, true);
        if (value == null || this.onTabletRemoved.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_TabletRemoved(new _IInkOverlayEvents_TabletRemovedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletRemoved)));
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
        this.m_Collector.remove_TabletRemoved(new _IInkOverlayEvents_TabletRemovedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletRemoved)));
      }
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkCollectorEventSystemGesture")]
    public event InkCollectorSystemGestureEventHandler SystemGesture
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onSystemGesture += value;
        this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_SystemGesture, true);
        if (value == null || this.onSystemGesture.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_SystemGesture(new _IInkOverlayEvents_SystemGestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SystemGesture)));
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
        this.m_Collector.remove_SystemGesture(new _IInkOverlayEvents_SystemGestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SystemGesture)));
      }
    }

    public event InkOverlayPaintingEventHandler Painting
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onPainting += value;
        if (value == null || this.onPainting.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_Painting(new _IInkOverlayEvents_PaintingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Painting)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onPainting == null)
          return;
        this.onPainting -= value;
        if (this.onPainting != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_Painting(new _IInkOverlayEvents_PaintingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Painting)));
      }
    }

    public event InkOverlayPaintedEventHandler Painted
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onPainted += value;
        if (value == null || this.onPainted.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_Painted(new _IInkOverlayEvents_PaintedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Painted)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onPainted == null)
          return;
        this.onPainted -= value;
        if (this.onPainted != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_Painted(new _IInkOverlayEvents_PaintedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Painted)));
      }
    }

    public event InkOverlaySelectionChangingEventHandler SelectionChanging
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onSelectionChanging += value;
        if (value == null || this.onSelectionChanging.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_SelectionChanging(new _IInkOverlayEvents_SelectionChangingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionChanging)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onSelectionChanging == null)
          return;
        this.onSelectionChanging -= value;
        if (this.onSelectionChanging != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_SelectionChanging(new _IInkOverlayEvents_SelectionChangingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionChanging)));
      }
    }

    public event InkOverlaySelectionChangedEventHandler SelectionChanged
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onSelectionChanged += value;
        if (value == null || this.onSelectionChanged.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_SelectionChanged(new _IInkOverlayEvents_SelectionChangedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionChanged)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onSelectionChanged == null)
          return;
        this.onSelectionChanged -= value;
        if (this.onSelectionChanged != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_SelectionChanged(new _IInkOverlayEvents_SelectionChangedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionChanged)));
      }
    }

    public event InkOverlaySelectionMovingEventHandler SelectionMoving
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onSelectionMoving += value;
        if (value == null || this.onSelectionMoving.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_SelectionMoving(new _IInkOverlayEvents_SelectionMovingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionMoving)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onSelectionMoving == null)
          return;
        this.onSelectionMoving -= value;
        if (this.onSelectionMoving != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_SelectionMoving(new _IInkOverlayEvents_SelectionMovingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionMoving)));
      }
    }

    public event InkOverlaySelectionResizingEventHandler SelectionResizing
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onSelectionResizing += value;
        if (value == null || this.onSelectionResizing.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_SelectionResizing(new _IInkOverlayEvents_SelectionResizingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionResizing)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onSelectionResizing == null)
          return;
        this.onSelectionResizing -= value;
        if (this.onSelectionResizing != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_SelectionResizing(new _IInkOverlayEvents_SelectionResizingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionResizing)));
      }
    }

    public event InkOverlaySelectionMovedEventHandler SelectionMoved
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onSelectionMoved += value;
        if (value == null || this.onSelectionMoved.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_SelectionMoved(new _IInkOverlayEvents_SelectionMovedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionMoved)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onSelectionMoved == null)
          return;
        this.onSelectionMoved -= value;
        if (this.onSelectionMoved != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_SelectionMoved(new _IInkOverlayEvents_SelectionMovedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionMoved)));
      }
    }

    public event InkOverlaySelectionResizedEventHandler SelectionResized
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onSelectionResized += value;
        if (value == null || this.onSelectionResized.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_SelectionResized(new _IInkOverlayEvents_SelectionResizedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionResized)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onSelectionResized == null)
          return;
        this.onSelectionResized -= value;
        if (this.onSelectionResized != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_SelectionResized(new _IInkOverlayEvents_SelectionResizedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionResized)));
      }
    }

    public event InkOverlayStrokesDeletingEventHandler StrokesDeleting
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onStrokesDeleting += value;
        if (value == null || this.onStrokesDeleting.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_StrokesDeleting(new _IInkOverlayEvents_StrokesDeletingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_StrokesDeleting)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onStrokesDeleting == null)
          return;
        this.onStrokesDeleting -= value;
        if (this.onStrokesDeleting != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_StrokesDeleting(new _IInkOverlayEvents_StrokesDeletingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_StrokesDeleting)));
      }
    }

    public event InkOverlayStrokesDeletedEventHandler StrokesDeleted
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onStrokesDeleted += value;
        if (value == null || this.onStrokesDeleted.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_StrokesDeleted(new _IInkOverlayEvents_StrokesDeletedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_StrokesDeleted)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onStrokesDeleted == null)
          return;
        this.onStrokesDeleted -= value;
        if (this.onStrokesDeleted != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_StrokesDeleted(new _IInkOverlayEvents_StrokesDeletedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_StrokesDeleted)));
      }
    }

    [SRDescription("InkCollectorEventGesture")]
    [SRCategory("CategoryInk")]
    public event InkCollectorGestureEventHandler Gesture
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onGesture += value;
        if (value == null || this.onGesture.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Collector.add_Gesture(new _IInkOverlayEvents_GestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Gesture)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onGesture == null)
          return;
        this.onGesture -= value;
        if (this.onGesture != null || this.m_Collector == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Collector.remove_Gesture(new _IInkOverlayEvents_GestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Gesture)));
      }
    }

    private bool disposed => this.IsDisposed;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [Browsable(false)]
    public new bool Disposing => this.m_disposing || base.Disposing;

    protected override void Dispose(bool disposing)
    {
      if (this.IsDisposed)
        return;
      if (this.Disposing)
        return;
      try
      {
        this.m_disposing = true;
        if (!disposing)
          return;
        if (this.m_Collector != null)
        {
          IntSecurity.RemoveComEventHandler.Assert();
          if (this.onStroke != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_Stroke(new _IInkOverlayEvents_StrokeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Stroke)));
          }
          if (this.onCursorDown != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_CursorDown(new _IInkOverlayEvents_CursorDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorDown)));
          }
          if (this.onNewPackets != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_NewPackets(new _IInkOverlayEvents_NewPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewPackets)));
          }
          if (this.onNewInAirPackets != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_NewInAirPackets(new _IInkOverlayEvents_NewInAirPacketsEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_NewInAirPackets)));
          }
          if (this.onCursorButtonDown != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_CursorButtonDown(new _IInkOverlayEvents_CursorButtonDownEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonDown)));
          }
          if (this.onCursorButtonUp != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_CursorButtonUp(new _IInkOverlayEvents_CursorButtonUpEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorButtonUp)));
          }
          if (this.onCursorInRange != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_CursorInRange(new _IInkOverlayEvents_CursorInRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorInRange)));
          }
          if (this.onCursorOutOfRange != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_CursorOutOfRange(new _IInkOverlayEvents_CursorOutOfRangeEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_CursorOutOfRange)));
          }
          if (this.onSystemGesture != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_SystemGesture(new _IInkOverlayEvents_SystemGestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SystemGesture)));
          }
          if (this.onGesture != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_Gesture(new _IInkOverlayEvents_GestureEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Gesture)));
          }
          if (this.onTabletAdded != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_TabletAdded(new _IInkOverlayEvents_TabletAddedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletAdded)));
          }
          if (this.onTabletRemoved != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_TabletRemoved(new _IInkOverlayEvents_TabletRemovedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_TabletRemoved)));
          }
          if (this.onPainting != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_Painting(new _IInkOverlayEvents_PaintingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Painting)));
          }
          if (this.onPainted != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_Painted(new _IInkOverlayEvents_PaintedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_Painted)));
          }
          if (this.onSelectionChanging != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_SelectionChanging(new _IInkOverlayEvents_SelectionChangingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionChanging)));
          }
          if (this.onSelectionChanged != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_SelectionChanged(new _IInkOverlayEvents_SelectionChangedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionChanged)));
          }
          if (this.onSelectionMoving != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_SelectionMoving(new _IInkOverlayEvents_SelectionMovingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionMoving)));
          }
          if (this.onSelectionResizing != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_SelectionResizing(new _IInkOverlayEvents_SelectionResizingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionResizing)));
          }
          if (this.onSelectionMoved != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_SelectionMoved(new _IInkOverlayEvents_SelectionMovedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionMoved)));
          }
          if (this.onSelectionResized != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_SelectionResized(new _IInkOverlayEvents_SelectionResizedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_SelectionResized)));
          }
          if (this.onStrokesDeleting != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_StrokesDeleting(new _IInkOverlayEvents_StrokesDeletingEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_StrokesDeleting)));
          }
          if (this.onStrokesDeleted != null)
          {
            // ISSUE: method pointer
            this.m_Collector.remove_StrokesDeleted(new _IInkOverlayEvents_StrokesDeletedEventHandler((object) this, (UIntPtr) __methodptr(m_Collector_StrokesDeleted)));
          }
          CodeAccessPermission.RevertAssert();
        }
        SystemEvents.UserPreferenceChanging -= new UserPreferenceChangingEventHandler(this.sysEventHandler_UserPreferenceChanging);
        try
        {
          if (this.m_Collector == null)
            return;
          this.InkEnabled = false;
        }
        catch (InvalidOperationException ex)
        {
        }
        finally
        {
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
          this.m_Collector = (InkOverlayPrivate) null;
          this.m_Errors = (ResourceManager) null;
        }
      }
      finally
      {
        this.m_disposing = false;
        base.Dispose(disposing);
      }
    }

    public InkPicture()
    {
      this.SetStyle(ControlStyles.Selectable, true);
      this.TabStop = true;
      this.m_Errors = new ResourceManager("Microsoft.Ink.Resources.Errors", this.GetType().Assembly);
      this.m_Collector = (InkOverlayPrivate) new InkOverlayClass();
      this.m_DrawingAttributes = new DrawingAttributes(new _InternalDrawingAttributes((IInkDrawingAttributes) this.m_Collector.DefaultDrawingAttributes));
      this.m_Renderer = new Renderer((IInkRenderer) this.m_Collector.Renderer);
      this.m_Ink = new Microsoft.Ink.Ink(this.m_Collector.Ink);
      this.m_Collector.SetEventInterest(InkCollectorEventInterest.ICEI_AllEvents, false);
      if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        this.m_Collector.SetEventInterest(~InkCollectorEventInterest.ICEI_Stroke, true);
      SystemEvents.UserPreferenceChanging += new UserPreferenceChangingEventHandler(this.sysEventHandler_UserPreferenceChanging);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
      base.OnHandleCreated(e);
      if (this.DesignMode || !this.IsHandleCreated)
        return;
      this.m_Collector.Enabled = false;
      this.m_Collector.hWnd = (long) (int) this.Handle;
      if (base.Cursor == System.Windows.Forms.Cursors.Default || base.Cursor == (System.Windows.Forms.Cursor) null)
      {
        this.m_Collector.MousePointer = InkMousePointer.IMP_Default;
        this.m_Collector.MouseIcon = (object) null;
      }
      else
      {
        Guid guid = typeof (SafeNativeMethods.IPictureDisp).GUID;
        IntSecurity.UnmanagedCode.Assert();
        this.m_Collector.MouseIcon = (object) SafeNativeMethods.OleCreateIPictureDispIndirect((object) new NativeMethods.PICTDESCicon(Icon.FromHandle(base.Cursor.Handle)), ref guid, false);
        this.m_Collector.MousePointer = InkMousePointer.IMP_Custom;
      }
      this.m_Collector.Enabled = this.m_cachedInkEnabled;
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
      if (this.m_Collector != null)
        this.m_Collector.Enabled = false;
      base.OnHandleDestroyed(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
      IntSecurity.AllWindows.Assert();
      this.Focus();
      CodeAccessPermission.RevertAssert();
      base.OnMouseDown(e);
    }

    [SRCategory("CategoryInk")]
    [DefaultValue(true)]
    [Browsable(true)]
    [SRDescription("InkCollectorPropertyAutoRedraw")]
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

    [SRCategory("CategoryInk")]
    [Browsable(true)]
    [DefaultValue(true)]
    [SRDescription("InkCollectorPropertyDynamicRendering")]
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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    [SRCategory("CategoryInk")]
    [SRDescription("InkCollectorPropertyCollectingInk")]
    public bool CollectingInk
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Collector.CollectingInk;
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [SRCategory("CategoryInk")]
    [Browsable(false)]
    [SRDescription("InkCollectorPropertyDefaultDrawingAttributes")]
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

    [SRCategory("CategoryInk")]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [SRDescription("InkCollectorPropertyRenderer")]
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

    [Browsable(false)]
    [SRDescription("InkCollectorPropertyCursors")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [SRCategory("CategoryInk")]
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

    [SRDescription("InkCollectorPropertyInk")]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [SRCategory("CategoryInk")]
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

    [SRCategory("CategoryInk")]
    [DefaultValue(true)]
    [Browsable(true)]
    [SRDescription("InkPicturePropertyInkEnabled")]
    public bool InkEnabled
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
          if (this.m_cachedInkEnabled == value)
            return;
          if (!this.DesignMode && this.IsHandleCreated)
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

    [SRCategory("CategoryInk")]
    [Browsable(true)]
    [DefaultValue(CollectionMode.InkOnly)]
    [SRDescription("InkCollectorPropertyCollectionMode")]
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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [SRCategory("CategoryInk")]
    [Browsable(false)]
    [SRDescription("InkCollectorPropertyDesiredPacketDescription")]
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

    [SRDescription("InkCollectorPropertyTablet")]
    [SRCategory("CategoryInk")]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

    [SRDescription("InkPicturePropertyCursor")]
    [SRCategory("CategoryInk")]
    [Browsable(true)]
    public override System.Windows.Forms.Cursor Cursor
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return base.Cursor;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (base.Cursor != value)
          base.Cursor = value;
        if (this.DesignMode)
          return;
        try
        {
          if (base.Cursor == System.Windows.Forms.Cursors.Default || base.Cursor == (System.Windows.Forms.Cursor) null)
          {
            this.m_Collector.MousePointer = InkMousePointer.IMP_Default;
            this.m_Collector.MouseIcon = (object) null;
          }
          else
          {
            Guid guid = typeof (SafeNativeMethods.IPictureDisp).GUID;
            IntSecurity.UnmanagedCode.Assert();
            this.m_Collector.MouseIcon = (object) SafeNativeMethods.OleCreateIPictureDispIndirect((object) new NativeMethods.PICTDESCicon(Icon.FromHandle(base.Cursor.Handle)), ref guid, false);
            this.m_Collector.MousePointer = InkMousePointer.IMP_Custom;
          }
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    [Browsable(true)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    public new bool TabStop
    {
      get => base.TabStop;
      set => base.TabStop = value;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
      base.OnKeyDown(e);
      if (e.Handled)
        return;
      if (e.Shift && e.KeyValue == 46 || e.Control && e.KeyValue == 88)
      {
        this._CutDeleteHandler(true);
        e.Handled = true;
      }
      else if (e.Control && e.KeyValue == 45 || e.Control && e.KeyValue == 67)
      {
        IntSecurity.ClipboardWrite.Assert();
        this.m_Ink.ClipboardCopy(this.Selection, InkClipboardFormats.CopyMask, InkClipboardModes.Copy);
        e.Handled = true;
      }
      else if (e.Shift && e.KeyValue == 45 || e.Control && e.KeyValue == 86)
      {
        Point pt = new Point(15, 15);
        using (Graphics graphics = this.CreateGraphics())
          this.m_Renderer.PixelToInkSpace(graphics, ref pt);
        IntSecurity.ClipboardRead.Assert();
        Strokes strokes = this.m_Ink.ClipboardPaste(pt);
        if (strokes == null)
          return;
        this.Selection = strokes;
      }
      else
      {
        if (e.Alt || e.Control || e.Shift || e.KeyValue != 46)
          return;
        this._CutDeleteHandler(false);
        e.Handled = true;
      }
    }

    private void _CutDeleteHandler(bool fCut)
    {
      Strokes selection = this.Selection;
      if (0 >= selection.Count)
        return;
      Rectangle rc1 = selection.GetBoundingBox();
      InkOverlayStrokesDeletingEventArgs e = new InkOverlayStrokesDeletingEventArgs(selection);
      try
      {
        this.OnStrokesDeleting(e);
        if (fCut)
        {
          IntSecurity.ClipboardWrite.Assert();
          this.m_Ink.ClipboardCopy(selection, InkClipboardFormats.CopyMask, InkClipboardModes.Copy);
          CodeAccessPermission.RevertAssert();
        }
        Point pt1 = new Point(rc1.Left, rc1.Top);
        Point pt2 = new Point(rc1.Right, rc1.Bottom);
        Rectangle rc2;
        Point pt3;
        Point pt4;
        using (Graphics graphics = this.CreateGraphics())
        {
          this.m_Renderer.InkSpaceToPixel(graphics, ref pt1);
          this.m_Renderer.InkSpaceToPixel(graphics, ref pt2);
          rc1 = new Rectangle(pt1.X, pt1.Y, pt2.X - pt1.X, pt2.Y - pt1.Y);
          rc1.Inflate(10, 10);
          rc2 = new Rectangle(0, 0, 0, 0);
          pt3 = new Point(rc2.Left, rc2.Top);
          pt4 = new Point(rc2.Right, rc2.Bottom);
          this.m_Renderer.InkSpaceToPixel(graphics, ref pt3);
          this.m_Renderer.InkSpaceToPixel(graphics, ref pt4);
        }
        rc2 = new Rectangle(pt3.X, pt3.Y, pt4.X - pt3.X, pt4.Y - pt3.Y);
        this.m_Ink.DeleteStrokes(selection);
        selection.Add(this.Selection);
        this.Selection = selection;
        this.Invalidate(rc1, false);
        this.Invalidate(rc2, false);
      }
      finally
      {
        this.OnStrokesDeleted(new EventArgs());
      }
    }

    [SRDescription("InkCollectorPropertySupportHighContrastInk")]
    [DefaultValue(true)]
    [Browsable(true)]
    [SRCategory("CategoryInk")]
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

    [SRCategory("CategoryInk")]
    [Browsable(true)]
    [SRDescription("InkOverlayPropertySupportHighContrastSelectionUI")]
    [DefaultValue(true)]
    public bool SupportHighContrastSelectionUI
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Collector.SupportHighContrastSelectionUI;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.m_Collector.SupportHighContrastSelectionUI = value;
      }
    }

    public SelectionHitResult HitTestSelection(int X, int Y)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      return (SelectionHitResult) this.m_Collector.HitTestSelection(X, Y);
    }

    [Browsable(true)]
    [DefaultValue(0)]
    [SRCategory("CategoryInk")]
    [SRDescription("InkCollectorPropertyMarginX")]
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

    [SRCategory("CategoryInk")]
    [DefaultValue(0)]
    [Browsable(true)]
    [SRDescription("InkCollectorPropertyMarginY")]
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

    [Browsable(true)]
    [SRCategory("CategoryInk")]
    [DefaultValue(InkOverlayEditingMode.Ink)]
    [SRDescription("InkOverlayPropertyEditingMode")]
    public InkOverlayEditingMode EditingMode
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (InkOverlayEditingMode) this.m_Collector.EditingMode;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Collector.EditingMode = (InkOverlayEditingModePrivate) value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    [DefaultValue(InkOverlayEraserMode.StrokeErase)]
    [SRDescription("InkOverlayPropertyEraserMode")]
    [SRCategory("CategoryInk")]
    [Browsable(true)]
    public InkOverlayEraserMode EraserMode
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (InkOverlayEraserMode) this.m_Collector.EraserMode;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Collector.EraserMode = (InkOverlayEraserModePrivate) value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    [DefaultValue(212)]
    [SRCategory("CategoryInk")]
    [Browsable(true)]
    [SRDescription("InkOverlayPropertyEraserWidth")]
    public int EraserWidth
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Collector.EraserWidth;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Collector.EraserWidth = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    [Browsable(false)]
    [SRCategory("CategoryInk")]
    [SRDescription("InkOverlayPropertySelection")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Strokes Selection
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return new Strokes(this.m_Collector.Selection);
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (value == null)
          throw new ArgumentNullException(nameof (value), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        try
        {
          this.m_Collector.Selection = value.m_Strokes;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    private void m_Collector_Painting(int hDC, InkRectangle Rect, ref bool Allow)
    {
      IntSecurity.UnmanagedCode.Assert();
      InkOverlayPaintingEventArgs e = new InkOverlayPaintingEventArgs(Graphics.FromHdc((IntPtr) hDC), new Rectangle(Rect.Left, Rect.Top, Rect.Right - Rect.Left, Rect.Bottom - Rect.Top), Allow);
      try
      {
        CodeAccessPermission.RevertAssert();
        this.OnPainting(e);
        Allow = !e.Cancel;
      }
      finally
      {
        e.Dispose();
      }
    }

    protected virtual void OnPainting(InkOverlayPaintingEventArgs e)
    {
      if (this.onPainting == null)
        return;
      this.onPainting((object) this, e);
    }

    private void m_Collector_Painted(int hDC, InkRectangle Rect)
    {
      IntSecurity.UnmanagedCode.Assert();
      PaintEventArgs e = new PaintEventArgs(Graphics.FromHdc((IntPtr) hDC), new Rectangle(Rect.Left, Rect.Top, Rect.Right - Rect.Left, Rect.Bottom - Rect.Top));
      try
      {
        CodeAccessPermission.RevertAssert();
        this.OnPainted(e);
      }
      finally
      {
        e.Dispose();
      }
    }

    protected virtual void OnPainted(PaintEventArgs e)
    {
      if (this.onPainted == null)
        return;
      this.onPainted((object) this, e);
    }

    private void m_Collector_SelectionChanging(InkStrokes Strokes) => this.OnSelectionChanging(new InkOverlaySelectionChangingEventArgs(new Strokes(Strokes)));

    protected virtual void OnSelectionChanging(InkOverlaySelectionChangingEventArgs e)
    {
      if (this.onSelectionChanging == null)
        return;
      this.onSelectionChanging((object) this, e);
    }

    private void m_Collector_SelectionChanged() => this.OnSelectionChanged(EventArgs.Empty);

    protected virtual void OnSelectionChanged(EventArgs e)
    {
      if (this.onSelectionChanged == null)
        return;
      this.onSelectionChanged((object) this, e);
    }

    private void m_Collector_SelectionMoving(InkRectangle Rect) => this.OnSelectionMoving(new InkOverlaySelectionMovingEventArgs(new Rectangle(Rect.Left, Rect.Top, Rect.Right - Rect.Left, Rect.Bottom - Rect.Top)));

    protected virtual void OnSelectionMoving(InkOverlaySelectionMovingEventArgs e)
    {
      if (this.onSelectionMoving == null)
        return;
      this.onSelectionMoving((object) this, e);
    }

    private void m_Collector_SelectionResizing(InkRectangle Rect) => this.OnSelectionResizing(new InkOverlaySelectionResizingEventArgs(new Rectangle(Rect.Left, Rect.Top, Rect.Right - Rect.Left, Rect.Bottom - Rect.Top)));

    protected virtual void OnSelectionResizing(InkOverlaySelectionResizingEventArgs e)
    {
      if (this.onSelectionResizing == null)
        return;
      this.onSelectionResizing((object) this, e);
    }

    private void m_Collector_SelectionMoved(InkRectangle Rect) => this.OnSelectionMoved(new InkOverlaySelectionMovedEventArgs(new Rectangle(Rect.Left, Rect.Top, Rect.Right - Rect.Left, Rect.Bottom - Rect.Top)));

    protected virtual void OnSelectionMoved(InkOverlaySelectionMovedEventArgs e) => this.onSelectionMoved((object) this, e);

    private void m_Collector_SelectionResized(InkRectangle Rect) => this.OnSelectionResized(new InkOverlaySelectionResizedEventArgs(new Rectangle(Rect.Left, Rect.Top, Rect.Right - Rect.Left, Rect.Bottom - Rect.Top)));

    protected virtual void OnSelectionResized(InkOverlaySelectionResizedEventArgs e)
    {
      if (this.onSelectionResized == null)
        return;
      this.onSelectionResized((object) this, e);
    }

    private void m_Collector_StrokesDeleting(InkStrokes Strokes) => this.OnStrokesDeleting(new InkOverlayStrokesDeletingEventArgs(new Strokes(Strokes)));

    protected virtual void OnStrokesDeleting(InkOverlayStrokesDeletingEventArgs e)
    {
      if (this.onStrokesDeleting == null)
        return;
      this.onStrokesDeleting((object) this, e);
    }

    private void m_Collector_StrokesDeleted() => this.OnStrokesDeleted(EventArgs.Empty);

    protected virtual void OnStrokesDeleted(EventArgs e)
    {
      if (this.onStrokesDeleted == null)
        return;
      this.onStrokesDeleted((object) this, e);
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
