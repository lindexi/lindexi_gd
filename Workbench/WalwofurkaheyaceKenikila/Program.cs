// See https://aka.ms/new-console-template for more information

using System.IO.Hashing;

var file = @"C:\lindexi\Work\file";
var crc32 = new Crc32();
await using var fileStream = File.OpenRead(file);
await crc32.AppendAsync(fileStream);
var crcText = crc32.GetCurrentHashAsUInt32().ToString("X");
Console.WriteLine("Hello, World!");
