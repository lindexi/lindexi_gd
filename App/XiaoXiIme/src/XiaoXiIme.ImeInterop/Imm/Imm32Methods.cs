using System.Runtime.InteropServices;

namespace XiaoXiIme.ImeInterop;

public static class Imm32Methods
{
    [DllImport("imm32.dll", ExactSpelling = true)]
    public static extern nint ImmLockIMC(HImc inputContext);

    [DllImport("imm32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ImmUnlockIMC(HImc inputContext);

    [DllImport("imm32.dll", ExactSpelling = true)]
    public static extern nint ImmLockIMCC(nint inputContextComponent);

    [DllImport("imm32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ImmUnlockIMCC(nint inputContextComponent);

    [DllImport("imm32.dll", ExactSpelling = true)]
    public static extern nint ImmReSizeIMCC(nint inputContextComponent, uint size);

    [DllImport("imm32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ImmGenerateMessage(HImc inputContext);
}