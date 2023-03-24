// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.StylusAsyncPluginNative
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.StylusInput
{
  [Guid("46EA2855-1838-4088-B1C4-010D53F65140")]
  [SuppressUnmanagedCodeSecurity]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [ComImport]
  internal interface StylusAsyncPluginNative : IStylusPluginNative
  {
    new void RtpEnabled([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] uint cTcidCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] uint[] tcidArray);

    new void RtpDisabled([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] uint cTcidCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] uint[] tcidArray);

    new void CursorNew([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] uint tcid, [In] uint cid);

    new void CursorInRange([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] uint tcid, [In] uint cid);

    new void CursorOutOfRange([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] uint tcid, [In] uint cid);

    new void CursorDown(
      [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
      [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
      [In] uint propCountPerPkt,
      [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In] int[] pktArray,
      [In, Out] ref IntPtr InOutPkt);

    new void CursorUp(
      [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
      [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
      [In] uint propCountPerPkt,
      [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In] int[] pktArray,
      [In, Out] ref IntPtr InOutPkt);

    new void InAirPackets(
      [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
      [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
      [In] uint pktCount,
      [In] uint cPktBuffLength,
      [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), In] int[] pktArray,
      [In, Out] ref uint cInOutPkts,
      [In, Out] ref IntPtr InOutPkts);

    new void Packets(
      [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
      [MarshalAs(UnmanagedType.LPStruct), In] StylusInfo stylusInfo,
      [In] uint pktCount,
      [In] uint cPktBuffLength,
      [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), In] int[] pktArray,
      [In, Out] ref uint cInOutPkts,
      [In, Out] ref IntPtr InOutPkts);

    new void StylusButtonUp(
      [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
      [In] uint cid,
      [MarshalAs(UnmanagedType.LPStruct), In] Guid StylusButtonGuid,
      [In, Out] ref IntPtr pStylusPos);

    new void StylusButtonDown(
      [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
      [In] uint cid,
      [MarshalAs(UnmanagedType.LPStruct), In] Guid StylusButtonGuid,
      [In, Out] ref IntPtr pStylusPos);

    new void SystemEvent(
      [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
      [In] uint tcid,
      [In] uint cid,
      [In] ushort systemEvent,
      [MarshalAs(UnmanagedType.Struct), In] SystemEventData eventdata);

    new void TabletAdded([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [MarshalAs(UnmanagedType.Interface), In] IInkTablet tablet);

    new void TabletRemoved([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [In] int iTabletIndex);

    new void CustomData([MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime, [MarshalAs(UnmanagedType.LPStruct), In] Guid guidId, [In] uint cbData, [In] IntPtr data);

    new void Error(
      [MarshalAs(UnmanagedType.Interface), In] IRealTimeStylusNative realTime,
      [In] IntPtr piEventSink,
      [In] RealTimeStylusDataInterest rtpei,
      [In] int hrErrorCode,
      [In, Out] ref IntPtr lptrKey);

    new void GetDataInterest(out RealTimeStylusDataInterest dataInterest);
  }
}
