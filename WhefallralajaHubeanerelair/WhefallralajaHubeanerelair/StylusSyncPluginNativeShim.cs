using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace WhefallralajaHubeanerelair;

internal sealed class StylusSyncPluginNativeShim : StylusSyncPluginNative, IStylusPluginNative
{
    void IStylusPluginNative.RtpEnabled(IRealTimeStylusNative realTime, uint cTcidCount, uint[] tcidArray)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.RtpDisabled(IRealTimeStylusNative realTime, uint cTcidCount, uint[] tcidArray)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.CursorNew(IRealTimeStylusNative realTime, uint tcid, uint cid)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.CursorInRange(IRealTimeStylusNative realTime, uint tcid, uint cid)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.CursorOutOfRange(IRealTimeStylusNative realTime, uint tcid, uint cid)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.CursorDown(IRealTimeStylusNative realTime, StylusInfo stylusInfo, uint propCountPerPkt, int[] pktArray,
        ref nint InOutPkt)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.CursorUp(IRealTimeStylusNative realTime, StylusInfo stylusInfo, uint propCountPerPkt, int[] pktArray,
        ref nint InOutPkt)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.InAirPackets(IRealTimeStylusNative realTime, StylusInfo stylusInfo, uint pktCount, uint cPktBuffLength,
        int[] pktArray, ref uint cInOutPkts, ref nint InOutPkts)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.Packets(IRealTimeStylusNative realTime, StylusInfo stylusInfo, uint pktCount, uint cPktBuffLength, int[] pktArray,
        ref uint cInOutPkts, ref nint InOutPkts)
    {
        // 这里将会进入
        var packetPropertyCount = (int) (cPktBuffLength / pktCount);
        var packetData = pktArray;

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine($"--------- {stylusInfo.cid} {stylusInfo.tcid}");
        for (int i = 0; i < packetPropertyCount; i++)
        {
            stringBuilder.AppendLine($"[{i}]{packetData[i]}");
        }
        stringBuilder.AppendLine("---------");
        stringBuilder.AppendLine();

        Log(stringBuilder.ToString());
    }

    private void LogMethod([CallerMemberName] string name = null!)
    {
        Log(name + "\r\n");
    }

    private void Log(string message)
    {
        MainWindow.TextBlock.Dispatcher.InvokeAsync(() =>
        {
            MainWindow.TextBlock.Text += message;

            if (MainWindow.TextBlock.Text.Length > 10000)
            {
                MainWindow.TextBlock.Text = MainWindow.TextBlock.Text.Substring(5000);
            }
        });
    }

    void StylusSyncPluginNative.StylusButtonUp(IRealTimeStylusNative realTime, uint cid, Guid StylusButtonGuid, ref nint pStylusPos)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.StylusButtonDown(IRealTimeStylusNative realTime, uint cid, Guid StylusButtonGuid, ref nint pStylusPos)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.SystemEvent(IRealTimeStylusNative realTime, uint tcid, uint cid, ushort systemEvent, SystemEventData eventdata)
    {
        LogMethod();
    }

    void StylusSyncPluginNative.TabletAdded(IRealTimeStylusNative realTime, IInkTablet tablet)
    {
    }

    void StylusSyncPluginNative.TabletRemoved(IRealTimeStylusNative realTime, int iTabletIndex)
    {
    }

    void StylusSyncPluginNative.CustomData(IRealTimeStylusNative realTime, Guid guidId, uint cbData, nint data)
    {
    }

    void StylusSyncPluginNative.Error(IRealTimeStylusNative realTime, nint piEventSink, RealTimeStylusDataInterest rtpei, int hrErrorCode,
        ref nint lptrKey)
    {
    }

    void StylusSyncPluginNative.GetDataInterest(out RealTimeStylusDataInterest dataInterest)
    {
        dataInterest = RealTimeStylusDataInterest.RTPEI_AllStylusData;
    }

    void StylusSyncPluginNative.RtpEnabled(IRealTimeStylusNative realTime, uint cTcidCount, uint[] tcidArray)
    {
    }

    void IStylusPluginNative.RtpDisabled(IRealTimeStylusNative realTime, uint cTcidCount, uint[] tcidArray)
    {
    }

    void IStylusPluginNative.CursorNew(IRealTimeStylusNative realTime, uint tcid, uint cid)
    {
    }

    void IStylusPluginNative.CursorInRange(IRealTimeStylusNative realTime, uint tcid, uint cid)
    {
    }

    void IStylusPluginNative.CursorOutOfRange(IRealTimeStylusNative realTime, uint tcid, uint cid)
    {
    }

    void IStylusPluginNative.CursorDown(IRealTimeStylusNative realTime, StylusInfo stylusInfo, uint propCountPerPkt, int[] pktArray,
        ref nint InOutPkt)
    {
    }

    void IStylusPluginNative.CursorUp(IRealTimeStylusNative realTime, StylusInfo stylusInfo, uint propCountPerPkt, int[] pktArray,
        ref nint InOutPkt)
    {
    }

    void IStylusPluginNative.InAirPackets(IRealTimeStylusNative realTime, StylusInfo stylusInfo, uint pktCount, uint cPktBuffLength,
        int[] pktArray, ref uint cInOutPkts, ref nint InOutPkts)
    {
        LogMethod();
    }

    void IStylusPluginNative.Packets(IRealTimeStylusNative realTime, StylusInfo stylusInfo, uint pktCount, uint cPktBuffLength, int[] pktArray,
        ref uint cInOutPkts, ref nint InOutPkts)
    {
        LogMethod();
    }

    void IStylusPluginNative.StylusButtonUp(IRealTimeStylusNative realTime, uint cid, Guid StylusButtonGuid, ref nint pStylusPos)
    {
    }

    void IStylusPluginNative.StylusButtonDown(IRealTimeStylusNative realTime, uint cid, Guid StylusButtonGuid, ref nint pStylusPos)
    {
    }

    void IStylusPluginNative.SystemEvent(IRealTimeStylusNative realTime, uint tcid, uint cid, ushort systemEvent, SystemEventData eventdata)
    {
    }

    void IStylusPluginNative.TabletAdded(IRealTimeStylusNative realTime, IInkTablet tablet)
    {
    }

    void IStylusPluginNative.TabletRemoved(IRealTimeStylusNative realTime, int iTabletIndex)
    {
    }

    void IStylusPluginNative.CustomData(IRealTimeStylusNative realTime, Guid guidId, uint cbData, nint data)
    {
    }

    void IStylusPluginNative.Error(IRealTimeStylusNative realTime, nint piEventSink, RealTimeStylusDataInterest rtpei, int hrErrorCode,
        ref nint lptrKey)
    {
    }

    void IStylusPluginNative.GetDataInterest(out RealTimeStylusDataInterest dataInterest)
    {
        dataInterest = RealTimeStylusDataInterest.RTPEI_AllStylusData;
    }
}