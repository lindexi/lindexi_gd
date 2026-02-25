// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using System.Text;

ulong t = 7226134936035550029;

Console.WriteLine(t.ToString("X"));

var span = MemoryMarshal.CreateSpan(ref t, 1);
var t2 = MemoryMarshal.AsBytes(span);
var s = Encoding.UTF8.GetString(t2);

for (int i = 0; i < s.Length; i++)
{
    Console.WriteLine($"[{i}] '{s[i]}'({(int) s[i]} 0x{(int) s[i]:X2})");
}

Console.WriteLine("Hello, World!");
