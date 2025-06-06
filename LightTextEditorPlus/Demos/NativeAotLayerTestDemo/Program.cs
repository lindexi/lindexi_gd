// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using LightTextEditorPlus;

if (Environment.Is64BitProcess)
{
    Console.WriteLine($"当前引用的是 x86 版本，不能使用 x64 方式运行");
}

string text = "这是一段文本";

TextEditorId textEditorId = TextEditorWrapper.CreateTextEditor();
Console.WriteLine(textEditorId);

var errorCode = TextEditorWrapper.AppendText(textEditorId, text);

errorCode = TextEditorWrapper.SaveAsImageFile(textEditorId, "1.png");

if (OperatingSystem.IsWindows())
{
    Process.Start(new ProcessStartInfo("explorer.exe", "/select,1.png"));
}

TextEditorWrapper.FreeTextEditor(textEditorId);

/*
System.DllNotFoundException:“Unable to load DLL 'CreateTextEditor' or one of its dependencies: 找不到指定的模块。 (0x8007007E)”
*/
Console.WriteLine("Hello, World!");

