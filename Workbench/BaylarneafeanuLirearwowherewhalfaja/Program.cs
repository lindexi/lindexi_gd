// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

long result = 5283938767475196740;
ReadOnlySpan<byte> value = "DCFBPRTI"u8;
var converted = MemoryMarshal.Read<long>(value);

Console.WriteLine(converted == result);
