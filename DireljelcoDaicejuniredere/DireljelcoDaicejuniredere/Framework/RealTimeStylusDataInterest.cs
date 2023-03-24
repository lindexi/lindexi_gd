namespace DireljelcoDaicejuniredere;

internal enum RealTimeStylusDataInterest
{
    RTPEI_AllStylusData = -1, // 0xFFFFFFFF
    RTPEI_Error = 1,
    RTPEI_RtpEnabled = 2,
    RTPEI_RtpDisabled = 4,
    RTPEI_CursorNew = 8,
    RTPEI_CursorInRange = 16, // 0x00000010
    RTPEI_InAirPackets = 32, // 0x00000020
    RTPEI_CursorOutOfRange = 64, // 0x00000040
    RTPEI_CursorDown = 128, // 0x00000080
    RTPEI_Packets = 256, // 0x00000100
    RTPEI_CursorUp = 512, // 0x00000200
    RTPEI_StylusButtonUp = 1024, // 0x00000400
    RTPEI_StylusButtonDown = 2048, // 0x00000800
    RTPEI_SystemEvents = 4096, // 0x00001000
    RTPEI_TabletAdded = 8192, // 0x00002000
    RTPEI_TabletRemoved = 16384, // 0x00004000
    RTPEI_CustomData = 32768, // 0x00008000
    RTPEI_DefaultStylusData = 37766, // 0x00009386
}