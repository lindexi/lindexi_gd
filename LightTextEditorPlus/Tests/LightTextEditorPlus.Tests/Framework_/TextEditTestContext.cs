using System.Diagnostics;
using System.Windows;

namespace LightTextEditorPlus.Tests;

public record TextEditTestContext(Window TestWindow, TextEditor TextEditor) : IDisposable
{
    public void Dispose()
    {
        if (TestFramework.IsDebug())
        {
            // 如果在附加调试，那就先不退出了
            return;
        }

        TestWindow.Close();
    }
}