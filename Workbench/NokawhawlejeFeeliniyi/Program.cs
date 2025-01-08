// See https://aka.ms/new-console-template for more information


using System.Diagnostics;

Process.Start(new ProcessStartInfo("http://tools.ietf.org/html/rfc7231#section-6.5.3")
{
    UseShellExecute = true
});

Console.WriteLine("Hello, World!");
