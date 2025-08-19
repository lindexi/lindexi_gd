// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

var path = @"C:\Users\lindexi\AppData\Roaming\";

var base64String = Convert.ToBase64String(MemoryMarshal.AsBytes(path.AsSpan()));
// System.IO.DirectoryNotFoundException:“Could not find a part of the path 'C:\Users\lindexi\AppData\Roaming\'.”
var name = new string(Enumerable.Repeat('c', 1000).ToArray());
Mutex.OpenExisting("xxxx");

var mutex = new Mutex(true, name, out var createdNew);

Task.Run(() =>
{
    var mutex2 = new Mutex(false, name, out var createdNew2);
    mutex2.WaitOne();
    Console.WriteLine("Mutex acquired in Task 2");
});


mutex.WaitOne();
mutex.WaitOne();

Thread.Sleep(0);
Thread.Sleep(0);
Thread.Sleep(0);

mutex.ReleaseMutex();
mutex.ReleaseMutex();
mutex.Close();
mutex.Dispose();

//var mutex3 = new Mutex(true, name, out var createdNew3);


Console.WriteLine("Hello, World!");
Console.ReadLine();
