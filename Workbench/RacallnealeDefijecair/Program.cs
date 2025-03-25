// See https://aka.ms/new-console-template for more information

using System.Globalization;

string? t = null;
var success = decimal.TryParse(t,NumberStyles.Any,null,out var v);

Console.WriteLine("Hello, World!");
