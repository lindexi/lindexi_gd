using System.Runtime.InteropServices;

namespace WhefallralajaHubeanerelair;

[TypeLibType(16)]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct InkRecoGuide
{
    public PenImcRect rectWritingBox;
    public PenImcRect rectDrawnBox;
    public int cRows;
    public int cColumns;
    public int Midline;
}