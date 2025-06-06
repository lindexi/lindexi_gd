// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

if (Environment.Is64BitProcess)
{
    Console.WriteLine($"当前引用的是 x86 版本，不能使用 x64 方式运行");
}

Console.WriteLine(Environment.Is64BitProcess);
CreateTextEditor();
/*
System.DllNotFoundException:“Unable to load DLL 'CreateTextEditor' or one of its dependencies: 找不到指定的模块。 (0x8007007E)”
 */
Console.WriteLine("Hello, World!");

[DllImport("LightTextEditorPlus.dll")]
static extern uint CreateTextEditor();