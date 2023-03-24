// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.RealTimeStylus
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
  public sealed class RealTimeStylus : IDisposable, IStylusAsyncPlugin, INativeImplementationWrapper
  {
    internal const int ShimErrorCode = -2147220924;
    private IntPtr m_Handle;
    private Control m_control;
    private Tablet m_Tablet;
    private IRealTimeStylusNative m_nativeIRTS;
    private bool m_disposed;
    private Hashtable m_styluses;
    private bool m_enabled;
    private StylusSyncPluginNativeShim m_addCursorShim;
    private StylusAsyncPluginNativeShim m_first;
    private StylusAsyncPluginNativeShim m_last;
    private StylusSyncPluginCollection m_syncCollection;
    private StylusAsyncPluginCollection m_asyncCollection;
    private Hashtable m_syncShimMap;
    private Hashtable m_asyncShimMap;
    internal Hashtable m_customDataMap;
    private RealTimeStylus m_cascadingRTS;
    private bool m_cascadedMode;
    private Hashtable m_exceptionMap;
    private object m_lock;

    public RealTimeStylus()
      : this(IntPtr.Zero, false, (Tablet) null, true, true)
    {
    }

    public RealTimeStylus(IntPtr handle)
      : this(handle, false, (Tablet) null, true, false)
    {
    }

    public RealTimeStylus(IntPtr handle, bool useMouseForInput)
      : this(handle, false, (Tablet) null, useMouseForInput, false)
    {
    }

    public RealTimeStylus(IntPtr handle, Tablet tablet)
      : this(handle, false, tablet, tablet == null, false)
    {
    }

    public RealTimeStylus(Control attachedControl)
      : this(attachedControl, (Tablet) null, true)
    {
    }

    public RealTimeStylus(Control attachedControl, bool useMouseForInput)
      : this(attachedControl, (Tablet) null, useMouseForInput)
    {
    }

    public RealTimeStylus(Control attachedControl, Tablet tablet)
      : this(attachedControl, tablet, tablet == null)
    {
    }

    private RealTimeStylus(Control attachedControl, Tablet tablet, bool useMouseForInput)
      : this(attachedControl == null ? IntPtr.Zero : attachedControl.Handle, true, tablet, useMouseForInput, false)
    {
      this.m_control = attachedControl;
      this.m_control.HandleCreated += new EventHandler(this.OnHandleCreated);
      this.m_control.HandleDestroyed += new EventHandler(this.OnHandleDestroyed);
    }

    private RealTimeStylus(
      IntPtr handle,
      bool isControl,
      Tablet tablet,
      bool useMouseForInput,
      bool allowNull)
    {
      this.m_lock = new object();
      if (!allowNull && handle == IntPtr.Zero)
        throw new ArgumentNullException(nameof (handle));
      this.m_Handle = isControl ? IntPtr.Zero : handle;
      this.m_Tablet = tablet;
      this.m_syncCollection = new StylusSyncPluginCollection(this, new Validate(this.PluginCollectionModificationValidate), (ItemInsert) null, new ItemInserted(this.SyncPluginCollectionInsert), new ItemRemove(this.SyncPluginCollectionRemove), new ListClear(this.SyncPluginCollectionClear), new ItemSet(this.SyncPluginCollectionSet));
      this.m_asyncCollection = new StylusAsyncPluginCollection(this, new Validate(this.PluginCollectionModificationValidate), new ItemInsert(this.ASyncPluginCollectionOnInsert), new ItemInserted(this.ASyncPluginCollectionInsertComplete), new ItemRemove(this.ASyncPluginCollectionRemove), new ListClear(this.ASyncPluginCollectionClear), new ItemSet(this.ASyncPluginCollectionSet));
      this.m_syncShimMap = new Hashtable();
      this.m_asyncShimMap = new Hashtable();
      this.m_customDataMap = new Hashtable();
      this.m_exceptionMap = new Hashtable();
      Guid clsid = new Guid("{DECBDC16-E824-436e-872D-14E8C7BF7D8B}");
      Guid iid = new Guid("{C6C77F97-545E-4873-85F2-E0FEE550B2E9}");
      string licenseKey = "{CAAD7274-4004-44e0-8A17-D6F1919C443A}";
      try
      {
        if (!isControl && handle != IntPtr.Zero)
          Microsoft.Ink.IntSecurity.DemandPermissionToCollectOnWindow(handle);
        this.m_nativeIRTS = (IRealTimeStylusNative) ComObjectCreator.CreateInstanceLicense(clsid, iid, licenseKey);
        if (this.m_nativeIRTS == null)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InitializeFail"));
        this.m_nativeIRTS.SetHWND(handle);
        if (this.m_Tablet != null)
          this.m_nativeIRTS.SetSingleTabletMode(this.m_Tablet.m_Tablet);
        else
          this.m_nativeIRTS.SetAllTabletsMode(useMouseForInput);
        this.m_addCursorShim = new StylusSyncPluginNativeShim((IStylusSyncPlugin) null, this, RealTimeStylusDataInterest.RTPEI_CursorInRange);
        this.m_addCursorShim.First = true;
        IntPtr interfaceForObject1 = Marshal.GetComInterfaceForObject((object) this.m_addCursorShim, typeof (StylusSyncPluginNative));
        if (IntPtr.Zero == interfaceForObject1)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InitializeFail"));
        try
        {
          this.m_nativeIRTS.AddStylusSyncPlugin(0U, interfaceForObject1);
        }
        finally
        {
          Marshal.Release(interfaceForObject1);
        }
        IntPtr zero1 = IntPtr.Zero;
        this.m_first = new StylusAsyncPluginNativeShim((IStylusAsyncPlugin) null, this, RealTimeStylusDataInterest.RTPEI_RtpEnabled);
        this.m_first.First = true;
        IntPtr zero2 = IntPtr.Zero;
        this.m_last = new StylusAsyncPluginNativeShim((IStylusAsyncPlugin) null, this, RealTimeStylusDataInterest.RTPEI_RtpDisabled | RealTimeStylusDataInterest.RTPEI_CustomData);
        this.m_last.Last = true;
        IntPtr interfaceForObject2 = Marshal.GetComInterfaceForObject((object) this.m_first, typeof (StylusAsyncPluginNative));
        IntPtr interfaceForObject3 = Marshal.GetComInterfaceForObject((object) this.m_last, typeof (StylusAsyncPluginNative));
        if (!(interfaceForObject2 == IntPtr.Zero))
        {
          if (!(interfaceForObject3 == IntPtr.Zero))
          {
            try
            {
              this.m_nativeIRTS.AddStylusAsyncPlugin(0U, interfaceForObject2);
              this.m_nativeIRTS.AddStylusAsyncPlugin(1U, interfaceForObject3);
            }
            finally
            {
              Marshal.Release(interfaceForObject2);
              Marshal.Release(interfaceForObject3);
            }
            this.m_styluses = new Hashtable();
            return;
          }
        }
        throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InitializeFail"));
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
      catch
      {
        if (this.m_nativeIRTS != null)
        {
          Marshal.ReleaseComObject((object) this.m_nativeIRTS);
          this.m_nativeIRTS = (IRealTimeStylusNative) null;
        }
        throw;
      }
    }

    ~RealTimeStylus() => this.Dispose(false);

    public Stylus[] GetStyluses()
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (!this.Enabled)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSMustBeEnabled"));
        Stylus[] styluses;
        if (this.m_styluses != null && this.m_styluses.Count != 0)
        {
          styluses = new Stylus[this.m_styluses.Count];
          this.m_styluses.Values.CopyTo((Array) styluses, 0);
        }
        else
          styluses = new Stylus[0];
        return styluses;
      }
    }

    public StylusSyncPluginCollection SyncPluginCollection
    {
      get
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_syncCollection;
      }
    }

    public StylusAsyncPluginCollection AsyncPluginCollection
    {
      get
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_asyncCollection;
      }
    }

    public Guid[] GetDesiredPacketDescription()
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this._GetDesiredPacketDescription();
      }
    }

    public void SetDesiredPacketDescription(Guid[] value)
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (this.Enabled)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSMustBeDisabled"));
        if (value == null)
          throw new ArgumentNullException(nameof (value), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        if (value.Length == 0)
          throw new ArgumentOutOfRangeException(Helpers.SharedResources.Errors.GetString("ValueCannotBeEmptyArray"));
        if (this.m_control == null)
        {
          if (IntPtr.Zero == this.m_Handle)
            throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("CannotSetCascadeRTSProperty"));
        }
        try
        {
          this.m_nativeIRTS.SetDesiredPacketDescription((uint) value.Length, value);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public TabletPropertyDescriptionCollection GetTabletPropertyDescriptionCollection(
      int tabletContextId)
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (!this.Enabled)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSMustBeEnabled"));
        try
        {
          uint cPropertyCount = 0;
          PacketProperty[] packetProperties1 = (PacketProperty[]) null;
          float inkToDeviceScaleX;
          float inkToDeviceScaleY;
          this.m_nativeIRTS.GetPacketDescriptionData((uint) tabletContextId, out inkToDeviceScaleX, out inkToDeviceScaleY, ref cPropertyCount, packetProperties1);
          PacketProperty[] packetProperties2 = new PacketProperty[(IntPtr) cPropertyCount];
          this.m_nativeIRTS.GetPacketDescriptionData((uint) tabletContextId, out inkToDeviceScaleX, out inkToDeviceScaleY, ref cPropertyCount, packetProperties2);
          TabletPropertyDescriptionCollection descriptionCollection = new TabletPropertyDescriptionCollection(inkToDeviceScaleX, inkToDeviceScaleY);
          foreach (PacketProperty packetProperty in packetProperties2)
            descriptionCollection.Add(new TabletPropertyDescription(packetProperty.guid, new TabletPropertyMetrics()
            {
              Minimum = packetProperty.metrics.minimum,
              Maximum = packetProperty.metrics.maximum,
              Units = (TabletPropertyMetricUnit) packetProperty.metrics.units,
              Resolution = packetProperty.metrics.Resolution
            }));
          return descriptionCollection;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
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
          bool fEnable;
          this.m_nativeIRTS.IsEnabled(out fEnable);
          return fEnable;
        }
      }
      set
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (this.m_control == null && IntPtr.Zero == this.m_Handle)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("CannotSetCascadeRTSProperty"));
        this.m_nativeIRTS.AcquireLock(RtsLockType.RtsSyncObjLock);
        try
        {
          lock (this.m_lock)
          {
            if (value && this.m_asyncCollection.Count == 0 && this.m_syncCollection.Count == 0)
              throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("NoPluginsConnected"));
            this.m_nativeIRTS.Enable(value);
            this.m_enabled = value;
          }
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
        finally
        {
          this.m_nativeIRTS.ReleaseLock(RtsLockType.RtsSyncObjLock);
        }
      }
    }

    public Rectangle WindowInputRectangle
    {
      get
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            tagRECT prcWndInputRect = new tagRECT();
            this.m_nativeIRTS.GetWindowInputRect(out prcWndInputRect);
            return new Rectangle(prcWndInputRect.Left, prcWndInputRect.Top, prcWndInputRect.Right - prcWndInputRect.Left, prcWndInputRect.Bottom - prcWndInputRect.Top);
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
          if (this.m_control == null)
          {
            if (IntPtr.Zero == this.m_Handle)
              throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("CannotSetCascadeRTSProperty"));
          }
          try
          {
            this.m_nativeIRTS.SetWindowInputRect(ref new tagRECT()
            {
              Left = value.Left,
              Top = value.Top,
              Right = value.Right,
              Bottom = value.Bottom
            });
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
    }

    public bool FlicksEnabled
    {
      get
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            bool fEnable;
            this.m_nativeIRTS.IsFlicksEnabled(out fEnable);
            return fEnable;
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
          if (this.m_control == null)
          {
            if (IntPtr.Zero == this.m_Handle)
              throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("CannotSetCascadeRTSProperty"));
          }
          try
          {
            this.m_nativeIRTS.FlicksEnable(value);
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
    }

    public bool AllTouchEnabled
    {
      get
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            bool fEnable;
            this.m_nativeIRTS.IsAllTouchEnabled(out fEnable);
            return fEnable;
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
          if (this.Enabled)
            throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSMustBeDisabled"));
          if (this.m_control == null)
          {
            if (IntPtr.Zero == this.m_Handle)
              throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("CannotSetCascadeRTSProperty"));
          }
          try
          {
            this.m_nativeIRTS.AllTouchEnable(value);
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
    }

    public bool MultiTouchEnabled
    {
      get
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            bool fEnable;
            this.m_nativeIRTS.IsMultiTouchEnabled(out fEnable);
            return fEnable;
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
          if (this.Enabled)
            throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSMustBeDisabled"));
          if (this.m_control == null)
          {
            if (IntPtr.Zero == this.m_Handle)
              throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("CannotSetCascadeRTSProperty"));
          }
          try
          {
            this.m_nativeIRTS.MultiTouchEnable(value);
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
    }

    public void AddCustomStylusDataToQueue(StylusQueues queue, Guid guid, object data)
    {
      IntPtr num = IntPtr.Zero;
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (data == null)
          throw new ArgumentNullException(nameof (data), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        if (!this.Enabled)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSMustBeEnabled"));
        if (queue != StylusQueues.Input && queue != StylusQueues.OutputImmediate && queue != StylusQueues.Output)
          throw new ArgumentException(Helpers.SharedResources.Errors.GetString("InvalidStylusQueues"));
        if ((queue == StylusQueues.Output || queue == StylusQueues.OutputImmediate) && this.m_asyncCollection.Count == 0 || queue == StylusQueues.Input && this.m_asyncCollection.Count == 0 && this.m_syncCollection.Count == 0)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("NoPluginsConnected"));
        if (guid == NativeSynchronizationMarkers.DynamicRenderer || guid == NativeSynchronizationMarkers.GestureRecognizer)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("ValueReserved"));
        num = Marshal.AllocCoTaskMem(1);
        this.AddToCustomDataMap(num, data);
      }
      try
      {
        IRealTimeStylusNative nativeIrts = this.m_nativeIRTS;
        int sq;
        switch (queue)
        {
          case StylusQueues.Input:
            sq = 1;
            break;
          case StylusQueues.OutputImmediate:
            sq = 2;
            break;
          default:
            sq = 3;
            break;
        }
        Guid guidId = guid;
        IntPtr pbData = num;
        nativeIrts.AddCustomStylusDataToQueue((StylusQueueNative) sq, guidId, 1U, pbData);
      }
      catch (COMException ex)
      {
        if (num != IntPtr.Zero)
        {
          lock (this.m_customDataMap.SyncRoot)
            this.m_customDataMap.Remove((object) num);
          Marshal.FreeCoTaskMem(num);
        }
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void ClearStylusQueues()
    {
      if (this.m_disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        this.m_nativeIRTS.ClearStylusQueues();
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public Tablet GetTabletFromTabletContextId(int tabletContextId)
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (!this.Enabled)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSMustBeEnabled"));
        try
        {
          return new Tablet(this.m_nativeIRTS.GetTabletForTabletContextId((uint) tabletContextId));
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public int GetTabletContextIdFromTablet(Tablet tablet)
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (tablet == null)
          throw new ArgumentNullException(nameof (tablet), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        if (!this.Enabled)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSMustBeEnabled"));
        try
        {
          return (int) this.m_nativeIRTS.GetTabletContextIdForTablet(tablet.m_Tablet);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    private void OnHandleDestroyed(object sender, EventArgs e)
    {
      if (this.m_disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (this.m_nativeIRTS == null)
        return;
      if (this.m_control != sender as Control)
        return;
      try
      {
        this.m_nativeIRTS.Enable(false);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    private void OnHandleCreated(object sender, EventArgs e)
    {
      if (this.m_disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      if (this.m_nativeIRTS == null)
        return;
      if (this.m_control != sender as Control)
        return;
      try
      {
        this.m_nativeIRTS.Enable(false);
        this.m_nativeIRTS.SetHWND(this.m_control.Handle);
        this.m_nativeIRTS.Enable(this.m_enabled);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    private void Dispose(bool disposing)
    {
      if (this.m_lock == null)
        return;
      lock (this.m_lock)
      {
        if (this.m_disposed)
          return;
        if (disposing)
        {
          if (this.m_control != null)
          {
            this.m_control.HandleCreated -= new EventHandler(this.OnHandleCreated);
            this.m_control.HandleDestroyed -= new EventHandler(this.OnHandleDestroyed);
            this.m_control = (Control) null;
          }
          if (this.m_syncCollection != null)
            this.m_syncCollection.Clear();
          if (this.m_asyncCollection != null)
            this.m_asyncCollection.Clear();
          if (this.m_nativeIRTS != null)
          {
            try
            {
              this.m_nativeIRTS.Enable(false);
            }
            catch (InvalidOperationException ex)
            {
            }
            try
            {
              Marshal.ReleaseComObject((object) this.m_nativeIRTS);
            }
            catch (InvalidCastException ex)
            {
            }
            this.m_nativeIRTS = (IRealTimeStylusNative) null;
          }
        }
        this.m_disposed = true;
      }
    }

    private Stylus CreateStylusFrom(Microsoft.Ink.Cursor cursor)
    {
      int contextIdForTablet = (int) this.m_nativeIRTS.GetTabletContextIdForTablet(cursor.Tablet.m_Tablet);
      return RealTimeStylus.CreateStylusFrom(cursor, contextIdForTablet);
    }

    private static Stylus CreateStylusFrom(Microsoft.Ink.Cursor cursor, int tcid)
    {
      int count = cursor.Buttons.Count;
      Guid[] ids = new Guid[count];
      string[] names = new string[count];
      int index = 0;
      foreach (CursorButton button in cursor.Buttons)
      {
        ids[index] = button.Id;
        names[index] = button.Name;
        ++index;
      }
      StylusButtons buttons = new StylusButtons(count, ids, names);
      return new Stylus(cursor.Id, tcid, cursor.Inverted, cursor.Name, buttons);
    }

    private Guid[] _GetDesiredPacketDescription()
    {
      Guid[] pPropertyGuids1 = (Guid[]) null;
      uint cPropertyCount = 0;
      this.m_nativeIRTS.GetDesiredPacketDescription(ref cPropertyCount, pPropertyGuids1);
      Guid[] pPropertyGuids2 = new Guid[(IntPtr) cPropertyCount];
      this.m_nativeIRTS.GetDesiredPacketDescription(ref cPropertyCount, pPropertyGuids2);
      return pPropertyGuids2;
    }

    private static RealTimeStylusDataInterest GetNativeDataInterestMask(
      DataInterestMask dataInterestMask)
    {
      return dataInterestMask != ~DataInterestMask.AllStylusData ? (RealTimeStylusDataInterest) dataInterestMask : throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InvalidDataInterestMask"));
    }

    internal void AddCursorToCollection(int cid, int tcid)
    {
      lock (this.m_styluses.SyncRoot)
      {
        if (!this.m_styluses.ContainsKey((object) cid))
        {
          Microsoft.Ink.Cursor cursor = new Microsoft.Ink.Cursor(this.m_nativeIRTS.GetCursorForId((uint) cid));
          this.m_styluses.Add((object) cid, (object) RealTimeStylus.CreateStylusFrom(cursor, tcid));
        }
        else
        {
          Stylus styluse = (Stylus) this.m_styluses[(object) cid];
          if (styluse.TabletContextId == tcid)
            return;
          styluse.SetTabletContextId(tcid);
        }
      }
    }

    internal void AddCursorToCollection(int cid)
    {
      lock (this.m_styluses.SyncRoot)
      {
        if (this.m_styluses.ContainsKey((object) cid))
          return;
        Microsoft.Ink.Cursor cursor = new Microsoft.Ink.Cursor(this.m_nativeIRTS.GetCursorForId((uint) cid));
        this.m_styluses.Add((object) cid, (object) this.CreateStylusFrom(cursor));
      }
    }

    internal Stylus GetStylusForId(int cid)
    {
      lock (this.m_styluses.SyncRoot)
      {
        Stylus styluse = (Stylus) this.m_styluses[(object) cid];
        if (styluse == null)
        {
          this.AddCursorToCollection(cid);
          styluse = (Stylus) this.m_styluses[(object) cid];
        }
        return styluse;
      }
    }

    internal Stylus GetStylusForId(int cid, int tcid)
    {
      lock (this.m_styluses.SyncRoot)
      {
        Stylus styluse = (Stylus) this.m_styluses[(object) cid];
        if (styluse == null)
        {
          this.AddCursorToCollection(cid, tcid);
          styluse = (Stylus) this.m_styluses[(object) cid];
        }
        return styluse;
      }
    }

    internal void AddToCustomDataMap(IntPtr key, object data)
    {
      lock (this.m_customDataMap.SyncRoot)
      {
        if (!this.m_customDataMap.ContainsKey((object) key))
          this.m_customDataMap.Add((object) key, data);
        if (this.m_cascadingRTS == null)
          return;
        this.m_cascadingRTS.AddToCustomDataMap(key, data);
      }
    }

    internal void AddToExceptionMap(int key, ErrorData value)
    {
      lock (this.m_exceptionMap.SyncRoot)
      {
        if (!this.m_exceptionMap.ContainsKey((object) key))
          this.m_exceptionMap.Add((object) key, (object) value);
        if (this.m_cascadingRTS == null)
          return;
        this.m_cascadingRTS.AddToExceptionMap(key, value);
      }
    }

    internal void RemoveFromExceptionMap(int key)
    {
      lock (this.m_exceptionMap.SyncRoot)
      {
        if (!this.m_exceptionMap.ContainsKey((object) key))
          return;
        this.m_exceptionMap.Remove((object) key);
      }
    }

    internal ErrorData GetExceptionFromMap(int key)
    {
      lock (this.m_exceptionMap.SyncRoot)
        return (ErrorData) this.m_exceptionMap[(object) key];
    }

    internal void PluginCollectionModificationValidate()
    {
      if (this.m_disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
    }

    internal void SyncPluginCollectionInsert(int index, object plugin)
    {
      this.m_nativeIRTS.AcquireLock(RtsLockType.RtsSyncObjLock);
      try
      {
        lock (this.m_lock)
        {
          IntPtr zero = IntPtr.Zero;
          StylusSyncPluginNativeShim o = (StylusSyncPluginNativeShim) null;
          IntPtr num;
          if (plugin is INativeImplementationWrapper implementationWrapper)
          {
            num = implementationWrapper.SyncPlugin != null ? Marshal.GetComInterfaceForObject((object) implementationWrapper.SyncPlugin, typeof (StylusSyncPluginNative)) : throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSInternalError"));
            if (num == IntPtr.Zero)
              throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSInternalError"));
          }
          else
          {
            IStylusSyncPlugin sink = (IStylusSyncPlugin) plugin;
            RealTimeStylusDataInterest dataInterestMask = RealTimeStylus.GetNativeDataInterestMask(sink.DataInterest);
            o = new StylusSyncPluginNativeShim(sink, this, dataInterestMask);
            o.Sink = sink;
            num = Marshal.GetComInterfaceForObject((object) o, typeof (StylusSyncPluginNative));
            if (num == IntPtr.Zero)
              throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSInternalError"));
          }
          try
          {
            this.m_nativeIRTS.AddStylusSyncPlugin((uint) (index + 1), num);
            if (o == null)
              return;
            this.m_syncShimMap.Add(plugin, (object) o);
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
          finally
          {
            Marshal.Release(num);
          }
        }
      }
      finally
      {
        this.m_nativeIRTS.ReleaseLock(RtsLockType.RtsSyncObjLock);
      }
    }

    internal void SyncPluginCollectionRemove(int index, object plugin)
    {
      this.m_nativeIRTS.AcquireLock(RtsLockType.RtsSyncObjLock);
      try
      {
        lock (this.m_lock)
        {
          IntPtr zero1 = IntPtr.Zero;
          this.m_nativeIRTS.RemoveStylusSyncPlugin((uint) (index + 1), ref zero1);
          if (zero1 != IntPtr.Zero)
          {
            Marshal.Release(zero1);
            IntPtr zero2 = IntPtr.Zero;
          }
          this.m_syncShimMap.Remove(plugin);
        }
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
      finally
      {
        this.m_nativeIRTS.ReleaseLock(RtsLockType.RtsSyncObjLock);
      }
    }

    internal void SyncPluginCollectionSet(int index, object oldValue, object newValue)
    {
      INativeImplementationWrapper implementationWrapper1 = oldValue as INativeImplementationWrapper;
      INativeImplementationWrapper implementationWrapper2 = newValue as INativeImplementationWrapper;
      IStylusSyncPlugin stylusSyncPlugin1 = (IStylusSyncPlugin) oldValue;
      IStylusSyncPlugin stylusSyncPlugin2 = (IStylusSyncPlugin) newValue;
      this.m_nativeIRTS.AcquireLock(RtsLockType.RtsSyncObjLock);
      try
      {
        lock (this.m_lock)
        {
          if (implementationWrapper1 == null && implementationWrapper2 == null && stylusSyncPlugin1.DataInterest == stylusSyncPlugin2.DataInterest)
          {
            StylusSyncPluginNativeShim syncShim = (StylusSyncPluginNativeShim) this.m_syncShimMap[oldValue];
            if (syncShim == null)
              throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSInternalError"));
            syncShim.Sink = stylusSyncPlugin2;
          }
          else
          {
            this.SyncPluginCollectionRemove(index, oldValue);
            this.SyncPluginCollectionInsert(index, newValue);
          }
        }
      }
      finally
      {
        this.m_nativeIRTS.ReleaseLock(RtsLockType.RtsSyncObjLock);
      }
    }

    internal void SyncPluginCollectionClear()
    {
      IntPtr zero = IntPtr.Zero;
      this.m_nativeIRTS.AcquireLock(RtsLockType.RtsSyncObjLock);
      try
      {
        lock (this.m_lock)
        {
          int count = this.m_syncCollection.Count;
          try
          {
            for (int iIndex = count; iIndex >= 1; --iIndex)
            {
              this.m_nativeIRTS.RemoveStylusSyncPlugin((uint) iIndex, ref zero);
              if (zero != IntPtr.Zero)
              {
                Marshal.Release(zero);
                zero = IntPtr.Zero;
              }
            }
            this.m_syncShimMap.Clear();
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
      finally
      {
        this.m_nativeIRTS.ReleaseLock(RtsLockType.RtsSyncObjLock);
      }
    }

    internal void ASyncPluginCollectionOnInsert(int index, object plugin)
    {
      lock (this.m_lock)
      {
        if (this.m_cascadingRTS != null)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("AsyncSinkCollectionContainsCascade"));
        if (!(plugin is RealTimeStylus realTimeStylus))
          return;
        if (0 < this.AsyncPluginCollection.Count)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("AsyncPluginCollectionNonEmpty"));
        if (realTimeStylus.m_cascadedMode)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("MultiLevelCascadingNotAllowed"));
        if (this == realTimeStylus)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("CircularCascadingNotAllowed"));
        if (this.m_control == null && IntPtr.Zero == this.m_Handle)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("NoArgRtsCantCascade"));
        if (IntPtr.Zero != realTimeStylus.m_Handle || realTimeStylus.m_control != null)
          throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("CascadeRTSConstructWithNoArgRts"));
        this.m_cascadingRTS = realTimeStylus;
        realTimeStylus.m_cascadedMode = true;
      }
    }

    internal void ASyncPluginCollectionInsertComplete(int index, object plugin)
    {
      this.m_nativeIRTS.AcquireLock(RtsLockType.RtsAsyncObjLock);
      try
      {
        lock (this.m_lock)
        {
          RealTimeStylus realTimeStylus = (RealTimeStylus) null;
          IntPtr zero = IntPtr.Zero;
          StylusAsyncPluginNativeShim o = (StylusAsyncPluginNativeShim) null;
          IntPtr num;
          if (plugin is INativeImplementationWrapper implementationWrapper)
          {
            num = implementationWrapper.AsyncPlugin != null ? Marshal.GetComInterfaceForObject((object) implementationWrapper.AsyncPlugin, typeof (StylusAsyncPluginNative)) : throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSInternalError"));
            realTimeStylus = plugin as RealTimeStylus;
          }
          else
          {
            IStylusAsyncPlugin sink = (IStylusAsyncPlugin) plugin;
            RealTimeStylusDataInterest dataInterestMask = RealTimeStylus.GetNativeDataInterestMask(sink.DataInterest);
            o = new StylusAsyncPluginNativeShim(sink, this, dataInterestMask);
            o.Sink = sink;
            num = Marshal.GetComInterfaceForObject((object) o, typeof (StylusAsyncPluginNative));
          }
          if (num == IntPtr.Zero)
            throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSInternalError"));
          try
          {
            this.m_nativeIRTS.AddStylusAsyncPlugin((uint) (index + 1), num);
            if (o == null)
              return;
            this.m_asyncShimMap.Add(plugin, (object) o);
          }
          catch (COMException ex)
          {
            if (realTimeStylus != null)
            {
              this.m_cascadingRTS = (RealTimeStylus) null;
              realTimeStylus.m_cascadedMode = false;
            }
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
          finally
          {
            Marshal.Release(num);
          }
        }
      }
      finally
      {
        this.m_nativeIRTS.ReleaseLock(RtsLockType.RtsAsyncObjLock);
      }
    }

    internal void ASyncPluginCollectionRemove(int index, object plugin)
    {
      this.m_nativeIRTS.AcquireLock(RtsLockType.RtsAsyncObjLock);
      try
      {
        lock (this.m_lock)
        {
          RealTimeStylus realTimeStylus = plugin as RealTimeStylus;
          IntPtr zero1 = IntPtr.Zero;
          this.m_nativeIRTS.RemoveStylusAsyncPlugin((uint) (index + 1), ref zero1);
          if (zero1 != IntPtr.Zero)
          {
            Marshal.Release(zero1);
            IntPtr zero2 = IntPtr.Zero;
          }
          if (realTimeStylus != null)
          {
            this.m_cascadingRTS = (RealTimeStylus) null;
            realTimeStylus.m_cascadedMode = false;
          }
          this.m_asyncShimMap.Remove(plugin);
        }
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
      finally
      {
        this.m_nativeIRTS.ReleaseLock(RtsLockType.RtsAsyncObjLock);
      }
    }

    internal void ASyncPluginCollectionSet(int index, object oldValue, object newValue)
    {
      INativeImplementationWrapper implementationWrapper1 = oldValue as INativeImplementationWrapper;
      INativeImplementationWrapper implementationWrapper2 = newValue as INativeImplementationWrapper;
      IStylusAsyncPlugin stylusAsyncPlugin1 = (IStylusAsyncPlugin) oldValue;
      IStylusAsyncPlugin stylusAsyncPlugin2 = (IStylusAsyncPlugin) newValue;
      this.m_nativeIRTS.AcquireLock(RtsLockType.RtsAsyncObjLock);
      try
      {
        lock (this.m_lock)
        {
          if (implementationWrapper1 == null && implementationWrapper2 == null && stylusAsyncPlugin1.DataInterest == stylusAsyncPlugin2.DataInterest)
          {
            StylusAsyncPluginNativeShim asyncShim = (StylusAsyncPluginNativeShim) this.m_asyncShimMap[oldValue];
            if (asyncShim == null)
              throw new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RTSInternalError"));
            asyncShim.Sink = stylusAsyncPlugin2;
          }
          else
          {
            this.ASyncPluginCollectionRemove(index, oldValue);
            this.ASyncPluginCollectionOnInsert(index, newValue);
            this.ASyncPluginCollectionInsertComplete(index, newValue);
          }
        }
      }
      finally
      {
        this.m_nativeIRTS.ReleaseLock(RtsLockType.RtsAsyncObjLock);
      }
    }

    internal void ASyncPluginCollectionClear()
    {
      IntPtr zero = IntPtr.Zero;
      this.m_nativeIRTS.AcquireLock(RtsLockType.RtsAsyncObjLock);
      try
      {
        lock (this.m_lock)
        {
          for (int count = this.m_asyncCollection.Count; count >= 1; --count)
          {
            this.m_nativeIRTS.RemoveStylusAsyncPlugin((uint) count, ref zero);
            if (zero != IntPtr.Zero)
            {
              Marshal.Release(zero);
              zero = IntPtr.Zero;
            }
          }
          if (this.m_cascadingRTS != null)
          {
            this.m_cascadingRTS.m_cascadedMode = false;
            this.m_cascadingRTS = (RealTimeStylus) null;
          }
          this.m_asyncShimMap.Clear();
        }
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
      finally
      {
        this.m_nativeIRTS.ReleaseLock(RtsLockType.RtsAsyncObjLock);
      }
    }

    internal void AcquireLock(RtsLockType lockType)
    {
      if (this.m_nativeIRTS == null)
        throw new InvalidOperationException();
      this.m_nativeIRTS.AcquireLock(lockType);
    }

    internal void ReleaseLock(RtsLockType lockType)
    {
      if (this.m_nativeIRTS == null)
        throw new InvalidOperationException();
      this.m_nativeIRTS.ReleaseLock(lockType);
    }

    public void Dispose()
    {
      lock (this.m_lock)
      {
        this.Dispose(true);
        GC.SuppressFinalize((object) this);
      }
    }

    void IStylusAsyncPlugin.RealTimeStylusEnabled(
      RealTimeStylus sender,
      RealTimeStylusEnabledData data)
    {
    }

    void IStylusAsyncPlugin.RealTimeStylusDisabled(
      RealTimeStylus sender,
      RealTimeStylusDisabledData data)
    {
    }

    void IStylusAsyncPlugin.StylusInRange(RealTimeStylus sender, StylusInRangeData data)
    {
    }

    void IStylusAsyncPlugin.StylusOutOfRange(RealTimeStylus sender, StylusOutOfRangeData data)
    {
    }

    void IStylusAsyncPlugin.StylusDown(RealTimeStylus sender, StylusDownData data)
    {
    }

    void IStylusAsyncPlugin.StylusUp(RealTimeStylus sender, StylusUpData data)
    {
    }

    void IStylusAsyncPlugin.StylusButtonDown(RealTimeStylus sender, StylusButtonDownData data)
    {
    }

    void IStylusAsyncPlugin.StylusButtonUp(RealTimeStylus sender, StylusButtonUpData data)
    {
    }

    void IStylusAsyncPlugin.InAirPackets(RealTimeStylus sender, InAirPacketsData data)
    {
    }

    void IStylusAsyncPlugin.Packets(RealTimeStylus sender, PacketsData data)
    {
    }

    void IStylusAsyncPlugin.SystemGesture(RealTimeStylus sender, SystemGestureData data)
    {
    }

    void IStylusAsyncPlugin.TabletAdded(RealTimeStylus sender, TabletAddedData data)
    {
    }

    void IStylusAsyncPlugin.TabletRemoved(RealTimeStylus sender, TabletRemovedData data)
    {
    }

    void IStylusAsyncPlugin.CustomStylusDataAdded(RealTimeStylus sender, CustomStylusData data)
    {
    }

    void IStylusAsyncPlugin.Error(RealTimeStylus sender, ErrorData data)
    {
    }

    DataInterestMask IStylusAsyncPlugin.DataInterest
    {
      get
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return DataInterestMask.DefaultStylusData;
      }
    }

    StylusSyncPluginNative INativeImplementationWrapper.SyncPlugin => throw new NotImplementedException(Helpers.SharedResources.Errors.GetString("RTSInterfaceNotImplemented"));

    StylusAsyncPluginNative INativeImplementationWrapper.AsyncPlugin
    {
      get
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (StylusAsyncPluginNative) this.m_nativeIRTS;
      }
    }
  }
}
