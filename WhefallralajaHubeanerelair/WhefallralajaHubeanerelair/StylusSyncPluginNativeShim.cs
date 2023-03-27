using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace WhefallralajaHubeanerelair;

internal sealed class StylusSyncPluginNativeShim : IStylusSyncPluginNative2, IStylusPluginNative
{
    void IStylusPluginNative.RtpEnabled(IRealTimeStylus realTime, uint cTcidCount, uint[] tcidArray)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.RtpDisabled(IRealTimeStylus realTime, uint cTcidCount, uint[] tcidArray)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.CursorNew(IRealTimeStylus realTime, uint tcid, uint cid)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.CursorInRange(IRealTimeStylus realTime, uint tcid, uint cid)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.CursorOutOfRange(IRealTimeStylus realTime, uint tcid, uint cid)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.CursorDown(IRealTimeStylus realTime, StylusInfo stylusInfo, uint propCountPerPkt, int[] pktArray,
        ref nint inOutPkt)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.CursorUp(IRealTimeStylus realTime, StylusInfo stylusInfo, uint propCountPerPkt, int[] pktArray,
        ref nint inOutPkt)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.InAirPackets(IRealTimeStylus realTime, StylusInfo stylusInfo, uint pktCount, uint cPktBuffLength,
        int[] pktArray, ref uint cInOutPkts, ref nint inOutPkts)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.Packets(IRealTimeStylus realTime, StylusInfo stylusInfo, uint pktCount, uint cPktBuffLength, int[] pktArray,
        ref uint cInOutPkts, ref nint inOutPkts)
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

    void IStylusSyncPluginNative2.StylusButtonUp(IRealTimeStylus realTime, uint cid, Guid stylusButtonGuid, ref nint pStylusPos)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.StylusButtonDown(IRealTimeStylus realTime, uint cid, Guid stylusButtonGuid, ref nint pStylusPos)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.SystemEvent(IRealTimeStylus realTime, uint tcid, uint cid, ushort systemEvent, SystemEventData eventData)
    {
        LogMethod();
    }

    void IStylusSyncPluginNative2.TabletAdded(IRealTimeStylus realTime, IInkTablet tablet)
    {
    }

    void IStylusSyncPluginNative2.TabletRemoved(IRealTimeStylus realTime, int iTabletIndex)
    {
    }

    void IStylusSyncPluginNative2.CustomData(IRealTimeStylus realTime, Guid guidId, uint cbData, nint data)
    {
    }

    void IStylusSyncPluginNative2.Error(IRealTimeStylus realTime, nint piEventSink, RealTimeStylusDataInterest rtpei, int hrErrorCode,
        ref nint lptrKey)
    {
    }

    void IStylusSyncPluginNative2.GetDataInterest(out RealTimeStylusDataInterest dataInterest)
    {
        dataInterest = RealTimeStylusDataInterest.AllStylusData;
    }

    void IStylusSyncPluginNative2.RtpEnabled(IRealTimeStylus realTime, uint cTcidCount, uint[] tcidArray)
    {
    }

    void IStylusPluginNative.RtpDisabled(IRealTimeStylus realTime, uint cTcidCount, uint[] tcidArray)
    {
    }

    void IStylusPluginNative.CursorNew(IRealTimeStylus realTime, uint tcid, uint cid)
    {
    }

    void IStylusPluginNative.CursorInRange(IRealTimeStylus realTime, uint tcid, uint cid)
    {
    }

    void IStylusPluginNative.CursorOutOfRange(IRealTimeStylus realTime, uint tcid, uint cid)
    {
    }

    void IStylusPluginNative.CursorDown(IRealTimeStylus realTime, StylusInfo stylusInfo, uint propCountPerPkt, int[] pktArray,
        ref nint inOutPkt)
    {
    }

    void IStylusPluginNative.CursorUp(IRealTimeStylus realTime, StylusInfo stylusInfo, uint propCountPerPkt, int[] pktArray,
        ref nint inOutPkt)
    {
    }

    void IStylusPluginNative.InAirPackets(IRealTimeStylus realTime, StylusInfo stylusInfo, uint pktCount, uint cPktBuffLength,
        int[] pktArray, ref uint cInOutPkts, ref nint inOutPkts)
    {
        LogMethod();
    }

    void IStylusPluginNative.Packets(IRealTimeStylus realTime, StylusInfo stylusInfo, uint pktCount, uint cPktBuffLength, int[] pktArray,
        ref uint cInOutPkts, ref nint inOutPkts)
    {
        LogMethod();
    }

    void IStylusPluginNative.StylusButtonUp(IRealTimeStylus realTime, uint cid, Guid stylusButtonGuid, ref nint pStylusPos)
    {
    }

    void IStylusPluginNative.StylusButtonDown(IRealTimeStylus realTime, uint cid, Guid stylusButtonGuid, ref nint pStylusPos)
    {
    }

    void IStylusPluginNative.SystemEvent(IRealTimeStylus realTime, uint tcid, uint cid, ushort systemEvent, SystemEventData eventData)
    {
    }

    void IStylusPluginNative.TabletAdded(IRealTimeStylus realTime, IInkTablet tablet)
    {
    }

    void IStylusPluginNative.TabletRemoved(IRealTimeStylus realTime, int iTabletIndex)
    {
    }

    void IStylusPluginNative.CustomData(IRealTimeStylus realTime, Guid guidId, uint cbData, nint data)
    {
    }

    void IStylusPluginNative.Error(IRealTimeStylus realTime, nint piEventSink, RealTimeStylusDataInterest rtpei, int hrErrorCode,
        ref nint lptrKey)
    {
    }

    void IStylusPluginNative.GetDataInterest(out RealTimeStylusDataInterest dataInterest)
    {
        dataInterest = RealTimeStylusDataInterest.AllStylusData;
    }
}