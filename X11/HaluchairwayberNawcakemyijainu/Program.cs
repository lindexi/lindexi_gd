// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;
unsafe
{
    var name = stackalloc char[256];
    var length = gethostname(name, 256);

    var t = new string(name, 0, length);

    Console.WriteLine(t);

    [DllImport("libc")]
    static extern int gethostname(char* name, int len);
}
