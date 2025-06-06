// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

Console.WriteLine(Environment.Is64BitProcess);
CreateTextEditor();
/*
System.DllNotFoundException:“Unable to load DLL 'CreateTextEditor' or one of its dependencies: 找不到指定的模块。 (0x8007007E)”
 */
Console.WriteLine("Hello, World!");

[DllImport("LightTextEditorPlus.Skia.AotLayer.dll")]
static extern uint CreateTextEditor();