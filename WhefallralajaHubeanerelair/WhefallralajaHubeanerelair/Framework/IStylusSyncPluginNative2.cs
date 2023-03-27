using System;
using System.Runtime.InteropServices;
using System.Security;

namespace WhefallralajaHubeanerelair;

[Guid("380D13B0-1992-49ea-9D80-32F3AF851132")]
[SuppressUnmanagedCodeSecurity]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
internal interface IStylusSyncPluginNative2 : IStylusPluginNative
{
    new void RtpEnabled([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime, [In] uint cTcidCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] uint[] tcidArray);

    new void RtpDisabled([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime, [In] uint cTcidCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] uint[] tcidArray);

    new void CursorNew([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime, [In] uint tcid, [In] uint cid);

    new void CursorInRange([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime, [In] uint tcid, [In] uint cid);

    new void CursorOutOfRange([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime, [In] uint tcid, [In] uint cid);

    new void CursorDown(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime,
        [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
        [In] uint propCountPerPkt,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In] int[] pktArray,
        [In, Out] ref IntPtr inOutPkt);

    new void CursorUp(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime,
        [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
        [In] uint propCountPerPkt,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In] int[] pktArray,
        [In, Out] ref IntPtr inOutPkt);

    new void InAirPackets(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime,
        [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
        [In] uint pktCount,
        [In] uint cPktBuffLength,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), In] int[] pktArray,
        [In, Out] ref uint cInOutPkts,
        [In, Out] ref IntPtr inOutPkts);

    new void Packets(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime,
        [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
        [In] uint pktCount,
        [In] uint cPktBuffLength,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), In] int[] pktArray,
        [In, Out] ref uint cInOutPkts,
        [In, Out] ref IntPtr inOutPkts);

    new void StylusButtonUp(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime,
        [In] uint cid,
        [MarshalAs(UnmanagedType.LPStruct), In] Guid stylusButtonGuid,
        [In, Out] ref IntPtr pStylusPos);

    new void StylusButtonDown(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime,
        [In] uint cid,
        [MarshalAs(UnmanagedType.LPStruct), In] Guid stylusButtonGuid,
        [In, Out] ref IntPtr pStylusPos);

    new void SystemEvent(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime,
        [In] uint tcid,
        [In] uint cid,
        [In] ushort systemEvent,
        [MarshalAs(UnmanagedType.Struct), In] SystemEventData eventData);

    new void TabletAdded([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime, [MarshalAs(UnmanagedType.Interface), In] IInkTablet tablet);

    new void TabletRemoved([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime, [In] int iTabletIndex);

    new void CustomData([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime, [MarshalAs(UnmanagedType.LPStruct), In] Guid guidId, [In] uint cbData, [In] IntPtr data);

    new void Error(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylus realTime,
        [In] IntPtr piEventSink,
        [In] RealTimeStylusDataInterest rtpei,
        [In] int hrErrorCode,
        [In, Out] ref IntPtr lptrKey);

    new void GetDataInterest(out RealTimeStylusDataInterest dataInterest);
}