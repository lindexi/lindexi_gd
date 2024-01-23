// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

Process.Start(new ProcessStartInfo("xdg-open", new[] { "http://blog.lindexi.com" })
{
    UseShellExecute = false
});