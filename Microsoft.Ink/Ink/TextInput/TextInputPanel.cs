// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.TextInput.TextInputPanel
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Microsoft.Ink.TextInput
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
  public class TextInputPanel : IDisposable
  {
    private Microsoft.Ink.TextInputPanel unmanagedPanel;
    private IInputPanelWindowHandle handleHelper;
    private TextInputPanelEventHelper eventHelper;
    private bool disposed;
    private bool disposing;
    private Control control;

    public TextInputPanel()
    {
      try
      {
        this.unmanagedPanel = (Microsoft.Ink.TextInputPanel) new TextInputPanelClass();
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
      this.handleHelper = (IInputPanelWindowHandle) this.unmanagedPanel;
      this.eventHelper = new TextInputPanelEventHelper(this.unmanagedPanel, this);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    public TextInputPanel(IntPtr attachHandle)
      : this()
    {
      this.AttachedEditWindow = attachHandle;
    }

    public TextInputPanel(Control attachControl)
      : this()
    {
      this.AttachedEditControl = attachControl;
    }

    ~TextInputPanel() => this.Dispose(false);

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
      if (this.disposing)
        return;
      try
      {
        this.disposing = true;
        if (!disposing)
          return;
        try
        {
          this.AttachedEditControl = (Control) null;
          this.AttachedEditWindow = IntPtr.Zero;
        }
        finally
        {
          this.eventHelper.Dispose();
          this.handleHelper = (IInputPanelWindowHandle) null;
          if (this.unmanagedPanel != null)
            Marshal.ReleaseComObject((object) this.unmanagedPanel);
          this.unmanagedPanel = (Microsoft.Ink.TextInputPanel) null;
        }
      }
      finally
      {
        this.disposing = false;
        this.disposed = true;
      }
    }

    private IntPtr AttachedEditHandle
    {
      get
      {
        if (IntPtr.Size == 4)
          return (IntPtr) this.handleHelper.AttachedEditWindow32;
        return IntPtr.Size == 8 ? (IntPtr) this.handleHelper.AttachedEditWindow64 : IntPtr.Zero;
      }
      set
      {
        if (IntPtr.Size == 4)
        {
          this.handleHelper.AttachedEditWindow32 = (int) value;
        }
        else
        {
          if (IntPtr.Size != 8)
            return;
          this.handleHelper.AttachedEditWindow64 = (long) value;
        }
      }
    }

    public IntPtr AttachedEditWindow
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.AttachedEditHandle;
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (this.control != null)
          this.AttachedEditControl = (Control) null;
        this.AttachedEditHandle = value;
      }
    }

    public Control AttachedEditControl
    {
      get => this.control;
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (this.control != null)
        {
          this.control.HandleCreated -= new EventHandler(this.OnAttachedControlHandleCreated);
          this.control.HandleDestroyed -= new EventHandler(this.OnAttachedControlHandleDestroyed);
        }
        if (this.AttachedEditHandle != IntPtr.Zero)
          this.AttachedEditHandle = IntPtr.Zero;
        this.control = value;
        if (this.control == null)
          return;
        try
        {
          this.AttachedEditHandle = this.control.Handle;
        }
        catch (COMException ex)
        {
          this.control = (Control) null;
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
        this.control.HandleCreated += new EventHandler(this.OnAttachedControlHandleCreated);
        this.control.HandleDestroyed += new EventHandler(this.OnAttachedControlHandleDestroyed);
      }
    }

    private void OnAttachedControlHandleDestroyed(object sender, EventArgs e)
    {
      if (this.unmanagedPanel == null)
        return;
      this.AttachedEditHandle = IntPtr.Zero;
    }

    private void OnAttachedControlHandleCreated(object sender, EventArgs e)
    {
      if (this.unmanagedPanel == null)
        return;
      try
      {
        this.AttachedEditHandle = this.control.Handle;
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public InteractionMode CurrentInteractionMode
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (InteractionMode) this.unmanagedPanel.CurrentInteractionMode;
      }
    }

    public InPlaceState DefaultInPlaceState
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (InPlaceState) this.unmanagedPanel.DefaultInPlaceState;
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.unmanagedPanel.DefaultInPlaceState = (Microsoft.Ink.InPlaceState) value;
      }
    }

    public InPlaceState CurrentInPlaceState
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (InPlaceState) this.unmanagedPanel.CurrentInPlaceState;
      }
    }

    public PanelInputArea DefaultInputArea
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (PanelInputArea) this.unmanagedPanel.DefaultInputArea;
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.unmanagedPanel.DefaultInputArea = (Microsoft.Ink.PanelInputArea) value;
      }
    }

    public PanelInputArea CurrentInputArea
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (PanelInputArea) this.unmanagedPanel.CurrentInputArea;
      }
    }

    public CorrectionMode CurrentCorrectionMode
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (CorrectionMode) this.unmanagedPanel.CurrentCorrectionMode;
      }
    }

    public InPlaceDirection PreferredInPlaceDirection
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (InPlaceDirection) this.unmanagedPanel.PreferredInPlaceDirection;
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.unmanagedPanel.PreferredInPlaceDirection = (Microsoft.Ink.InPlaceDirection) value;
      }
    }

    public bool ExpandPostInsertionCorrection
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.unmanagedPanel.ExpandPostInsertionCorrection != 0;
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.unmanagedPanel.ExpandPostInsertionCorrection = value ? 1 : 0;
      }
    }

    public bool InPlaceVisibleOnFocus
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.unmanagedPanel.InPlaceVisibleOnFocus != 0;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.unmanagedPanel.InPlaceVisibleOnFocus = value ? 1 : 0;
      }
    }

    public Rectangle InPlaceBoundingRectangle
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return TextInputPanelEventHelper.RectangleConversion(this.unmanagedPanel.InPlaceBoundingRectangle);
      }
    }

    public int PopUpCorrectionHeight
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.unmanagedPanel.PopUpCorrectionHeight;
      }
    }

    public int PopDownCorrectionHeight
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.unmanagedPanel.PopDownCorrectionHeight;
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    public void CommitPendingInput()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.unmanagedPanel.CommitPendingInput();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public void SetInPlaceVisibility(bool visible)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.unmanagedPanel.SetInPlaceVisibility(visible ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    public void SetInPlacePosition(int x, int y, CorrectionPosition position)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.unmanagedPanel.SetInPlacePosition(x, y, (Microsoft.Ink.CorrectionPosition) position);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public void SetInPlaceHoverTargetPosition(int x, int y)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.unmanagedPanel.SetInPlaceHoverTargetPosition(x, y);
    }

    private event EventHandler<InPlaceStateChangeEventArgs> onInPlaceStateChanging;

    private event EventHandler<InPlaceStateChangeEventArgs> onInPlaceStateChanged;

    internal void DoInPlaceStateChanging(InPlaceStateChangeEventArgs e)
    {
      if (this.onInPlaceStateChanging == null)
        return;
      this.onInPlaceStateChanging((object) this, e);
    }

    internal void DoInPlaceStateChanged(InPlaceStateChangeEventArgs e)
    {
      if (this.onInPlaceStateChanged == null)
        return;
      this.onInPlaceStateChanged((object) this, e);
    }

    public event EventHandler<InPlaceStateChangeEventArgs> InPlaceStateChanging
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] add
      {
        this.onInPlaceStateChanging += value;
        if (value == null || this.onInPlaceStateChanging.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_InPlaceStateChanging);
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] remove
      {
        if (this.onInPlaceStateChanging == null)
          return;
        this.onInPlaceStateChanging -= value;
        if (this.onInPlaceStateChanging != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_InPlaceStateChanging);
      }
    }

    public event EventHandler<InPlaceStateChangeEventArgs> InPlaceStateChanged
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] add
      {
        this.onInPlaceStateChanged += value;
        if (value == null || this.onInPlaceStateChanged.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_InPlaceStateChanged);
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] remove
      {
        if (this.onInPlaceStateChanged == null)
          return;
        this.onInPlaceStateChanged -= value;
        if (this.onInPlaceStateChanged != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_InPlaceStateChanged);
      }
    }

    private event EventHandler<InPlaceSizeChangeEventArgs> onInPlaceSizeChanging;

    private event EventHandler<InPlaceSizeChangeEventArgs> onInPlaceSizeChanged;

    internal void DoInPlaceSizeChanging(InPlaceSizeChangeEventArgs e)
    {
      if (this.onInPlaceSizeChanging == null)
        return;
      this.onInPlaceSizeChanging((object) this, e);
    }

    internal void DoInPlaceSizeChanged(InPlaceSizeChangeEventArgs e)
    {
      if (this.onInPlaceSizeChanged == null)
        return;
      this.onInPlaceSizeChanged((object) this, e);
    }

    public event EventHandler<InPlaceSizeChangeEventArgs> InPlaceSizeChanging
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] add
      {
        this.onInPlaceSizeChanging += value;
        if (value == null || this.onInPlaceSizeChanging.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_InPlaceSizeChanging);
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] remove
      {
        if (this.onInPlaceSizeChanging == null)
          return;
        this.onInPlaceSizeChanging -= value;
        if (this.onInPlaceSizeChanging != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_InPlaceSizeChanging);
      }
    }

    public event EventHandler<InPlaceSizeChangeEventArgs> InPlaceSizeChanged
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] add
      {
        this.onInPlaceSizeChanged += value;
        if (value == null || this.onInPlaceSizeChanged.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_InPlaceSizeChanged);
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] remove
      {
        if (this.onInPlaceSizeChanged == null)
          return;
        this.onInPlaceSizeChanged -= value;
        if (this.onInPlaceSizeChanged != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_InPlaceSizeChanged);
      }
    }

    private event EventHandler<InputAreaChangeEventArgs> onInputAreaChanging;

    private event EventHandler<InputAreaChangeEventArgs> onInputAreaChanged;

    internal void DoInputAreaChanging(InputAreaChangeEventArgs e)
    {
      if (this.onInputAreaChanging == null)
        return;
      this.onInputAreaChanging((object) this, e);
    }

    internal void DoInputAreaChanged(InputAreaChangeEventArgs e)
    {
      if (this.onInputAreaChanged == null)
        return;
      this.onInputAreaChanged((object) this, e);
    }

    public event EventHandler<InputAreaChangeEventArgs> InputAreaChanging
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] add
      {
        this.onInputAreaChanging += value;
        if (value == null || this.onInputAreaChanging.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_InputAreaChanging);
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] remove
      {
        if (this.onInputAreaChanging == null)
          return;
        this.onInputAreaChanging -= value;
        if (this.onInputAreaChanging != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_InputAreaChanging);
      }
    }

    public event EventHandler<InputAreaChangeEventArgs> InputAreaChanged
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] add
      {
        this.onInputAreaChanged += value;
        if (value == null || this.onInputAreaChanged.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_InputAreaChanged);
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] remove
      {
        if (this.onInputAreaChanged == null)
          return;
        this.onInputAreaChanged -= value;
        if (this.onInputAreaChanged != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_InputAreaChanged);
      }
    }

    private event EventHandler<CorrectionModeChangeEventArgs> onCorrectionModeChanging;

    private event EventHandler<CorrectionModeChangeEventArgs> onCorrectionModeChanged;

    internal void DoCorrectionModeChanging(CorrectionModeChangeEventArgs e)
    {
      if (this.onCorrectionModeChanging == null)
        return;
      this.onCorrectionModeChanging((object) this, e);
    }

    internal void DoCorrectionModeChanged(CorrectionModeChangeEventArgs e)
    {
      if (this.onCorrectionModeChanged == null)
        return;
      this.onCorrectionModeChanged((object) this, e);
    }

    public event EventHandler<CorrectionModeChangeEventArgs> CorrectionModeChanging
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] add
      {
        this.onCorrectionModeChanging += value;
        if (value == null || this.onCorrectionModeChanging.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_CorrectionModeChanging);
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] remove
      {
        if (this.onCorrectionModeChanging == null)
          return;
        this.onCorrectionModeChanging -= value;
        if (this.onCorrectionModeChanging != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_CorrectionModeChanging);
      }
    }

    public event EventHandler<CorrectionModeChangeEventArgs> CorrectionModeChanged
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] add
      {
        this.onCorrectionModeChanged += value;
        if (value == null || this.onCorrectionModeChanged.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_CorrectionModeChanged);
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] remove
      {
        if (this.onCorrectionModeChanged == null)
          return;
        this.onCorrectionModeChanged -= value;
        if (this.onCorrectionModeChanged != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_CorrectionModeChanged);
      }
    }

    private event EventHandler<InPlaceVisibilityChangeEventArgs> onInPlaceVisibilityChanging;

    private event EventHandler<InPlaceVisibilityChangeEventArgs> onInPlaceVisibilityChanged;

    internal void DoInPlaceVisibilityChanging(InPlaceVisibilityChangeEventArgs e)
    {
      if (this.onInPlaceVisibilityChanging == null)
        return;
      this.onInPlaceVisibilityChanging((object) this, e);
    }

    internal void DoInPlaceVisibilityChanged(InPlaceVisibilityChangeEventArgs e)
    {
      if (this.onInPlaceVisibilityChanged == null)
        return;
      this.onInPlaceVisibilityChanged((object) this, e);
    }

    public event EventHandler<InPlaceVisibilityChangeEventArgs> InPlaceVisibilityChanging
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] add
      {
        this.onInPlaceVisibilityChanging += value;
        if (value == null || this.onInPlaceVisibilityChanging.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_InPlaceVisibilityChanging);
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] remove
      {
        if (this.onInPlaceVisibilityChanging == null)
          return;
        this.onInPlaceVisibilityChanging -= value;
        if (this.onInPlaceVisibilityChanging != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_InPlaceVisibilityChanging);
      }
    }

    public event EventHandler<InPlaceVisibilityChangeEventArgs> InPlaceVisibilityChanged
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] add
      {
        this.onInPlaceVisibilityChanged += value;
        if (value == null || this.onInPlaceVisibilityChanged.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_InPlaceVisibilityChanged);
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] remove
      {
        if (this.onInPlaceVisibilityChanged == null)
          return;
        this.onInPlaceVisibilityChanged -= value;
        if (this.onInPlaceVisibilityChanged != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_InPlaceVisibilityChanged);
      }
    }

    private event EventHandler<TextInsertionEventArgs> onTextInserting;

    private event EventHandler<TextInsertionEventArgs> onTextInserted;

    internal void DoTextInserting(TextInsertionEventArgs e)
    {
      if (this.onTextInserting == null)
        return;
      this.onTextInserting((object) this, e);
    }

    internal void DoTextInserted(TextInsertionEventArgs e)
    {
      if (this.onTextInserted == null)
        return;
      this.onTextInserted((object) this, e);
    }

    public event EventHandler<TextInsertionEventArgs> TextInserting
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] add
      {
        this.onTextInserting += value;
        if (value == null || this.onTextInserting.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_TextInserting);
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] remove
      {
        if (this.onTextInserting == null)
          return;
        this.onTextInserting -= value;
        if (this.onTextInserting != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_TextInserting);
      }
    }

    public event EventHandler<TextInsertionEventArgs> TextInserted
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] add
      {
        this.onTextInserted += value;
        if (value == null || this.onTextInserted.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.eventHelper.AddEvent(EventMask.EventMask_TextInserted);
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] remove
      {
        if (this.onTextInserted == null)
          return;
        this.onTextInserted -= value;
        if (this.onTextInserted != null)
          return;
        this.eventHelper.RemoveEvent(EventMask.EventMask_TextInserted);
      }
    }
  }
}
