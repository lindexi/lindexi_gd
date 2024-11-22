// See https://aka.ms/new-console-template for more information

using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

var file = @"C:\Windows\System32\ntdll.dll";

using var fileStream = File.OpenRead(file);
var peHeaders = new PEHeaders(fileStream);
var peHeader = peHeaders.PEHeader!;
var debugTableDirectory = peHeader.DebugTableDirectory;

var seek = fileStream.Seek(debugTableDirectory.RelativeVirtualAddress,SeekOrigin.Begin);
var buffer = new byte[debugTableDirectory.Size];
var readCount = fileStream.Read(buffer, 0, buffer.Length);

var offset =  4 + Marshal.SizeOf<Guid>() + sizeof(uint);
var allSize = offset  + 255;

var slice = buffer.AsSpan().Slice(offset);

var unicodeText = Encoding.Unicode.GetString(buffer.AsSpan());

var text = Encoding.UTF32.GetString(buffer.AsSpan());

Console.WriteLine("Hello, World!");
