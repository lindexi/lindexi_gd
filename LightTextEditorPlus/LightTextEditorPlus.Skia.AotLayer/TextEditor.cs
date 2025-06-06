using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Skia.AotLayer;

internal static class TextEditor
{
    [UnmanagedCallersOnly(EntryPoint = "CreateTextEditor")]
    public static uint CreateTextEditor()
    {
        var skiaTextEditor = new SkiaTextEditor();
        uint id = Interlocked.Increment(ref _id);

        Console.WriteLine($"SkiaTextEditor={id}");

        SkiaTextEditorDictionary[id] = skiaTextEditor;
        return id;
    }


    private static uint _id = 0;

    private static readonly ConcurrentDictionary<uint/*Id*/, SkiaTextEditor> SkiaTextEditorDictionary = new ConcurrentDictionary<uint, SkiaTextEditor>();
}
