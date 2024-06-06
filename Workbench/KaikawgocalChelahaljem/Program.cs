// See https://aka.ms/new-console-template for more information

var list = new List<string>()
{
    "aa",
    "aa",
    "bb",
};

var result = list.Distinct().ToArray();

Console.WriteLine("Hello, World!");
