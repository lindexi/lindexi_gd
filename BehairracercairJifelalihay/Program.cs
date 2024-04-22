// See https://aka.ms/new-console-template for more information

using EDIDParser;

var file = "edid";

var data = File.ReadAllBytes(file);
unsafe
{
    int[] n = [1, 2, 3];

    if (n.AsSpan() is [1, 2, 3])
    {

    }
}
var edid = new EDID(data);

Console.WriteLine("Hello, World!");
