// See https://aka.ms/new-console-template for more information

var choices = "0123456789abcdef";
Span<char> dest = stackalloc char[choices.Length];
Random.Shared.GetItems(choices, dest);

foreach (var t in dest)
{
    Console.Write(t);
}

Console.WriteLine("Hello, World!");
