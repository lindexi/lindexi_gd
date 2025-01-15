// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;

var x = GetSystemMetrics(SystemMetric.SM_CXDOUBLECLK);
var y = GetSystemMetrics(SystemMetric.SM_CYDOUBLECLK);

var t = GetDoubleClickTime();
Console.WriteLine("Hello, World!");



const string LibraryName = "user32";

[DllImport(LibraryName)]
static extern uint GetDoubleClickTime();

[DllImport(LibraryName)]
static extern int GetSystemMetrics(SystemMetric smIndex);
enum SystemMetric
{
    SM_CXDOUBLECLK = 36, // 0x24
    SM_CYDOUBLECLK = 37, // 0x25
}