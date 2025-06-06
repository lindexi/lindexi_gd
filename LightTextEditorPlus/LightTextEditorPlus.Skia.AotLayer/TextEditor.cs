using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Skia.AotLayer;

internal static class TextEditor
{
    [UnmanagedCallersOnly(EntryPoint = "GetSystemInfoWrite")]
    public static void GetSystemInfo()
    {
        Console.WriteLine($"ProcessorCount: {Environment.ProcessorCount}");
        Console.WriteLine($"MachineName: {Environment.MachineName}");
    }
}
