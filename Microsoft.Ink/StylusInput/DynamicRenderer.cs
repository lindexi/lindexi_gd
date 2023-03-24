// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.DynamicRenderer
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using Microsoft.StylusInput.PluginData;
using System;
using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Microsoft.StylusInput
{
  [UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.SafeTopLevelWindows)]
  public sealed class DynamicRenderer : IStylusSyncPlugin, INativeImplementationWrapper, IDisposable
  {
    private static Hashtable s_nativeToManagedMap = new Hashtable();
    public static readonly Guid DynamicRendererCachedDataGuid = new Guid("{590585BC-D65E-4390-AC15-D4776A47A098}");
    private IInkDynamicRendererNative m_DynamicRendererNative;
    private DrawingAttributes m_drawingAttributes;
    private IntPtr m_hwnd;
    private Control m_control;
    private bool m_disposed;
    private bool m_enabled;
    private bool m_enableDataCache;
    private Rectangle m_clipRectangle;
    private object m_lock;

    private DynamicRenderer(bool isControl, IntPtr handle)
    {
      this.m_lock = new object();
      if (handle == IntPtr.Zero)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("DynamicRendererNullHandleDisallowed"));
      if (!isControl)
        Microsoft.Ink.IntSecurity.DemandPermissionToCollectOnWindow(handle);
      this.m_DynamicRendererNative = (IInkDynamicRendererNative) ComObjectCreator.CreateInstanceLicense(new Guid("99E89F48-A745-416d-A4E0-ECF53C65DFA0"), new Guid("2F59E338-88F1-4ad5-A46F-B52C706A8393"), "{CAAD7274-4004-44e0-8A17-D6F1919C443A}");
      this.m_drawingAttributes = new DrawingAttributes();
      this.DrawingAttributes = this.m_drawingAttributes;
      try
      {
        this.m_DynamicRendererNative.set_hWnd(handle);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
      DynamicRenderer.AddToMap(this.m_DynamicRendererNative, this);
    }

    public DynamicRenderer(Control control)
      : this(true, control != null ? control.Handle : IntPtr.Zero)
    {
      this.m_control = control;
      this.m_control.HandleCreated += new EventHandler(this.OnHandleCreated);
      this.m_control.HandleDestroyed += new EventHandler(this.OnHandleDestroyed);
    }

    public DynamicRenderer(IntPtr handle)
      : this(false, handle)
    {
      this.m_hwnd = handle;
    }

    ~DynamicRenderer() => this.Dispose(false);

    public DrawingAttributes DrawingAttributes
    {
      get
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          return this.m_drawingAttributes;
        }
      }
      set
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          if (value == null)
            throw new ArgumentNullException(nameof (value), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
          try
          {
            this.m_DynamicRendererNative.set_DrawingAttributes(value.m_DrawingAttributes);
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
          this.m_drawingAttributes = value;
        }
      }
    }

    public bool Enabled
    {
      get
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            return this.m_DynamicRendererNative.get_Enabled();
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
      set
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            this.m_DynamicRendererNative.set_Enabled(value);
            this.m_enabled = value;
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
    }

    public Rectangle ClipRectangle
    {
      get
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          return this.m_clipRectangle;
        }
      }
      set
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            if (!(this.m_clipRectangle != value))
              return;
            this.m_DynamicRendererNative.set_ClipRectangle(ref new tagRECT()
            {
              Left = value.Left,
              Top = value.Top,
              Right = value.Right,
              Bottom = value.Bottom
            });
            this.m_clipRectangle = value;
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
    }

    public bool EnableDataCache
    {
      get
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          return this.m_enableDataCache;
        }
      }
      set
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            if (this.m_enableDataCache == value)
              return;
            this.m_DynamicRendererNative.set_EnableDataCache(value);
            this.m_enableDataCache = value;
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
    }

    public void ReleaseCachedData(int cachedDataId)
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (cachedDataId < 0)
          throw new ArgumentOutOfRangeException(nameof (cachedDataId), Helpers.SharedResources.Errors.GetString("ValueCannotBeSmallerThanZero"));
        try
        {
          this.m_DynamicRendererNative.ReleaseCachedData((uint) cachedDataId);
        }
        catch (COMException ex)
        {
          if (ex.ErrorCode == -2147220958)
            throw new ArgumentException(Helpers.SharedResources.Errors.GetString("InvalidStroke"));
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public void Refresh()
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_DynamicRendererNative.Refresh();
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public void Draw(Graphics g)
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        int savedDC;
        IntPtr hregion;
        IntPtr fullHdc = Renderer.GetFullHdc(g, out savedDC, out hregion, (object) this);
        try
        {
          Microsoft.Ink.IntSecurity.UnmanagedCode.Assert();
          ((IInkDynamicRendererNative2) this.m_DynamicRendererNative).Draw(fullHdc);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
        finally
        {
          if (savedDC != 0)
            Microsoft.Ink.UnsafeNativeMethods.RestoreDC(fullHdc, savedDC);
          if (IntPtr.Zero != hregion)
            Microsoft.Ink.SafeNativeMethods.DeleteObject(new HandleRef((object) this, hregion));
          g.ReleaseHdc(fullHdc);
        }
      }
    }

    internal static void AddToMap(IInkDynamicRendererNative native, DynamicRenderer managed)
    {
      lock (DynamicRenderer.s_nativeToManagedMap.SyncRoot)
        DynamicRenderer.s_nativeToManagedMap.Add((object) native, (object) managed);
    }

    internal static void RemoveFromMap(IInkDynamicRendererNative native)
    {
      lock (DynamicRenderer.s_nativeToManagedMap.SyncRoot)
        DynamicRenderer.s_nativeToManagedMap.Remove((object) native);
    }

    internal static DynamicRenderer GetManagedForNative(IntPtr nativeInterface)
    {
      DynamicRenderer managedForNative = (DynamicRenderer) null;
      IntPtr ppv = IntPtr.Zero;
      Guid iid = new Guid("2F59E338-88F1-4ad5-A46F-B52C706A8393");
      lock (DynamicRenderer.s_nativeToManagedMap.SyncRoot)
      {
        Marshal.QueryInterface(nativeInterface, ref iid, out ppv);
        if (ppv != IntPtr.Zero)
        {
          if (Marshal.GetTypedObjectForIUnknown(nativeInterface, typeof (IInkDynamicRendererNative)) is IInkDynamicRendererNative objectForIunknown)
            managedForNative = (DynamicRenderer) DynamicRenderer.s_nativeToManagedMap[(object) objectForIunknown];
          Marshal.Release(ppv);
        }
      }
      return managedForNative;
    }

    private void Dispose(bool disposing)
    {
      if (this.m_lock == null)
        return;
      lock (this.m_lock)
      {
        if (this.m_disposed)
          return;
        if (this.m_DynamicRendererNative != null)
          DynamicRenderer.RemoveFromMap(this.m_DynamicRendererNative);
        if (disposing)
        {
          this.m_DynamicRendererNative.set_Enabled(false);
          this.InternalSetHandle(IntPtr.Zero);
          if (this.m_DynamicRendererNative != null)
          {
            Marshal.ReleaseComObject((object) this.m_DynamicRendererNative);
            this.m_DynamicRendererNative = (IInkDynamicRendererNative) null;
          }
        }
        this.m_disposed = true;
      }
    }

    private void InternalSetHandle(IntPtr value)
    {
      try
      {
        this.m_DynamicRendererNative.set_hWnd(value);
        if (this.m_control != null)
        {
          this.m_control.HandleCreated -= new EventHandler(this.OnHandleCreated);
          this.m_control.HandleDestroyed -= new EventHandler(this.OnHandleDestroyed);
        }
        this.m_hwnd = IntPtr.Zero;
        this.m_control = (Control) null;
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    private void OnHandleDestroyed(object sender, EventArgs e)
    {
      if (this.m_DynamicRendererNative == null)
        return;
      if (this.m_control != sender as Control)
        return;
      try
      {
        this.m_DynamicRendererNative.set_Enabled(false);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    private void OnHandleCreated(object sender, EventArgs e)
    {
      if (this.m_DynamicRendererNative == null)
        return;
      if (this.m_control != sender as Control)
        return;
      try
      {
        this.m_DynamicRendererNative.set_Enabled(false);
        this.m_DynamicRendererNative.set_hWnd(this.m_control.Handle);
        this.m_DynamicRendererNative.set_Enabled(this.m_enabled);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    void IStylusSyncPlugin.RealTimeStylusEnabled(
      RealTimeStylus sender,
      RealTimeStylusEnabledData data)
    {
    }

    void IStylusSyncPlugin.RealTimeStylusDisabled(
      RealTimeStylus sender,
      RealTimeStylusDisabledData data)
    {
    }

    void IStylusSyncPlugin.StylusInRange(RealTimeStylus sender, StylusInRangeData data)
    {
    }

    void IStylusSyncPlugin.StylusOutOfRange(RealTimeStylus sender, StylusOutOfRangeData data)
    {
    }

    void IStylusSyncPlugin.StylusDown(RealTimeStylus sender, StylusDownData data)
    {
    }

    void IStylusSyncPlugin.StylusUp(RealTimeStylus sender, StylusUpData data)
    {
    }

    void IStylusSyncPlugin.StylusButtonDown(RealTimeStylus sender, StylusButtonDownData data)
    {
    }

    void IStylusSyncPlugin.StylusButtonUp(RealTimeStylus sender, StylusButtonUpData data)
    {
    }

    void IStylusSyncPlugin.InAirPackets(RealTimeStylus sender, InAirPacketsData data)
    {
    }

    void IStylusSyncPlugin.Packets(RealTimeStylus sender, PacketsData data)
    {
    }

    void IStylusSyncPlugin.SystemGesture(RealTimeStylus sender, SystemGestureData data)
    {
    }

    void IStylusSyncPlugin.TabletAdded(RealTimeStylus sender, TabletAddedData data)
    {
    }

    void IStylusSyncPlugin.TabletRemoved(RealTimeStylus sender, TabletRemovedData data)
    {
    }

    void IStylusSyncPlugin.CustomStylusDataAdded(RealTimeStylus sender, CustomStylusData data)
    {
    }

    void IStylusSyncPlugin.Error(RealTimeStylus sender, ErrorData data)
    {
    }

    DataInterestMask IStylusSyncPlugin.DataInterest
    {
      get
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return DataInterestMask.DefaultStylusData;
      }
    }

    StylusSyncPluginNative INativeImplementationWrapper.SyncPlugin
    {
      get
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (StylusSyncPluginNative) this.m_DynamicRendererNative;
      }
    }

    StylusAsyncPluginNative INativeImplementationWrapper.AsyncPlugin => (StylusAsyncPluginNative) null;

    public void Dispose()
    {
      lock (this.m_lock)
      {
        this.Dispose(true);
        GC.SuppressFinalize((object) this);
      }
    }
  }
}
