using System.Runtime.InteropServices;

namespace XiaoXiIme.ImeInterop;

[StructLayout(LayoutKind.Sequential)]
public struct GuiThreadInfo
{
    public uint Size;
    public uint Flags;
    public nint Active;
    public nint Focus;
    public nint Capture;
    public nint MenuOwner;
    public nint MoveSize;
    public nint Caret;
    public Rect CaretRect;
}