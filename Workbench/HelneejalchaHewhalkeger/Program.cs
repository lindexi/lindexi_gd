// See https://aka.ms/new-console-template for more information

Version.TryParse("1.2.3.2", out var version);

string standardVersion;

var major = version.Major;
var minor = version.Minor;
var build = version.Build;
var revision = version.Revision;

major = Math.Max(0, major);
minor = Math.Max(0, minor);
build = Math.Max(0, build);

if (revision >= 0)
{
    standardVersion = $"{major}.{minor}.{build}.{revision}";
}
else
{
    standardVersion = $"{major}.{minor}.{build}";
}


var t = version.ToString(4);

Console.WriteLine("Hello, World!");
