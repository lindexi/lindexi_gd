// See https://aka.ms/new-console-template for more information
using static CPF.Linux.XLib;

var display = XOpenDisplay(IntPtr.Zero);

if (XCompositeQueryExtension(display, out var eventBase, out var errorBase) == 0)
{
    Console.WriteLine("Error: Composite extension is not supported");
    XCloseDisplay(display);
    throw new NotSupportedException("Error: Composite extension is not supported");
    return;
}

Console.WriteLine("Hello, World!");
