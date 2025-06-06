using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace LightTextEditorPlus;

internal class NativeTextEditor
{
    public const string DllName = "LightTextEditorPlus.dll";

    [DllImport(DllName)]
    public static extern uint CreateTextEditor();
}
