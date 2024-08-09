// See https://aka.ms/new-console-template for more information
using System.Runtime.CompilerServices;

ReadUninitializedMemory();
Console.WriteLine("Hello, World!");



[SkipLocalsInit]
static void ReadUninitializedMemory()
{
    Span<int> numbers = stackalloc int[1024];
    for (int i = 0; i < numbers.Length; i++)
    {
        Console.WriteLine(numbers[i]);
    }
}