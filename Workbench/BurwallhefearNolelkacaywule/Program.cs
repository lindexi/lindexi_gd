// See https://aka.ms/new-console-template for more information
var path = @"C:\Users\lindexi\AppData\Roaming\";
// System.IO.DirectoryNotFoundException:“Could not find a part of the path 'C:\Users\lindexi\AppData\Roaming\'.”
var mutex = new Mutex(true, path, out var createdNew);

Console.WriteLine("Hello, World!");
