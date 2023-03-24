using System.Runtime.InteropServices;

namespace DireljelcoDaicejuniredere;

[StructLayout(LayoutKind.Sequential)]
internal class StylusInfo
{
    public uint tcid;
    public uint cid;
    public bool isInvertedCursor;

    public StylusInfo()
    {
        this.tcid = 0U;
        this.cid = 0U;
        this.isInvertedCursor = false;
    }
}