// See https://aka.ms/new-console-template for more information

using System.Numerics;
using System.Security.Cryptography;

var choices = "0123456789abcdef";
Span<char> dest = stackalloc char[choices.Length];
Random.Shared.GetItems(choices, dest);

foreach (var t in dest)
{
    Console.Write(t);
}

Console.WriteLine();

for (var i = 0; i < choices.Length; i++)
{
    dest[i] = choices[i];
}

Random.Shared.Shuffle(dest);

foreach (var t in dest)
{
    Console.Write(t);
}

var isPow2 = BitOperations.IsPow2(10);
Console.WriteLine(BitOperations.IsPow2(15));

Console.WriteLine();
