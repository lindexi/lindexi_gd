// See https://aka.ms/new-console-template for more information

List<int> list = [];

for (int i = 0; i < 100; i++)
{
    list.Add(Random.Shared.Next(10000));
}

var dictionary = new Dictionary<int, int>();
foreach (var value in list)
{
    dictionary[value] = value;
}

var n = 0;
foreach (var keyValuePair in dictionary)
{
    var value = list[n];
    var key = keyValuePair.Key;

    if (key == value)
        Console.WriteLine($"Key: {key} Value: {value} are equal");
    else
        Console.WriteLine($"Key: {key} Value: {value} are not equal");

    n++;
}

Console.WriteLine("Hello, World!");
