// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;

var targetFramework = "net9.0";
Console.WriteLine(Regex.IsMatch(targetFramework, @"net\d"));
