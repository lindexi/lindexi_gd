// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.__MIDL___MIDL_itf_msinkaut_tip_merged_0001_0127_0007
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal enum __MIDL___MIDL_itf_msinkaut_tip_merged_0001_0127_0007
  {
    EventMask_InPlaceStateChanging = 1,
    EventMask_InPlaceStateChanged = 2,
    EventMask_InPlaceSizeChanging = 4,
    EventMask_InPlaceSizeChanged = 8,
    EventMask_InputAreaChanging = 16, // 0x00000010
    EventMask_InputAreaChanged = 32, // 0x00000020
    EventMask_CorrectionModeChanging = 64, // 0x00000040
    EventMask_CorrectionModeChanged = 128, // 0x00000080
    EventMask_InPlaceVisibilityChanging = 256, // 0x00000100
    EventMask_InPlaceVisibilityChanged = 512, // 0x00000200
    EventMask_TextInserting = 1024, // 0x00000400
    EventMask_TextInserted = 2048, // 0x00000800
    EventMask_All = 4095, // 0x00000FFF
  }
}
