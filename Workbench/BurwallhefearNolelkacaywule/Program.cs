// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

var path = @"C:\Users\lindexi\AppData\Roaming\";

var base64String = Convert.ToBase64String(MemoryMarshal.AsBytes(path.AsSpan()));
// System.IO.DirectoryNotFoundException:“Could not find a part of the path 'C:\Users\lindexi\AppData\Roaming\'.”
var mutex = new Mutex(true, base64String, out var createdNew);

Console.WriteLine("Hello, World!");
