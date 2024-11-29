// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;
unsafe
{
    var name = stackalloc char[1024];
    var length = gethostname(name, 1024);

    var t = new string(name, 0, length);

    Console.WriteLine($"name={t};Length={length}");

    [DllImport("libc")]
    static extern int gethostname(char* name, int len);
}
