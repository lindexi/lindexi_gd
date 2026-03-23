using System;
using System.Diagnostics;

#if USE_Avalonia
namespace LightTextEditorPlus.Avalonia.Tests;
#else
namespace LightTextEditorPlus.Tests;
#endif

partial class TestFramework
{
    public static bool IsDebug()
    {
#if DEBUG
        return Debugger.IsAttached;
#else
        return false;
#endif
    }
}
