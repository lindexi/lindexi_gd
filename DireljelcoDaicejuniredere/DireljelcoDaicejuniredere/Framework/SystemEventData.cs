using System.Runtime.InteropServices;

namespace DireljelcoDaicejuniredere;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct SystemEventData
{
    public byte modifier;
    public char key;
    public int xPos;
    public int yPos;
    public byte cursorMode;
    public int buttonState;
}