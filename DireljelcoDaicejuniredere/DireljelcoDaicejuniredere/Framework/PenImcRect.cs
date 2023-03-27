using System.Runtime.InteropServices;

namespace DireljelcoDaicejuniredere;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct PenImcRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}