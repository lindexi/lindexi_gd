using System.Runtime.InteropServices;

namespace WhefallralajaHubeanerelair;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct PenImcRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}