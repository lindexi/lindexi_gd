using System.Runtime.InteropServices;

namespace WhefallralajaHubeanerelair;

[TypeLibType(16)]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct InkRecoGuide
{
    // 参考 https://learn.microsoft.com/zh-cn/windows/win32/tablet/inkanalysisrecognizerguide 保持命名相同
    public PenImcRect rectWritingBox;
    public PenImcRect rectDrawnBox;
    public int cRows;
    public int cColumns;
    public int Midline;
}