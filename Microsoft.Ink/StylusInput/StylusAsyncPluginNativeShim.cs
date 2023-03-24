// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.StylusAsyncPluginNativeShim
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using Microsoft.StylusInput.PluginData;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Microsoft.StylusInput
{
  internal sealed class StylusAsyncPluginNativeShim : StylusAsyncPluginNative, IStylusPluginNative
  {
    private IStylusAsyncPlugin m_Sink;
    private RealTimeStylus m_RealTimeStylus;
    private bool m_first;
    private bool m_last;
    private int m_notifyErrorMarker;
    private RealTimeStylusDataInterest m_rpei;

    internal StylusAsyncPluginNativeShim(
      IStylusAsyncPlugin sink,
      RealTimeStylus rts,
      RealTimeStylusDataInterest rei)
    {
      if (rts == null)
        throw new ArgumentNullException(nameof (rts), Helpers.SharedResources.Errors.GetString("RTSInternalError"));
      this.m_Sink = sink;
      this.m_RealTimeStylus = rts;
      this.m_rpei = rei;
    }

    public bool First
    {
      set => this.m_first = value;
    }

    public bool Last
    {
      set => this.m_last = value;
    }

    public IStylusAsyncPlugin Sink
    {
      set => this.m_Sink = value;
    }

    private static void MarshalQueueDataIfChanged(
      StylusDataBase data,
      ref IntPtr queueArray,
      ref uint cPkts)
    {
      if (!data.PacketDataModified)
        return;
      IntPtr num1 = IntPtr.Zero;
      int length = 0;
      if (data.Count != 0)
      {
        length = data.Count;
        int num2 = Marshal.SizeOf(typeof (int));
        try
        {
          num1 = Marshal.AllocCoTaskMem(num2 * length);
          Marshal.Copy(data.GetData(), 0, num1, length);
        }
        catch (Exception ex)
        {
          if (num1 != IntPtr.Zero)
            Marshal.FreeCoTaskMem(num1);
          throw;
        }
      }
      cPkts = (uint) (length / data.PacketPropertyCount);
      queueArray = num1;
    }

    private void NotifyError(Exception e, DataInterestMask rtpei)
    {
      COMException comException = new COMException((string) null, -2147220924);
      this.m_notifyErrorMarker = ExceptionKeyGenerator.GetUniqueKey();
      this.m_RealTimeStylus.AddToExceptionMap(this.m_notifyErrorMarker, new ErrorData((object) this.m_Sink, rtpei, e));
      throw comException;
    }

    public void RtpEnabled(IRealTimeStylusNative realTime, uint cTcidCount, uint[] tcidArray)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        int[] tcids = new int[(IntPtr) cTcidCount];
        for (uint index = 0; index < cTcidCount; ++index)
          tcids[(IntPtr) index] = (int) tcidArray[(IntPtr) index];
        this.m_Sink.RealTimeStylusEnabled(this.m_RealTimeStylus, new RealTimeStylusEnabledData(tcids));
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.RealTimeStylusEnabled);
        throw;
      }
    }

    public void RtpDisabled(IRealTimeStylusNative realTime, uint cTcidCount, uint[] tcidArray)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        int[] tcids = new int[(IntPtr) cTcidCount];
        for (uint index = 0; index < cTcidCount; ++index)
          tcids[(IntPtr) index] = (int) tcidArray[(IntPtr) index];
        this.m_Sink.RealTimeStylusDisabled(this.m_RealTimeStylus, new RealTimeStylusDisabledData(tcids));
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.RealTimeStylusDisabled);
        throw;
      }
    }

    public void CursorNew(IRealTimeStylusNative realTime, uint tcid, uint cid)
    {
    }

    public void CursorInRange(IRealTimeStylusNative realTime, uint tcid, uint cid)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        this.m_Sink.StylusInRange(this.m_RealTimeStylus, new StylusInRangeData(this.m_RealTimeStylus.GetStylusForId((int) cid, (int) tcid)));
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.StylusInRange);
        throw;
      }
    }

    public void CursorOutOfRange(IRealTimeStylusNative realTime, uint tcid, uint cid)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        this.m_Sink.StylusOutOfRange(this.m_RealTimeStylus, new StylusOutOfRangeData(this.m_RealTimeStylus.GetStylusForId((int) cid, (int) tcid)));
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.StylusOutOfRange);
        throw;
      }
    }

    public void CursorDown(
      IRealTimeStylusNative realTime,
      StylusInfo stylusInfo,
      uint pktArraySize,
      int[] pktArray,
      ref IntPtr InOutPkt)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        StylusDownData data = new StylusDownData(this.m_RealTimeStylus.GetStylusForId((int) stylusInfo.cid, (int) stylusInfo.tcid), (int) pktArraySize, pktArray);
        data.m_stylusInfo = stylusInfo;
        this.m_Sink.StylusDown(this.m_RealTimeStylus, data);
        uint cPkts = 1;
        StylusAsyncPluginNativeShim.MarshalQueueDataIfChanged((StylusDataBase) data, ref InOutPkt, ref cPkts);
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.StylusDown);
        throw;
      }
    }

    public void CursorUp(
      IRealTimeStylusNative realTime,
      StylusInfo stylusInfo,
      uint pktArraySize,
      int[] pktArray,
      ref IntPtr InOutPkt)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        StylusUpData data = new StylusUpData(this.m_RealTimeStylus.GetStylusForId((int) stylusInfo.cid, (int) stylusInfo.tcid), (int) pktArraySize, pktArray);
        data.m_stylusInfo = stylusInfo;
        this.m_Sink.StylusUp(this.m_RealTimeStylus, data);
        uint cPkts = 1;
        StylusAsyncPluginNativeShim.MarshalQueueDataIfChanged((StylusDataBase) data, ref InOutPkt, ref cPkts);
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.StylusUp);
        throw;
      }
    }

    public void InAirPackets(
      IRealTimeStylusNative realTime,
      StylusInfo stylusInfo,
      uint pktCount,
      uint cPktBuffLength,
      int[] pktArray,
      ref uint cInOutPkts,
      ref IntPtr InOutPkts)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        InAirPacketsData data = new InAirPacketsData(this.m_RealTimeStylus.GetStylusForId((int) stylusInfo.cid, (int) stylusInfo.tcid), (int) (cPktBuffLength / pktCount), pktArray);
        this.m_Sink.InAirPackets(this.m_RealTimeStylus, data);
        StylusAsyncPluginNativeShim.MarshalQueueDataIfChanged((StylusDataBase) data, ref InOutPkts, ref cInOutPkts);
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.InAirPackets);
        throw;
      }
    }

    public void Packets(
      IRealTimeStylusNative realTime,
      StylusInfo stylusInfo,
      uint pktCount,
      uint cPktBuffLength,
      int[] pktArray,
      ref uint cInOutPkts,
      ref IntPtr InOutPkts)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        PacketsData data = new PacketsData(this.m_RealTimeStylus.GetStylusForId((int) stylusInfo.cid, (int) stylusInfo.tcid), (int) (cPktBuffLength / pktCount), pktArray);
        data.m_stylusInfo = stylusInfo;
        data.m_pktCount = pktCount;
        this.m_Sink.Packets(this.m_RealTimeStylus, data);
        StylusAsyncPluginNativeShim.MarshalQueueDataIfChanged((StylusDataBase) data, ref InOutPkts, ref cInOutPkts);
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.Packets);
        throw;
      }
    }

    public void StylusButtonUp(
      IRealTimeStylusNative realTime,
      uint cid,
      Guid StylusButtonGuid,
      ref IntPtr pStylusPos)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        Stylus stylusForId = this.m_RealTimeStylus.GetStylusForId((int) cid);
        int num = 0;
        while (num < stylusForId.Buttons.Count && !(stylusForId.Buttons.GetId(num) == StylusButtonGuid))
          ++num;
        this.m_Sink.StylusButtonUp(this.m_RealTimeStylus, new StylusButtonUpData(stylusForId, num));
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.StylusButtonUp);
        throw;
      }
    }

    public void StylusButtonDown(
      IRealTimeStylusNative realTime,
      uint cid,
      Guid StylusButtonGuid,
      ref IntPtr pStylusPos)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        Stylus stylusForId = this.m_RealTimeStylus.GetStylusForId((int) cid);
        int num = 0;
        while (num < stylusForId.Buttons.Count && !(stylusForId.Buttons.GetId(num) == StylusButtonGuid))
          ++num;
        this.m_Sink.StylusButtonDown(this.m_RealTimeStylus, new StylusButtonDownData(stylusForId, num));
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.StylusButtonDown);
        throw;
      }
    }

    public void SystemEvent(
      IRealTimeStylusNative realTime,
      uint tcid,
      uint cid,
      ushort systemEvent,
      SystemEventData eventdata)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        this.m_Sink.SystemGesture(this.m_RealTimeStylus, new SystemGestureData(this.m_RealTimeStylus.GetStylusForId((int) cid, (int) tcid), (SystemGesture) systemEvent, new Point(eventdata.xPos, eventdata.yPos), systemEvent == (ushort) 31 ? eventdata.buttonState : (int) eventdata.modifier, eventdata.key, (int) eventdata.cursorMode));
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.SystemGesture);
        throw;
      }
    }

    public void TabletAdded(IRealTimeStylusNative realTime, IInkTablet tablet)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        this.m_Sink.TabletAdded(this.m_RealTimeStylus, new TabletAddedData(new Tablet(tablet)));
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.TabletAdded);
        throw;
      }
    }

    public void TabletRemoved(IRealTimeStylusNative realTime, int iTabletIndex)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        this.m_Sink.TabletRemoved(this.m_RealTimeStylus, new TabletRemovedData(iTabletIndex));
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.TabletRemoved);
        throw;
      }
    }

    public void CustomData(IRealTimeStylusNative realTime, Guid guidId, uint cbData, IntPtr data)
    {
      try
      {
        if (this.m_Sink == null)
          return;
        if (guidId == NativeSynchronizationMarkers.DynamicRenderer)
        {
          if (cbData <= 1U)
            return;
          DynamicRendererCachedDataNative structure = (DynamicRendererCachedDataNative) Marshal.PtrToStructure(data, typeof (DynamicRendererCachedDataNative));
          DynamicRenderer managedForNative = DynamicRenderer.GetManagedForNative(structure.dynamicRenderer);
          DynamicRendererCachedData data1 = new DynamicRendererCachedData(structure.cachedDataId, managedForNative);
          this.m_Sink.CustomStylusDataAdded(this.m_RealTimeStylus, new CustomStylusData(DynamicRenderer.DynamicRendererCachedDataGuid, (object) data1));
        }
        else if (guidId == NativeSynchronizationMarkers.GestureRecognizer)
        {
          if (cbData <= 1U)
            return;
          int num = Marshal.SizeOf(typeof (GestureAlternateNative));
          int length = (int) cbData / num;
          if (length <= 0)
            return;
          long ptr = (long) data;
          GestureAlternate[] gestureAlternates = new GestureAlternate[length];
          int index = 0;
          while (index < length)
          {
            GestureAlternateNative structure = (GestureAlternateNative) Marshal.PtrToStructure((IntPtr) ptr, typeof (GestureAlternateNative));
            gestureAlternates[index] = new GestureAlternate(structure);
            ++index;
            ptr += (long) num;
          }
          GestureRecognitionData data2 = new GestureRecognitionData(gestureAlternates);
          this.m_Sink.CustomStylusDataAdded(this.m_RealTimeStylus, new CustomStylusData(GestureRecognizer.GestureRecognitionDataGuid, (object) data2));
        }
        else
        {
          object data3 = (object) null;
          lock (this.m_RealTimeStylus.m_customDataMap.SyncRoot)
            data3 = this.m_RealTimeStylus.m_customDataMap[(object) data];
          if (data3 == null)
            return;
          this.m_Sink.CustomStylusDataAdded(this.m_RealTimeStylus, new CustomStylusData(guidId, data3));
        }
      }
      catch (Exception ex)
      {
        this.NotifyError(ex, DataInterestMask.CustomStylusDataAdded);
        throw;
      }
      finally
      {
        if (this.m_last)
        {
          lock (this.m_RealTimeStylus.m_customDataMap.SyncRoot)
            this.m_RealTimeStylus.m_customDataMap.Remove((object) data);
        }
      }
    }

    public void Error(
      IRealTimeStylusNative realTime,
      IntPtr piEventSink,
      RealTimeStylusDataInterest rtpei,
      int hrErrorCode,
      ref IntPtr lptrKey)
    {
      ErrorData data = (ErrorData) null;
      if (hrErrorCode == -2147220924 && lptrKey == IntPtr.Zero && this.m_notifyErrorMarker != 0)
      {
        data = this.m_RealTimeStylus.GetExceptionFromMap(this.m_notifyErrorMarker);
        lptrKey = (IntPtr) this.m_notifyErrorMarker;
        this.m_notifyErrorMarker = 0;
      }
      else if (hrErrorCode == -2147220924 && lptrKey != IntPtr.Zero)
        data = this.m_RealTimeStylus.GetExceptionFromMap((int) lptrKey);
      else if (hrErrorCode != -2147220924)
      {
        Exception innerException = InkErrors.GetExceptionForInkError(hrErrorCode) ?? (Exception) new COMException((string) null, hrErrorCode);
        data = new ErrorData((object) DynamicRenderer.GetManagedForNative(piEventSink) ?? (object) GestureRecognizer.GetManagedForNative(piEventSink), (DataInterestMask) rtpei, innerException);
      }
      if (this.m_last && hrErrorCode == -2147220924)
      {
        this.m_RealTimeStylus.RemoveFromExceptionMap((int) lptrKey);
        lptrKey = IntPtr.Zero;
      }
      if (this.m_Sink == null || (this.m_rpei & RealTimeStylusDataInterest.RTPEI_Error) != RealTimeStylusDataInterest.RTPEI_Error)
        return;
      this.m_Sink.Error(this.m_RealTimeStylus, data);
    }

    public void GetDataInterest(out RealTimeStylusDataInterest rpei) => rpei = this.m_rpei | RealTimeStylusDataInterest.RTPEI_Error;
  }
}
