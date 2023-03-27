namespace WhefallralajaHubeanerelair;

internal enum RealTimeStylusDataInterest
{
    AllStylusData = -1, // 0xFFFFFFFF
    Error = 1,
    RtpEnabled = 2,
    RtpDisabled = 4,
    CursorNew = 8,
    CursorInRange = 16, // 0x00000010
    InAirPackets = 32, // 0x00000020
    CursorOutOfRange = 64, // 0x00000040
    CursorDown = 128, // 0x00000080
    Packets = 256, // 0x00000100
    CursorUp = 512, // 0x00000200
    StylusButtonUp = 1024, // 0x00000400
    StylusButtonDown = 2048, // 0x00000800
    SystemEvents = 4096, // 0x00001000
    TabletAdded = 8192, // 0x00002000
    TabletRemoved = 16384, // 0x00004000
    CustomData = 32768, // 0x00008000
    DefaultStylusData = 37766, // 0x00009386
}