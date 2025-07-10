// See https://aka.ms/new-console-template for more information

using System.Buffers;

var buffer = new byte[1024];
ArrayPool<byte>.Shared.Return(buffer);
Console.WriteLine("Hello, World!");