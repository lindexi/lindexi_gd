// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;

var str = "abc";
var match = Regex.Match(str,@"abc(\d*)");
if (match.Success)
{
    var value = match.Groups[1].Value;
}
Console.WriteLine("Hello, World!");
