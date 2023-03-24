// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.IRealTimeStylusNative
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.StylusInput
{
  [Guid("C6C77F97-545E-4873-85F2-E0FEE550B2E9")]
  [SuppressUnmanagedCodeSecurity]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IRealTimeStylusNative
  {
    void Enable([MarshalAs(UnmanagedType.Bool), In] bool fEnable);

    void IsEnabled([MarshalAs(UnmanagedType.Bool)] out bool fEnable);

    void GetHWND(out IntPtr hWnd);

    void SetHWND([In] IntPtr hWnd);

    void AcquireLock([In] RtsLockType lockTarget);

    void ReleaseLock([In] RtsLockType lockTarget);

    void AddStylusSyncPlugin([In] uint iIndex, [In] IntPtr RtpSink);

    void RemoveStylusSyncPlugin([In] uint iIndex, [In, Out] ref IntPtr RtpSink);

    void GetStylusSyncPlugin([In] uint iIndex, out IntPtr RtpSink);

    void GetStylusSyncPluginCount(out uint cSyncSinkCount);

    void AddStylusAsyncPlugin([In] uint iIndex, [In] IntPtr RtpQueueSink);

    void RemoveStylusAsyncPlugin([In] uint iIndex, [In, Out] ref IntPtr RtpQueueSink);

    void GetStylusAsyncPlugin([In] uint iIndex, out IntPtr RtpQueueSink);

    void GetStylusAsyncPluginCount(out uint cAsycSinkCount);

    void AddCustomStylusDataToQueue([In] StylusQueueNative sq, [MarshalAs(UnmanagedType.LPStruct), In] Guid guidId, [In] uint cbData, [In] IntPtr pbData);

    void ClearStylusQueues();

    void SetAllTabletsMode([MarshalAs(UnmanagedType.Bool), In] bool fUseMouseForInput);

    void SetSingleTabletMode([MarshalAs(UnmanagedType.Interface), In] IInkTablet piTablet);

    [return: MarshalAs(UnmanagedType.Interface)]
    IInkTablet GetTablet();

    uint GetTabletContextIdForTablet([MarshalAs(UnmanagedType.Interface), In] IInkTablet piTablet);

    [return: MarshalAs(UnmanagedType.Interface)]
    IInkTablet GetTabletForTabletContextId([ComAliasName("TABLET_CONTEXT_ID"), In] uint tcid);

    [return: MarshalAs(UnmanagedType.Interface)]
    IInkCursors GetCursors();

    [return: MarshalAs(UnmanagedType.Interface)]
    IInkCursor GetCursorForId([ComAliasName("CURSOR_ID"), In] uint cid);

    void SetDesiredPacketDescription([In] uint cPropertyCount, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Struct), In] Guid[] pPropertyGuids);

    void GetDesiredPacketDescription([In, Out] ref uint cPropertyCount, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Struct), In, Out] Guid[] pPropertyGuids);

    void GetPacketDescriptionData(
      [In] uint tcid,
      out float inkToDeviceScaleX,
      out float inkToDeviceScaleY,
      [In, Out] ref uint cPropertyCount,
      [MarshalAs(UnmanagedType.LPArray), In, Out] PacketProperty[] packetProperties);

    void GetWindowInputRect([MarshalAs(UnmanagedType.Struct)] out tagRECT prcWndInputRect);

    void SetWindowInputRect([MarshalAs(UnmanagedType.Struct), In] ref tagRECT prcWndInputRect);

    void FlicksEnable([MarshalAs(UnmanagedType.Bool), In] bool fEnable);

    void IsFlicksEnabled([MarshalAs(UnmanagedType.Bool)] out bool fEnable);

    void AllTouchEnable([MarshalAs(UnmanagedType.Bool), In] bool fEnable);

    void IsAllTouchEnabled([MarshalAs(UnmanagedType.Bool)] out bool fEnable);

    void MultiTouchEnable([MarshalAs(UnmanagedType.Bool), In] bool fEnable);

    void IsMultiTouchEnabled([MarshalAs(UnmanagedType.Bool)] out bool fEnable);
  }
}
