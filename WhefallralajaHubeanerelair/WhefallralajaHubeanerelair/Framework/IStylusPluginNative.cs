using System;
using System.Runtime.InteropServices;
using System.Security;

namespace WhefallralajaHubeanerelair;

[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SuppressUnmanagedCodeSecurity]
[Guid("3A2CCD76-AFB5-41b9-A9E3-FC02BF5C4299")]
[ComImport]
internal interface IStylusPluginNative
{
    void RtpEnabled([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] uint cTcidCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] uint[] tcidArray);

    void RtpDisabled([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] uint cTcidCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] uint[] tcidArray);

    void CursorNew([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] uint tcid, [In] uint cid);

    void CursorInRange([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] uint tcid, [In] uint cid);

    void CursorOutOfRange([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] uint tcid, [In] uint cid);

    void CursorDown(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
        [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
        [In] uint propCountPerPkt,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In] int[] pktArray,
        [In, Out] ref IntPtr InOutPkt);

    void CursorUp(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
        [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
        [In] uint propCountPerPkt,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In] int[] pktArray,
        [In, Out] ref IntPtr InOutPkt);

    void InAirPackets(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
        [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
        [In] uint pktCount,
        [In] uint cPktBuffLength,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), In] int[] pktArray,
        [In, Out] ref uint cInOutPkts,
        [In, Out] ref IntPtr InOutPkts);

    void Packets(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
        [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
        [In] uint pktCount,
        [In] uint cPktBuffLength,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), In] int[] pktArray,
        [In, Out] ref uint cInOutPkts,
        [In, Out] ref IntPtr InOutPkts);

    void StylusButtonUp(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
        [In] uint cid,
        [MarshalAs(UnmanagedType.LPStruct), In] Guid StylusButtonGuid,
        [In, Out] ref IntPtr pStylusPos);

    void StylusButtonDown(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
        [In] uint cid,
        [MarshalAs(UnmanagedType.LPStruct), In] Guid StylusButtonGuid,
        [In, Out] ref IntPtr pStylusPos);

    void SystemEvent(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
        [In] uint tcid,
        [In] uint cid,
        [In] ushort systemEvent,
        [MarshalAs(UnmanagedType.Struct), In] SystemEventData eventdata);

    void TabletAdded([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [MarshalAs(UnmanagedType.Interface), In] IInkTablet tablet);

    void TabletRemoved([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] int iTabletIndex);

    void CustomData([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [MarshalAs(UnmanagedType.LPStruct), In] Guid guidId, [In] uint cbData, [In] IntPtr data);

    void Error(
        [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
        [In] IntPtr piEventSink,
        [In] RealTimeStylusDataInterest rtpei,
        [In] int hrErrorCode,
        [In, Out] ref IntPtr lptrKey);

    void GetDataInterest(out RealTimeStylusDataInterest dataInterest);
}