// See https://aka.ms/new-console-template for more information

using System;

Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

for (int i = 0; i < 10; i++)
{
    result[((char) (('a' + i))).ToString()] = i;
}

Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> alternateLookup = result.GetAlternateLookup<ReadOnlySpan<char>>();

for (int i = 0; i < 10; i++)
{
    if (alternateLookup.TryGetValue(((char) (('a' + i))).ToString().AsSpan(),out var value))
    {
        
    }
}

var lookup = result.GetAlternateLookup<char[]>();

Console.WriteLine("Hello, World!");
