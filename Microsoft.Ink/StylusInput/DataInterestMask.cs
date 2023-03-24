// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.DataInterestMask
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

namespace Microsoft.StylusInput
{
  public enum DataInterestMask
  {
    AllStylusData = -1, // 0xFFFFFFFF
    Error = 1,
    RealTimeStylusEnabled = 2,
    RealTimeStylusDisabled = 4,
    StylusInRange = 16, // 0x00000010
    InAirPackets = 32, // 0x00000020
    StylusOutOfRange = 64, // 0x00000040
    StylusDown = 128, // 0x00000080
    Packets = 256, // 0x00000100
    StylusUp = 512, // 0x00000200
    StylusButtonUp = 1024, // 0x00000400
    StylusButtonDown = 2048, // 0x00000800
    SystemGesture = 4096, // 0x00001000
    TabletAdded = 8192, // 0x00002000
    TabletRemoved = 16384, // 0x00004000
    CustomStylusDataAdded = 32768, // 0x00008000
    DefaultStylusData = 37766, // 0x00009386
  }
}
