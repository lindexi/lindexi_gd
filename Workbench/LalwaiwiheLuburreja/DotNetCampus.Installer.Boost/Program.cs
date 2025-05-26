// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using Lsj.Util.Win32.NativeUI;

Console.WriteLine($"Hello, World!测试中文 RuntimeIdentifier={RuntimeInformation.RuntimeIdentifier} FrameworkDescription={RuntimeInformation.FrameworkDescription}");

var win32Window = new Win32Window()
{
    Text = "Lsj 窗口",
};
win32Window.Show();
win32Window.StartMessageLoop();
Console.Read();