// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

var path = @"C:\Users\lindexi\AppData\Roaming\";

var base64String = Convert.ToBase64String(MemoryMarshal.AsBytes(path.AsSpan()));
// System.IO.DirectoryNotFoundException:“Could not find a part of the path 'C:\Users\lindexi\AppData\Roaming\'.”
var name = new string(Enumerable.Repeat('c', 1000).ToArray());
var mutex = new Mutex(true, name, out var createdNew);
mutex.WaitOne();
Console.WriteLine("Hello, World!");
Console.ReadLine();
