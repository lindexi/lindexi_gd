// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.GestureRecognizer
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using Microsoft.StylusInput.PluginData;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Microsoft.StylusInput
{
  public sealed class GestureRecognizer : 
    IStylusSyncPlugin,
    IStylusAsyncPlugin,
    INativeImplementationWrapper,
    IDisposable
  {
    private static Hashtable s_nativeToManagedMap = new Hashtable();
    public static readonly Guid GestureRecognitionDataGuid = new Guid("{5AD3A953-2074-421d-B415-C6B88FCB72E3}");
    private IInkGestureRecognizerNative m_GestureRecognizerNative;
    private bool m_disposed;
    private bool m_enabled;
    private int m_maxStrokeCount = 1500;
    private object m_lock;

    public GestureRecognizer()
    {
      try
      {
        this.m_lock = new object();
        this.m_GestureRecognizerNative = (IInkGestureRecognizerNative) ComObjectCreator.CreateInstanceLicense(new Guid("639F5AF5-BCED-4369-AC34-360B16D955FD"), new Guid("F7E9B72A-A6FA-43fa-A0D8-A0C038B97203"), "{CAAD7274-4004-44e0-8A17-D6F1919C443A}");
        GestureRecognizer.AddToMap(this.m_GestureRecognizerNative, this);
        this.m_enabled = this.m_GestureRecognizerNative.get_Enabled();
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    ~GestureRecognizer() => this.Dispose(false);

    internal static void AddToMap(IInkGestureRecognizerNative native, GestureRecognizer managed)
    {
      lock (GestureRecognizer.s_nativeToManagedMap.SyncRoot)
        GestureRecognizer.s_nativeToManagedMap.Add((object) native, (object) managed);
    }

    internal static void RemoveFromMap(IInkGestureRecognizerNative native)
    {
      lock (GestureRecognizer.s_nativeToManagedMap.SyncRoot)
        GestureRecognizer.s_nativeToManagedMap.Remove((object) native);
    }

    internal static GestureRecognizer GetManagedForNative(IntPtr nativeInterface)
    {
      GestureRecognizer managedForNative = (GestureRecognizer) null;
      IntPtr ppv = IntPtr.Zero;
      Guid iid = new Guid("F7E9B72A-A6FA-43fa-A0D8-A0C038B97203");
      lock (GestureRecognizer.s_nativeToManagedMap.SyncRoot)
      {
        Marshal.QueryInterface(nativeInterface, ref iid, out ppv);
        if (ppv != IntPtr.Zero)
        {
          if (Marshal.GetTypedObjectForIUnknown(nativeInterface, typeof (IInkGestureRecognizerNative)) is IInkGestureRecognizerNative objectForIunknown)
            managedForNative = (GestureRecognizer) GestureRecognizer.s_nativeToManagedMap[(object) objectForIunknown];
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
        if (this.m_GestureRecognizerNative != null)
          GestureRecognizer.RemoveFromMap(this.m_GestureRecognizerNative);
        if (disposing)
        {
          this.m_GestureRecognizerNative.set_Enabled(false);
          if (this.m_GestureRecognizerNative != null)
          {
            Marshal.ReleaseComObject((object) this.m_GestureRecognizerNative);
            this.m_GestureRecognizerNative = (IInkGestureRecognizerNative) null;
          }
        }
        this.m_disposed = true;
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
            return this.m_enabled;
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
            this.m_GestureRecognizerNative.set_Enabled(value);
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

    public int MaxStrokeCount
    {
      get
      {
        lock (this.m_lock)
        {
          if (this.m_disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          try
          {
            this.m_maxStrokeCount = this.m_GestureRecognizerNative.get_MaxStrokeCount();
            return this.m_maxStrokeCount;
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
            this.m_GestureRecognizerNative.set_MaxStrokeCount(value);
            this.m_maxStrokeCount = value;
          }
          catch (COMException ex)
          {
            InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
            throw;
          }
        }
      }
    }

    public void EnableGestures(ApplicationGesture[] gestures)
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          if (this.m_GestureRecognizerNative == null)
            return;
          uint length = (uint) gestures.Length;
          int[] gestureArray = new int[(IntPtr) length];
          for (uint index = 0; index < length; ++index)
            gestureArray[(IntPtr) index] = (int) gestures[(IntPtr) index];
          this.m_GestureRecognizerNative.EnableGestures(length, gestureArray);
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public void Reset()
    {
      lock (this.m_lock)
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          if (this.m_GestureRecognizerNative == null)
            return;
          this.m_GestureRecognizerNative.Reset();
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
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

    StylusSyncPluginNative INativeImplementationWrapper.SyncPlugin
    {
      get
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (StylusSyncPluginNative) this.m_GestureRecognizerNative;
      }
    }

    StylusAsyncPluginNative INativeImplementationWrapper.AsyncPlugin
    {
      get
      {
        if (this.m_disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (StylusAsyncPluginNative) this.m_GestureRecognizerNative;
      }
    }

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
