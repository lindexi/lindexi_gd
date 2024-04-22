// See https://aka.ms/new-console-template for more information

using EDIDParser;

var file = "edid";

var data = File.ReadAllBytes(file);
var edid = new EDID(data);

Console.WriteLine("Hello, World!");
