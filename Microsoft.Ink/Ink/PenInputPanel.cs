// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.PenInputPanel
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Microsoft.Ink
{
  [DefaultEvent("InputFailed")]
  [DefaultProperty("AttachedEditControl")]
  [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class PenInputPanel : IDisposable
  {
    private PenInputPanelPrivate m_PIP;
    private bool m_enableTsfCalled;
    private bool m_enableTsfValue;
    private bool m_disposing;
    private bool disposed;
    private Control m_control;
    private PenInputPanelVisibleChangedEventHandler onVisibleChanged;
    private PenInputPanelMovingEventHandler onPanelMoving;
    private PenInputPanelChangedEventHandler onPanelChanged;
    private PenInputPanelInputFailedEventHandler onInputFailed;

    public PenInputPanel()
    {
      try
      {
        this.m_PIP = (PenInputPanelPrivate) new PenInputPanelClass();
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    public PenInputPanel(IntPtr attachHandle)
      : this()
    {
      this.AttachedEditWindow = attachHandle;
    }

    public PenInputPanel(Control attachControl)
      : this()
    {
      this.AttachedEditControl = attachControl;
    }

    ~PenInputPanel() => this.Dispose(false);

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
        if (!disposing)
          return;
        try
        {
          if (this.m_PIP != null)
          {
            IntSecurity.RemoveComEventHandler.Assert();
            if (this.onInputFailed != null)
            {
              // ISSUE: method pointer
              this.m_PIP.remove_InputFailed(new _IPenInputPanelEvents_InputFailedEventHandler((object) this, (UIntPtr) __methodptr(InternalInputFailed)));
            }
            if (this.onPanelChanged != null)
            {
              // ISSUE: method pointer
              this.m_PIP.remove_PanelChanged(new _IPenInputPanelEvents_PanelChangedEventHandler((object) this, (UIntPtr) __methodptr(InternalPanelChanged)));
            }
            if (this.onPanelMoving != null)
            {
              // ISSUE: method pointer
              this.m_PIP.remove_PanelMoving(new _IPenInputPanelEvents_PanelMovingEventHandler((object) this, (UIntPtr) __methodptr(InternalPanelMoving)));
            }
            if (this.onVisibleChanged != null)
            {
              // ISSUE: method pointer
              this.m_PIP.remove_VisibleChanged(new _IPenInputPanelEvents_VisibleChangedEventHandler((object) this, (UIntPtr) __methodptr(InternalVisibleChanged)));
            }
            CodeAccessPermission.RevertAssert();
          }
          try
          {
            this.CommitPendingInput();
          }
          catch (COMException ex)
          {
          }
          try
          {
            this.Visible = false;
          }
          catch (COMException ex)
          {
          }
          this.AttachedEditControl = (Control) null;
          try
          {
            this.AttachedEditWindow = IntPtr.Zero;
          }
          catch (SecurityException ex)
          {
          }
        }
        finally
        {
          if (this.m_PIP != null)
            Marshal.ReleaseComObject((object) this.m_PIP);
          this.m_PIP = (PenInputPanelPrivate) null;
        }
      }
      finally
      {
        this.m_disposing = false;
        this.disposed = true;
      }
    }

    public IntPtr AttachedEditWindow
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (IntPtr) this.m_PIP.AttachedEditWindow;
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (this.m_control != null)
          this.AttachedEditControl = (Control) null;
        try
        {
          this.m_PIP.AttachedEditWindow = (int) value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public Control AttachedEditControl
    {
      get
      {
        lock (this)
          return this.m_control;
      }
      set
      {
        lock (this)
        {
          if (this.disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          if (this.m_control != null)
          {
            this.m_control.HandleCreated -= new EventHandler(this.OnAttachedControlHandleCreated);
            this.m_control.HandleDestroyed -= new EventHandler(this.OnAttachedControlHandleDestroyed);
          }
          if (this.m_PIP.AttachedEditWindow != 0)
            this.m_PIP.AttachedEditWindow = 0;
          this.m_enableTsfCalled = false;
          this.m_control = value;
          if (this.m_control == null)
            return;
          try
          {
            this.m_PIP.AttachedEditWindow = (int) this.m_control.Handle;
          }
          catch (COMException ex)
          {
            this.m_control = (Control) null;
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
          this.m_control.HandleCreated += new EventHandler(this.OnAttachedControlHandleCreated);
          this.m_control.HandleDestroyed += new EventHandler(this.OnAttachedControlHandleDestroyed);
        }
      }
    }

    private void OnAttachedControlHandleDestroyed(object sender, EventArgs e)
    {
      if (this.m_PIP == null)
        return;
      this.m_PIP.AttachedEditWindow = 0;
    }

    private void OnAttachedControlHandleCreated(object sender, EventArgs e)
    {
      if (this.m_PIP != null)
      {
        try
        {
          this.m_PIP.AttachedEditWindow = (int) this.m_control.Handle;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
      if (!this.m_enableTsfCalled)
        return;
      this.EnableTsf(this.m_enableTsfValue);
    }

    public bool Busy
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_PIP.Busy;
      }
    }

    public string Factoid
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get => this.m_PIP.Factoid;
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] set
      {
        try
        {
          this.m_PIP.Factoid = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public PanelType CurrentPanel
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (PanelType) this.m_PIP.CurrentPanel;
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_PIP.CurrentPanel = (PanelTypePrivate) value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public PanelType DefaultPanel
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (PanelType) this.m_PIP.DefaultPanel;
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_PIP.DefaultPanel = (PanelTypePrivate) value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public bool AutoShow
    {
      [MethodImpl(MethodImplOptions.Synchronized)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_PIP.AutoShow;
      }
      [MethodImpl(MethodImplOptions.Synchronized)] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_PIP.AutoShow = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public bool Visible
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_PIP.Visible;
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_PIP.Visible = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public int Top
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_PIP.Top;
      }
    }

    public int Left
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_PIP.Left;
      }
    }

    public int Width
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_PIP.Width;
      }
    }

    public int Height
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_PIP.Height;
      }
    }

    public int VerticalOffset
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_PIP.VerticalOffset;
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_PIP.VerticalOffset = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public int HorizontalOffset
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_PIP.HorizontalOffset;
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_PIP.HorizontalOffset = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    public void MoveTo(int left, int top)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        this.m_PIP.MoveTo(left, top);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public void CommitPendingInput()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        this.m_PIP.CommitPendingInput();
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    public void Refresh()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        this.m_PIP.Refresh();
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
    [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
    public void EnableTsf(bool enable)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        this.m_PIP.EnableTsf(enable);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
      this.m_enableTsfCalled = true;
      this.m_enableTsfValue = enable;
    }

    private void InternalVisibleChanged(bool NewVisibility) => this.OnVisibleChanged(new PenInputPanelVisibleChangedEventArgs(NewVisibility));

    public event PenInputPanelVisibleChangedEventHandler VisibleChanged
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] add
      {
        this.onVisibleChanged += value;
        if (value == null || this.onVisibleChanged.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.m_PIP.add_VisibleChanged(new _IPenInputPanelEvents_VisibleChangedEventHandler((object) this, (UIntPtr) __methodptr(InternalVisibleChanged)));
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] remove
      {
        if (this.onVisibleChanged == null)
          return;
        this.onVisibleChanged -= value;
        if (this.onVisibleChanged != null || this.m_PIP == null)
          return;
        this.m_PIP.remove_VisibleChanged(new _IPenInputPanelEvents_VisibleChangedEventHandler((object) this, (UIntPtr) __methodptr(InternalVisibleChanged)));
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    protected virtual void OnVisibleChanged(PenInputPanelVisibleChangedEventArgs e)
    {
      if (this.onVisibleChanged == null)
        return;
      this.onVisibleChanged((object) this, e);
    }

    private void InternalPanelMoving(ref int Left, ref int Top)
    {
      PenInputPanelMovingEventArgs e = new PenInputPanelMovingEventArgs(Left, Top);
      this.OnPanelMoving(e);
      Left = e.Left;
      Top = e.Top;
    }

    public event PenInputPanelMovingEventHandler PanelMoving
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] add
      {
        this.onPanelMoving += value;
        if (value == null || this.onPanelMoving.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.m_PIP.add_PanelMoving(new _IPenInputPanelEvents_PanelMovingEventHandler((object) this, (UIntPtr) __methodptr(InternalPanelMoving)));
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] remove
      {
        if (this.onPanelMoving == null)
          return;
        this.onPanelMoving -= value;
        if (this.onPanelMoving != null || this.m_PIP == null)
          return;
        this.m_PIP.remove_PanelMoving(new _IPenInputPanelEvents_PanelMovingEventHandler((object) this, (UIntPtr) __methodptr(InternalPanelMoving)));
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    protected virtual void OnPanelMoving(PenInputPanelMovingEventArgs e)
    {
      if (this.onPanelMoving == null)
        return;
      this.onPanelMoving((object) this, e);
    }

    private void InternalPanelChanged(PanelTypePrivate NewPanelType) => this.OnPanelChanged(new PenInputPanelChangedEventArgs((PanelType) NewPanelType));

    public event PenInputPanelChangedEventHandler PanelChanged
    {
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] add
      {
        this.onPanelChanged += value;
        if (value == null || this.onPanelChanged.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.m_PIP.add_PanelChanged(new _IPenInputPanelEvents_PanelChangedEventHandler((object) this, (UIntPtr) __methodptr(InternalPanelChanged)));
      }
      [MethodImpl(MethodImplOptions.Synchronized), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")] remove
      {
        if (this.onPanelChanged == null)
          return;
        this.onPanelChanged -= value;
        if (this.onPanelChanged != null || this.m_PIP == null)
          return;
        this.m_PIP.remove_PanelChanged(new _IPenInputPanelEvents_PanelChangedEventHandler((object) this, (UIntPtr) __methodptr(InternalPanelChanged)));
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    protected virtual void OnPanelChanged(PenInputPanelChangedEventArgs e)
    {
      if (this.onPanelChanged == null)
        return;
      this.onPanelChanged((object) this, e);
    }

    private void InternalInputFailed(int hWnd, int Key, string Text, short ShiftKey) => this.OnInputFailed(new PenInputPanelInputFailedEventArgs((IntPtr) hWnd, (Keys) (Key + ((int) ShiftKey << 16)), Text));

    public event PenInputPanelInputFailedEventHandler InputFailed
    {
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] add
      {
        this.onInputFailed += value;
        if (value == null || this.onInputFailed.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        this.m_PIP.add_InputFailed(new _IPenInputPanelEvents_InputFailedEventHandler((object) this, (UIntPtr) __methodptr(InternalInputFailed)));
      }
      [MethodImpl(MethodImplOptions.Synchronized), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows), SecurityPermission(SecurityAction.Demand, Unrestricted = true)] remove
      {
        if (this.onInputFailed == null)
          return;
        this.onInputFailed -= value;
        if (this.onInputFailed != null || this.m_PIP == null)
          return;
        this.m_PIP.remove_InputFailed(new _IPenInputPanelEvents_InputFailedEventHandler((object) this, (UIntPtr) __methodptr(InternalInputFailed)));
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    protected virtual void OnInputFailed(PenInputPanelInputFailedEventArgs e)
    {
      if (this.onInputFailed == null)
        return;
      this.onInputFailed((object) this, e);
    }
  }
}
