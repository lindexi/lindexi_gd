using System.Runtime.InteropServices;

namespace LightTextEditorPlus;

internal static class Win32Interop
{
    /// <summary>
    /// 获取光标闪烁时间
    /// </summary>
    /// <returns>毫秒</returns>
    [DllImport("user32.dll")]
    internal static extern int GetCaretBlinkTime();
}